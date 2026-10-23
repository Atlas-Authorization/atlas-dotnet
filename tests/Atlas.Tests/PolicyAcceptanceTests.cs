using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Atlas.Verification;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Policy acceptance: the Backend API resources and the policy source protocol helpers.</summary>
    public class PolicyAcceptanceTests
    {
        // Vectors shared with the TypeScript reference (packages/backend policy-source).
        private const string Secret = "whsec_test_secret_0123456789";
        private const long Ts = 1760000000000L;
        private const string SigKey = "YfnQOCSMi2qm50WuVdA/aIV4avYpC6Hms9wdKrZcgHI=";
        private const string SigFull = "KT51ui3s0XNh9yr/2FedfQgVWCpYAMLQRKOvohc9VTM=";
        private const string SigEvent = "1q9qbfr02vQN6Xwsj0NjzR65jJiWo39qg8+JutS9ebs=";

        private const string PolicyJson = "{\"object\":\"policy\",\"id\":\"pol_1\",\"key\":\"tos\",\"title\":\"Terms\",\"display\":\"scroll\","
            + "\"required\":true,\"applies_to\":\"sign_in_and_sign_up\",\"reaccept\":\"grace\",\"grace_days\":14,\"source\":\"atlas\","
            + "\"position\":0,\"status\":\"active\",\"overrides\":[],\"current_version\":{\"id\":\"polv_1\",\"version\":\"2026-10-24\","
            + "\"status\":\"published\",\"content_sha256\":\"abc\",\"published_at\":1,\"effective_at\":2},\"upcoming_version\":null,"
            + "\"latest_version\":null,\"retired_at\":null,\"created_at\":1,\"updated_at\":2}";

        private static AtlasClient Client(MockHttpMessageHandler handler) =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

        [Fact]
        public async Task CreateAndReadAPolicy()
        {
            var handler = MockHttpMessageHandler.Json(PolicyJson);
            using var client = Client(handler);
            var p = await client.Policies.CreateAsync(new CreatePolicyBody
            {
                Key = "tos", Title = "Terms", Display = "scroll", Required = true,
                AppliesTo = "sign_in_and_sign_up", Reaccept = "grace", GraceDays = 14,
            });
            Assert.Equal("POST", handler.Requests[0].Method);
            Assert.Equal("/v1/policies", handler.Requests[0].Url.AbsolutePath);
            using var doc = JsonDocument.Parse(handler.Requests[0].Body);
            Assert.Equal("sign_in_and_sign_up", doc.RootElement.GetProperty("applies_to").GetString());
            Assert.Equal(14, doc.RootElement.GetProperty("grace_days").GetInt32());
            Assert.Equal("polv_1", p.CurrentVersion!.Id);
            Assert.Null(p.UpcomingVersion);
            Assert.Equal(14, p.GraceDays);
        }

        [Fact]
        public async Task UpdateClearsGraceDaysWithAnExplicitNull()
        {
            var handler = MockHttpMessageHandler.Json(PolicyJson);
            using var client = Client(handler);
            await client.Policies.UpdateAsync("tos", new UpdatePolicyBody { Reaccept = "none" }.Clear("grace_days"));
            Assert.Equal("PATCH", handler.Requests[0].Method);
            Assert.Equal("/v1/policies/tos", handler.Requests[0].Url.AbsolutePath);
            using var doc = JsonDocument.Parse(handler.Requests[0].Body);
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("grace_days").ValueKind);
            Assert.Equal("none", doc.RootElement.GetProperty("reaccept").GetString());
            Assert.False(doc.RootElement.TryGetProperty("title", out _));
        }

        [Fact]
        public async Task DeleteReportsRetireVersusDelete()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"policy\",\"id\":\"pol_1\",\"key\":\"tos\",\"deleted\":false,\"retired\":true}");
            using var client = Client(handler);
            var d = await client.Policies.DeleteAsync("pol_1");
            Assert.Equal("DELETE", handler.Requests[0].Method);
            Assert.True(d.Retired);
            Assert.False(d.Deleted);
        }

        [Fact]
        public async Task VersionsPublishRetireAndDrafts()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"policy_version\",\"id\":\"polv_2\",\"policy_id\":\"pol_1\",\"policy\":\"tos\",\"version\":\"2026-11-01\","
                + "\"status\":\"draft\",\"content_html\":\"<p>x</p>\",\"content_sha256\":\"h\",\"created_at\":1,\"updated_at\":1}");
            using var client = Client(handler);
            var v = await client.Policies.CreateVersionAsync("tos", new CreatePolicyVersionBody
            {
                Version = "2026-11-01", ContentMarkdown = "x", SummaryOfChanges = "Fees",
            });
            Assert.Equal("/v1/policies/tos/versions", handler.Requests[0].Url.AbsolutePath);
            Assert.Equal("draft", v.Status);
            Assert.Contains("\"summary_of_changes\":\"Fees\"", handler.Requests[0].Body);

            await client.Policies.PublishVersionAsync("tos", "2026-11-01", 1760000000000L);
            Assert.Equal("POST", handler.Requests[1].Method);
            Assert.Equal("/v1/policies/tos/versions/2026-11-01/publish", handler.Requests[1].Url.AbsolutePath);
            Assert.Contains("\"effective_at\":1760000000000", handler.Requests[1].Body);

            await client.Policies.PublishVersionAtAsync("tos", "2026-11-01", "2026-12-01T00:00:00Z");
            Assert.Contains("\"effective_at\":\"2026-12-01T00:00:00Z\"", handler.Requests[2].Body);

            await client.Policies.RetireVersionAsync("tos", "polv_2");
            Assert.Equal("/v1/policies/tos/versions/polv_2/retire", handler.Requests[3].Url.AbsolutePath);
            await client.Policies.DeleteVersionAsync("tos", "polv_2");
            Assert.Equal("DELETE", handler.Requests[4].Method);
        }

        [Fact]
        public async Task AcceptancesPageAndCsv()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[{\"object\":\"policy_acceptance\",\"id\":\"c1\",\"user_id\":\"user_1\",\"policy\":\"tos\","
                + "\"version\":\"2026-10-24\",\"status\":\"accepted\",\"scrolled_to_end\":true,\"view_ms\":4200,\"accepted_at\":5}],"
                + "\"has_more\":true,\"next_cursor\":\"c1\"}");
            using var client = Client(handler);
            var page = await client.Policies.ListAcceptancesAsync("tos", version: "2026-10-24", status: "all", limit: 10);
            var url = handler.Requests[0].Url;
            Assert.Equal("/v1/policies/tos/acceptances", url.AbsolutePath);
            Assert.Contains("version=2026-10-24", url.Query);
            Assert.Contains("status=all", url.Query);
            Assert.Contains("limit=10", url.Query);
            Assert.True(page.HasMore);
            Assert.Equal("c1", page.NextCursor);
            Assert.Equal(4200, page.Data[0].ViewMs);

            var csvHandler = MockHttpMessageHandler.Json("user_id,policy\nuser_1,tos\n");
            using var csvClient = Client(csvHandler);
            var csv = await csvClient.Policies.ExportAcceptancesCsvAsync("tos");
            Assert.Contains("format=csv", csvHandler.Requests[0].Url.Query);
            Assert.StartsWith("user_id,policy", csv);
        }

        [Fact]
        public async Task UserStandingAndSettings()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"list\",\"user_id\":\"user_1\",\"needs_acceptance\":true,\"data\":[{\"object\":\"policy_standing\",\"policy\":\"tos\","
                + "\"needs_reaccept\":true,\"blocking\":false,\"grace_ends_at\":9}]}");
            using var client = Client(handler);
            var u = await client.Policies.ForUserAsync("user_1");
            Assert.Equal("/v1/users/user_1/policies", handler.Requests[0].Url.AbsolutePath);
            Assert.True(u.NeedsAcceptance);
            Assert.Equal(9L, u.Data[0].GraceEndsAt);

            var h2 = MockHttpMessageHandler.Json("{\"object\":\"policy_settings\",\"session_claim\":true}");
            using var c2 = Client(h2);
            var s = await c2.Policies.UpdateSettingsAsync(true);
            Assert.Equal("PATCH", h2.Requests[0].Method);
            Assert.Equal("/v1/instance/policy_settings", h2.Requests[0].Url.AbsolutePath);
            Assert.Contains("\"session_claim\":true", h2.Requests[0].Body);
            Assert.True(s.SessionClaim);
        }

        [Fact]
        public async Task PolicySourceSecretIsWriteOnly()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"policy_source\",\"enabled\":true,\"endpoint_url\":\"https://cms.example.com/atlas\",\"signing_secret_set\":true,"
                + "\"sync\":\"webhook\",\"poll_interval_minutes\":60,\"webhook_url\":\"https://api.example.com/v1/webhooks/policies/ins_1\","
                + "\"protocol_version\":\"2026-10-24\"}");
            using var client = Client(handler);
            var src = await client.PolicySource.UpdateAsync(new UpdatePolicySourceBody
            {
                Enabled = true, EndpointUrl = "https://cms.example.com/atlas", SigningSecret = Secret, Sync = "webhook",
            });
            Assert.Equal("PUT", handler.Requests[0].Method);
            Assert.Equal("/v1/instance/policy_source", handler.Requests[0].Url.AbsolutePath);
            Assert.Contains(Secret, handler.Requests[0].Body);
            Assert.True(src.SigningSecretSet);
            Assert.Equal("webhook", src.Sync);

            await client.PolicySource.TestAsync();
            Assert.Equal("/v1/instance/policy_source/test", handler.Requests[1].Url.AbsolutePath);
            await client.PolicySource.SyncAsync();
            Assert.Equal("/v1/instance/policy_source/sync", handler.Requests[2].Url.AbsolutePath);
            await client.PolicySource.ListDeliveriesAsync(5, kind: "fetch", status: "failed");
            Assert.Contains("kind=fetch", handler.Requests[3].Url.Query);
            await client.PolicySource.DeleteAsync();
            Assert.Equal("DELETE", handler.Requests[4].Method);
        }

        // ---- policy source protocol ----

        private static System.Func<string, string?> Headers(string? id, string? sig)
        {
            var m = new Dictionary<string, string>();
            if (id != null) m["atlas-id"] = id;
            m["atlas-timestamp"] = Ts.ToString();
            if (sig != null) m["atlas-signature"] = sig;
            m["atlas-policies-version"] = "2026-10-24";
            return name => m.TryGetValue(name, out var v) ? v : null;
        }

        [Fact]
        public void VerifiesAtlasFeedRequestAgainstTheReferenceVectors()
        {
            var r = PolicySourceProtocol.VerifyFeedRequest("GET", "/atlas/policies?key=tos", Headers("msg_01", "v1," + SigKey), Secret, Ts + 1000);
            Assert.True(r.Verified);
            Assert.Equal("tos", r.Key);
            Assert.Equal("msg_01", r.DeliveryId);
            Assert.Equal("2026-10-24", r.ProtocolVersion);

            var full = PolicySourceProtocol.VerifyFeedRequest("get", "/atlas/policies", Headers("msg_01", "v1," + SigFull), Secret, Ts);
            Assert.True(full.Verified);
            Assert.Null(full.Key);
        }

        [Fact]
        public void AcceptsAnyOfSeveralV1ValuesAndIgnoresOtherVersions()
        {
            var r = PolicySourceProtocol.VerifyFeedRequest("GET", "/atlas/policies?key=tos", Headers("msg_01", "v1,AAAA v2,zzzz v1," + SigKey), Secret, Ts);
            Assert.True(r.Verified);
        }

        [Fact]
        public void RefusesBadFeedRequests()
        {
            Assert.Equal(PolicyFeedFailure.SignatureMismatch, PolicySourceProtocol.VerifyFeedRequest("GET", "/atlas/policies?key=other", Headers("msg_01", "v1," + SigKey), Secret, Ts).Failure);
            Assert.Equal(PolicyFeedFailure.SignatureMismatch, PolicySourceProtocol.VerifyFeedRequest("GET", "/atlas/policies?key=tos", Headers("msg_01", "v1," + SigKey), "wrong", Ts).Failure);
            Assert.Equal(PolicyFeedFailure.StaleTimestamp, PolicySourceProtocol.VerifyFeedRequest("GET", "/atlas/policies?key=tos", Headers("msg_01", "v1," + SigKey), Secret, Ts + 300001).Failure);
            Assert.True(PolicySourceProtocol.VerifyFeedRequest("GET", "/atlas/policies?key=tos", Headers("msg_01", "v1," + SigKey), Secret, Ts + 300000).Verified);
            Assert.Equal(PolicyFeedFailure.MissingHeaders, PolicySourceProtocol.VerifyFeedRequest("GET", "/x", Headers(null, "v1,AA"), Secret, Ts).Failure);
            Assert.Equal(PolicyFeedFailure.MalformedSignature, PolicySourceProtocol.VerifyFeedRequest("GET", "/x", Headers("a", "v2,AA"), Secret, Ts).Failure);
            Assert.Equal(PolicyFeedFailure.WrongMethod, PolicySourceProtocol.VerifyFeedRequest("POST", "/x", Headers("a", "v1,AA"), Secret, Ts).Failure);
        }

        [Fact]
        public void SignsEventsLikeTheReference()
        {
            const string body = "{\"occurred_at\":\"2026-10-24T00:00:00.000Z\",\"type\":\"policy.published\",\"key\":\"tos\",\"version\":\"2026-10-24\"}";
            var signed = PolicySourceProtocol.SignBody(body, Secret, "evt_1", Ts);
            Assert.Equal(body, signed.Body);
            Assert.Equal("v1," + SigEvent, signed.Headers["atlas-signature"]);
            Assert.Equal("evt_1", signed.Headers["atlas-id"]);
            Assert.Equal(Ts.ToString(), signed.Headers["atlas-timestamp"]);

            var pub = PolicySourceProtocol.SignPublished("tos", "2026-10-24", Secret);
            using var doc = JsonDocument.Parse(pub.Body);
            Assert.Equal("policy.published", doc.RootElement.GetProperty("type").GetString());
            Assert.Equal("2026-10-24", doc.RootElement.GetProperty("version").GetString());
            Assert.True(doc.RootElement.TryGetProperty("occurred_at", out _));
            Assert.StartsWith("evt_", pub.Headers["atlas-id"]);

            var ret = PolicySourceProtocol.SignRetired("tos", Secret);
            using var retDoc = JsonDocument.Parse(ret.Body);
            Assert.False(retDoc.RootElement.TryGetProperty("version", out _));
            Assert.Throws<System.ArgumentException>(() => PolicySourceProtocol.SignEvent("policy.published", "tos", null, Secret));
            Assert.Throws<System.ArgumentException>(() => PolicySourceProtocol.SignEvent("nope", "tos", null, Secret));
        }

        [Fact]
        public void PolClaimAndConstants()
        {
            using var doc = JsonDocument.Parse("{\"tos\":\"2026-10-24\"}");
            var claims = new SessionClaims { Extra = new Dictionary<string, JsonElement> { ["pol"] = doc.RootElement.Clone() } };
            Assert.Equal("2026-10-24", claims.Pol()["tos"]);
            Assert.True(claims.HasAcceptedPolicy("tos", "2026-10-24"));
            Assert.False(claims.HasAcceptedPolicy("tos", "old"));
            Assert.Empty(new SessionClaims().Pol());
            Assert.Equal("user.policy_accepted", AtlasConstants.WebhookEventUserPolicyAccepted);
            Assert.Equal("policy.version_published", AtlasConstants.WebhookEventPolicyVersionPublished);
        }
    }
}
