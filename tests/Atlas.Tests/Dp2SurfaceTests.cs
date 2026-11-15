using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Primitives;
using Atlas.Rp;
using Atlas.Verification;
using Xunit;

namespace Atlas.Tests
{
    public class Dp2SurfaceTests
    {
        private static AtlasClient Client(MockHttpMessageHandler h, ResilienceOptions? r = null) =>
            new AtlasClient(new AtlasClientOptions { SecretKey = "sk_test_123", ApiUrl = "https://api.test.example", HttpClient = new HttpClient(h), Resilience = r });

        // ---- generated catalog ----

        [Fact]
        public async Task Catalog_TypedMethod_HitsPath_WithIdempotencyKey()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"app_request\",\"id\":\"r1\"}");
            using var c = Client(h);
            var r = await c.Catalog.AppCatalog.RequestsVoteAsync("req_1", idempotencyKey: "k1");
            Assert.Equal("r1", r.Id);
            var req = Assert.Single(h.Requests);
            Assert.Equal("POST", req.Method);
            Assert.Equal("/v1/app_catalog/requests/req_1/vote", req.Path);
            Assert.Equal("k1", req.IdempotencyKey);
        }

        [Fact]
        public async Task Catalog_PaginationIterator_FollowsCursor()
        {
            var pages = new Queue<string>(new[]
            {
                "{\"data\":[{\"id\":\"a\"}],\"has_more\":true,\"next_cursor\":\"a\"}",
                "{\"data\":[{\"id\":\"b\"}],\"has_more\":false,\"next_cursor\":null}",
            });
            var h = new MockHttpMessageHandler((_, __) => (HttpStatusCode.OK, pages.Dequeue()));
            using var c = Client(h);
            var ids = new List<string?>();
            await foreach (var item in c.Catalog.AbuseSignals.ListAllAsync()) ids.Add(item.Id);
            Assert.Equal(new[] { "a", "b" }, ids);
            Assert.Contains("starting_after=a", h.Requests[1].Query);
        }

        // ---- resilience + telemetry ----

        [Fact]
        public async Task Resilience_RetriesGet_On503_AndReportsDelay()
        {
            var n = 0;
            var h = new MockHttpMessageHandler((_, __) => ++n < 3 ? ((HttpStatusCode)503, "{\"errors\":[{\"code\":\"X\",\"message\":\"m\"}]}") : (HttpStatusCode.OK, "{\"data\":[],\"has_more\":false}"));
            var waits = new List<TimeSpan>();
            using var c = Client(h, new ResilienceOptions { StatusAware = false, Delay = (d, _) => { waits.Add(d); return Task.CompletedTask; }, Random = () => 0 });
            await c.Users.ListAsync();
            Assert.Equal(3, h.Requests.Count);
            Assert.Equal(2, waits.Count);
            Assert.True(waits[1] > waits[0]);
        }

        [Fact]
        public async Task Resilience_DoesNotRetryWriteWithoutIdempotencyKey_On503()
        {
            var h = MockHttpMessageHandler.Json("{\"errors\":[{\"code\":\"X\",\"message\":\"m\"}]}", (HttpStatusCode)503);
            using var c = Client(h, new ResilienceOptions { StatusAware = false, Delay = (_, __) => Task.CompletedTask });
            await Assert.ThrowsAsync<AtlasServerException>(() => c.Catalog.AppCatalog.RequestsVoteAsync("r"));
            Assert.Single(h.Requests);
        }

        // ---- explicit null (clear) ----

        [Fact]
        public async Task RequestRecord_OmitsUnset_AndSendsNullOnlyWhenCleared()
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var c = Client(h);
            await c.Catalog.AdministrativeUnits.UpdateAsync("au_1", new Atlas.Generated.AdministrativeUnitsUpdateBody { Name = "n" });
            Assert.Equal("{\"name\":\"n\"}", h.Requests[0].Body);
            await c.Catalog.AdministrativeUnits.UpdateAsync("au_1", new Atlas.Generated.AdministrativeUnitsUpdateBody { Name = "n" }.ClearDescription().ClearFilter());
            using var d = JsonDocument.Parse(h.Requests[1].Body);
            Assert.Equal(JsonValueKind.Null, d.RootElement.GetProperty("description").ValueKind);
            Assert.Equal(JsonValueKind.Null, d.RootElement.GetProperty("filter").ValueKind);
            Assert.Equal("n", d.RootElement.GetProperty("name").GetString());
        }

        // ---- recent auth ----

        [Fact]
        public void RecentAuth_Helpers()
        {
            using var d = JsonDocument.Parse("{\"auth_time\":" + (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 100) + ",\"amr\":[\"pwd\",\"mfa\"],\"org_mfa_required\":true,\"org_mfa_ok\":false}");
            var c = d.RootElement;
            Assert.True(c.AuthenticatedWithin(300));
            Assert.False(c.AuthenticatedWithin(10));
            Assert.True(c.UsedMultiFactor());
            Assert.True(c.MfaSatisfied());
            Assert.False(AtlasJsonClaims("{}").AuthenticatedWithin(300));
        }

        private static JsonElement AtlasJsonClaims(string json) { using var d = JsonDocument.Parse(json); return d.RootElement.Clone(); }

        // ---- webhook dedupe ----

        [Fact]
        public async Task Dedupe_ClaimsOnce_ReleaseAllowsRetry()
        {
            var store = new MemoryWebhookDedupeStore();
            Assert.True(await store.SetIfAbsentAsync("k", TimeSpan.FromHours(1)));
            Assert.False(await store.SetIfAbsentAsync("k", TimeSpan.FromHours(1)));
            await store.DeleteAsync("k");
            Assert.True(await store.SetIfAbsentAsync("k", TimeSpan.FromHours(1)));
        }

        // ---- machine client: RFC 8032 vector ----

        [Fact]
        public void Machine_SignChallenge_MatchesRfc8032EmptyMessage()
        {
            var seed = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
            var pem = "-----BEGIN PRIVATE KEY-----\n" + Convert.ToBase64String(new byte[] { 0x30, 0x2e, 0x02, 0x01, 0x00, 0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70, 0x04, 0x22, 0x04, 0x20 }.Concat(seed).ToArray()) + "\n-----END PRIVATE KEY-----\n";
            var sig = MachineClient.SignChallenge(pem, "");
            Assert.Equal("e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b",
                Convert.ToHexString(Convert.FromBase64String(sig.Replace('-', '+').Replace('_', '/') + "==")).ToLowerInvariant());
        }

        [Fact]
        public async Task Machine_EnrollThenToken_CachesAndSharesMint()
        {
            var calls = new List<string>();
            var h = new MockHttpMessageHandler((r, body) =>
            {
                calls.Add(r.RequestUri!.AbsolutePath);
                return r.RequestUri.AbsolutePath switch
                {
                    "/v1/machines/enroll" => (HttpStatusCode.OK, "{\"id\":\"mch_1\",\"status\":\"active\",\"approval_required\":false,\"enrolled_at\":1}"),
                    "/v1/machines/challenge" => (HttpStatusCode.OK, "{\"nonce\":\"n1\"}"),
                    _ => (HttpStatusCode.OK, "{\"token\":\"t1\",\"expires_in\":600}"),
                };
            });
            var m = new MachineClient(new MachineClientOptions { Issuer = "https://id.test.example", HttpClient = new HttpClient(h) });
            var e = await m.EnrollAsync(new MachineEnrollOptions { Credential = "met_x", Name = "box" });
            Assert.Equal("mch_1", e.MachineId);
            var t1 = await m.GetTokenAsync(new MachineTokenRequest { Audience = "api" });
            var t2 = await m.GetTokenAsync(new MachineTokenRequest { Audience = "api" });
            Assert.Equal("t1", t1.Token);
            Assert.Equal(t1.Token, t2.Token);
            Assert.Equal(1, calls.Count(c => c == "/v1/machines/token"));
        }

        // ---- web bot auth: sign then verify ----

        [Fact]
        public async Task WebBotAuth_SignedRequest_Verifies_AndTamperFails()
        {
            var seed = new byte[32]; seed[0] = 7;
            var pub = Sodium.PublicKeyAuth.GenerateKeyPair(seed).PublicKey;
            var x = WebBotAuth.B64U(pub);
            var now = 1_800_000_000L;
            var url = "https://site.example/page?a=1";
            var headers = WebBotAuth.SignRequest("GET", url, "https://bot.example", seed, now, now + 300, "n1");
            Task<IReadOnlyList<string>?> Keys(string u, bool r) => Task.FromResult<IReadOnlyList<string>?>(u == "https://bot.example" + WebBotAuth.DirectoryPath ? new[] { x } : null);
            var req = new WebBotAuthRequest { Method = "GET", Url = url, Headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase) };
            var ok = await WebBotAuth.VerifyAsync(req, Keys, now * 1000);
            Assert.True(ok.Verified, ok.Reason.ToString());
            Assert.Equal("https://bot.example", ok.Directory);
            var d = WebBotAuth.Decide(ok, new WebBotAuthPolicy { Allow = new[] { new WebBotAuthListEntry("bot.example", "Bot") } });
            Assert.Equal("allow", d.Decision);
            req.Url = "https://other.example/page?a=1"; // @authority no longer matches the signed one
            var bad = await WebBotAuth.VerifyAsync(req, Keys, now * 1000);
            Assert.False(bad.Verified);
            Assert.Equal(WebBotAuthFailure.BadSignature, bad.Reason);
        }

        // ---- key mirror ----

        [Fact]
        public async Task KeyMirror_VerifiesLocally_RevokeFirst()
        {
            var store = new InMemoryKeyMirrorStore();
            var api = new FakeKeyApi();
            var mirror = new KeyMirror(new KeyMirrorOptions { Store = store, Api = api });
            await store.PutAsync(new MirroredApiKey { Id = "eak_1", Hash = KeyMirror.HashSecret("ak_secret"), SubjectType = "user", SubjectId = "u", Scopes = new[] { "read" }, AllowedIps = new[] { "10.0.0.0/8" } });
            Assert.False((await mirror.VerifyAsync("ak_secret", clientIp: "8.8.8.8")).Valid);
            Assert.Equal(KeyMirrorCode.ClientIpRequired, (await mirror.VerifyAsync("ak_secret")).Code);
            var v = await mirror.VerifyAsync("ak_secret", new[] { "read" }, "10.1.2.3");
            Assert.True(v.Valid);
            Assert.Equal(KeyMirrorCode.InsufficientScope, (await mirror.VerifyAsync("ak_secret", new[] { "write" }, "10.1.2.3")).Code);
            api.Fail = true;
            await Assert.ThrowsAsync<InvalidOperationException>(() => mirror.RevokeAsync("eak_1"));
            Assert.False((await mirror.VerifyAsync("ak_secret", clientIp: "10.1.2.3")).Valid); // revoked locally even though Atlas failed
        }

        private sealed class FakeKeyApi : IKeyMirrorApi
        {
            public bool Fail;
            public Task<Atlas.Resources.ApiKeyWithSecret> CreateAsync(Atlas.Resources.CreateApiKeyBody b, string? i, System.Threading.CancellationToken ct) => throw new NotSupportedException();
            public Task DeleteAsync(string id, System.Threading.CancellationToken ct) => Fail ? throw new InvalidOperationException("down") : Task.CompletedTask;
            public Task<(IReadOnlyList<(string Id, long? RevokedAt, string? Reason)> Data, bool HasMore, string? NextCursor)> ListRevokedAsync(string? since, System.Threading.CancellationToken ct)
                => Task.FromResult<(IReadOnlyList<(string, long?, string?)>, bool, string?)>((new List<(string, long?, string?)> { ("eak_1", 5L, "rotated") }, false, "c1"));
            public Task<Atlas.Resources.ApiKeyVerification?> VerifyAsync(string s, string? ip, System.Threading.CancellationToken ct) => Task.FromResult<Atlas.Resources.ApiKeyVerification?>(null);
            public Task<Atlas.Resources.ApiKey?> GetAsync(string id, System.Threading.CancellationToken ct) => Task.FromResult<Atlas.Resources.ApiKey?>(null);
        }

        [Fact]
        public async Task KeyMirror_Sync_AppliesFeed_AndStalenessRefuses()
        {
            var store = new InMemoryKeyMirrorStore();
            var mirror = new KeyMirror(new KeyMirrorOptions { Store = store, Api = new FakeKeyApi(), MaxStaleness = TimeSpan.FromMinutes(5) });
            await store.PutAsync(new MirroredApiKey { Id = "eak_1", Hash = KeyMirror.HashSecret("s") });
            Assert.Equal(KeyMirrorCode.Stale, (await mirror.VerifyAsync("s")).Code); // never synced
            Assert.Equal(1, await mirror.SyncAsync());
            Assert.False((await mirror.VerifyAsync("s")).Valid); // revoked by the feed
            Assert.Equal("c1", await store.GetCursorAsync());
        }

        // ---- resource role mirror ----

        [Fact]
        public async Task ResourceRoleMirror_InheritsDownTree_AndHonoursInheritFlag()
        {
            var store = new InMemoryResourceRoleMirrorStore();
            await store.ReplaceTypesAsync(new[] { new MirroredResourceType("doc", true, new[] { new MirroredRole("viewer", new[] { "read" }, Array.Empty<string>()), new MirroredRole("owner", new[] { "write" }, new[] { "viewer" }) }) });
            await store.ReplaceResourcesAsync(new[] { new MirroredResource("r1", "doc", "folder", null), new MirroredResource("r2", "doc", "file", "r1") });
            await store.ApplyChangesAsync(new[]
            {
                new MirroredAssignmentChange(false, new MirroredAssignment("a1", "r1", "user:u", "owner", true)),
                new MirroredAssignmentChange(false, new MirroredAssignment("a2", "r1", "user:v", "viewer", false)),
            });
            var m = new ResourceRoleMirror(new ResourceRoleMirrorOptions { Store = store, Api = new NoApi() });
            var file = MirrorResourceRef.ByExternal("doc", "file");
            Assert.True(await m.CanAsync(MirrorSubject.User("u"), "read", file));   // owner includes viewer, inherited
            Assert.False(await m.CanAsync(MirrorSubject.User("v"), "read", file));  // inherit=false stops at the folder
            Assert.Equal("unknown_resource", (await m.CheckAsync(MirrorSubject.User("u"), "read", MirrorResourceRef.ById("zz"))).Code);
        }

        private sealed class NoApi : IResourceRoleMirrorApi
        {
            public Task<JsonElement> ListTypesAsync(System.Threading.CancellationToken ct) => throw new NotSupportedException();
            public Task<JsonElement> ListResourcesAsync(string? s, System.Threading.CancellationToken ct) => throw new NotSupportedException();
            public Task<JsonElement> ChangesAsync(string? a, System.Threading.CancellationToken ct) => throw new NotSupportedException();
        }

        // ---- relying party ----

        [Fact]
        public async Task RelyingParty_Start_SealsFlow_AndCallbackRejectsStateMismatch()
        {
            var h = new MockHttpMessageHandler((r, _) => (HttpStatusCode.OK,
                "{\"issuer\":\"https://id.test.example\",\"authorization_endpoint\":\"https://id.test.example/oauth2/authorize\",\"token_endpoint\":\"https://id.test.example/oauth2/token\",\"jwks_uri\":\"https://id.test.example/jwks\"}"));
            var rp = new RelyingParty(new RelyingPartyOptions
            {
                Issuer = "https://id.test.example", ClientId = "c1", RedirectUri = "https://app.test.example/auth/callback",
                Secret = new string('s', 40), HttpClient = new HttpClient(h), ForwardParams = new[] { "ref" },
            });
            var start = await rp.StartAsync(new Dictionary<string, string> { ["ref"] = "abc", ["return_to"] = "//evil.example" });
            Assert.Contains("code_challenge_method=S256", start.Location);
            Assert.Contains("ref=abc", start.Location);
            Assert.DoesNotContain("evil", start.Location);
            var cookie = Assert.Single(start.SetCookies);
            Assert.Contains("HttpOnly", cookie);
            Assert.Contains("Path=/auth/callback", cookie);
            var ex = await Assert.ThrowsAsync<AtlasOidcException>(() =>
                rp.CallbackAsync(new Dictionary<string, string> { ["state"] = "wrong", ["code"] = "x" }, cookie.Split(';')[0]));
            Assert.Equal(AtlasOidcErrorCode.StateMismatch, ex.Code);
            Assert.NotNull(ex.ClearCookie);
            var none = await Assert.ThrowsAsync<AtlasOidcException>(() => rp.CallbackAsync(new Dictionary<string, string>(), null));
            Assert.Equal(AtlasOidcErrorCode.StateMismatch, none.Code);
        }

        [Fact]
        public async Task RelyingParty_TouchIfDue_SkipsWithinInterval_AndMapsEnded()
        {
            var h = new MockHttpMessageHandler((r, _) => ((HttpStatusCode)409, "{\"errors\":[{\"code\":\"SESSION_REVOKED\",\"message\":\"x\"}]}"));
            var rp = new RelyingParty(new RelyingPartyOptions
            {
                Issuer = "https://id.test.example", ClientId = "c1", RedirectUri = "https://app.test.example/cb", Secret = new string('s', 40),
                SecretKey = "sk_test_1", ApiUrl = "https://api.test.example", HttpClient = new HttpClient(h), Now = () => 1_000_000,
            });
            Assert.Equal(TouchStatus.Skipped, (await rp.TouchIfDueAsync("sid1", 999_000)).Status);
            var r = await rp.TouchIfDueAsync("sid1", null);
            Assert.Equal(TouchStatus.Ended, r.Status);
            Assert.Equal("revoked", r.Reason);
        }

        // ---- doctor ----

        [Fact]
        public async Task Doctor_ReportsKeyAndSkew()
        {
            var h = new MockHttpMessageHandler((r, _) => r.RequestUri!.AbsolutePath == "/v1/status"
                ? (HttpStatusCode.OK, "{\"status\":\"operational\"}") : ((HttpStatusCode)401, "{\"errors\":[{\"code\":\"UNAUTHORIZED\",\"message\":\"no\"}]}"));
            using var c = Client(h);
            var report = await c.DoctorAsync();
            Assert.False(report.Ok);
            Assert.Contains(report.Checks, x => x.Name == "key accepted" && x.Status == DoctorStatus.Fail);
            Assert.Contains(report.Checks, x => x.Name == "service status" && x.Status == DoctorStatus.Ok);
        }
    }
}
