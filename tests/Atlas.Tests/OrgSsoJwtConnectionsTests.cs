using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>The 2026-11-01 surface (1.21.0): organization provisioning and SSO setup, trusted-JWT connections, path-mount domains, webhook secrets, hash imports.</summary>
    public class OrgSsoJwtConnectionsTests
    {
        private static (AtlasClient, MockHttpMessageHandler) Client(string json = "{}")
        {
            var handler = MockHttpMessageHandler.Json(json);
            var client = new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                PlatformKey = "psk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });
            return (client, handler);
        }

        private static JsonElement Sent(MockHttpMessageHandler h) => JsonDocument.Parse(h.Requests[h.Requests.Count - 1].Body).RootElement;

        [Fact]
        public async Task Organizations_CreateWithoutCreator_SendsTheNewFields_AndParsesTheOwnerInvitation()
        {
            var (c, h) = Client("{\"object\":\"organization\",\"id\":\"org_1\",\"name\":\"Acme\",\"slug\":\"acme\",\"created_by\":null,\"external_id\":\"cust_1\",\"role_mappings\":{\"org:admin\":\"Owner\"},\"owner_invitation\":{\"object\":\"organization_invitation\",\"id\":\"orginv_1\",\"email\":\"owner@example.com\",\"role\":\"org:admin\",\"status\":\"pending\",\"expires_at\":null}}");
            var org = await c.Organizations.CreateAsync(new CreateOrganizationBody
            {
                Name = "Acme",
                Slug = "acme",
                ExternalId = "cust_1",
                OwnerEmail = "owner@example.com",
                RoleMappings = new Dictionary<string, string> { ["org:admin"] = "Owner" },
            });
            Assert.Equal("POST", h.Requests[0].Method);
            Assert.Equal("/v1/organizations", h.Requests[0].Path);
            var body = Sent(h);
            Assert.False(body.TryGetProperty("created_by", out _));
            Assert.Equal("cust_1", body.GetProperty("external_id").GetString());
            Assert.Equal("owner@example.com", body.GetProperty("owner_email").GetString());
            Assert.Equal("Owner", body.GetProperty("role_mappings").GetProperty("org:admin").GetString());
            Assert.Null(org.CreatedBy);
            Assert.Equal("cust_1", org.ExternalId);
            Assert.Equal("Owner", org.RoleMappings!["org:admin"]);
            Assert.Equal("orginv_1", org.OwnerInvitation!.Id);
            Assert.Null(org.OwnerInvitation!.ExpiresAt);
        }

        [Fact]
        public async Task Organizations_UpdateClearsExternalId()
        {
            var (c, h) = Client();
            await c.Organizations.UpdateAsync("org_1", new UpdateOrganizationBody().Clear("external_id"));
            Assert.Equal("PATCH", h.Requests[0].Method);
            Assert.Equal(JsonValueKind.Null, Sent(h).GetProperty("external_id").ValueKind);
        }

        [Fact]
        public async Task Organizations_ListFilters_ByDomain_Policy_AndSsoBlock()
        {
            var (c, h) = Client("{\"object\":\"organization\",\"id\":\"org_1\",\"sso\":{\"connections\":[{\"id\":\"sso_1\",\"type\":\"saml\",\"status\":\"active\"}],\"scim_base_url\":\"https://api.example.com/scim/v2\"},\"domain\":{\"object\":\"org_domain\",\"id\":\"orgdom_1\",\"domain\":\"example.com\",\"status\":\"verified\",\"verified_at\":1}}");
            await c.Organizations.ListAsync(new ListOrganizationsParams { ExternalId = "cust_1", Metadata = new Dictionary<string, object?> { ["plan"] = "pro" } });
            Assert.Equal("/v1/organizations", h.Requests[0].Path);
            Assert.Equal("?external_id=cust_1&metadata%5Bplan%5D=pro", h.Requests[0].Query);

            var byDomain = await c.Organizations.GetByDomainAsync("example.com", includeUnverified: true);
            Assert.Equal("GET", h.Requests[1].Method);
            Assert.Equal("/v1/organizations/by_domain/example.com", h.Requests[1].Path);
            Assert.Equal("?include_unverified=true", h.Requests[1].Query);
            Assert.Equal("verified", byDomain.Domain!.Status);

            await c.Organizations.GetByDomainAsync("user@example.com");
            Assert.Equal("", h.Requests[2].Query);

            await c.Organizations.GetPolicyAsync("org_1");
            Assert.Equal("GET", h.Requests[3].Method);
            Assert.Equal("/v1/organizations/org_1/policy", h.Requests[3].Path);

            var org = await c.Organizations.GetAsync("org_1");
            Assert.Equal("active", org.Sso!.Connections[0].Status);
            Assert.Equal("https://api.example.com/scim/v2", org.Sso!.ScimBaseUrl);
        }

        [Fact]
        public async Task OrgDomains_VerifyByMethod_AndVerificationEmail()
        {
            var (c, h) = Client("{\"object\":\"org_domain\",\"id\":\"orgdom_1\",\"status\":\"pending\",\"verification_method\":null,\"sent_to\":\"admin@example.com\",\"verification_methods\":{\"dns_txt\":{\"record_name\":\"_atlas.example.com\",\"record_type\":\"TXT\",\"record_value\":\"v\"},\"well_known\":{\"url\":\"https://example.com/.well-known/atlas\",\"body\":\"b\",\"content_type\":\"text/plain\"},\"email\":{\"addresses\":[\"admin@example.com\"],\"sent_to\":null,\"expires_at\":null}}}");
            await c.Organizations.Domains.VerifyAsync("org_1", "orgdom_1");
            Assert.Equal("", h.Requests[0].Body);

            await c.Organizations.Domains.VerifyAsync("org_1", "orgdom_1", "email", "123456");
            Assert.Equal("/v1/organizations/org_1/domains/orgdom_1/verify", h.Requests[1].Path);
            var body = Sent(h);
            Assert.Equal("email", body.GetProperty("method").GetString());
            Assert.Equal("123456", body.GetProperty("code").GetString());

            var d = await c.Organizations.Domains.SendVerificationEmailAsync("org_1", "orgdom_1", "postmaster");
            Assert.Equal("POST", h.Requests[2].Method);
            Assert.Equal("/v1/organizations/org_1/domains/orgdom_1/verification_email", h.Requests[2].Path);
            Assert.Equal("postmaster", Sent(h).GetProperty("address").GetString());
            Assert.Equal("admin@example.com", d.SentTo);
            Assert.Equal("text/plain", d.VerificationMethods!.WellKnown!.ContentType);
            Assert.Equal("admin@example.com", d.VerificationMethods!.Email!.Addresses[0]);
        }

        [Fact]
        public async Task JwtConnections_Crud()
        {
            var (c, h) = Client("{\"object\":\"jwt_connection\",\"id\":\"jwtc_1\",\"organization_id\":null,\"status\":\"draft\",\"jwks_uri\":\"https://example.com/jwks.json\",\"issuer\":null,\"audience\":null,\"email_claim\":\"email\",\"max_age_seconds\":300,\"default_role_id\":null,\"login_url\":\"https://acme.example.com/v1/jwt_connections/jwtc_1/login\",\"created_at\":1,\"updated_at\":2}");
            var created = await c.JwtConnections.CreateAsync(new JwtConnectionBody { JwksUri = "https://example.com/jwks.json", MaxAgeSeconds = 300 }, "idem_1");
            Assert.Equal("POST", h.Requests[0].Method);
            Assert.Equal("/v1/jwt_connections", h.Requests[0].Path);
            Assert.Equal("idem_1", h.Requests[0].IdempotencyKey);
            Assert.Equal(300, Sent(h).GetProperty("max_age_seconds").GetInt32());
            Assert.Equal("https://acme.example.com/v1/jwt_connections/jwtc_1/login", created.LoginUrl);
            Assert.Null(created.OrganizationId);

            await c.JwtConnections.ListAsync();
            await c.JwtConnections.GetAsync("jwtc_1");
            await c.JwtConnections.UpdateAsync("jwtc_1", new JwtConnectionBody { Status = "active" });
            await c.JwtConnections.DeleteAsync("jwtc_1");
            Assert.Equal("GET /v1/jwt_connections", h.Requests[1].Method + " " + h.Requests[1].Path);
            Assert.Equal("GET /v1/jwt_connections/jwtc_1", h.Requests[2].Method + " " + h.Requests[2].Path);
            Assert.Equal("PATCH /v1/jwt_connections/jwtc_1", h.Requests[3].Method + " " + h.Requests[3].Path);
            Assert.Equal("active", JsonDocument.Parse(h.Requests[3].Body).RootElement.GetProperty("status").GetString());
            Assert.Equal("DELETE /v1/jwt_connections/jwtc_1", h.Requests[4].Method + " " + h.Requests[4].Path);
        }

        [Fact]
        public async Task SsoOnboarding_ReturnUrl_NameFilter_AndScimTokenWarnings()
        {
            var (c, h) = Client("{\"object\":\"sso_onboarding_ticket\",\"id\":\"ssot_1\",\"return_url\":null,\"effective_return_url\":\"https://app.example.com/done\",\"secret\":\"s\",\"scim_base_url\":\"https://api.example.com/scim/v2\",\"warnings\":[{\"code\":\"no_sso_connection\",\"message\":\"m\"}]}");
            await c.SsoOnboarding.ListAsync("Acme setup");
            Assert.Equal("?name=Acme%20setup", h.Requests[0].Query);

            var t = await c.SsoOnboarding.CreateTicketAsync("ssop_1", new CreateSsoOnboardingTicketBody { ReturnUrl = "https://app.example.com/done", ExpiresInSeconds = 600 });
            var body = Sent(h);
            Assert.Equal("https://app.example.com/done", body.GetProperty("return_url").GetString());
            Assert.Equal(600, body.GetProperty("expires_in_seconds").GetInt32());
            Assert.Equal("https://app.example.com/done", t.EffectiveReturnUrl);

            var tok = await c.ScimTokens.CreateAsync(new CreateScimTokenBody { OrganizationId = "org_1" });
            Assert.Equal("https://api.example.com/scim/v2", tok.ScimBaseUrl);
            Assert.Equal("no_sso_connection", tok.Warnings![0].Code);
        }

        [Fact]
        public async Task Domains_PathMount_SendsMethodAndParsesVerification()
        {
            var (c, h) = Client("{\"object\":\"custom_domain\",\"id\":\"dom_1\",\"method\":\"path\",\"cname_target\":null,\"base_path\":\"/__atlas\",\"frontend_api_url\":\"https://app.example.com/__atlas\",\"verification\":{\"method\":\"https_file\",\"url\":\"https://app.example.com/.well-known/atlas-verify/t\",\"token\":\"t\",\"content\":\"t\",\"instructions\":\"i\"}}");
            var d = await c.Domains.CreateAsync(new CreateDomainBody { Method = "path", Hostname = "app.example.com" });
            var body = Sent(h);
            Assert.Equal("path", body.GetProperty("method").GetString());
            Assert.Equal("app.example.com", body.GetProperty("hostname").GetString());
            Assert.False(body.TryGetProperty("host", out _));
            Assert.Null(d.CnameTarget);
            Assert.Equal("/__atlas", d.BasePath);
            Assert.Equal("https_file", d.Verification!.Method);
        }

        [Fact]
        public async Task Webhooks_ClientChosenSecret_OnCreateAndRotate()
        {
            var (c, h) = Client("{\"object\":\"webhook_endpoint\",\"id\":\"we_1\"}");
            var secret = "whsec_" + new string('a', 32);
            await c.Webhooks.Endpoints.CreateAsync(new CreateWebhookEndpointBody { Url = "https://example.com/hook", Active = false, Secret = secret });
            var body = Sent(h);
            Assert.False(body.GetProperty("active").GetBoolean());
            Assert.Equal(secret, body.GetProperty("secret").GetString());

            await c.Webhooks.Endpoints.RotateSecretAsync("we_1", new RotateWebhookSecretBody { Secret = secret, OverlapSeconds = 60 });
            Assert.Equal("/v1/webhook_endpoints/we_1/rotate_secret", h.Requests[1].Path);
            body = Sent(h);
            Assert.Equal(secret, body.GetProperty("secret").GetString());
            Assert.Equal(60, body.GetProperty("overlap_seconds").GetInt32());
        }

        [Fact]
        public async Task Users_CreateWithNamedHash()
        {
            var (c, h) = Client("{\"object\":\"user\",\"id\":\"user_1\"}");
            await c.Users.CreateAsync(new CreateUserBody
            {
                EmailAddress = "a@example.com",
                PasswordHash = "abc",
                PasswordHashAlgorithm = "sha256",
                PasswordSalt = "s",
                PasswordHashParams = new Dictionary<string, object?> { ["salt_encoding"] = "hex" },
            });
            var body = Sent(h);
            Assert.Equal("sha256", body.GetProperty("password_hash_algorithm").GetString());
            Assert.Equal("s", body.GetProperty("password_salt").GetString());
            Assert.Equal("hex", body.GetProperty("password_hash_params").GetProperty("salt_encoding").GetString());
        }

        [Fact]
        public async Task Platform_RevealInitialSecret_DryRun()
        {
            var (c, h) = Client("{\"object\":\"initial_secret\",\"app_id\":\"app_1\",\"secret\":null,\"available\":true,\"expires_at\":5}");
            var r = await c.Platform.Apps.RevealInitialSecretAsync("app_1", dryRun: true);
            Assert.Equal("POST", h.Requests[0].Method);
            Assert.Equal("/v1/platform/apps/app_1/initial_secret/reveal", h.Requests[0].Path);
            Assert.Equal("?dry_run=true", h.Requests[0].Query);
            Assert.True(r.Available);
            Assert.Null(r.Secret);
        }

        [Fact]
        public async Task InstanceSettings_ReferralsCookieConsentAndAccountPortal()
        {
            var (c, h) = Client();
            await c.Referrals.UpdateAsync(new UpdateReferralSettingsBody { PartnerPortalUrl = "https://partners.example.com", ReportLinkedAtlasIdSignups = true });
            var body = Sent(h);
            Assert.Equal("https://partners.example.com", body.GetProperty("partner_portal_url").GetString());
            Assert.True(body.GetProperty("report_linked_atlas_id_signups").GetBoolean());

            await c.Instance.ReplaceCookieConsentAsync(new CookieConsentBody { HostAliases = new List<string> { "acme.example.com" } });
            Assert.Equal("acme.example.com", Sent(h).GetProperty("host_aliases")[0].GetString());

            await c.Branding.UpdateAsync(new Branding { AccountPortal = new BrandingAccountPortal { Panels = new Dictionary<string, bool?> { ["delete_account"] = false } } });
            Assert.False(Sent(h).GetProperty("accountPortal").GetProperty("panels").GetProperty("delete_account").GetBoolean());
        }
    }
}
