using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Partner platforms, per-call audience, orchestrator fields, include_deleted, operator audiences (1.18.0).</summary>
    public class R3PartnerPlatformsTests
    {
        private const string Platform = "{\"object\":\"partner_platform\",\"id\":\"pp_1\",\"partner\":\"acme\",\"name\":\"Acme\",\"preset\":false,"
            + "\"platform_id\":\"plat_1\",\"enabled\":true,\"jwks_url\":\"https://p.example/jwks\",\"signature_header\":\"x-sig\",\"signature_scheme\":\"t_kid_ed25519\","
            + "\"tolerance_seconds\":300,\"allowed_sources\":[\"a\"],\"record_consents\":true,\"receiver_url\":\"https://r.example\",\"receiver_url_template\":\"\","
            + "\"serves_deployment\":false,\"registration\":{\"status\":\"registered\",\"registered_at\":5},\"token_set\":true,\"token_prefix\":\"pt_ab\","
            + "\"last_delivery_at\":null,\"last_delivery_error\":null,\"created_at\":1,\"updated_at\":2}";

        private static AtlasClient Client(MockHttpMessageHandler h) => new AtlasClient(new AtlasClientOptions
        {
            SecretKey = "sk_test_123",
            ApiUrl = "https://api.test.example",
            HttpClient = new HttpClient(h),
        });

        [Fact]
        public async Task PartnerPlatformRoutes()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[" + Platform + "],\"has_more\":false,\"presets\":[{\"id\":\"x\"}]}");
            using var c = Client(h);
            var list = await c.PartnerPlatforms.ListAsync();
            Assert.Equal("/v1/partner_platforms", h.Requests[0].Path);
            Assert.Equal("acme", list.Data[0].Partner);
            Assert.Single(list.Presets);

            h = MockHttpMessageHandler.Json(Platform);
            using var c2 = Client(h);
            var p = await c2.PartnerPlatforms.GetAsync("acme");
            Assert.Equal("registered", p.Registration.Status);
            Assert.True(p.TokenSet);
            await c2.PartnerPlatforms.SetAsync("acme", new SetPartnerPlatformBody { JwksUrl = "https://p.example/jwks", RecordConsents = true }.Set("name", null));
            var put = h.Requests.Last();
            Assert.Equal("PUT", put.Method);
            Assert.Equal("/v1/partner_platforms/acme", put.Path);
            Assert.Contains("\"jwks_url\":\"https://p.example/jwks\"", put.Body);
            Assert.Contains("\"record_consents\":true", put.Body);
            Assert.Contains("\"name\":null", put.Body);

            h = MockHttpMessageHandler.Json("{\"object\":\"partner_platform\",\"partner\":\"acme\",\"deleted\":true}");
            using var c3 = Client(h);
            Assert.True((await c3.PartnerPlatforms.RemoveAsync("acme")).Deleted);
            Assert.Equal("DELETE", h.Requests[0].Method);

            h = MockHttpMessageHandler.Json("{\"object\":\"partner_platform_registration\",\"partner\":\"acme\",\"platform_id\":\"plat_1\",\"receiver_url\":\"https://r\",\"receiver_url_is_template\":false,"
                + "\"external_id\":null,\"events\":[\"consent.recorded\"],\"signature\":{\"header\":\"h\",\"scheme\":\"s\",\"format\":\"f\",\"jwks_url\":\"j\",\"tolerance_seconds\":300},"
                + "\"send_to\":{\"contact\":\"c\",\"docs\":\"d\"},\"token_prefix\":null,\"next_step\":\"n\"}");
            using var c4 = Client(h);
            var reg = await c4.PartnerPlatforms.RegistrationAsync("acme");
            Assert.Equal("/v1/partner_platforms/acme/registration", h.Requests[0].Path);
            Assert.Equal(300, reg.Signature.ToleranceSeconds);
            Assert.Equal("c", reg.SendTo!.Contact);

            h = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[{\"object\":\"partner_delivery\",\"event_id\":\"e\",\"event_type\":\"t\",\"outcome\":\"recorded\",\"user_id\":\"u\",\"received_at\":9}],\"has_more\":false}");
            using var c5 = Client(h);
            var d = await c5.PartnerPlatforms.DeliveriesAsync("acme", 5);
            Assert.Equal("/v1/partner_platforms/acme/deliveries", h.Requests[0].Path);
            Assert.Contains("limit=5", h.Requests[0].Url.Query);
            Assert.Equal("recorded", d.Data[0].Outcome);
        }

        [Fact]
        public async Task TokenIsSentButNeverInAResponseModelOrToString()
        {
            const string secret = "pt_live_SUPERSECRETVALUE";
            var h = MockHttpMessageHandler.Json(Platform);
            using var c = Client(h);
            var body = new StorePartnerPlatformTokenBody { Token = secret, Rotate = true, ApiBaseUrl = "https://p.example" };
            var p = await c.PartnerPlatforms.StoreTokenAsync("acme", body);
            var r = h.Requests[0];
            Assert.Equal("POST", r.Method);
            Assert.Equal("/v1/partner_platforms/acme/token", r.Path);
            Assert.Contains(secret, r.Body);
            Assert.Contains("\"api_base_url\":\"https://p.example\"", r.Body);
            Assert.DoesNotContain(secret, body.ToString());
            Assert.DoesNotContain(secret, p.ToString());
            Assert.DoesNotContain(secret, JsonSerializer.Serialize(p));
            Assert.Equal("pt_ab", p.TokenPrefix);
        }

        [Fact]
        public async Task PerCallAudienceOnTheIntegrationAndTheMint()
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var c = Client(h);
            await c.Instance.SetIntegrationAsync("acme", new SetIntegrationBody
            {
                PerCallAudience = new PerCallAudience { Param = "site", Template = "acme:{site}", Allowed = new List<string> { "s1" }, AnyMatching = false },
            });
            Assert.Contains("\"per_call_audience\":{", h.Requests.Last().Body);
            Assert.Contains("\"template\":\"acme:{site}\"", h.Requests.Last().Body);
            Assert.Contains("\"any_matching\":false", h.Requests.Last().Body);
            await c.Instance.SetIntegrationAsync("acme", new SetIntegrationBody().Set("per_call_audience", null));
            Assert.Contains("\"per_call_audience\":null", h.Requests.Last().Body);

            await c.Sessions.GetIntegrationTokenAsync("sess_1", "acme", null, new Dictionary<string, string> { ["site"] = "s1" });
            Assert.Equal("/v1/sessions/sess_1/integration_tokens/acme", h.Requests.Last().Path);
            Assert.Equal("?site=s1", h.Requests.Last().Url.Query);
            await c.Sessions.GetIntegrationTokenAsync("sess_1", "acme", "acme:s2", null);
            Assert.Equal("?audience=acme%3As2", h.Requests.Last().Url.Query);
            await c.Sessions.GetIntegrationTokenAsync("sess_1", "acme");
            Assert.Equal("", h.Requests.Last().Url.Query);
        }

        [Fact]
        public async Task OrchestratorFieldsAndIncludeDeleted()
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var c = Client(h);
            await c.Privacy.Orchestrator.SetAsync(new SetPrivacyOrchestratorBody { SiteId = "s1", PartnerLinkId = "pl_1", CancelDeletionOnSignIn = true });
            var body = h.Requests.Last().Body;
            Assert.Contains("\"site_id\":\"s1\"", body);
            Assert.Contains("\"partner_link_id\":\"pl_1\"", body);
            Assert.Contains("\"cancel_deletion_on_sign_in\":true", body);

            await c.Users.ListAsync(new ListUsersParams { IncludeDeleted = true });
            Assert.Contains("include_deleted=true", h.Requests.Last().Url.Query);
        }

        [Fact]
        public void OperatorAudiencesAndErrorCode()
        {
            var cfg = JsonSerializer.Deserialize<OperatorConfiguration>("{\"issuer\":\"i\",\"audiences\":[\"a\"],\"access_token_claims\":[\"sub\"]}", AtlasJson.Options)!;
            Assert.Equal("a", cfg.Audiences![0]);
            Assert.Equal("sub", cfg.AccessTokenClaims![0]);
            Assert.Equal("MANAGED_WAF_NOT_CONFIGURED", ErrorCodes.ManagedWafNotConfigured);
        }
    }
}
