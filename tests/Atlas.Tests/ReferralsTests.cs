using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Referral attribution: settings, connection test, call log, user referral fields and the fraud flag.</summary>
    public class ReferralsTests
    {
        private const string Settings = "{\"object\":\"referral_settings\",\"enabled\":true,\"provider\":\"webhook\","
            + "\"endpoint_url\":\"https://referrals.example.com/atlas\",\"program_id\":null,\"connection_id\":null,"
            + "\"signing_secret_set\":true,\"api_key_set\":false,\"url_params\":[\"ref\",\"via\",\"aff\"],\"context_cookie\":null,"
            + "\"consent_category\":\"referrals\",\"consent_fallback_category\":\"marketing\",\"auto_enroll_users\":true,"
            + "\"link_template\":\"https://app.example.com/join?ref={code}\",\"timeout_ms\":3000,"
            + "\"security\":{\"challenge_referred_signups\":\"always\",\"on_high_risk\":\"flag\",\"hold_until_email_verified\":false,\"max_referred_signups_per_ip_hour\":5},"
            + "\"missing\":[],\"connection\":{\"last_test\":{\"ok\":true,\"at\":1,\"http_status\":404,\"error\":null},\"failed_calls_7d\":2,\"retrying_calls\":1,\"last_success_at\":1},"
            + "\"providers\":[{\"id\":\"webhook\",\"name\":\"Atlas referral protocol (any platform)\",\"capabilities\":{\"signup\":true,\"erase\":true,\"export\":true,\"partner\":true,\"auto_enroll\":true,\"signal\":true},"
            + "\"needs\":[\"endpoint_url\",\"signing_secret\"],\"default_context_cookie\":null,\"docs_url\":null}],\"updated_at\":1}";

        private const string Call = "{\"object\":\"referral_call\",\"id\":\"rfc_1\",\"kind\":\"signup\",\"status\":\"succeeded\",\"outcome\":\"referred\","
            + "\"customer_ref\":\"user_1\",\"user_id\":\"user_1\",\"http_status\":200,\"last_error\":null,\"attempts\":2,\"next_attempt_at\":null,"
            + "\"risk\":{\"level\":\"low\",\"score\":5,\"signals\":[],\"captcha\":\"passed\",\"emailVerified\":true},\"payload\":{\"customerRef\":\"user_1\"},"
            + "\"created_at\":1,\"updated_at\":2}";

        private static AtlasClient Client(MockHttpMessageHandler handler) =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

        [Fact]
        public async Task Settings_GetUpdateAndTest()
        {
            var handler = MockHttpMessageHandler.Json(Settings);
            using var client = Client(handler);

            var view = await client.Referrals.GetAsync();
            Assert.Equal("GET", handler.Requests[0].Method);
            Assert.Equal("/v1/instance/referrals", handler.Requests[0].Path);
            Assert.Equal("flag", view.Security.OnHighRisk);
            Assert.Equal(5, view.Security.MaxReferredSignupsPerIpHour);
            Assert.True(view.Providers[0].Capabilities.Signal);
            Assert.Equal(2, view.Connection!.FailedCalls7d);
            Assert.Equal(404, view.Connection.LastTest!.HttpStatus);

            await client.Referrals.UpdateAsync(new UpdateReferralSettingsBody
            {
                Enabled = true,
                SigningSecret = new string('x', 32),
                Security = new UpdateReferralSecurityBody { OnHighRisk = "hold" },
            }.Clear("link_template"), idempotencyKey: "idem_1");
            Assert.Equal("PATCH", handler.Requests[1].Method);
            using (var doc = JsonDocument.Parse(handler.Requests[1].Body))
            {
                var root = doc.RootElement;
                Assert.True(root.GetProperty("enabled").GetBoolean());
                Assert.Equal("hold", root.GetProperty("security").GetProperty("on_high_risk").GetString());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("link_template").ValueKind);
                Assert.False(root.TryGetProperty("endpoint_url", out _));
            }
            Assert.Equal("idem_1", handler.Requests[1].IdempotencyKey);
        }

        [Fact]
        public async Task ConnectionTest_Decodes()
        {
            var handler = MockHttpMessageHandler.Json("{\"ok\":false,\"http_status\":401,\"latency_ms\":12,\"error\":{\"code\":\"signature_rejected\",\"message\":\"m\"}}");
            using var client = Client(handler);
            var test = await client.Referrals.TestAsync();
            Assert.Equal("POST", handler.Requests[0].Method);
            Assert.Equal("/v1/instance/referrals/test", handler.Requests[0].Path);
            Assert.False(test.Ok);
            Assert.Equal("signature_rejected", test.Error!.Code);
        }

        [Fact]
        public async Task CallLog_AndRetry()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[" + Call + "],\"has_more\":false}");
            using var client = Client(handler);
            var page = await client.Referrals.ListCallsAsync(limit: 10, status: "failed", kind: "signup", userId: "user_1");
            Assert.Equal("/v1/instance/referrals/calls", handler.Requests[0].Path);
            Assert.Equal("?limit=10&status=failed&kind=signup&user_id=user_1", handler.Requests[0].Query);
            Assert.True(page.Data[0].Risk!.EmailVerified);

            var retry = MockHttpMessageHandler.Json(Call);
            using var client2 = Client(retry);
            var call = await client2.Referrals.RetryCallAsync("rfc_1", "idem_2");
            Assert.Equal("POST", retry.Requests[0].Method);
            Assert.Equal("/v1/instance/referrals/calls/rfc_1/retry", retry.Requests[0].Path);
            Assert.Equal("idem_2", retry.Requests[0].IdempotencyKey);
            Assert.Equal("referred", call.Outcome);
        }

        [Fact]
        public async Task UserReferralFields_AndFlag()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"user\",\"id\":\"user_1\",\"referral\":{\"partner_id\":\"pn_1\",\"referrer_external_id\":\"cust_9\","
                + "\"referrer_user_id\":\"user_9\",\"matched_via\":\"code\",\"status\":\"lead\",\"risk_level\":\"low\",\"flagged\":false},"
                + "\"referral_code\":\"ada-1\",\"referral_link\":\"https://app.example.com/join?ref=ada-1\"}");
            using var client = Client(handler);

            var created = await client.Users.CreateAsync(new CreateUserBody
            {
                EmailAddress = "a@example.com",
                Referral = new CreateUserReferral { Code = "ACME10", ClickId = "clk_123" },
                ReferralCode = "ada-1",
            });
            Assert.Equal("{\"email_address\":\"a@example.com\",\"referral\":{\"code\":\"ACME10\",\"click_id\":\"clk_123\"},\"referral_code\":\"ada-1\"}", handler.Requests[0].Body);
            Assert.Equal("user_9", created.Referral!.ReferrerUserId);
            Assert.Equal("ada-1", created.ReferralCode);

            await client.Users.UpdateAsync("user_1", new UpdateUserBody().ClearReferralCode());
            Assert.Equal("{\"referral_code\":null}", handler.Requests[1].Body);
            await client.Users.UpdateAsync("user_1", new UpdateUserBody { ReferralCode = "ada-2" });
            Assert.Equal("{\"referral_code\":\"ada-2\"}", handler.Requests[2].Body);

            var flagHandler = MockHttpMessageHandler.Json("{\"object\":\"user_referral\",\"user_id\":\"user_1\",\"status\":\"flagged\",\"flagged\":true,\"flag_reason\":\"ring\",\"flagged_at\":5,\"risk_level\":\"high\"}");
            using var flagClient = Client(flagHandler);
            var flag = await flagClient.Users.FlagReferralAsync("user_1", "ring", "idem_3");
            Assert.Equal("/v1/users/user_1/referral/flag", flagHandler.Requests[0].Path);
            Assert.Equal("{\"reason\":\"ring\"}", flagHandler.Requests[0].Body);
            Assert.Equal("idem_3", flagHandler.Requests[0].IdempotencyKey);
            Assert.True(flag.Flagged);
            Assert.Equal(5L, flag.FlaggedAt);
            await flagClient.Users.FlagReferralAsync("user_1");
            Assert.Equal("{}", flagHandler.Requests[1].Body);
        }

        [Fact]
        public async Task ReferralCodeTaken_Surfaces()
        {
            var handler = MockHttpMessageHandler.Json("{\"errors\":[{\"code\":\"REFERRAL_CODE_TAKEN\",\"message\":\"taken\",\"param\":\"referral_code\"}]}", HttpStatusCode.Conflict);
            using var client = Client(handler);
            var e = await Assert.ThrowsAnyAsync<AtlasException>(() => client.Users.UpdateAsync("user_1", new UpdateUserBody { ReferralCode = "ada-1" }));
            Assert.Equal(409, e.Status);
            Assert.Equal(ErrorCodes.ReferralCodeTaken, e.Code);
            Assert.Equal("REFERRALS_NOT_CONFIGURED", ErrorCodes.ReferralsNotConfigured);
        }
    }
}
