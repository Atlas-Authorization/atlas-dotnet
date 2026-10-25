using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Sign in with Atlas test and review accounts, account domains, app test-mode fields (1.19.0).</summary>
    public class R4ProvisionedAccountsTests
    {
        private const string Account = "{\"object\":\"atlas_id_provisioned_account\",\"id\":\"pa_1\",\"sub\":\"s_1\",\"email\":\"qa@example.com\",\"email_verified\":true,"
            + "\"label\":\"reviewer\",\"kind\":\"provisioned\",\"provisioned_by\":\"app\",\"app_ids\":[\"app_1\"],\"apps\":[{\"id\":\"app_1\",\"client_id\":\"c\",\"name\":\"A\"}],"
            + "\"any_app\":false,\"two_factor_exempt\":false,\"expires_at\":null,\"expired\":false,\"developer_account_id\":null,\"created_by\":{\"type\":\"platform\",\"id\":\"p\"},"
            + "\"last_sign_in_at\":null,\"password_rotated_at\":null,\"created_at\":1,\"updated_at\":2";

        private static AtlasClient Client(MockHttpMessageHandler h) => new AtlasClient(new AtlasClientOptions
        {
            PlatformKey = "psk_test_123",
            ApiUrl = "https://api.test.example",
            HttpClient = new HttpClient(h),
        });

        [Fact]
        public async Task ProvisionedAccountRoutes_AndPasswordIsNotPrinted()
        {
            var h = MockHttpMessageHandler.Json(Account + ",\"password\":\"hunter2hunter2\"}");
            using var c = Client(h);
            var created = await c.Platform.AtlasIdApps.ProvisionedAccounts.CreateAsync("app_1",
                new CreateAtlasIdProvisionedAccountBody { Email = "qa@example.com", Label = "reviewer", Password = "hunter2hunter2", TwoFactorExempt = true });
            var req = h.Requests[0];
            Assert.Equal("POST", req.Method);
            Assert.Equal("/v1/platform/atlas_id_apps/app_1/provisioned_accounts", req.Path);
            Assert.Contains("\"two_factor_exempt\":true", req.Body);
            Assert.Equal("hunter2hunter2", created.Password);
            Assert.DoesNotContain("hunter2", created.ToString());
            Assert.DoesNotContain("hunter2", new CreateAtlasIdProvisionedAccountBody { Email = "a@b.c", Label = "l", Password = "hunter2hunter2" }.ToString());

            h = MockHttpMessageHandler.Json(Account + ",\"sealed_password\":\"sealed-blob\"}");
            using var c2 = Client(h);
            var rot = await c2.Platform.AtlasIdApps.ProvisionedAccounts.RotatePasswordAsync("app_1", "pa_1");
            Assert.Equal("/v1/platform/atlas_id_apps/app_1/provisioned_accounts/pa_1/password", h.Requests[0].Path);
            Assert.Equal("sealed-blob", rot.SealedPassword);
            Assert.Null(rot.Password);
            Assert.DoesNotContain("sealed-blob", rot.ToString());
            await c2.Platform.AtlasIdApps.ProvisionedAccounts.RotatePasswordAsync("app_1", "pa_1", new RotateAtlasIdProvisionedPasswordBody { Password = "another-long-pass" });
            Assert.Contains("another-long-pass", h.Requests.Last().Body);

            h = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[" + Account + "}],\"limit\":25,\"total\":1}");
            using var c3 = Client(h);
            var list = await c3.Platform.AtlasIdApps.ProvisionedAccounts.ListAsync("app_1");
            Assert.Equal(25, list.Limit);
            Assert.Equal("qa@example.com", list.Data[0].Email);
            Assert.Equal("app_1", list.Data[0].Apps[0].Id);

            h = MockHttpMessageHandler.Json("{\"object\":\"atlas_id_provisioned_account\",\"id\":\"pa_1\",\"deleted\":true,\"sessions_ended\":2}");
            using var c4 = Client(h);
            var gone = await c4.Platform.AtlasIdApps.ProvisionedAccounts.RemoveAsync("app_1", "pa_1");
            Assert.Equal("DELETE", h.Requests[0].Method);
            Assert.Equal(2, gone.SessionsEnded);
            await c4.Platform.AtlasIdApps.ProvisionedAccounts.GetAsync("app_1", "pa_1");
            Assert.Equal("GET", h.Requests.Last().Method);
        }

        [Fact]
        public async Task DomainRoutes()
        {
            var dom = "{\"object\":\"account_domain\",\"id\":\"dom_1\",\"domain\":\"example.com\",\"verified\":false,\"verified_at\":null,\"last_checked_at\":null,"
                + "\"verification\":{\"type\":\"TXT\",\"name\":\"_atlas.example.com\",\"value\":\"v\"}}";
            var h = MockHttpMessageHandler.Json(dom);
            using var c = Client(h);
            var d = await c.Platform.Domains.ClaimAsync("example.com");
            Assert.Equal("POST", h.Requests[0].Method);
            Assert.Equal("/v1/platform/domains", h.Requests[0].Path);
            Assert.Contains("\"domain\":\"example.com\"", h.Requests[0].Body);
            Assert.Equal("TXT", d.Verification!.Type);
            await c.Platform.Domains.CheckAsync("dom_1");
            Assert.Equal("/v1/platform/domains/dom_1/verify", h.Requests.Last().Path);
            await c.Platform.Domains.VerifyAsync("dom_1");
            Assert.Equal("/v1/platform/domains/dom_1/verify", h.Requests.Last().Path);
            await c.Platform.Domains.RemoveAsync("dom_1");
            Assert.Equal("DELETE", h.Requests.Last().Method);

            h = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[" + dom + "],\"verified_domains\":[\"example.com\"]}");
            using var c2 = Client(h);
            var list = await c2.Platform.Domains.ListAsync();
            Assert.Equal("example.com", list.VerifiedDomains[0]);
        }

        [Fact]
        public async Task AppTestModeFields_AndErrorCodes()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"atlas_id_app\",\"id\":\"app_1\",\"test_mode\":true,\"provisioned_verified_domains_only\":true}");
            using var c = Client(h);
            var app = await c.Platform.AtlasIdApps.UpdateAsync("app_1", new UpdateAtlasIdAppBody { TestMode = true, ProvisionedVerifiedDomainsOnly = true });
            Assert.Contains("\"test_mode\":true", h.Requests[0].Body);
            Assert.Contains("\"provisioned_verified_domains_only\":true", h.Requests[0].Body);
            Assert.True(app.TestMode);
            Assert.True(app.ProvisionedVerifiedDomainsOnly);
            Assert.Equal("TEST_ACCOUNT_RESTRICTED", ErrorCodes.TestAccountRestricted);
            Assert.Equal("PROVISIONED_ACCOUNT_LIMIT_REACHED", ErrorCodes.ProvisionedAccountLimitReached);
            Assert.Equal("DOMAIN_NOT_VERIFIED", ErrorCodes.DomainNotVerified);
        }
    }
}
