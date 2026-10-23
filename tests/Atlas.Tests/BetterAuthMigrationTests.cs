using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas;
using Atlas.AspNetCore;
using Atlas.Resources;
using Atlas.Verification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>
    /// Migrate from Better Auth: sealed-bundle imports (upload / wait helpers), the bridge settings, the classified secret-key
    /// exchange, the cookie bridge (framework-neutral and ASP.NET Core middleware) and legacy-issuer JWTs.
    /// </summary>
    public class BetterAuthMigrationTests
    {
        private static AtlasClient Client(HttpMessageHandler h) => new AtlasClient(new AtlasClientOptions
        {
            SecretKey = "sk_test_123",
            ApiUrl = "https://api.test.example",
            HttpClient = new HttpClient(h),
        });

        private static JsonElement Sent(RecordedRequest r) => JsonDocument.Parse(r.Body).RootElement;

        // ---- imports ----

        [Fact]
        public async Task Imports_EveryOperation_HitsItsRoute()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"better_auth_import\",\"id\":\"bai_1\",\"status\":\"ready\",\"data\":[],\"has_more\":false}");
            using var c = Client(h);

            var key = await c.BetterAuthImports.GetPublicKeyAsync();
            Assert.Equal("GET", h.Requests[0].Method);
            Assert.Equal("/v1/better_auth_imports/public_key", h.Requests[0].Path);
            Assert.NotNull(key);

            await c.BetterAuthImports.RotatePublicKeyAsync("idem-1");
            Assert.Equal("POST", h.Requests[1].Method);
            Assert.Equal("/v1/better_auth_imports/public_key/rotate", h.Requests[1].Path);
            Assert.Equal("idem-1", h.Requests[1].IdempotencyKey);

            await c.BetterAuthImports.CreateAsync(new BetterAuthImportOptions { OnEmailConflict = "link", JwksCutoverDays = 14, RetainBundle = true });
            var created = Sent(h.Requests[2]).GetProperty("options");
            Assert.Equal("/v1/better_auth_imports", h.Requests[2].Path);
            Assert.Equal("link", created.GetProperty("on_email_conflict").GetString());
            Assert.Equal(14, created.GetProperty("jwks_cutover_days").GetInt32());
            Assert.True(created.GetProperty("retain_bundle").GetBoolean());

            await c.BetterAuthImports.CreateAsync();
            Assert.Equal("{}", h.Requests[3].Body);

            await c.BetterAuthImports.ListAsync(limit: 5, startingAfter: "bai_0");
            Assert.Equal("GET", h.Requests[4].Method);
            Assert.Equal("?limit=5&starting_after=bai_0", h.Requests[4].Query);

            await c.BetterAuthImports.GetAsync("bai_1");
            Assert.Equal("/v1/better_auth_imports/bai_1", h.Requests[5].Path);

            await c.BetterAuthImports.UploadPartAsync("bai_1", 2, "line-1\nline-2\n");
            Assert.Equal("PUT", h.Requests[6].Method);
            Assert.Equal("/v1/better_auth_imports/bai_1/parts/2", h.Requests[6].Path);
            Assert.Equal("line-1\nline-2\n", Sent(h.Requests[6]).GetProperty("data").GetString());

            await c.BetterAuthImports.UploadPartAsync("bai_1", 3, Encoding.UTF8.GetBytes("raw-line\n"));
            Assert.Equal("/v1/better_auth_imports/bai_1/parts/3", h.Requests[7].Path);
            Assert.StartsWith("text/plain", h.Requests[7].ContentType);
            Assert.Equal("raw-line\n", h.Requests[7].Body);

            await c.BetterAuthImports.FinalizeAsync("bai_1");
            Assert.Equal("POST", h.Requests[8].Method);
            Assert.Equal("/v1/better_auth_imports/bai_1/finalize", h.Requests[8].Path);

            await c.BetterAuthImports.DryRunAsync("bai_1", new BetterAuthImportOptions { DefaultRole = "member" });
            Assert.Equal("/v1/better_auth_imports/bai_1/dry_run", h.Requests[9].Path);
            Assert.Equal("member", Sent(h.Requests[9]).GetProperty("options").GetProperty("default_role").GetString());

            await c.BetterAuthImports.ApplyAsync("bai_1", confirmConflicts: true);
            Assert.Equal("/v1/better_auth_imports/bai_1/apply", h.Requests[10].Path);
            Assert.True(Sent(h.Requests[10]).GetProperty("confirm_conflicts").GetBoolean());
            await c.BetterAuthImports.ApplyAsync("bai_1");
            Assert.Equal("{}", h.Requests[11].Body);

            await c.BetterAuthImports.DeleteAsync("bai_1");
            Assert.Equal("DELETE", h.Requests[12].Method);
            Assert.Equal("/v1/better_auth_imports/bai_1", h.Requests[12].Path);

            await c.BetterAuthImports.ListJwksAsync();
            Assert.Equal("/v1/better_auth_imports/jwks", h.Requests[13].Path);
            await c.BetterAuthImports.DeleteJwkAsync("kid/1");
            Assert.Equal("DELETE", h.Requests[14].Method);
            Assert.EndsWith("/v1/better_auth_imports/jwks/kid%2F1", h.Requests[14].Url.AbsoluteUri);
            Assert.All(h.Requests, r => Assert.Equal("Bearer sk_test_123", r.Authorization));
        }

        [Fact]
        public async Task Imports_ParsesTheImportResponse()
        {
            var h = MockHttpMessageHandler.Json(
                "{\"object\":\"better_auth_import\",\"id\":\"bai_1\",\"status\":\"ready\",\"parts\":2,\"bytes\":10,\"progress\":{\"total\":4,\"processed\":2,\"percent\":50},"
                + "\"report\":{\"action_items\":[{\"code\":\"x\"}]},\"error\":{\"code\":\"bundle_wrong_key\",\"message\":\"m\"},\"created_at\":1}");
            using var c = Client(h);
            var imp = await c.BetterAuthImports.GetAsync("bai_1");
            Assert.Equal("ready", imp.Status);
            Assert.Equal(2L, imp.Parts);
            Assert.Equal(50L, imp.Progress!.Percent);
            Assert.Equal("bundle_wrong_key", imp.Error!.Code);
            Assert.Equal(1, imp.Report!["action_items"].GetArrayLength());
        }

        [Fact]
        public void SplitBundleParts_IsLineAlignedAndBounded()
        {
            var parts = BetterAuthImportsResource.SplitBundleParts("aaaa\r\n\r\nbbbb\ncccc\ndddd\n", 10);
            Assert.Equal(new[] { "aaaa\nbbbb\n", "cccc\ndddd\n" }, parts);
            // A line longer than the limit stands alone rather than being cut.
            Assert.Equal(new[] { "aaaa\n", "bbbbbbbbbbbbbbbb\n", "cc\n" }, BetterAuthImportsResource.SplitBundleParts("aaaa\nbbbbbbbbbbbbbbbb\ncc", 8));
            // Bytes, not characters: "é" is two bytes.
            Assert.Equal(2, BetterAuthImportsResource.SplitBundleParts("éééé\néééé\n", 9).Count);
            Assert.Empty(BetterAuthImportsResource.SplitBundleParts("\n\r\n", 10));
            Assert.Single(BetterAuthImportsResource.SplitBundleParts("one line", BetterAuthImportsResource.PartBytes));
            Assert.Throws<ArgumentOutOfRangeException>(() => BetterAuthImportsResource.SplitBundleParts("x", 0));
        }

        [Fact]
        public async Task Upload_CreatesUploadsPartsInOrderAndFinalizes()
        {
            var h = new MockHttpMessageHandler((req, _) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                if (path.EndsWith("/finalize")) return (HttpStatusCode.OK, "{\"id\":\"bai_9\",\"status\":\"ready\"}");
                if (path == "/v1/better_auth_imports") return (HttpStatusCode.OK, "{\"id\":\"bai_9\",\"status\":\"uploading\"}");
                return (HttpStatusCode.OK, "{\"object\":\"better_auth_import_part\"}");
            });
            using var c = Client(h);
            var progress = new List<(int, int)>();
            var imp = await c.BetterAuthImports.UploadAsync(
                "aaaa\nbbbb\ncccc\n", new BetterAuthImportOptions { ImportSessions = true }, partBytes: 10, onPart: (i, n) => progress.Add((i, n)));

            Assert.Equal("ready", imp.Status);
            Assert.Equal(new[] { "POST /v1/better_auth_imports", "PUT /v1/better_auth_imports/bai_9/parts/1", "PUT /v1/better_auth_imports/bai_9/parts/2", "POST /v1/better_auth_imports/bai_9/finalize" },
                h.Requests.Select(r => r.Method + " " + r.Path).ToArray());
            Assert.True(Sent(h.Requests[0]).GetProperty("options").GetProperty("import_sessions").GetBoolean());
            Assert.Equal("aaaa\nbbbb\n", Sent(h.Requests[1]).GetProperty("data").GetString());
            Assert.Equal("cccc\n", Sent(h.Requests[2]).GetProperty("data").GetString());
            Assert.Equal(new[] { (1, 2), (2, 2) }, progress.ToArray());
        }

        [Fact]
        public async Task Upload_RefusesAnEmptyBundle_BeforeAnyRequest()
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var c = Client(h);
            await Assert.ThrowsAsync<ArgumentException>(() => c.BetterAuthImports.UploadAsync("\n\n"));
            Assert.Empty(h.Requests);
        }

        [Fact]
        public async Task WaitFor_PollsUntilTheImportLeavesFlight()
        {
            var statuses = new Queue<string>(new[] { "analyzing", "queued", "applying", "completed" });
            var h = new MockHttpMessageHandler((_, __) => (HttpStatusCode.OK, "{\"id\":\"bai_1\",\"status\":\"" + statuses.Dequeue() + "\"}"));
            using var c = Client(h);
            var seen = new List<string?>();
            var done = await c.BetterAuthImports.WaitForAsync("bai_1", TimeSpan.FromMilliseconds(1), onProgress: i => seen.Add(i.Status));
            Assert.Equal("completed", done.Status);
            Assert.Equal(new string?[] { "analyzing", "queued", "applying", "completed" }, seen.ToArray());
            Assert.Equal(4, h.Requests.Count);
        }

        [Fact]
        public async Task WaitFor_ReturnsAtOnceForSettledStatuses_AndTimesOutOtherwise()
        {
            var ready = MockHttpMessageHandler.Json("{\"id\":\"bai_1\",\"status\":\"invalid\"}");
            using (var c = Client(ready))
            {
                Assert.Equal("invalid", (await c.BetterAuthImports.WaitForAsync("bai_1")).Status);
                Assert.Single(ready.Requests);
            }
            var stuck = MockHttpMessageHandler.Json("{\"id\":\"bai_1\",\"status\":\"applying\"}");
            using var c2 = Client(stuck);
            await Assert.ThrowsAsync<TimeoutException>(() =>
                c2.BetterAuthImports.WaitForAsync("bai_1", TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(20)));
        }

        // ---- bridge settings + exchange ----

        [Fact]
        public async Task BridgeSettings_GetUpdateDelete()
        {
            var h = MockHttpMessageHandler.Json(
                "{\"object\":\"better_auth_bridge\",\"enabled\":true,\"ends_at\":99,\"cookie_prefix\":\"x\",\"live_fallback\":{\"enabled\":true,\"base_url\":\"https://old.example/api/auth\",\"has_secret\":true},"
                + "\"exchange_path\":\"/v1/migrations/better_auth/exchange\",\"stats\":{\"exchanged\":3,\"from_import\":2,\"from_live\":1}}");
            using var c = Client(h);

            var s = await c.BetterAuthBridge.GetAsync();
            Assert.Equal("/v1/migrations/better_auth/bridge", h.Requests[0].Path);
            Assert.True(s.LiveFallback!.HasSecret);
            Assert.Equal(1L, s.Stats!.FromLive);

            await c.BetterAuthBridge.UpdateAsync(new UpdateBetterAuthBridgeBody
            {
                Enabled = true,
                EndsAt = "2026-12-01T00:00:00Z",
                CookiePrefix = "my-app",
                LiveFallback = new BetterAuthLiveFallbackBody { Enabled = true, BaseUrl = "https://old.example/api/auth", Secret = "s3cret" },
            }, "idem-2");
            Assert.Equal("PUT", h.Requests[1].Method);
            Assert.Equal("idem-2", h.Requests[1].IdempotencyKey);
            var body = Sent(h.Requests[1]);
            Assert.Equal("my-app", body.GetProperty("cookie_prefix").GetString());
            Assert.Equal("2026-12-01T00:00:00Z", body.GetProperty("ends_at").GetString());
            Assert.Equal("https://old.example/api/auth", body.GetProperty("live_fallback").GetProperty("base_url").GetString());

            // Explicit nulls: open-ended window, clear the stored secret.
            await c.BetterAuthBridge.UpdateAsync(new UpdateBetterAuthBridgeBody { LiveFallback = new BetterAuthLiveFallbackBody().Set("secret", null) }.Set("ends_at", null));
            var nulls = Sent(h.Requests[2]);
            Assert.Equal(JsonValueKind.Null, nulls.GetProperty("ends_at").ValueKind);
            Assert.Equal(JsonValueKind.Null, nulls.GetProperty("live_fallback").GetProperty("secret").ValueKind);

            await c.BetterAuthBridge.DeleteAsync();
            Assert.Equal("DELETE", h.Requests[3].Method);
            Assert.Equal("/v1/migrations/better_auth/bridge", h.Requests[3].Path);
        }

        private const string SessionJson = "{\"object\":\"session_tokens\",\"session_id\":\"sess_1\",\"user_id\":\"user_1\",\"jwt\":\"JWT\",\"refresh_token\":\"RT\",\"expires_in\":60}";
        private const string AttemptJson = "{\"object\":\"sign_in_attempt\",\"id\":\"sia_1\",\"status\":\"needs_second_factor\"}";

        private static string ErrorJson(string outcome) => "{\"errors\":[{\"code\":\"X\",\"message\":\"m\",\"meta\":{\"bridge_outcome\":\"" + outcome + "\"}}]}";

        [Fact]
        public async Task Exchange_SendsTokenSecretKeyAndForwardsTheEndUser()
        {
            var h = MockHttpMessageHandler.Json(SessionJson);
            using var c = Client(h);
            var raw = await c.BetterAuthBridge.ExchangeAsync("tok.sig%3D", "203.0.113.9", "Mozilla/5.0 (X11)", remember: false);
            Assert.Equal("JWT", raw.Jwt);

            var r = h.Requests[0];
            Assert.Equal("POST", r.Method);
            Assert.Equal("/v1/migrations/better_auth/exchange", r.Path);
            Assert.Equal("Bearer sk_test_123", r.Authorization);
            Assert.Equal("203.0.113.9", r.Header("X-Atlas-Client-IP"));
            Assert.Equal("Mozilla/5.0 (X11)", r.Header("X-Atlas-Client-User-Agent"));
            Assert.Equal("tok.sig%3D", Sent(r).GetProperty("token").GetString());
            Assert.False(Sent(r).GetProperty("remember").GetBoolean());

            // Without them, nothing is invented.
            await c.BetterAuthBridge.ExchangeAsync("t");
            Assert.Null(h.Requests[1].Header("X-Atlas-Client-IP"));
            Assert.False(Sent(h.Requests[1]).TryGetProperty("remember", out _));
        }

        [Fact]
        public async Task Exchange_Classified_SessionAndNeedsMfa()
        {
            using var ok = Client(MockHttpMessageHandler.Json(SessionJson));
            var s = await BetterAuthExchange.ExchangeAsync(ok, "t", "1.2.3.4", "ua");
            Assert.Equal(BetterAuthExchangeKind.Session, s.Kind);
            Assert.Equal("JWT", s.Jwt);
            Assert.Equal("RT", s.RefreshToken);
            Assert.Equal("sess_1", s.SessionId);
            Assert.Equal("user_1", s.UserId);
            Assert.Equal(60, s.ExpiresIn);

            using var mfa = Client(MockHttpMessageHandler.Json(AttemptJson));
            var m = await BetterAuthExchange.ExchangeAsync(mfa, "t");
            Assert.Equal(BetterAuthExchangeKind.NeedsMfa, m.Kind);
            Assert.Equal("sia_1", m.AttemptId);
            Assert.Equal("needs_second_factor", m.Status);

            // A 200 that is neither is not a verdict on the session.
            using var odd = Client(MockHttpMessageHandler.Json("{\"object\":\"nope\"}"));
            var o = await BetterAuthExchange.ExchangeAsync(odd, "t");
            Assert.Equal(BetterAuthExchangeKind.Refused, o.Kind);
            Assert.Equal("unavailable", o.Outcome);
        }

        [Theory]
        [InlineData("closed", 403)]
        [InlineData("invalid", 401)]
        [InlineData("signature_invalid", 401)]
        [InlineData("expired", 401)]
        [InlineData("reused", 409)]
        [InlineData("user_unavailable", 403)]
        public async Task Exchange_Classified_RefusalOutcomes(string outcome, int status)
        {
            using var c = Client(MockHttpMessageHandler.Json(ErrorJson(outcome), (HttpStatusCode)status));
            var r = await BetterAuthExchange.ExchangeAsync(c, "t");
            Assert.Equal(BetterAuthExchangeKind.Refused, r.Kind);
            Assert.Equal(outcome, r.Outcome);
            Assert.Equal(status, r.HttpStatus);
        }

        [Fact]
        public async Task Exchange_Classified_RateLimitServerErrorsAndTransportFailuresNeverThrow()
        {
            using var limited = Client(MockHttpMessageHandler.Json("{\"errors\":[{\"code\":\"RATE_LIMITED\",\"message\":\"slow\"}]}", (HttpStatusCode)429));
            Assert.Equal("rate_limited", (await BetterAuthExchange.ExchangeAsync(limited, "t")).Outcome);

            using var down = Client(MockHttpMessageHandler.Json("boom", HttpStatusCode.BadGateway));
            var d = await BetterAuthExchange.ExchangeAsync(down, "t");
            Assert.Equal("unavailable", d.Outcome);
            Assert.Equal(502, d.HttpStatus);

            using var unknownMeta = Client(MockHttpMessageHandler.Json(ErrorJson("something_new"), HttpStatusCode.Forbidden));
            Assert.Equal("unavailable", (await BetterAuthExchange.ExchangeAsync(unknownMeta, "t")).Outcome);

            using var broken = Client(new MockHttpMessageHandler((_, __) => throw new HttpRequestException("connection refused")));
            var b = await BetterAuthExchange.ExchangeAsync(broken, "t");
            Assert.Equal("unavailable", b.Outcome);
            Assert.Equal(0, b.HttpStatus);

            // A timeout is a transport failure; the caller cancelling is not swallowed.
            using var timedOut = Client(new MockHttpMessageHandler((_, __) => throw new TaskCanceledException("timeout")));
            Assert.Equal("unavailable", (await BetterAuthExchange.ExchangeAsync(timedOut, "t")).Outcome);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            using var cancelled = Client(new MockHttpMessageHandler((_, __) => throw new TaskCanceledException("cancelled")));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => BetterAuthExchange.ExchangeAsync(cancelled, "t", ct: cts.Token));
        }

        // ---- the cookie bridge ----

        [Fact]
        public void Cookies_NamesReadAndParse()
        {
            var names = BetterAuthCookies.Names();
            Assert.Equal(new[] { "__Secure-better-auth.session_token", "better-auth.session_token" }, names.SessionToken);
            Assert.Equal(8, names.All.Length);
            Assert.Equal("__Secure-my.session_token", BetterAuthCookies.Names("my").SessionToken[0]);

            var jar = BetterAuthCookies.ParseHeader("a=1; better-auth.session_token=plain; __Secure-better-auth.session_token=secure; a=2; junk");
            Assert.Equal("1", jar["a"]);
            Assert.Equal("secure", BetterAuthCookies.ReadSessionCookie(jar));
            Assert.Equal("plain", BetterAuthCookies.ReadSessionCookie("better-auth.session_token=plain"));
            Assert.Null(BetterAuthCookies.ReadSessionCookie("other=1"));
            Assert.Equal("x", BetterAuthCookies.ReadSessionCookie("custom=x", cookieName: "custom"));
            Assert.Null(BetterAuthCookies.ReadSessionCookie((string?)null));
        }

        [Fact]
        public void BridgeCookie_Serializes()
        {
            Assert.Equal("__session=J; Path=/; Secure; SameSite=Lax", new BridgeCookie { Name = "__session", Value = "J", Secure = true, SameSite = "lax" }.Serialize());
            Assert.Equal(
                "n=; Path=/; Domain=example.com; Max-Age=0; Expires=Thu, 01 Jan 1970 00:00:00 GMT; HttpOnly",
                new BridgeCookie { Name = "n", Domain = "example.com", MaxAge = 0, HttpOnly = true }.Serialize());
        }

        private static BetterAuthSessionBridge BridgeReturning(BetterAuthExchangeResult result, List<(string Token, string? Ip, string? Ua)>? calls = null, BetterAuthBridgeOptions? options = null)
            => new BetterAuthSessionBridge((token, ip, ua, _) =>
            {
                calls?.Add((token, ip, ua));
                return Task.FromResult(result);
            }, options);

        private static readonly BetterAuthExchangeResult SessionResult = new BetterAuthExchangeResult
        {
            Kind = BetterAuthExchangeKind.Session, Jwt = "JWT", RefreshToken = "RT",
        };

        [Fact]
        public async Task Bridge_NoBetterAuthCookie_OrAnAtlasSession_DoesNothingAndCallsNothing()
        {
            var calls = new List<(string, string?, string?)>();
            var bridge = BridgeReturning(SessionResult, calls);
            Assert.Equal(BridgeAction.None, (await bridge.DecideAsync("other=1")).Action);
            Assert.Equal(BridgeAction.None, (await bridge.DecideAsync("better-auth.session_token=t; __session=jwt")).Action);
            Assert.Equal(BridgeAction.None, (await bridge.DecideAsync("better-auth.session_token=t; __atlas_rt=rt")).Action);
            Assert.Empty(calls);
        }

        [Fact]
        public async Task Bridge_SignedIn_SetsAtlasCookiesAndClearsTheOldOnes()
        {
            var calls = new List<(string Token, string? Ip, string? Ua)>();
            var bridge = BridgeReturning(SessionResult, calls, new BetterAuthBridgeOptions { BetterAuthCookieDomain = "example.com" });
            var d = await bridge.DecideAsync("__Secure-better-auth.session_token=tok.sig", userAgent: "UA", forwardedFor: "198.51.100.7, 10.0.0.1");

            Assert.Equal(BridgeAction.SignedIn, d.Action);
            Assert.Equal(("tok.sig", "198.51.100.7", "UA"), calls.Single());
            var set = d.Cookies.Take(2).ToList();
            Assert.Equal("__session=JWT; Path=/; Secure; SameSite=Lax", set[0].Serialize());
            Assert.Equal("__atlas_rt=RT; Path=/; HttpOnly; Secure; SameSite=Lax", set[1].Serialize());
            var cleared = d.Cookies.Skip(2).ToList();
            Assert.Equal(8, cleared.Count);
            Assert.All(cleared, k => { Assert.Equal(0, k.MaxAge); Assert.Equal("example.com", k.Domain); });
            Assert.True(cleared.Single(k => k.Name == "__Secure-better-auth.session_token").Secure);
            Assert.False(cleared.Single(k => k.Name == "better-auth.session_token").Secure);

            // The framework's own client address wins over the forwarded header.
            await bridge.DecideAsync("better-auth.session_token=t2", clientIp: "192.0.2.1", forwardedFor: "198.51.100.7");
            Assert.Equal("192.0.2.1", calls[1].Ip);
        }

        [Fact]
        public async Task Bridge_SecureCanBeTurnedOffForLocalHttp()
        {
            var d = await BridgeReturning(SessionResult, options: new BetterAuthBridgeOptions { Secure = false }).DecideAsync("better-auth.session_token=t");
            Assert.False(d.Cookies[0].Secure);
            Assert.False(d.Cookies[1].Secure);
        }

        [Fact]
        public async Task Bridge_CustomCookieNameAndPrefix()
        {
            var calls = new List<(string Token, string? Ip, string? Ua)>();
            var bridge = BridgeReturning(SessionResult, calls, new BetterAuthBridgeOptions { CookiePrefix = "my-app" });
            await bridge.DecideAsync("my-app.session_token=a");
            await bridge.DecideAsync("better-auth.session_token=ignored");
            Assert.Equal("a", calls.Single().Token);

            var named = BridgeReturning(SessionResult, calls, new BetterAuthBridgeOptions { CookieName = "sid" });
            var d = await named.DecideAsync("__Secure-sid=b");
            Assert.Equal("b", calls[1].Token);
            Assert.Contains(d.Cookies, k => k.Name == "sid" && k.MaxAge == 0);
            Assert.Contains(d.Cookies, k => k.Name == "__Secure-sid" && k.Secure);
        }

        [Fact]
        public async Task Bridge_NeedsMfa_ClearsTheSpentCookiesAndBuildsTheContinuationUrl()
        {
            var bridge = BridgeReturning(new BetterAuthExchangeResult { Kind = BetterAuthExchangeKind.NeedsMfa, AttemptId = "sia 1", Status = "needs_second_factor" });
            var d = await bridge.DecideAsync("better-auth.session_token=t");
            Assert.Equal(BridgeAction.NeedsMfa, d.Action);
            Assert.Equal(8, d.Cookies.Count);
            Assert.All(d.Cookies, k => Assert.Equal(0, k.MaxAge));
            Assert.Equal("/sign-in?next=%2F&__atlas_attempt=sia%201&__atlas_status=needs_second_factor",
                d.ContinuationUrl("/sign-in?next=%2F&__atlas_attempt=old&__atlas_status=old"));
            Assert.Equal("https://x.test/a?__atlas_attempt=sia%201&__atlas_status=needs_second_factor#h",
                d.ContinuationUrl("https://x.test/a#h"));
        }

        [Theory]
        [InlineData("invalid", true)]
        [InlineData("signature_invalid", true)]
        [InlineData("expired", true)]
        [InlineData("reused", true)]
        [InlineData("user_unavailable", true)]
        [InlineData("closed", false)]
        [InlineData("rate_limited", false)]
        [InlineData("unavailable", false)]
        public async Task Bridge_Refused_ClearsTheCookiesOnlyForFinalOutcomes(string outcome, bool clears)
        {
            var bridge = BridgeReturning(new BetterAuthExchangeResult { Kind = BetterAuthExchangeKind.Refused, Outcome = outcome });
            var d = await bridge.DecideAsync("better-auth.session_token=t");
            Assert.Equal(BridgeAction.Refused, d.Action);
            Assert.Equal(outcome, d.Outcome);
            Assert.Equal(clears, d.Cookies.Count == 8);
        }

        [Fact]
        public async Task Bridge_OverAClient_UsesTheSecretKeyExchange()
        {
            var h = MockHttpMessageHandler.Json(SessionJson);
            using var c = Client(h);
            var d = await new BetterAuthSessionBridge(c).DecideAsync("better-auth.session_token=tok.sig", "203.0.113.5", "UA/1");
            Assert.Equal(BridgeAction.SignedIn, d.Action);
            Assert.Equal("/v1/migrations/better_auth/exchange", h.Requests[0].Path);
            Assert.Equal("203.0.113.5", h.Requests[0].Header("X-Atlas-Client-IP"));
            Assert.Equal("UA/1", h.Requests[0].Header("X-Atlas-Client-User-Agent"));
        }

        // ---- ASP.NET Core middleware ----

        private static TestServer BridgeServer(BetterAuthSessionBridge bridge)
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.UseAtlasBetterAuthBridge(bridge);
                app.Run(ctx => ctx.Response.WriteAsync("app:" + ctx.Request.Method + ":" + ctx.Request.Path + ctx.Request.QueryString));
            });
            return new TestServer(builder);
        }

        private static HttpRequestMessage Req(HttpMethod m, string url, string cookie)
        {
            var r = new HttpRequestMessage(m, url);
            r.Headers.TryAddWithoutValidation("Cookie", cookie);
            r.Headers.TryAddWithoutValidation("User-Agent", "TestUA/1.0");
            return r;
        }

        [Fact]
        public async Task Middleware_SignedIn_RedirectsToTheSameUrlWithAtlasCookiesSet()
        {
            var calls = new List<(string Token, string? Ip, string? Ua)>();
            using var server = BridgeServer(BridgeReturning(SessionResult, calls));
            var res = await server.CreateClient().SendAsync(Req(HttpMethod.Get, "http://localhost/dash?tab=2", "better-auth.session_token=tok.sig"));

            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Equal("/dash?tab=2", res.Headers.Location!.OriginalString);
            Assert.Equal("no-store", res.Headers.CacheControl!.ToString());
            var setCookies = res.Headers.GetValues("Set-Cookie").ToList();
            Assert.Contains("__session=JWT; Path=/; Secure; SameSite=Lax", setCookies);
            Assert.Contains("__atlas_rt=RT; Path=/; HttpOnly; Secure; SameSite=Lax", setCookies);
            Assert.Equal(10, setCookies.Count);
            Assert.Contains(setCookies, c => c.StartsWith("better-auth.session_token=; Path=/; Max-Age=0"));
            Assert.Equal("tok.sig", calls.Single().Token);
            Assert.Equal("TestUA/1.0", calls.Single().Ua);
        }

        [Fact]
        public async Task Middleware_NeedsMfa_RedirectsToTheSignInPageWithTheAttempt()
        {
            var bridge = BridgeReturning(
                new BetterAuthExchangeResult { Kind = BetterAuthExchangeKind.NeedsMfa, AttemptId = "sia_1", Status = "needs_mfa_enrollment" },
                options: new BetterAuthBridgeOptions { SignInUrl = "/sign-in" });
            using var server = BridgeServer(bridge);
            var res = await server.CreateClient().SendAsync(Req(HttpMethod.Get, "http://localhost/dash", "better-auth.session_token=t"));
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Equal("/sign-in?__atlas_attempt=sia_1&__atlas_status=needs_mfa_enrollment", res.Headers.Location!.OriginalString);
            Assert.Equal(8, res.Headers.GetValues("Set-Cookie").Count());
        }

        [Fact]
        public async Task Middleware_FinalRefusal_ClearsTheCookiesAndCarriesOn()
        {
            var bridge = BridgeReturning(new BetterAuthExchangeResult { Kind = BetterAuthExchangeKind.Refused, Outcome = "expired" });
            using var server = BridgeServer(bridge);
            var res = await server.CreateClient().SendAsync(Req(HttpMethod.Get, "/public", "better-auth.session_token=t"));
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("app:GET:/public", await res.Content.ReadAsStringAsync());
            Assert.Equal(8, res.Headers.GetValues("Set-Cookie").Count());
        }

        [Fact]
        public async Task Middleware_RetryableRefusal_KeepsTheCookiesAndCarriesOn()
        {
            var bridge = BridgeReturning(new BetterAuthExchangeResult { Kind = BetterAuthExchangeKind.Refused, Outcome = "rate_limited" });
            using var server = BridgeServer(bridge);
            var res = await server.CreateClient().SendAsync(Req(HttpMethod.Get, "/public", "better-auth.session_token=t"));
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.False(res.Headers.Contains("Set-Cookie"));
        }

        [Fact]
        public async Task Middleware_OnlyGetAndHead_AndNeverForAnAtlasSession()
        {
            var calls = new List<(string, string?, string?)>();
            using var server = BridgeServer(BridgeReturning(SessionResult, calls));
            var post = await server.CreateClient().SendAsync(Req(HttpMethod.Post, "/form", "better-auth.session_token=t"));
            Assert.Equal(HttpStatusCode.OK, post.StatusCode);
            Assert.Equal("app:POST:/form", await post.Content.ReadAsStringAsync());
            var signedIn = await server.CreateClient().SendAsync(Req(HttpMethod.Get, "/x", "better-auth.session_token=t; __session=jwt"));
            Assert.Equal("app:GET:/x", await signedIn.Content.ReadAsStringAsync());
            var none = await server.CreateClient().GetAsync("/y");
            Assert.Equal("app:GET:/y", await none.Content.ReadAsStringAsync());
            Assert.Empty(calls);

            var head = await server.CreateClient().SendAsync(Req(HttpMethod.Head, "http://localhost/h", "better-auth.session_token=t"));
            Assert.Equal(HttpStatusCode.Redirect, head.StatusCode);
            Assert.Single(calls);
        }

        [Fact]
        public async Task Middleware_OverAClient_ExchangesOnceWithTheSecretKey()
        {
            var h = MockHttpMessageHandler.Json(SessionJson);
            using var c = Client(h);
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.UseAtlasBetterAuthBridge(c, o => o.Secure = false);
                app.Run(ctx => ctx.Response.WriteAsync("app"));
            });
            using var server = new TestServer(builder);
            var res = await server.CreateClient().SendAsync(Req(HttpMethod.Get, "http://localhost/", "better-auth.session_token=tok.sig"));
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.Contains("__session=JWT; Path=/; SameSite=Lax", res.Headers.GetValues("Set-Cookie"));
            Assert.Single(h.Requests);
            Assert.Equal("Bearer sk_test_123", h.Requests[0].Authorization);
            Assert.Equal("TestUA/1.0", h.Requests[0].Header("X-Atlas-Client-User-Agent"));
        }

        // ---- legacy-issuer JWTs ----

        private const string AtlasIssuer = "https://issuer.test";
        private const string LegacyIss = "https://old-app.example/api/auth";
        private const string AtlasJwksUrl = "https://jwks.test/keys";

        private readonly RSA _atlasRsa = RSA.Create(2048);
        private readonly RSA _legacyRsa = RSA.Create(2048);

        private static string RsaJwks(RSA rsa, string kid)
        {
            var p = rsa.ExportParameters(false);
            return "{\"keys\":[{\"kty\":\"RSA\",\"kid\":\"" + kid + "\",\"n\":\"" + Base64UrlEncoder.Encode(p.Modulus) + "\",\"e\":\"" + Base64UrlEncoder.Encode(p.Exponent) + "\"}]}";
        }

        private static string SignRsa(RSA rsa, string kid, string issuer, IDictionary<string, object> claims, DateTime? expires, string alg = SecurityAlgorithms.RsaSha256)
            // No default exp / nbf / iat: the tests choose them (a token without exp must stay without one).
            => new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Claims = claims,
                Expires = expires,
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = kid }, alg),
            });

        private static Dictionary<string, object> LegacyClaims(string sub = "ba_user_1") => new Dictionary<string, object> { ["sub"] = sub, ["email"] = "a@b.test" };

        private sealed class Lookups
        {
            public int Count;
            public string? Auth;
            public string? Query;
            public string Answer = "{\"object\":\"list\",\"data\":[{\"id\":\"user_atlas_1\"}],\"has_more\":false}";
            public HttpStatusCode Status = HttpStatusCode.OK;
        }

        private AtlasBackend Backend(Lookups lookups, Action<AtlasBackendOptions>? tweak = null, Func<long>? now = null)
        {
            var handler = new MockHttpMessageHandler((req, _) =>
            {
                if (req.RequestUri!.AbsolutePath == "/v1/users")
                {
                    lookups.Count++;
                    lookups.Auth = req.Headers.Authorization?.ToString();
                    lookups.Query = req.RequestUri.Query;
                    return (lookups.Status, lookups.Answer);
                }
                if (req.RequestUri.Host == "old-app.example") return (HttpStatusCode.OK, RsaJwks(_legacyRsa, "legacy-1"));
                return (HttpStatusCode.OK, RsaJwks(_atlasRsa, "atlas-1"));
            });
            var options = new AtlasBackendOptions
            {
                JwksUrl = AtlasJwksUrl,
                Issuer = AtlasIssuer,
                SecretKey = "sk_test_123",
                BapiBaseUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
                Now = now,
                LegacyIssuers = new[]
                {
                    new LegacyIssuer { Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(30), Jwks = RsaJwks(_legacyRsa, "legacy-1") },
                },
            };
            tweak?.Invoke(options);
            return new AtlasBackend(options);
        }

        [Fact]
        public async Task Legacy_ValidToken_MapsSubToTheAtlasUserThroughExternalId()
        {
            var lookups = new Lookups();
            var backend = Backend(lookups);
            var token = SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(5));

            var result = await backend.VerifyAsync(token);

            Assert.True(result.Ok);
            var claims = result.Claims!;
            Assert.Equal("user_atlas_1", claims.Sub);
            Assert.Equal(LegacyIss, claims.Iss);
            Assert.Equal("ba_user_1", claims.Extra["external_id"].GetString());
            var legacy = claims.Extra["legacy"];
            Assert.Equal("better_auth", legacy.GetProperty("kind").GetString());
            Assert.Equal(LegacyIss, legacy.GetProperty("issuer").GetString());
            Assert.Equal("ba_user_1", legacy.GetProperty("sub").GetString());
            Assert.Equal("a@b.test", claims.Extra["email"].GetString());
            Assert.Equal("Bearer sk_test_123", lookups.Auth);
            Assert.Equal("?external_id=ba_user_1&limit=1", lookups.Query);
        }

        [Fact]
        public async Task Legacy_SubjectLookupIsCached_SuccessAndNotFound_ButNotFailures()
        {
            var lookups = new Lookups();
            long now = 1_000_000_000_000;
            var backend = Backend(lookups, now: () => now);
            string Tok(string sub) => SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(sub), DateTimeOffset.FromUnixTimeMilliseconds(now).UtcDateTime.AddMinutes(5));

            Assert.True((await backend.VerifyAsync(Tok("ba_user_1"))).Ok);
            Assert.True((await backend.VerifyAsync(Tok("ba_user_1"))).Ok);
            Assert.Equal(1, lookups.Count);

            // After the 5 minute TTL the lookup runs again.
            now += 5 * 60_000 + 1;
            Assert.True((await backend.VerifyAsync(Tok("ba_user_1"))).Ok);
            Assert.Equal(2, lookups.Count);

            // An unknown user is refused, and that answer is cached too.
            lookups.Answer = "{\"data\":[]}";
            Assert.False((await backend.VerifyAsync(Tok("ba_ghost"))).Ok);
            Assert.False((await backend.VerifyAsync(Tok("ba_ghost"))).Ok);
            Assert.Equal(3, lookups.Count);

            // A failed lookup refuses the token but is not remembered.
            lookups.Status = HttpStatusCode.InternalServerError;
            Assert.False((await backend.VerifyAsync(Tok("ba_other"))).Ok);
            lookups.Status = HttpStatusCode.OK;
            lookups.Answer = "{\"data\":[{\"id\":\"user_atlas_2\"}]}";
            var again = await backend.VerifyAsync(Tok("ba_other"));
            Assert.True(again.Ok);
            Assert.Equal("user_atlas_2", again.Claims!.Sub);
        }

        [Fact]
        public async Task Legacy_ResolveSubject_ReplacesTheLookup_AndNullRefuses()
        {
            var lookups = new Lookups();
            var seen = new List<string>();
            var backend = Backend(lookups, o =>
            {
                o.SecretKey = null;
                o.LegacyIssuers = new[]
                {
                    new LegacyIssuer
                    {
                        Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(1), Jwks = RsaJwks(_legacyRsa, "legacy-1"),
                        ResolveSubject = (sub, claims, _) => { seen.Add(sub + ":" + claims["email"].GetString()); return Task.FromResult<string?>(sub == "ba_user_1" ? "mapped_1" : null); },
                    },
                };
            });
            string Tok(string sub) => SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(sub), DateTime.UtcNow.AddMinutes(5));

            var ok = await backend.VerifyAsync(Tok("ba_user_1"));
            Assert.Equal("mapped_1", ok.Claims!.Sub);
            Assert.False((await backend.VerifyAsync(Tok("ba_user_2"))).Ok);
            Assert.Equal(new[] { "ba_user_1:a@b.test", "ba_user_2:a@b.test" }, seen.ToArray());
            Assert.Equal(0, lookups.Count);
        }

        [Fact]
        public async Task Legacy_ResolveSubjectThrowing_RefusesTheToken()
        {
            var backend = Backend(new Lookups(), o =>
            {
                o.LegacyIssuers = new[]
                {
                    new LegacyIssuer
                    {
                        Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(1), Jwks = RsaJwks(_legacyRsa, "legacy-1"),
                        ResolveSubject = (_, __, ___) => throw new InvalidOperationException("db down"),
                    },
                };
            });
            var r = await backend.VerifyAsync(SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(5)));
            Assert.False(r.Ok);
            Assert.Equal(VerifyFailureReason.Invalid, r.Reason);
        }

        [Fact]
        public async Task Legacy_RefusesAfterTheWindow_NoExp_ExpiredBadSignatureAndUnknownIssuer()
        {
            var lookups = new Lookups();
            var now = DateTimeOffset.UtcNow;
            var backend = Backend(lookups, o =>
            {
                o.LegacyIssuers = new[] { new LegacyIssuer { Issuer = LegacyIss, Until = now.AddMinutes(10), Jwks = RsaJwks(_legacyRsa, "legacy-1") } };
            });

            // exp is required.
            var noExp = SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), null);
            var r1 = await backend.VerifyAsync(noExp);
            Assert.False(r1.Ok);
            Assert.Equal(VerifyFailureReason.Invalid, r1.Reason);

            // Expired (beyond the 5 s skew).
            var expired = SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(-1), SecurityAlgorithms.RsaSha256);
            Assert.False((await backend.VerifyAsync(expired)).Ok);

            // Signed by someone else's key under the same kid.
            using var stranger = RSA.Create(2048);
            Assert.False((await backend.VerifyAsync(SignRsa(stranger, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(5)))).Ok);

            // An issuer nobody configured is just a foreign Atlas token: invalid.
            Assert.False((await backend.VerifyAsync(SignRsa(_legacyRsa, "legacy-1", "https://elsewhere.example", LegacyClaims(), DateTime.UtcNow.AddMinutes(5)))).Ok);

            // The window: a token minted before, presented after.
            var closed = Backend(lookups, o =>
            {
                o.Now = () => now.AddMinutes(11).ToUnixTimeMilliseconds();
                o.LegacyIssuers = new[] { new LegacyIssuer { Issuer = LegacyIss, Until = now.AddMinutes(10), Jwks = RsaJwks(_legacyRsa, "legacy-1") } };
            });
            var inWindow = SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddHours(2));
            Assert.False((await closed.VerifyAsync(inWindow)).Ok);
            Assert.Equal(0, lookups.Count);
        }

        [Fact]
        public async Task Legacy_AlgorithmsAreBounded()
        {
            var backend = Backend(new Lookups(), o =>
            {
                o.LegacyIssuers = new[]
                {
                    new LegacyIssuer { Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(1), Jwks = RsaJwks(_legacyRsa, "legacy-1"), Algorithms = new[] { "ES256" } },
                };
            });
            // RS256 is a Better Auth algorithm, but this issuer narrowed itself to ES256.
            Assert.False((await backend.VerifyAsync(SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(5)))).Ok);

            // HS256 / none never verify, whatever the key set says.
            string B64(string json) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
            var none = B64("{\"alg\":\"none\"}") + "." + B64("{\"iss\":\"" + LegacyIss + "\",\"sub\":\"x\",\"exp\":9999999999}") + ".";
            Assert.False((await Backend(new Lookups()).VerifyAsync(none)).Ok);
            var hs = B64("{\"alg\":\"HS256\",\"kid\":\"legacy-1\"}") + "." + B64("{\"iss\":\"" + LegacyIss + "\",\"sub\":\"x\",\"exp\":9999999999}") + "." + Base64UrlEncoder.Encode(new byte[32]);
            Assert.False((await Backend(new Lookups()).VerifyAsync(hs)).Ok);
        }

        [Fact]
        public async Task Legacy_ES256_And_PS256_Verify()
        {
            using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var q = ec.ExportParameters(false).Q;
            var ecJwks = "{\"keys\":[{\"kty\":\"EC\",\"crv\":\"P-256\",\"kid\":\"ec-1\",\"x\":\"" + Base64UrlEncoder.Encode(q.X) + "\",\"y\":\"" + Base64UrlEncoder.Encode(q.Y) + "\"}]}";
            var backend = Backend(new Lookups(), o =>
            {
                o.LegacyIssuers = new[] { new LegacyIssuer { Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(1), Jwks = ecJwks } };
            });
            var es = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = LegacyIss,
                Claims = LegacyClaims(),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(ec) { KeyId = "ec-1" }, SecurityAlgorithms.EcdsaSha256),
            });
            Assert.True((await backend.VerifyAsync(es)).Ok);

            var ps = Backend(new Lookups());
            var psToken = SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(5), SecurityAlgorithms.RsaSsaPssSha256);
            Assert.True((await ps.VerifyAsync(psToken)).Ok);
        }

        [Fact]
        public async Task Legacy_AudienceAndAuthorizedParties()
        {
            var aud = Backend(new Lookups(), o =>
            {
                o.LegacyIssuers = new[]
                {
                    new LegacyIssuer { Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(1), Jwks = RsaJwks(_legacyRsa, "legacy-1"), Audiences = new[] { "my-api" } },
                };
            });
            var withAud = LegacyClaims(); withAud["aud"] = new[] { "other", "my-api" };
            Assert.True((await aud.VerifyAsync(SignRsa(_legacyRsa, "legacy-1", LegacyIss, withAud, DateTime.UtcNow.AddMinutes(5)))).Ok);
            var wrongAud = LegacyClaims(); wrongAud["aud"] = "other";
            Assert.False((await aud.VerifyAsync(SignRsa(_legacyRsa, "legacy-1", LegacyIss, wrongAud, DateTime.UtcNow.AddMinutes(5)))).Ok);
            Assert.False((await aud.VerifyAsync(SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(5)))).Ok);

            var azp = Backend(new Lookups(), o => o.AuthorizedParties = new[] { "https://app.test" });
            var bad = LegacyClaims(); bad["azp"] = "https://evil.test";
            var r = await azp.VerifyAsync(SignRsa(_legacyRsa, "legacy-1", LegacyIss, bad, DateTime.UtcNow.AddMinutes(5)));
            Assert.Equal(VerifyFailureReason.UnauthorizedParty, r.Reason);
            // A legacy token carries no azp; it is not refused for lacking one.
            Assert.True((await azp.VerifyAsync(SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(5)))).Ok);
        }

        [Fact]
        public async Task Legacy_JwksUrl_IsFetched_AndAnOutageIsNoKeys()
        {
            var fetched = 0;
            var handler = new MockHttpMessageHandler((req, _) =>
            {
                if (req.RequestUri!.Host == "old-app.example") { fetched++; return (HttpStatusCode.OK, RsaJwks(_legacyRsa, "legacy-1")); }
                if (req.RequestUri.AbsolutePath == "/v1/users") return (HttpStatusCode.OK, "{\"data\":[{\"id\":\"user_atlas_1\"}]}");
                return (HttpStatusCode.OK, RsaJwks(_atlasRsa, "atlas-1"));
            });
            AtlasBackend Make(string url) => new AtlasBackend(new AtlasBackendOptions
            {
                JwksUrl = AtlasJwksUrl, Issuer = AtlasIssuer, SecretKey = "sk_test_123", HttpClient = new HttpClient(handler),
                LegacyIssuers = new[] { new LegacyIssuer { Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(1), JwksUrl = url } },
            });
            var token = SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(5));

            var backend = Make("https://old-app.example/api/auth/jwks");
            Assert.True((await backend.VerifyAsync(token)).Ok);
            Assert.True((await backend.VerifyAsync(token)).Ok);
            Assert.Equal(1, fetched);

            var down = new MockHttpMessageHandler((req, _) => req.RequestUri!.Host == "old-app.example"
                ? (HttpStatusCode.InternalServerError, "no")
                : (HttpStatusCode.OK, RsaJwks(_atlasRsa, "atlas-1")));
            var outage = new AtlasBackend(new AtlasBackendOptions
            {
                JwksUrl = AtlasJwksUrl, Issuer = AtlasIssuer, SecretKey = "sk_test_123", HttpClient = new HttpClient(down),
                LegacyIssuers = new[] { new LegacyIssuer { Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(1), JwksUrl = "https://old-app.example/api/auth/jwks" } },
            });
            Assert.Equal(VerifyFailureReason.NoKeys, (await outage.VerifyAsync(token)).Reason);
        }

        [Fact]
        public async Task Legacy_AtlasTokensAreUnaffected()
        {
            var lookups = new Lookups();
            var backend = Backend(lookups);
            var claims = new Dictionary<string, object> { ["sub"] = "user_1", ["sid"] = "sess_1", ["token_use"] = "session" };
            var token = SignRsa(_atlasRsa, "atlas-1", AtlasIssuer, claims, DateTime.UtcNow.AddMinutes(1));
            var r = await backend.VerifyAsync(token);
            Assert.True(r.Ok);
            Assert.Equal("user_1", r.Claims!.Sub);
            Assert.False(r.Claims.Extra.ContainsKey("legacy"));
            Assert.Equal(0, lookups.Count);
        }

        [Fact]
        public void Legacy_ConfigurationErrorsThrowAtConstruction()
        {
            AtlasBackend Make(Action<LegacyIssuer> tweak, Action<AtlasBackendOptions>? o = null)
            {
                var issuer = new LegacyIssuer { Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(1), Jwks = RsaJwks(_legacyRsa, "k") };
                tweak(issuer);
                var options = new AtlasBackendOptions
                {
                    JwksUrl = AtlasJwksUrl, Issuer = AtlasIssuer, SecretKey = "sk", HttpClient = new HttpClient(MockHttpMessageHandler.Json("{}")),
                    LegacyIssuers = new[] { issuer },
                };
                o?.Invoke(options);
                return new AtlasBackend(options);
            }

            Assert.NotNull(Make(_ => { }));
            Assert.Contains("Until", Assert.Throws<ArgumentException>(() => Make(i => i.Until = default)).Message);
            Assert.Contains("exactly one", Assert.Throws<ArgumentException>(() => Make(i => i.JwksUrl = "https://x.test/jwks")).Message);
            Assert.Contains("exactly one", Assert.Throws<ArgumentException>(() => Make(i => i.Jwks = null)).Message);
            Assert.Contains("not a JWKS", Assert.Throws<ArgumentException>(() => Make(i => i.Jwks = "{\"nope\":1}")).Message);
            Assert.Contains("not a JWKS", Assert.Throws<ArgumentException>(() => Make(i => i.Jwks = "garbage")).Message);
            Assert.Contains("Atlas issuer", Assert.Throws<ArgumentException>(() => Make(i => i.Issuer = AtlasIssuer)).Message);
            Assert.Contains("Issuer", Assert.Throws<ArgumentException>(() => Make(i => i.Issuer = "")).Message);
            Assert.Contains("SecretKey", Assert.Throws<ArgumentException>(() => Make(_ => { }, o => o.SecretKey = null)).Message);
            Assert.NotNull(Make(i => i.ResolveSubject = (_, __, ___) => Task.FromResult<string?>("x"), o => o.SecretKey = null));
        }

        [Fact]
        public async Task Legacy_ThroughTheAspNetCoreHandler_AuthenticatesALegacyBearer()
        {
            var lookups = new Lookups();
            var handler = new MockHttpMessageHandler((req, _) => req.RequestUri!.AbsolutePath == "/v1/users"
                ? (HttpStatusCode.OK, lookups.Answer)
                : (HttpStatusCode.OK, RsaJwks(_atlasRsa, "atlas-1")));
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAtlasAuthentication(o =>
                    {
                        o.JwksUrl = AtlasJwksUrl;
                        o.Issuer = AtlasIssuer;
                        o.SecretKey = "sk_test_123";
                        o.BapiBaseUrl = "https://api.test.example";
                        o.HttpClient = new HttpClient(handler);
                        o.LegacyIssuers = new[] { new LegacyIssuer { Issuer = LegacyIss, Until = DateTimeOffset.UtcNow.AddDays(1), Jwks = RsaJwks(_legacyRsa, "legacy-1") } };
                    });
                    services.AddAuthorization();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(e => e.MapGet("/me", ctx => ctx.Response.WriteAsync(ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "")).RequireAuthorization());
                });
            using var server = new TestServer(builder);
            var req = new HttpRequestMessage(HttpMethod.Get, "/me");
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + SignRsa(_legacyRsa, "legacy-1", LegacyIss, LegacyClaims(), DateTime.UtcNow.AddMinutes(5)));
            var res = await server.CreateClient().SendAsync(req);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("user_atlas_1", await res.Content.ReadAsStringAsync());
        }
    }
}
