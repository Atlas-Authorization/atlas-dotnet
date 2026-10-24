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
using Atlas.Resources;
using Atlas.Verification;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>The integrator-platform surface (1.16.0): credentials, privacy, integrations, status, compliance, webhooks v2, provisioning.</summary>
    public class SuiteSpecTests
    {
        private static AtlasClient Client(MockHttpMessageHandler handler, Action<AtlasClientOptions>? tweak = null)
        {
            var o = new AtlasClientOptions
            {
                SecretKey = "atlas_test_sk_abcdef",
                PlatformKey = "atlas_live_psk_abcdef",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            };
            tweak?.Invoke(o);
            return new AtlasClient(o);
        }

        private static JsonElement Sent(RecordedRequest r) => JsonDocument.Parse(r.Body).RootElement;

        // ---- wire surface -------------------------------------------------------------

        private sealed record Case(string Name, Func<AtlasClient, Task> Call, string Method, string Path, string? Query = null, string? BodyContains = null);

        private static IEnumerable<Case> Cases()
        {
            yield return new Case("secret_keys.create", c => c.SecretKeys.CreateAsync(new CreateSecretKeyBody { Name = "ci", Scopes = new List<string> { "read_only" }, AllowedOrigins = new List<string> { "https://*.example.com" }, ExternalRef = "ref1" }), "POST", "/v1/secret_keys", null, "\"allowed_origins\":[\"https://*.example.com\"]");
            yield return new Case("secret_keys.list", c => c.SecretKeys.ListAsync(), "GET", "/v1/secret_keys");
            yield return new Case("secret_keys.list.meta", c => c.SecretKeys.ListAsync(new Dictionary<string, object?> { ["env"] = "ci", ["n"] = 2 }), "GET", "/v1/secret_keys", "?metadata%5Benv%5D=ci&metadata%5Bn%5D=2");
            yield return new Case("secret_keys.get", c => c.SecretKeys.GetAsync("sk 1"), "GET", "/v1/secret_keys/sk%201");
            yield return new Case("secret_keys.update", c => c.SecretKeys.UpdateAsync("k1", new UpdateSecretKeyBody { Name = "x" }.Set("owner", null)), "PATCH", "/v1/secret_keys/k1", null, "\"owner\":null");
            yield return new Case("secret_keys.rotate", c => c.SecretKeys.RotateAsync("k1", new RotateSecretKeyBody { OverlapSeconds = 0 }), "POST", "/v1/secret_keys/k1/rotate", null, "\"overlap_seconds\":0");
            yield return new Case("secret_keys.revoke", c => c.SecretKeys.RevokeAsync("k1"), "POST", "/v1/secret_keys/k1/revoke");
            yield return new Case("keys.list", c => c.Keys.ListAsync(), "GET", "/v1/keys");
            yield return new Case("whoami", c => c.Whoami.GetAsync(), "GET", "/v1/whoami");
            yield return new Case("platform.whoami", c => c.Platform.WhoamiAsync(), "GET", "/v1/platform/whoami");
            yield return new Case("partner_links.create", c => c.PartnerLinks.CreateAsync(new CreatePartnerLinkBody { Partner = "chatly", Scopes = new List<string> { "users:read" }, CertFingerprints = new List<string> { "ab" } }), "POST", "/v1/partner_links", null, "\"cert_fingerprints\":[\"ab\"]");
            yield return new Case("partner_links.mtls", c => c.PartnerLinks.SetCertFingerprintsAsync("pl1", new List<string>()), "PATCH", "/v1/partner_links/pl1", null, "\"cert_fingerprints\":[]");
            yield return new Case("partner_links.rotate", c => c.PartnerLinks.RotateSecretAsync("pl1"), "POST", "/v1/partner_links/pl1/rotate_secret");
            yield return new Case("partner_links.revoke", c => c.PartnerLinks.RevokeAsync("pl1"), "DELETE", "/v1/partner_links/pl1");
            yield return new Case("partner_links.requests.list", c => c.PartnerLinks.Requests.ListAsync("outbound", "pending,delivery_failed"), "GET", "/v1/partner_links/requests", "?direction=outbound&status=pending%2Cdelivery_failed");
            yield return new Case("partner_links.requests.approve", c => c.PartnerLinks.Requests.ApproveAsync("plr1", new ApprovePartnerLinkRequestBody { Scopes = new List<string> { "users:read" } }), "POST", "/v1/partner_links/requests/plr1/approve", null, "\"scopes\":[\"users:read\"]");
            yield return new Case("partner_links.requests.deny", c => c.PartnerLinks.Requests.DenyAsync("plr1"), "POST", "/v1/partner_links/requests/plr1/deny");
            yield return new Case("partner_links.outbound.save", c => c.PartnerLinks.Outbound.SaveAsync("cookiemunch", new SaveOutboundPartnerLinkBody { ClientId = "c", ClientSecret = "s" }), "PUT", "/v1/partner_links/outbound/cookiemunch", null, "\"client_secret\":\"s\"");
            yield return new Case("partner_links.outbound.request", c => c.PartnerLinks.Outbound.RequestAsync("acme", new RequestOutboundPartnerLinkBody { Scopes = new List<string> { "a" }, BaseUrl = "https://acme.test" }), "POST", "/v1/partner_links/outbound/acme/request", null, "\"base_url\":\"https://acme.test\"");
            yield return new Case("atlas_id_apps.lookup", c => c.AtlasIdGrants.LookupAsync("app1", "a@b.co"), "GET", "/v1/atlas_id_apps/app1/users", "?email=a%40b.co");
            yield return new Case("atlas_id_apps.revoke", c => c.AtlasIdGrants.RevokeAsync("app1", "g1"), "DELETE", "/v1/atlas_id_apps/app1/grants/g1");
            yield return new Case("privacy.lookup", c => c.Privacy.LookupAsync(new PrivacyLookupBody { Emails = new List<string> { "a@b.co" }, Atlas = new PrivacySubject { Sub = "u1" } }), "POST", "/v1/privacy/lookup", null, "\"atlas\":{\"sub\":\"u1\"}");
            yield return new Case("privacy.create", c => c.Privacy.CreateRequestAsync(new CreatePrivacyRequestBody { SubjectId = "u1", Type = PrivacyTypes.Deletion, CaseId = "c1", ExecuteAfter = 1234L }, "idem-1"), "POST", "/v1/privacy/requests", null, "\"executeAfter\":1234");
            yield return new Case("privacy.cancel", c => c.Privacy.CancelRequestAsync("pr1"), "POST", "/v1/privacy/requests/pr1/cancel");
            yield return new Case("privacy.orchestrator.set", c => c.Privacy.Orchestrator.SetAsync(new SetPrivacyOrchestratorBody { Provider = "cookiemunch", SignRequests = true }.Set("api_key", null)), "PUT", "/v1/instance/privacy_orchestrator", null, "\"sign_requests\":true");
            yield return new Case("privacy.orchestrator.delete", c => c.Privacy.Orchestrator.DeleteAsync(), "DELETE", "/v1/instance/privacy_orchestrator");
            yield return new Case("integrations.list", c => c.Instance.ListIntegrationsAsync(), "GET", "/v1/instance/integrations");
            yield return new Case("integrations.set", c => c.Instance.SetIntegrationAsync("chatly", new SetIntegrationBody { Enabled = true, Audience = "chatly:ch1", Cmp = new IntegrationCmpBody { ScriptUrl = "https://cmp.test/x.js", SettingsPreset = "cookiemunch" } }.Set("events_url", null)), "PUT", "/v1/instance/integrations/chatly", null, "\"script_url\":\"https://cmp.test/x.js\"");
            yield return new Case("integrations.dry_run", c => c.WithDryRun().Instance.SetIntegrationAsync("custom1", new SetIntegrationBody { Name = "x" }), "PUT", "/v1/instance/integrations/custom1", "?dry_run=true");
            yield return new Case("integrations.deliveries", c => c.Instance.IntegrationDeliveriesAsync("chatly", 5, "abc", "event"), "GET", "/v1/instance/integrations/chatly/deliveries", "?limit=5&trace=abc&kind=event");
            yield return new Case("integrations.events_secret", c => c.Instance.RevealIntegrationEventsSecretAsync("chatly"), "POST", "/v1/instance/integrations/chatly/events_secret");
            yield return new Case("integrations.atlas_id_tokens", c => c.Instance.IntegrationAtlasIdTokenAsync("chatly", "at_x"), "POST", "/v1/instance/integrations/chatly/atlas_id_tokens", null, "\"access_token\":\"at_x\"");
            yield return new Case("sessions.integration_token", c => c.Sessions.GetIntegrationTokenAsync("sess_1", "chatly"), "POST", "/v1/sessions/sess_1/integration_tokens/chatly");
            yield return new Case("integrations.verify_errors", c => c.Integrations.ReportVerifyErrorAsync(new ReportVerifyErrorBody { Integration = "chatly", Error = IntegrationVerifyErrors.AudMismatch }), "POST", "/v1/integrations/verify_errors", null, "\"error\":\"aud_mismatch\"");
            yield return new Case("integrations.card", c => c.Integrations.CardAsync("chatly", "u1", "https://iss"), "GET", "/v1/integrations/chatly/card", "?sub=u1&iss=https%3A%2F%2Fiss");
            yield return new Case("integrations.revoke_sessions", c => c.Integrations.RevokeSessionsAsync("chatly", "u1", null, new IntegrationAgent { Email = "a@b.co" }), "POST", "/v1/integrations/chatly/card/actions/revoke_sessions", null, "\"agent\":{\"email\":\"a@b.co\"}");
            yield return new Case("public.instance", c => c.Integrations.InstanceMetadataAsync("ins_1"), "GET", "/v1/public/instances/ins_1");
            yield return new Case("status.service", c => c.Status.ServiceAsync(), "GET", "/v1/status");
            yield return new Case("status.subscription.put", c => c.Status.UpdateSubscriptionAsync(new UpdateStatusSubscriptionBody { Regions = new List<string>(), MinImpact = "major" }), "PUT", "/v1/instance/status_subscription", null, "\"min_impact\":\"major\"");
            yield return new Case("status.rotate_feed", c => c.Status.RotateFeedAsync(), "POST", "/v1/instance/status_subscription/rotate_feed");
            yield return new Case("status.incidents", c => c.Status.ListIncidentsAsync(true, 10), "GET", "/v1/instance/status_incidents", "?open=true&limit=10");
            yield return new Case("status.dependency.create", c => c.Status.CreateDependencyAsync(new CreateStatusDependencyBody { Name = "Stripe", Url = "https://status.stripe.com" }), "POST", "/v1/instance/status_dependencies", null, "\"url\":\"https://status.stripe.com\"");
            yield return new Case("status.dependency.refresh", c => c.Status.RefreshDependencyAsync("sd1"), "POST", "/v1/instance/status_dependencies/sd1/refresh");
            yield return new Case("notices.list", c => c.Notices.ListAsync(true), "GET", "/v1/instance/notices", "?all=true");
            yield return new Case("notices.ack", c => c.Notices.AcknowledgeAsync("n1"), "POST", "/v1/instance/notices/n1/acknowledge");
            yield return new Case("advisories.list", c => c.SecurityAdvisories.ListAsync(), "GET", "/v1/security_advisories");
            yield return new Case("advisories.catalog", c => c.SecurityAdvisories.CatalogAsync(), "GET", "/v1/public/security_advisories");
            yield return new Case("sub_processors", c => c.SubProcessors.GetAsync(), "GET", "/v1/instance/sub_processors");
            yield return new Case("audit_exports.create", c => c.AuditExports.CreateAsync(new CreateAuditExportBody { Name = "siem", Type = "webhook", Destination = new Dictionary<string, object?> { ["url"] = "https://siem.test" }, BackfillHours = 24 }), "POST", "/v1/audit_exports", null, "\"backfill_hours\":24");
            yield return new Case("audit_exports.clear_filter", c => c.AuditExports.UpdateAsync("ae1", new UpdateAuditExportBody().ClearActionFilter()), "PATCH", "/v1/audit_exports/ae1", null, "\"action_filter\":null");
            yield return new Case("audit_exports.test", c => c.AuditExports.TestAsync("ae1"), "POST", "/v1/audit_exports/ae1/test");
            yield return new Case("entitlements", c => c.Entitlements.GetAsync("org", "o1"), "GET", "/v1/entitlements", "?subject_type=org&subject_id=o1");
            yield return new Case("webhooks.get", c => c.Webhooks.Endpoints.GetAsync("we1"), "GET", "/v1/webhook_endpoints/we1");
            yield return new Case("webhooks.upsert", c => c.Webhooks.Endpoints.UpsertByUrlAsync(new UpsertWebhookEndpointBody { Url = "https://a.test/h", SignatureVersions = new List<string> { "v1", "v2" }, Destination = new WebhookDestination { Type = WebhookDestinationTypes.AwsEventBridge, Region = "us-east-1" }, DestinationCredentials = new Dictionary<string, object?> { ["access_key_id"] = "k" } }), "PUT", "/v1/webhook_endpoints/by_url", null, "\"signature_versions\":[\"v1\",\"v2\"]");
            yield return new Case("webhooks.test_now", c => c.Webhooks.Endpoints.TestNowAsync("we1", "5s"), "POST", "/v1/webhook_endpoints/we1/test", "?wait=5s");
            yield return new Case("webhooks.redeliver_window", c => c.Webhooks.Endpoints.RedeliverWindowAsync("we1", new RedeliverWebhookWindowBody { Since = "failing_since", Status = "failed" }), "POST", "/v1/webhook_endpoints/we1/redeliver", null, "\"since\":\"failing_since\"");
            yield return new Case("webhooks.replay", c => c.Webhooks.Endpoints.ReplayAsync("we1", new ReplayWebhookBody { Since = 1000L, EventType = "user.created" }), "POST", "/v1/webhook_endpoints/we1/replay", null, "\"event_type\":\"user.created\"");
            yield return new Case("webhooks.deliveries.params", c => c.Webhooks.DeliveriesAsync("we1", new ListWebhookDeliveriesParams { Limit = 500, Status = "failed", Since = 5, Trace = "abc", StartingAfter = "d1" }), "GET", "/v1/webhook_endpoints/we1/deliveries", "?limit=500&starting_after=d1&status=failed&since=5&trace=abc");
            yield return new Case("webhooks.list.deprecated", c => c.Webhooks.Endpoints.ListWithEventsAsync(true, new Dictionary<string, object?> { ["a"] = "b" }), "GET", "/v1/webhook_endpoints", "?metadata%5Ba%5D=b&include_deprecated=true");
            yield return new Case("delivery.egress", c => c.Delivery.EgressIpsAsync(), "GET", "/v1/meta/egress_ips");
            yield return new Case("delivery.health", c => c.Delivery.HealthAsync(DeliveryTargetKinds.Integration, "all"), "GET", "/v1/delivery_health", "?kind=integration&state=all");
            yield return new Case("delivery.alert_contacts", c => c.Delivery.SetAlertContactsAsync("webhook_endpoint", "we1", new[] { "ops@example.com" }), "PUT", "/v1/delivery_health/webhook_endpoint/we1/alert_contacts", null, "\"alert_contacts\":[\"ops@example.com\"]");
            yield return new Case("instance.preflight.params", c => c.Instance.PreflightAsync("warning", true), "GET", "/v1/instance/preflight", "?min_severity=warning&include_suppressed=true");
            yield return new Case("instance.unknown_fields", c => c.Instance.UnknownFieldsReportAsync(7), "GET", "/v1/instance/unknown_fields_report", "?days=7");
            yield return new Case("instance.openapi_diff", c => c.Instance.OpenApiDiffAsync("2026-10-01", "bapi"), "GET", "/v1/openapi/diff", "?since=2026-10-01&surface=bapi");
            yield return new Case("instance.integration_health", c => c.Instance.IntegrationHealthAsync(new[] { "webhooks", "email" }, false), "GET", "/v1/integration-health", "?only=webhooks%2Cemail&probe=false");
            yield return new Case("instance.integration_health.providers", c => c.Instance.IntegrationHealthProvidersAsync(), "GET", "/v1/integration-health/providers");
            yield return new Case("instance.embed", c => c.Instance.EmbedAsync("ins_1", "https://app.example.com", "button"), "GET", "/v1/embed", "?instance=ins_1&origin=https%3A%2F%2Fapp.example.com&shape=button");
            yield return new Case("messaging.channel", c => c.Messaging.ChannelAsync("email"), "GET", "/v1/messaging_providers/email");
            yield return new Case("design.import_brand_url", c => c.Design.ImportBrandUrlAsync("https://brand.example.com/tokens.json"), "PUT", "/v1/instance/design", null, "\"brand_url\"");
            yield return new Case("design.remove_brand", c => c.Design.RemoveBrandAsync(), "PUT", "/v1/instance/design", null, "\"brand\":null");
            yield return new Case("referrals.install", c => c.Referrals.InstallAsync("referalli", "https://app.example.com/done"), "POST", "/v1/instance/referrals/referalli/install", null, "\"return_to\"");
            yield return new Case("referrals.calls.trace", c => c.Referrals.ListCallsAsync(new ListReferralCallsQuery { Trace = "abc" }), "GET", "/v1/instance/referrals/calls", "?trace=abc");
            yield return new Case("users.updated_after", c => c.Users.ListAsync(new ListUsersParams { UpdatedAfter = 5, UpdatedBefore = 9, OrderBy = "-updated_at" }), "GET", "/v1/users", "updated_after=5&updated_before=9&order_by=-updated_at");
            yield return new Case("events.since", c => c.Analytics.EventsPageAsync(new ListEventsParams { Since = "2026-10-01T00:00:00Z", Order = "asc" }), "GET", "/v1/events", "since=2026-10-01T00%3A00%3A00Z");
            yield return new Case("config_changelog.no_op", c => c.ConfigChangelog.ListAsync(new ListConfigChangelogParams { IncludeNoOp = false }), "GET", "/v1/config_changelog", "include_no_op=false");
            yield return new Case("api_keys.revoked.cursor", c => c.ApiKeys.ListRevokedPageAsync(null, 10, "ak1"), "GET", "/v1/api_keys/revoked", "limit=10&starting_after=ak1");
            yield return new Case("logout_notify.deliveries.cursor", c => c.LogoutNotifyEndpoints.ListDeliveriesPageAsync("ln1", 10, "d1", "t1"), "GET", "/v1/logout_notify_endpoints/ln1/deliveries", "starting_after=d1&trace=t1");
            yield return new Case("app_roles.users.cursor", c => c.AppRoles.ListUsersPageAsync("admin", 10, "u1"), "GET", "/v1/app_roles/admin/users", "limit=10&starting_after=u1");
            yield return new Case("org.memberships.cursor", c => c.Organizations.Memberships.ListPageAsync("org1", 10, "m1"), "GET", "/v1/organizations/org1/memberships", "limit=10&starting_after=m1");
            yield return new Case("platform.keys.list", c => c.Platform.Keys.ListAsync("secret_key", "active", "ins_1"), "GET", "/v1/platform/keys", "?kind=secret_key&status=active&instance_id=ins_1");
            yield return new Case("platform.keys.create", c => c.Platform.Keys.CreateAsync(new CreatePlatformKeyBody { Scopes = new List<string> { "apps:read" }, AllowedOrigins = new List<string> { "https://x.test" } }), "POST", "/v1/platform/keys", null, "\"allowed_origins\":[\"https://x.test\"]");
            yield return new Case("platform.keys.revoke", c => c.Platform.Keys.RevokeAsync("pk1"), "POST", "/v1/platform/keys/pk1/revoke");
            yield return new Case("platform.audit.list", c => c.Platform.Audit.ListAsync(5, "a1", "key.created"), "GET", "/v1/platform/audit_logs", "?limit=5&starting_after=a1&action=key.created");
            yield return new Case("platform.audit.streams.create", c => c.Platform.Audit.Streams.CreateAsync(new CreateAccountAuditStreamBody { Url = "https://siem.test", Format = "cloudevents" }), "POST", "/v1/platform/audit_streams", null, "\"format\":\"cloudevents\"");
            yield return new Case("platform.audit.streams.test", c => c.Platform.Audit.Streams.TestAsync("as1"), "POST", "/v1/platform/audit_streams/as1/test");
            yield return new Case("platform.apps.create_secret_key", c => c.Platform.Apps.CreateSecretKeyAsync("app1", new CreatePlatformSecretKeyBody { Name = "ci", ExternalRef = "r", Metadata = new Dictionary<string, object?> { ["env"] = "ci" } }), "POST", "/v1/platform/apps/app1/secret_keys", null, "\"external_ref\":\"r\"");
            yield return new Case("platform.apps.clone_config", c => c.Platform.Apps.CloneConfigAsync("app1", new CloneConfigBody { ToEnvironment = "development", DryRun = true }), "POST", "/v1/platform/apps/app1/clone_config", null, "\"dry_run\":true");
            yield return new Case("platform.apps.sandbox_teardown", c => c.Platform.Apps.SandboxTeardownAsync(null, true), "POST", "/v1/platform/apps/bulk_delete", null, "\"metadata\":{\"sandbox\":true}");
            yield return new Case("platform.apps.list.metadata", c => c.Platform.Apps.ListAsync(new ListPlatformAppsParams { Metadata = new Dictionary<string, object?> { ["sandbox"] = true } }), "GET", "/v1/platform/apps", "metadata%5Bsandbox%5D=true");
            yield return new Case("platform.apps.create.external_ref", c => c.Platform.Apps.CreateAsync(new CreatePlatformAppBody { Name = "x", ExternalRef = "ext-1", Metadata = new Dictionary<string, object?> { ["product"] = "acme" } }), "POST", "/v1/platform/apps", null, "\"external_ref\":\"ext-1\"");
            yield return new Case("oauth_clients.list.metadata", c => c.OAuthClients.ListAsync(new Dictionary<string, object?> { ["product"] = "acme" }), "GET", "/v1/oauth_clients", "metadata%5Bproduct%5D=acme");
        }

        public static IEnumerable<object[]> CaseData() => Cases().Select(c => new object[] { c.Name });

        [Theory]
        [MemberData(nameof(CaseData))]
        public async Task WireSurface(string name)
        {
            var c = Cases().Single(x => x.Name == name);
            var h = MockHttpMessageHandler.Json("{}");
            using var client = Client(h);
            await c.Call(client);
            var r = h.Requests.Last();
            Assert.Equal(c.Method, r.Method);
            Assert.Equal(c.Path, r.Path);
            if (c.Query != null) Assert.Contains(c.Query.TrimStart('?'), r.Url.Query.TrimStart('?'));
            if (c.BodyContains != null) Assert.Contains(c.BodyContains, r.Body);
        }

        [Fact]
        public async Task WireSurface_IsSplitAcrossSecretAndPlatformKeys()
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var client = Client(h);
            await client.Whoami.GetAsync();
            await client.Platform.WhoamiAsync();
            Assert.Equal("Bearer atlas_test_sk_abcdef", h.Requests[0].Authorization);
            Assert.Equal("Bearer atlas_live_psk_abcdef", h.Requests[1].Authorization);
        }

        // ---- key formats ---------------------------------------------------------------

        [Theory]
        [InlineData("atlas_live_sk_abc123", KeyKind.Secret, "live", true)]
        [InlineData("atlas_test_psk_abc", KeyKind.Platform, "test", true)]
        [InlineData("atlas_live_whsec_xyz", KeyKind.WebhookSecret, "live", true)]
        [InlineData("atlas_live_pl_xyz", KeyKind.PartnerLink, "live", true)]
        [InlineData("atlas_live_at_xyz", KeyKind.AccessToken, "live", true)]
        [InlineData("atlas_test_dsk_xyz", KeyKind.Deploy, "test", true)]
        [InlineData("sk_live_abc", KeyKind.Secret, "live", false)]
        [InlineData("sk_test_abc", KeyKind.Secret, "test", false)]
        [InlineData("pk_live_abc", KeyKind.Publishable, "live", false)]
        [InlineData("psk_abc", KeyKind.Platform, null, false)]
        [InlineData("psk_live_abc", KeyKind.Platform, "live", false)]
        [InlineData("whsec_abc", KeyKind.WebhookSecret, null, false)]
        public void KeyPrefixes_AcceptBothFormats(string key, KeyKind kind, string? env, bool suite)
        {
            Assert.True(AtlasKeys.TryParse(key, out var parsed));
            Assert.Equal(kind, parsed!.Kind);
            Assert.Equal(env, parsed.Environment);
            Assert.Equal(suite, parsed.Suite);
        }

        [Theory]
        [InlineData("")]
        [InlineData("atlas_live_zz_abc")]
        [InlineData("atlas_prod_sk_abc")]
        [InlineData("atlas_live_pk_abc")]
        [InlineData("sk_abc")]
        [InlineData("pk_abc")]
        [InlineData("nonsense")]
        public void KeyPrefixes_RejectOthers(string key)
        {
            Assert.False(AtlasKeys.TryParse(key, out _));
        }

        [Fact]
        public void KeyHelpers()
        {
            Assert.True(AtlasKeys.IsSecretKey("atlas_live_sk_x") && AtlasKeys.IsSecretKey("sk_live_x"));
            Assert.False(AtlasKeys.IsSecretKey("atlas_live_psk_x"));
            Assert.True(AtlasKeys.IsPlatformKey("atlas_live_psk_x") && AtlasKeys.IsPlatformKey("psk_x"));
            Assert.True(AtlasKeys.IsPublishableKey("pk_test_x"));
            Assert.True(AtlasKeys.IsWebhookSecret("whsec_x") && AtlasKeys.IsWebhookSecret("atlas_test_whsec_x"));
            Assert.True(AtlasKeys.IsPartnerLinkSecret("atlas_live_pl_x"));
            Assert.True(AtlasKeys.IsAccessToken("atlas_live_at_x"));
            Assert.False(AtlasKeys.IsAccessToken(null));
        }

        [Fact]
        public async Task SuiteFormatKeys_AreSentAsBearer()
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var client = Client(h);
            await client.Keys.ListAsync();
            Assert.Equal("Bearer atlas_test_sk_abcdef", h.Requests[0].Authorization);
        }

        // ---- dry run, traceparent, idempotency ----------------------------------------------

        [Fact]
        public async Task DryRun_AddsTheQueryToWritesOnly_AndDropsTheIdempotencyKey()
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var client = Client(h);
            var dry = client.WithDryRun();
            await dry.SecretKeys.CreateAsync(new CreateSecretKeyBody { Scopes = new List<string> { "read_only" } }, "idem-1");
            await dry.SecretKeys.ListAsync();
            await client.SecretKeys.CreateAsync(new CreateSecretKeyBody { Scopes = new List<string> { "read_only" } }, "idem-2");
            Assert.Contains("dry_run=true", h.Requests[0].Url.Query);
            Assert.Null(h.Requests[0].IdempotencyKey);
            Assert.DoesNotContain("dry_run", h.Requests[1].Url.Query);
            Assert.DoesNotContain("dry_run", h.Requests[2].Url.Query);
            Assert.Equal("idem-2", h.Requests[2].IdempotencyKey);
        }

        [Fact]
        public async Task DryRunAsync_ReturnsThePlan()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"dry_run\",\"dry_run\":true,\"mode\":\"plan\",\"validated\":false,\"method\":\"PATCH\",\"path\":\"/v1/platform/apps/ins_1\",\"changes\":[{\"section\":\"name\",\"before\":\"A\",\"after\":\"B\"}],\"note\":\"n\"}");
            using var client = Client(h);
            var plan = await client.DryRunAsync(HttpVerb.Patch, "/v1/platform/apps/ins_1", new { name = "B" }, platform: true);
            Assert.True(plan.DryRun);
            Assert.Equal("plan", plan.Mode);
            Assert.Single(plan.Changes);
            Assert.Equal("name", plan.Changes[0].Section);
            Assert.Equal("B", plan.Changes[0].After!.Value.GetString());
            Assert.Equal("PATCH", h.Requests[0].Method);
            Assert.Equal("Bearer atlas_live_psk_abcdef", h.Requests[0].Authorization);
            Assert.Contains("dry_run=true", h.Requests[0].Url.Query);
            await Assert.ThrowsAsync<ArgumentException>(() => client.DryRunAsync(HttpVerb.Get, "/v1/users"));
        }

        [Fact]
        public async Task Traceparent_IsSentOnEveryCall()
        {
            var h = MockHttpMessageHandler.Json("{}");
            var tp = AtlasTrace.NewTraceparent();
            using var client = Client(h, o => o.Traceparent = tp);
            await client.Whoami.GetAsync();
            await client.WithTraceparent("00-" + new string('a', 32) + "-" + new string('b', 16) + "-01").Whoami.GetAsync();
            Assert.Equal(tp, h.Requests[0].Header("traceparent"));
            Assert.Equal("00-" + new string('a', 32) + "-" + new string('b', 16) + "-01", h.Requests[1].Header("traceparent"));
        }

        [Fact]
        public void Traceparent_MintAndParse()
        {
            var tp = AtlasTrace.NewTraceparent();
            Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-01$", tp);
            Assert.True(AtlasTrace.TryParseTraceparent(tp, out var traceId, out var spanId, out var flags));
            Assert.Equal(32, traceId.Length);
            Assert.Equal(16, spanId.Length);
            Assert.Equal("01", flags);
            Assert.False(AtlasTrace.TryParseTraceparent("00-" + new string('0', 32) + "-" + new string('1', 16) + "-01", out _, out _, out _));
            Assert.False(AtlasTrace.TryParseTraceparent("garbage", out _, out _, out _));
            Assert.Equal(tp, AtlasTrace.TraceparentFromHeaders(new Dictionary<string, string> { ["Traceparent"] = tp }));
            Assert.Null(AtlasTrace.TraceparentFromHeaders(new Dictionary<string, string> { ["traceparent"] = "nope" }));
            Assert.Null(AtlasTrace.TraceparentFromHeaders(new Dictionary<string, string>()));
        }

        [Fact]
        public void MetadataQuery_IsSortedAndBracketed()
        {
            var q = AtlasProvisioning.MetadataQuery(new Dictionary<string, object?> { ["b"] = true, ["a"] = "x", ["n"] = 3 });
            Assert.Equal(new[] { "metadata[a]", "metadata[b]", "metadata[n]" }, q.Select(p => p.Key).ToArray());
            Assert.Equal(new object?[] { "x", "true", "3" }, q.Select(p => p.Value).ToArray());
        }

        [Fact]
        public async Task SecretRecipient_IsSentAsAHeader()
        {
            var h = MockHttpMessageHandler.Json("{\"id\":\"k1\",\"sealed_secret\":\"AAAA\"}");
            using var client = Client(h, o => o.SecretRecipient = "x25519:AAAA");
            var created = await client.SecretKeys.CreateAsync(new CreateSecretKeyBody { Scopes = new List<string> { "read_only" } });
            Assert.Equal("x25519:AAAA", h.Requests[0].Header("X-Secret-Recipient"));
            Assert.Equal("AAAA", created.SealedSecret);
            Assert.Null(created.Secret);
        }

        [Fact]
        public async Task R2_SetManagedBy_PreflightAndSealedFields()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"atlas_id_app\",\"id\":\"app_1\",\"managed_via\":\"adopted\",\"changed\":true,\"ok\":true,\"button_rendered_origins\":[{\"origin\":\"https://a.example\",\"last_seen_at\":1,\"requests\":2}],\"authorize_requests_7d\":{\"button\":1,\"js\":0,\"fedcm\":0,\"fallback\":2,\"unmarked\":0},\"homepage_includes_script\":false,\"sealed_client_secret\":\"AAAA\",\"secrets_sealed\":{\"algorithm\":\"x25519-sealed-box\",\"recipient\":\"x25519:k\",\"fields\":[\"client_secret\"]}}");
            using var client = Client(h);
            var r = await client.Platform.AtlasIdApps.SetManagedByAsync("app_1", "ins_1");
            Assert.Equal("PUT", h.Requests[0].Method);
            Assert.Equal("/v1/platform/atlas_id_apps/app_1/managed_by", h.Requests[0].Url.AbsolutePath);
            Assert.Equal("{\"instance_id\":\"ins_1\"}", h.Requests[0].Body);
            Assert.True(r.Changed);
            Assert.Equal("adopted", r.ManagedVia);
            await client.Platform.AtlasIdApps.SetManagedByAsync("app_1", null);
            Assert.Equal("{\"instance_id\":null}", h.Requests[1].Body);

            var pf = await client.AtlasIdGrants.PreflightAsync("app_1");
            Assert.Equal("/v1/atlas_id_apps/app_1/preflight", h.Requests[2].Url.AbsolutePath);
            Assert.Equal("https://a.example", pf.ButtonRenderedOrigins![0].Origin);
            Assert.Equal(2, pf.AuthorizeRequests7d!.Fallback);
            Assert.False(pf.HomepageIncludesScript);

            var app = await client.Platform.AtlasIdApps.RotateSecretAsync("app_1");
            Assert.Equal("AAAA", app.SealedClientSecret);
            Assert.Equal("client_secret", app.SecretsSealed!.Fields[0]);
        }

        [Fact]
        public void R2_DryRunResultAndSsoClaimsDecode()
        {
            var d = JsonSerializer.Deserialize<DryRunResult>("{\"object\":\"dry_run\",\"dry_run\":true,\"mode\":\"rollback\",\"validated\":true,\"changes\":[],\"warnings\":[{\"code\":\"origin_still_referenced\"}],\"would_return_status\":201,\"suppressed\":[\"email\"],\"validation_skipped\":{\"reason\":\"external_call\",\"detail\":\"d\"},\"note\":\"\"}", AtlasJson.Options)!;
            Assert.Equal("rollback", d.Mode);
            Assert.Equal(201, d.WouldReturnStatus);
            Assert.Equal("email", d.Suppressed![0]);
            Assert.Equal("external_call", d.ValidationSkipped!.Reason);
            Assert.Single(d.Warnings!);
            var c = JsonSerializer.Deserialize<SessionClaims>("{\"sub\":\"u\",\"sso_connection_id\":\"sso_1\",\"sso_organization_id\":\"org_1\",\"sso_connection_type\":\"saml\"}", AtlasJson.Options)!;
            Assert.Equal("org_1", c.SsoOrganizationId);
            Assert.Equal("saml", c.SsoConnectionType);
        }

        // ---- errors ----------------------------------------------------------------------------

        [Fact]
        public async Task InsufficientScope_AndTheFormerSpelling_AreBothRecognised()
        {
            foreach (var code in new[] { "INSUFFICIENT_SCOPE", "SCOPE_MISSING" })
            {
                var h = new MockHttpMessageHandler((_, __) => (HttpStatusCode.Forbidden,
                    "{\"errors\":[{\"code\":\"" + code + "\",\"message\":\"no\",\"meta\":{\"required_scope\":\"secret_keys:write\",\"legacy_code\":\"SCOPE_MISSING\"},\"docs_url\":\"https://atlasauth.net/errors/" + code + "\"}]}"));
                using var client = Client(h);
                var e = await Assert.ThrowsAsync<AtlasAuthorizationException>(() => client.SecretKeys.RevokeAsync("k1"));
                Assert.True(e.IsInsufficientScope);
                Assert.Equal("secret_keys:write", e.RequiredScope);
                Assert.Equal("https://atlasauth.net/errors/" + code, e.Errors[0].DocsUrl);
                Assert.False(e.IsStepUpRequired);
            }
        }

        [Fact]
        public async Task StepUpRequired_And_OriginNotAllowed()
        {
            var h = new MockHttpMessageHandler((req, _) => req.RequestUri!.AbsolutePath.EndsWith("/keys")
                ? (HttpStatusCode.Forbidden, "{\"errors\":[{\"code\":\"STEP_UP_REQUIRED\",\"message\":\"mfa\"}]}")
                : (HttpStatusCode.Forbidden, "{\"errors\":[{\"code\":\"ORIGIN_NOT_ALLOWED\",\"message\":\"origin\"}]}"));
            using var client = Client(h);
            var e1 = await Assert.ThrowsAsync<AtlasAuthorizationException>(() => client.Platform.Keys.CreateAsync(new CreatePlatformKeyBody()));
            Assert.True(e1.IsStepUpRequired);
            Assert.Equal(ErrorCodes.StepUpRequired, e1.Code);
            var e2 = await Assert.ThrowsAsync<AtlasAuthorizationException>(() => client.Whoami.GetAsync());
            Assert.True(e2.IsOriginNotAllowed);
        }

        [Fact]
        public async Task RouteNotFound_CarriesSuggestions()
        {
            var h = new MockHttpMessageHandler((_, __) => (HttpStatusCode.NotFound,
                "{\"errors\":[{\"code\":\"ROUTE_NOT_FOUND\",\"message\":\"no\",\"did_you_mean\":[\"/v1/users\",\"/v1/user\"],\"allowed_methods\":[\"GET\"]}]}"));
            using var client = Client(h);
            var e = await Assert.ThrowsAsync<AtlasNotFoundException>(() => client.Whoami.GetAsync());
            Assert.True(e.IsRouteNotFound);
            Assert.Equal(new[] { "/v1/users", "/v1/user" }, e.DidYouMean.ToArray());
            Assert.Equal(new[] { "GET" }, e.Errors[0].AllowedMethods!.ToArray());
        }

        [Fact]
        public void NewErrorCodes()
        {
            Assert.Equal("NOT_A_LIVE_INSTANCE", ErrorCodes.NotALiveInstance);
            Assert.Equal("TEST_MODE_ONLY", ErrorCodes.TestModeOnly);
            Assert.Equal("TESTING_UNAVAILABLE", ErrorCodes.TestingUnavailable);
            Assert.Equal("PARTNER_LINK_UNAVAILABLE", ErrorCodes.PartnerLinkUnavailable);
            Assert.Equal("SCOPE_MISSING", ErrorCodes.ScopeMissing);
        }

        // ---- decoding ----------------------------------------------------------------------

        [Fact]
        public async Task SecretKey_Decodes()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"secret_key\",\"id\":\"sk_1\",\"kind\":\"secret_key\",\"format\":\"atlas\",\"prefix\":\"atlas_live_sk_ab\",\"name\":\"ci\",\"owner\":\"user:u1\",\"scopes\":[\"read_only\"],\"environment\":\"live\",\"resource\":{\"type\":\"instance\",\"id\":\"ins_1\"},\"expires_at\":1800000000000,\"expires_in_days\":null,\"status\":\"active\",\"ip_allowlist\":[],\"origin_allowlist\":[\"https://a.test\"],\"allowed_origins\":[\"https://a.test\"],\"last_used_at\":null,\"last_used_ip\":null,\"created_at\":1,\"revoked_at\":null,\"metadata\":{\"env\":\"ci\"},\"external_ref\":\"r1\",\"idempotent_replay\":true,\"secret\":\"atlas_live_sk_secret\"}");
            using var client = Client(h);
            var k = await client.SecretKeys.CreateAsync(new CreateSecretKeyBody());
            Assert.Equal("sk_1", k.Id);
            Assert.Equal("atlas", k.Format);
            Assert.Equal("ins_1", k.Resource!.Id);
            Assert.Equal(new[] { "https://a.test" }, k.AllowedOrigins!.ToArray());
            Assert.Equal("ci", k.Metadata!["env"].GetString());
            Assert.True(k.IdempotentReplay);
            Assert.Equal("r1", k.ExternalRef);
            Assert.Equal("atlas_live_sk_secret", k.Secret);
            Assert.True(AtlasKeys.IsSecretKey(k.Secret));
        }

        [Fact]
        public async Task Whoami_Decodes()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"whoami\",\"key\":{\"id\":\"sk_1\",\"type\":\"secret\",\"prefix\":\"atlas_live_sk_ab\",\"environment\":\"live\",\"scopes\":[\"read_only\"],\"effective_scopes\":[\"users:read\"],\"owner\":null,\"created_at\":1,\"expires_at\":2,\"last_used_at\":3},\"instance\":{\"id\":\"ins_1\",\"environment\":\"live\",\"issuer\":\"https://iss\",\"discovery_url\":\"https://iss/.well-known/openid-configuration\",\"jwks_uri\":\"https://iss/jwks\"},\"account\":{\"id\":\"acct_1\"},\"partner\":{\"link_id\":\"pl1\",\"id\":\"chatly\",\"name\":null}}");
            using var client = Client(h);
            var w = await client.Whoami.GetAsync();
            Assert.Equal("secret", w.Key.Type);
            Assert.Equal("users:read", w.Key.EffectiveScopes[0]);
            Assert.Equal("https://iss/jwks", w.Instance!.JwksUri);
            Assert.Equal("acct_1", w.Account!.Id);
            Assert.Equal("pl1", w.Partner!.LinkId);
        }

        [Fact]
        public async Task PartnerLink_Decodes_WithCalls30dAndMtls()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"partner_link\",\"id\":\"pl1\",\"client_id\":\"pl1\",\"partner\":\"chatly\",\"name\":\"Chatly\",\"preset\":true,\"resource\":\"ins_1\",\"scopes\":[\"users:read\"],\"client_secret_prefix\":\"atlas_\",\"token_endpoint\":\"https://t\",\"cert_fingerprints\":[\"abc\"],\"auth_methods\":[\"bearer\",\"tls_client_auth\"],\"request_id\":\"plr1\",\"status\":\"active\",\"expires_at\":9,\"calls_30d\":42,\"created_at\":1}");
            using var client = Client(h);
            var l = await client.PartnerLinks.GetAsync("pl1");
            Assert.Equal(42, l.Calls30d);
            Assert.Equal("plr1", l.RequestId);
            Assert.Equal(new[] { "abc" }, l.CertFingerprints!.ToArray());
            Assert.Contains("tls_client_auth", l.AuthMethods!);
        }

        [Fact]
        public async Task PrivacyRequest_UsesCamelCaseOnTheWire()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"privacy_request\",\"id\":\"pr1\",\"type\":\"deletion\",\"canonicalType\":\"deletion\",\"requestedType\":\"erase\",\"status\":\"scheduled\",\"subjectId\":\"u1\",\"caseId\":\"c1\",\"executeAfter\":\"2026-11-01T00:00:00Z\",\"eta\":\"2026-11-01T00:00:00Z\",\"detail\":null,\"retention\":[{\"category\":\"billing\",\"basis\":\"legal\",\"until\":\"2033\"}],\"steps\":[{\"name\":\"sessions\",\"status\":\"pending\"}],\"trace\":\"t1\",\"createdAt\":\"x\",\"updatedAt\":\"y\",\"completedAt\":null}");
            using var client = Client(h);
            var r = await client.Privacy.CreateRequestAsync(new CreatePrivacyRequestBody { SubjectId = "u1", Type = "erase", CaseId = "c1", ExecuteAfter = "2026-11-01T00:00:00Z" }, "k1");
            Assert.Equal("k1", h.Requests[0].IdempotencyKey);
            Assert.Contains("\"subjectId\":\"u1\"", h.Requests[0].Body);
            Assert.Contains("\"caseId\":\"c1\"", h.Requests[0].Body);
            Assert.Equal("scheduled", r.Status);
            Assert.Equal("u1", r.SubjectId);
            Assert.Equal("erase", r.RequestedType);
            Assert.Equal("billing", r.Retention![0].Category);
            Assert.Equal("t1", r.Trace);
        }

        [Fact]
        public async Task PrivacyLookup_DecodesPrincipalMatched()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"privacy_lookup\",\"status\":\"found\",\"subjectId\":\"u1\",\"matchedBy\":\"atlas.sub\",\"detail\":null,\"principal\":\"p1\",\"principalMatched\":true}");
            using var client = Client(h);
            var r = await client.Privacy.LookupAsync(new PrivacyLookupBody { Emails = new List<string> { "a@b.co" } });
            Assert.Equal("u1", r.SubjectId);
            Assert.Equal("atlas.sub", r.MatchedBy);
            Assert.True(r.PrincipalMatched);
        }

        [Fact]
        public async Task Integration_Decodes_AndKeepsPresetFieldsInExtra()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"integration\",\"id\":\"chatly\",\"product\":\"chatly\",\"preset\":\"chatly\",\"role\":\"support_chat\",\"enabled\":true,\"connected\":true,\"audience\":\"chatly:ch1\",\"ttl_seconds\":120,\"events_url\":\"https://e\",\"events_url_default\":true,\"events\":[\"user.created\"],\"cmp\":{\"script_url\":\"https://cmp\",\"site_id\":\"s\"},\"external_ref\":\"r\",\"channel\":\"ch1\",\"template\":\"chatly\",\"health\":{\"last_minted_at\":5,\"mints_24h\":3,\"verify_errors_24h\":1,\"last_verify_error\":{\"error\":\"expired\",\"audience\":null,\"at\":1,\"reported_at\":2},\"events\":{\"url\":\"https://e\",\"endpoint_id\":\"we1\",\"state\":\"failing\",\"failing_since\":9,\"consecutive_failures\":4,\"success_rate_24h\":0.5},\"backchannel_logout\":null}}");
            using var client = Client(h);
            var i = await client.Instance.GetIntegrationAsync("chatly");
            Assert.True(i.Connected);
            Assert.Equal(120, i.TtlSeconds);
            Assert.Equal("https://cmp", i.Cmp!.ScriptUrl);
            Assert.Equal(3, i.Health!.Mints24h);
            Assert.Equal(1, i.Health.VerifyErrors24h);
            Assert.Equal("failing", i.Health.Events!.State);
            Assert.Equal(0.5, i.Health.Events.SuccessRate24h);
            Assert.Equal("ch1", i.Extra!["channel"].GetString());
        }

        [Fact]
        public async Task WebhookEndpoint_Decodes_Health()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"webhook_endpoint\",\"id\":\"we1\",\"url\":\"https://a\",\"enabled_events\":[\"*\"],\"active\":true,\"disabled_at\":null,\"created_at\":1,\"failing_since\":5,\"last_failing_since\":4,\"event_naming\":\"canonical\",\"signature_versions\":[\"v1\",\"v2\"],\"destination\":{\"type\":\"aws_eventbridge\",\"region\":\"us-east-1\",\"event_bus_name\":\"default\",\"credentials_set\":true},\"health\":\"failing\",\"last_failure\":{\"at\":7,\"status\":500,\"error\":\"webhook_http_error\"},\"consecutive_failures\":3,\"success_rate_24h\":0.25,\"attempts_24h\":{\"succeeded\":1,\"failed\":3},\"alert_contacts\":[\"ops@example.com\"],\"alerts_sent\":{\"first_sent_at\":1,\"second_sent_at\":null,\"disabled_sent_at\":null},\"auto_disable\":{\"after_failing_ms\":100,\"after_consecutive_failures\":10,\"consecutive_failures_min_failing_ms\":50,\"description\":\"d\"},\"replay\":{\"since\":3,\"method\":\"POST\",\"path\":\"/v1/webhook_endpoints/we1/replay\",\"note\":\"n\"},\"metadata\":{\"product\":\"acme\"},\"external_ref\":\"r\"}");
            using var client = Client(h);
            var e = await client.Webhooks.Endpoints.GetAsync("we1");
            Assert.Equal("failing", e.Health);
            Assert.Equal(4, e.LastFailingSince);
            Assert.Equal(500, e.LastFailure!.HttpStatus);
            Assert.Equal(0.25, e.SuccessRate24h);
            Assert.Equal(3, e.Attempts24h!.Failed);
            Assert.Equal("aws_eventbridge", e.Destination!.Type);
            Assert.Equal("default", e.Destination.EventBusName);
            Assert.True(e.Destination.CredentialsSet);
            Assert.Equal(10, e.AutoDisable!.AfterConsecutiveFailures);
            Assert.Equal("/v1/webhook_endpoints/we1/replay", e.Replay!.Path);
            Assert.Equal("acme", e.Metadata!["product"].GetString());
        }

        [Fact]
        public async Task WebhookDelivery_HasLatencyAndTrace()
        {
            var h = MockHttpMessageHandler.Json("{\"data\":[{\"id\":\"d1\",\"event_id\":\"e1\",\"attempt_number\":1,\"status\":\"success\",\"http_status\":200,\"latency_ms\":42,\"trace_id\":\"abc\",\"created_at\":1}],\"has_more\":false,\"next_cursor\":null}");
            using var client = Client(h);
            var page = await client.Webhooks.DeliveriesAsync("we1", new ListWebhookDeliveriesParams());
            Assert.Equal(42, page.Data[0].LatencyMs);
            Assert.Equal("abc", page.Data[0].TraceId);
        }

        [Fact]
        public async Task TestNow_Decodes()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"webhook_test\",\"delivery_id\":\"d1\",\"event_id\":\"e1\",\"status\":\"failed\",\"http_status\":500,\"latency_ms\":120,\"response_snippet\":\"oops\",\"error\":\"webhook_http_error\",\"signature_header_sent\":\"v1,abc\",\"will_retry\":true}");
            using var client = Client(h);
            var r = await client.Webhooks.Endpoints.TestNowAsync("we1");
            Assert.Equal("wait=10s", h.Requests[0].Url.Query.TrimStart('?'));
            Assert.True(r.WillRetry);
            Assert.Equal(120, r.LatencyMs);
            Assert.Equal("v1,abc", r.SignatureHeaderSent);
        }

        [Fact]
        public async Task StatusSubscription_AndDependencies_Decode()
        {
            var h = new MockHttpMessageHandler((req, _) => req.RequestUri!.AbsolutePath.EndsWith("status_dependencies")
                ? (HttpStatusCode.OK, "{\"object\":\"list\",\"data\":[{\"object\":\"status_dependency\",\"id\":\"sd1\",\"name\":\"Stripe\",\"url\":\"https://s\",\"component_filter\":[],\"enabled\":true,\"state\":\"degraded\",\"indicator\":\"minor\",\"description\":null,\"components\":[{\"id\":\"c\",\"name\":\"API\",\"status\":\"degraded\"}],\"incidents\":[{\"id\":\"i\",\"name\":\"n\",\"status\":\"investigating\",\"impact\":\"minor\",\"updatedAt\":\"t\"}],\"last_checked_at\":1,\"last_success_at\":1,\"last_error\":null,\"stale\":false,\"created_at\":1}],\"state\":\"degraded\"}")
                : (HttpStatusCode.OK, "{\"object\":\"status_subscription\",\"subscribed\":true,\"enabled\":true,\"regions\":[],\"component_ids\":[\"backend_api\"],\"min_impact\":\"major\",\"include_maintenance\":false,\"email_owners\":true,\"webhook\":true,\"rss_url\":\"https://r\",\"atom_url\":\"https://a\",\"available_regions\":[\"us\"],\"available_components\":[{\"id\":\"backend_api\",\"name\":\"Backend API\"}],\"updated_at\":3}"));
            using var client = Client(h);
            var s = await client.Status.GetSubscriptionAsync();
            Assert.Equal("major", s.MinImpact);
            Assert.Equal("backend_api", s.ComponentIds[0]);
            Assert.Equal("https://r", s.RssUrl);
            var d = await client.Status.ListDependenciesAsync();
            Assert.Equal("degraded", d.State);
            Assert.Equal("investigating", d.Data[0].Incidents![0].Status);
            Assert.Equal("t", d.Data[0].Incidents![0].UpdatedAt);
        }

        [Fact]
        public async Task Compliance_Decode()
        {
            var h = new MockHttpMessageHandler((req, _) =>
            {
                var p = req.RequestUri!.AbsolutePath;
                if (p.EndsWith("/notices")) return (HttpStatusCode.OK, "{\"object\":\"list\",\"data\":[{\"object\":\"notice\",\"id\":\"n1\",\"date\":\"2026-10-01\",\"type\":\"deprecation\",\"tags\":[\"breaking\"],\"title\":\"t\",\"summary\":\"s\",\"affects\":[\"webhooks\"],\"docs_url\":\"https://d\",\"changelog_id\":\"c\",\"applies\":true,\"detail\":\"you\",\"count\":2,\"first_seen_at\":1,\"emailed_at\":null,\"acknowledged_at\":null}],\"has_more\":false,\"unacknowledged\":1,\"changelog_url\":\"https://c\"}");
                if (p.EndsWith("/sub_processors")) return (HttpStatusCode.OK, "{\"object\":\"sub_processors\",\"instance_id\":\"ins_1\",\"roles\":{\"atlas\":\"processor\"},\"data_region\":\"eu\",\"processor\":{\"name\":\"Atlas\",\"role\":\"processor\",\"url\":\"https://a\"},\"atlas_sub_processors\":[{\"id\":\"sp1\",\"name\":\"AWS\",\"role\":\"atlas_sub_processor\",\"source\":\"atlas\",\"purpose\":\"hosting\",\"data\":[\"all\"],\"location\":\"eu\",\"enabled_by\":null,\"url\":null}],\"engaged_by_you\":[],\"dpa_url\":\"https://dpa\"}");
                if (p.EndsWith("/entitlements")) return (HttpStatusCode.OK, "{\"object\":\"entitlements\",\"$schema\":\"https://atlasauth.net/schemas/entitlements-v1.json\",\"subject\":{\"type\":\"org\",\"id\":\"o1\"},\"plan\":\"pro\",\"planSource\":\"internal\",\"entitlements\":[{\"key\":\"seats\",\"enabled\":true,\"limit\":10,\"used\":3,\"source\":\"override\"}]}");
                return (HttpStatusCode.OK, "{\"object\":\"audit_export\",\"id\":\"ae1\",\"name\":\"siem\",\"type\":\"webhook\",\"enabled\":true,\"status\":\"active\",\"action_filter\":null,\"destination\":{\"url\":\"https://s\",\"has_secret\":true},\"cursor\":{\"id\":null,\"created_at\":null},\"consecutive_failures\":0,\"last_error\":null,\"last_delivered_at\":null,\"exported_count\":7,\"mandatory_actions\":[\"secret_key.created\"],\"secret\":\"whsec_x\",\"created_at\":1,\"updated_at\":2}");
            });
            using var client = Client(h);
            var n = await client.Notices.ListAsync();
            Assert.Equal(1, n.Unacknowledged);
            Assert.True(n.Data[0].Applies);
            var sp = await client.SubProcessors.GetAsync();
            Assert.Equal("eu", sp.DataRegion);
            Assert.Equal("AWS", sp.AtlasSubProcessors[0].Name);
            var e = await client.Entitlements.GetAsync("org", "o1");
            Assert.Equal("internal", e.PlanSource);
            Assert.Equal(10, e.Items[0].Limit);
            Assert.Equal("https://atlasauth.net/schemas/entitlements-v1.json", e.Schema);
            var ae = await client.AuditExports.GetAsync("ae1");
            Assert.Equal(7, ae.ExportedCount);
            Assert.Equal("whsec_x", ae.Secret);
            Assert.True(ae.Destination!["has_secret"].GetBoolean());
        }

        [Fact]
        public async Task Preflight_Decodes_NewFields()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"preflight\",\"ok\":true,\"checked_at\":1,\"findings\":[{\"severity\":\"warning\",\"code\":\"pkce_not_required\",\"message\":\"m\",\"fix\":\"f\",\"group\":\"security_posture\"}],\"min_severity\":\"info\",\"suppressed\":[{\"code\":\"x\",\"severity\":\"info\",\"subject\":{\"a\":\"b\"},\"reason\":\"r\"}],\"integrations\":[{\"origin\":\"https://a\",\"integration\":\"redirect\",\"source\":\"default\",\"confident\":false,\"reason\":\"r\"}]}");
            using var client = Client(h);
            var p = await client.Instance.PreflightAsync("info", true);
            Assert.Equal("security_posture", p.Findings[0].Group);
            Assert.Equal(PreflightCodes.PkceNotRequired, p.Findings[0].Code);
            Assert.Equal("r", p.Suppressed![0].Reason);
            Assert.False(p.Integrations![0].Confident);
        }

        [Fact]
        public async Task Embed_Decodes_CamelCase()
        {
            var h = MockHttpMessageHandler.Json("{\"schema\":\"embed-v1\",\"product\":\"atlas\",\"updated\":\"2026-10-01\",\"checkers\":{\"userAgent\":\"AtlasBot\",\"egress\":[\"1.2.3.4\"]},\"surfaces\":{\"button\":{\"tenant\":true,\"servedBy\":\"frontend\",\"csp\":{\"script-src\":[\"https://x\"]},\"trustedTypes\":[\"atlas\"],\"consentCategory\":\"necessary\"}},\"changes\":[{\"effective\":\"2026-10-01\",\"surface\":\"button\",\"add\":{\"script-src\":[\"https://y\"]},\"remove\":{}}],\"tenant\":{\"instance\":\"ins_1\",\"environment\":\"live\",\"frontendApiHosts\":[\"clerk.example.com\"],\"shape\":\"button\",\"origin\":\"https://a\",\"originAllowed\":true,\"policy\":\"script-src https://x\",\"sources\":{\"script-src\":[\"https://x\"]}}}");
            using var client = Client(h);
            var m = await client.Instance.EmbedAsync("ins_1");
            Assert.Equal("AtlasBot", m.Checkers!.UserAgent);
            Assert.Equal("frontend", m.Surfaces["button"].ServedBy);
            Assert.Equal("https://x", m.Surfaces["button"].Csp!["script-src"][0]);
            Assert.Equal("clerk.example.com", m.Tenant!.FrontendApiHosts![0]);
            Assert.True(m.Tenant.OriginAllowed);
            Assert.Equal("script-src https://x", m.Tenant.Policy);
        }

        [Fact]
        public async Task Singletons_ReportConfigured()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"messaging_provider\",\"configured\":false,\"channel\":\"email\"}");
            using var client = Client(h);
            var m = await client.Messaging.ChannelAsync("email");
            Assert.False(m.Configured);
        }

        [Fact]
        public async Task ConfigChangelog_And_AuditLog_NewFields()
        {
            var h = new MockHttpMessageHandler((req, _) => req.RequestUri!.AbsolutePath.Contains("audit")
                ? (HttpStatusCode.OK, "{\"object\":\"list\",\"data\":[{\"object\":\"audit_log\",\"id\":\"a1\",\"actor_type\":\"api_key\",\"action\":\"x\",\"created_at\":1,\"actor\":{\"type\":\"api_key\",\"id\":\"k1\",\"key_name\":\"ci\",\"key_prefix\":\"atlas_live_sk_ab\"}}],\"has_more\":false}")
                : (HttpStatusCode.OK, "{\"data\":[{\"id\":\"c1\",\"at\":1,\"action\":\"a\",\"no_op\":true,\"recorded\":\"fields\",\"changes\":[{\"section\":\"s\",\"before\":null,\"after\":null,\"values_recorded\":false}]}],\"has_more\":false}"));
            using var client = Client(h);
            var a = await client.AuditLogs.ListAsync();
            Assert.Equal("ci", a.Data[0].Actor!.KeyName);
            var c = await client.ConfigChangelog.ListAsync();
            Assert.True(c.Data[0].NoOp);
            Assert.Equal("fields", c.Data[0].Recorded);
            Assert.False(c.Data[0].Changes![0].ValuesRecorded);
        }

        [Fact]
        public async Task DataSubjectRequest_NewFields()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"data_subject_request\",\"id\":\"d1\",\"type\":\"restriction\",\"status\":\"pending\",\"user_id\":\"u1\",\"requested_at\":1,\"updated_at\":2,\"canonical_type\":\"restriction\",\"source\":\"orchestrator\",\"case_id\":\"c1\",\"trace_id\":\"t\",\"steps\":[{\"name\":\"s\",\"status\":\"done\"}],\"deletion_effective_at\":99}");
            using var client = Client(h);
            var d = await client.DataSubjectRequests.GetAsync("d1");
            Assert.Equal("restriction", d.CanonicalType);
            Assert.Equal("c1", d.CaseId);
            Assert.Equal(99, d.DeletionEffectiveAt);
        }

        [Fact]
        public async Task EntitlementOverride_Internal()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"entitlement_override\",\"id\":\"eo1\",\"subject_type\":\"org\",\"subject_id\":\"o1\",\"internal\":true,\"created_at\":1}");
            using var client = Client(h);
            var o = await client.EntitlementOverrides.CreateAsync(new CreateEntitlementOverrideBody { SubjectType = "org", SubjectId = "o1", Internal = true });
            Assert.Contains("\"internal\":true", h.Requests[0].Body);
            Assert.True(o.Internal);
        }

        [Fact]
        public async Task Referrals_NewFields()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"referral_settings\",\"enabled\":false,\"provider\":\"referalli\",\"context_cookies\":[\"a\",\"b\"],\"provider_aliases\":[\"referally\"],\"capabilities\":{\"signup\":true},\"one_click\":{\"id\":\"referalli\",\"name\":\"Referalli\",\"available\":true,\"provider\":\"referalli\",\"scope\":\"s\",\"redirect_uri\":\"https://r\"},\"one_click_presets\":[],\"providers\":[{\"id\":\"referalli\",\"name\":\"Referalli\",\"capabilities\":{},\"needs\":[],\"default_context_cookies\":[\"a\"],\"aliases\":[\"referally\"]}]}");
            using var client = Client(h);
            var s = await client.Referrals.GetAsync();
            Assert.Equal(new[] { "a", "b" }, s.ContextCookies!.ToArray());
            Assert.Equal("referally", s.ProviderAliases![0]);
            Assert.True(s.OneClick!.Available);
            Assert.Equal("referally", s.Providers[0].Aliases![0]);
            Assert.Equal(ReferralProviders.Referalli, s.Provider);
            Assert.Equal("referally", ReferralProviders.ReferalliLegacy);
        }

        [Fact]
        public async Task DesignSettings_Brand()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"design_settings\",\"brand\":{\"source\":\"url\",\"url\":\"https://b\",\"imported_at\":5,\"document_sha256\":\"abc\",\"mapping\":[{\"token\":\"color.primary\",\"to\":\"palette.primary\",\"value\":\"#fff\"}],\"ignored\":[{\"token\":\"color.sparkle\",\"reason\":\"unknown\"}],\"meta\":{\"name\":\"Acme\",\"legalName\":\"Acme Inc\",\"supportEmail\":\"s@a.co\"}},\"precedence\":[{\"layer\":\"preset\",\"source\":null},{\"layer\":\"brand\",\"source\":\"https://b\"}],\"sources\":{\"palette.primary\":\"brand\"},\"ignored\":[{\"token\":\"color.sparkle\",\"reason\":\"unknown\"}],\"warnings\":[]}");
            using var client = Client(h);
            var s = await client.Design.ImportBrandUrlAsync("https://b");
            Assert.Equal("abc", s.Brand!.DocumentSha256);
            Assert.Equal("Acme Inc", s.Brand.Meta!.LegalName);
            Assert.Equal("brand", s.Precedence![1].Layer);
            Assert.Equal("brand", s.Sources!["palette.primary"]);
            Assert.Equal("color.sparkle", s.Ignored![0].Token);
        }

        [Fact]
        public async Task PlatformApp_Provisioning_AndAtlasIdButtonFields()
        {
            var h = new MockHttpMessageHandler((req, _) => req.RequestUri!.AbsolutePath.Contains("atlas_id_apps")
                ? (HttpStatusCode.OK, "{\"object\":\"atlas_id_app\",\"id\":\"a1\",\"client_id\":\"c\",\"name\":\"n\",\"button_rendered_origins\":[{\"origin\":\"https://a\",\"last_seen_at\":1,\"requests\":3}],\"authorize_requests_7d\":{\"button\":4,\"js\":1,\"fedcm\":0,\"fallback\":2,\"unmarked\":0},\"fallback_origins\":[],\"homepage_includes_script\":false}")
                : (HttpStatusCode.OK, "{\"object\":\"app\",\"id\":\"ins_1\",\"name\":\"x\",\"environment\":\"production\",\"publishable_key\":\"pk_live_x\",\"metadata\":{\"product\":\"acme\",\"sandbox\":true},\"external_ref\":\"ext-1\",\"idempotent_replay\":true}"));
            using var client = Client(h);
            var app = await client.Platform.Apps.GetAsync("ins_1");
            Assert.Equal("ext-1", app.ExternalRef);
            Assert.True(app.IdempotentReplay);
            Assert.True(app.Metadata!["sandbox"].GetBoolean());
            var siwa = await client.Platform.AtlasIdApps.GetAsync("a1");
            Assert.Equal(3, siwa.ButtonRenderedOrigins![0].Requests);
            Assert.Equal(4, siwa.AuthorizeRequests7d!.Button);
            Assert.False(siwa.HomepageIncludesScript);
        }

        [Fact]
        public async Task OrgMembershipPage_AndCursorPages_DecodeNextCursor()
        {
            var h = MockHttpMessageHandler.Json("{\"data\":[],\"has_more\":true,\"next_cursor\":\"m9\"}");
            using var client = Client(h);
            var page = await client.Organizations.Memberships.ListPageAsync("o1", 1);
            Assert.True(page.HasMore);
            Assert.Equal("m9", page.NextCursor);
        }

        [Fact]
        public void SessionClaims_CarryOrgsAndSso()
        {
            var claims = AtlasJson.Deserialize<SessionClaims>("{\"iss\":\"i\",\"sub\":\"s\",\"sid\":\"x\",\"exp\":1,\"org_id\":\"o1\",\"orgs\":[{\"id\":\"o1\",\"slug\":\"acme\",\"role\":\"admin\"}],\"sso_connection_id\":\"c1\",\"sso_tenant_id\":\"t\",\"sso_issuer\":\"https://login\",\"sso_domain\":\"acme.com\"}")!;
            Assert.Equal("acme", claims.Orgs![0].Slug);
            Assert.Equal("c1", claims.SsoConnectionId);
            Assert.Equal("t", claims.SsoTenantId);
            Assert.Equal("https://login", claims.SsoIssuer);
            Assert.Equal("acme.com", claims.SsoDomain);
        }

        // ---- webhook events + signature v2 -------------------------------------------------------

        [Fact]
        public void WebhookEventAliases()
        {
            Assert.Equal("organization_membership.created", WebhookEvents.Canonical("organizationMembership.created"));
            Assert.Equal("user.created", WebhookEvents.Canonical("user.created"));
            Assert.True(WebhookEvents.IsDeprecated("session.ended"));
            Assert.False(WebhookEvents.IsDeprecated("session.revoked"));
            var spellings = WebhookEvents.Spellings("organization_membership.deleted");
            Assert.Equal(new[] { "organization_membership.deleted", "organization.membership.deleted", "organizationMembership.deleted" }, spellings.ToArray());
            Assert.Equal("session.revoked", WebhookEvents.Spellings("session.ended")[0]);
            Assert.Equal("companion", WebhookEvents.Aliases["user.deletion_cancelled"].Kind);
            Assert.Equal("user.deletion_canceled", WebhookEvents.Aliases["user.deletion_cancelled"].Canonical);
            Assert.Equal("webhook_endpoint.failing", WebhookEvents.WebhookEndpointFailing);
            Assert.Equal("secret_key.expiring", WebhookEvents.SecretKeyExpiring);
            Assert.Equal("webhooks:reveal", WebhookEvents.ScopeWebhooksReveal);
            Assert.Equal("privacy_request.completed", PrivacyEvents.RequestCompleted);
            Assert.Equal("status.incident.resolved", StatusEvents.IncidentResolved);
        }

        private const string Secret = "whsec_test_secret";
        private const string Url = "https://app.example.com/hooks/atlas";
        private const long Now = 1_757_937_600_000;
        private const string Body = "{\"id\":\"evt_1\",\"type\":\"user.updated\",\"data\":{}}";

        private static string Mac(string text)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        private static Dictionary<string, string> Headers(string signature) => new Dictionary<string, string>
        {
            ["atlas-id"] = "evt_1",
            ["atlas-timestamp"] = Now.ToString(),
            ["atlas-signature"] = signature,
        };

        [Fact]
        public void V2Signature_BindsTheUrl()
        {
            var v1 = "v1," + Mac($"evt_1.{Now}.{Body}");
            var v2 = "v2," + Mac($"evt_1.{Now}.{Url}.{Body}");
            var verifier = new WebhookVerifier(Secret);
            var bytes = Encoding.UTF8.GetBytes(Body);

            Assert.True(verifier.VerifyUrl(bytes, Headers(v1 + " " + v2), Url, false, Now).Verified);
            Assert.True(verifier.VerifyUrl(bytes, Headers(v2), Url, true, Now).Verified);
            // v2 over another URL is refused.
            Assert.Equal("signature_mismatch", verifier.VerifyUrl(bytes, Headers(v2), "https://evil.example.com/h", true, Now).Reason);
            // requireV2 refuses a v1-only delivery.
            var v1Only = verifier.VerifyUrl(bytes, Headers(v1), Url, true, Now);
            Assert.False(v1Only.Verified);
            Assert.Equal("malformed_signature", v1Only.Reason);
            // v1 is still accepted without requireV2, and unknown versions are ignored.
            Assert.True(verifier.VerifyUrl(bytes, Headers("v9,zzz " + v1), Url, false, Now).Verified);
            // The plain verifier ignores v2.
            Assert.Equal("malformed_signature", verifier.Verify(bytes, Headers(v2), Now).Reason);
            Assert.True(verifier.Verify(bytes, Headers(v1 + " " + v2), Now).Verified);
        }

        // ---- privacy call verification --------------------------------------------------------------

        [Fact]
        public void PrivacyCall_VerifiesCaseCalls_AndEvents()
        {
            var tp = AtlasTrace.NewTraceparent();
            var caseBody = "{\"case\":\"c1\"}";
            var h = Headers("v1," + Mac($"evt_1.{Now}.{caseBody}"));
            h["traceparent"] = tp;
            var call = PrivacyCall.Verify(Encoding.UTF8.GetBytes(caseBody), h, Secret, out var reason, Now);
            Assert.NotNull(call);
            Assert.Null(reason);
            Assert.Equal("evt_1", call!.DeliveryId);
            Assert.Equal(Now, call.Timestamp);
            Assert.Equal(tp, call.Traceparent);
            Assert.Null(call.Event);

            var evBody = "{\"id\":\"evt_2\",\"type\":\"privacy_request.completed\",\"data\":{\"id\":\"pr1\",\"status\":\"completed\",\"subjectId\":\"u1\"}}";
            var h2 = Headers("v1," + Mac($"evt_1.{Now}.{evBody}"));
            var ev = PrivacyCall.Verify(Encoding.UTF8.GetBytes(evBody), h2, Secret, out _, Now);
            Assert.NotNull(ev!.Event);
            var req = ev.RequestFromEvent()!;
            Assert.Equal("completed", req.Status);
            Assert.Equal("u1", req.SubjectId);

            Assert.Null(PrivacyCall.Verify(Encoding.UTF8.GetBytes(evBody + " "), h2, Secret, out var bad, Now));
            Assert.Equal("signature_mismatch", bad);
        }

        // ---- sealed boxes ---------------------------------------------------------------------------

        [Fact]
        public void SealedBox_RoundTrips_AndRejectsTampering()
        {
            var pair = SealedBox.GenerateKeyPair();
            Assert.StartsWith("x25519:", pair.Recipient);
            Assert.Equal(32, pair.PublicKey.Length);
            var sealedBytes = SealedBox.Seal(Encoding.UTF8.GetBytes("atlas_live_sk_secret"), pair.PublicKey);
            var b64 = Convert.ToBase64String(sealedBytes);
            Assert.Equal("atlas_live_sk_secret", SealedBox.OpenSealedSecret(b64, pair));

            var other = SealedBox.GenerateKeyPair();
            Assert.Throws<SealedBoxException>(() => SealedBox.OpenSealedSecret(b64, other));
            sealedBytes[sealedBytes.Length - 1] ^= 1;
            Assert.Throws<SealedBoxException>(() => SealedBox.OpenSealedSecret(Convert.ToBase64String(sealedBytes), pair));
            Assert.Throws<SealedBoxException>(() => SealedBox.OpenSealedSecret("!!!", pair));
            Assert.Throws<SealedBoxException>(() => SealedBox.OpenSealedSecret(Convert.ToBase64String(new byte[10]), pair));
        }

        // ---- operator device flow -------------------------------------------------------------------

        [Fact]
        public async Task OperatorLogin_DeviceFlow_AndTokenExchange()
        {
            var polls = 0;
            var delays = new List<TimeSpan>();
            var handler = new MockHttpMessageHandler((req, body) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                if (path == "/v1/operator")
                    return (HttpStatusCode.OK, "{\"issuer\":\"https://atlasauth.net\",\"device_authorization_endpoint\":\"https://api.test.example/oauth2/device\",\"token_endpoint\":\"https://api.test.example/oauth2/token\",\"client_id\":\"atlas-cli\",\"scope\":\"openid\",\"token_exchange_endpoint\":\"https://api.test.example/v1/oauth/token\",\"token_exchange_audience\":\"atlas\",\"root_action_max_mfa_age_seconds\":300}");
                if (path == "/oauth2/device")
                    return (HttpStatusCode.OK, "{\"device_code\":\"dc\",\"user_code\":\"ABCD-EFGH\",\"verification_uri\":\"https://v\",\"verification_uri_complete\":\"https://v?c=ABCD-EFGH\",\"expires_in\":600,\"interval\":5}");
                if (path == "/oauth2/token")
                {
                    polls++;
                    if (polls == 1) return (HttpStatusCode.BadRequest, "{\"error\":\"authorization_pending\"}");
                    if (polls == 2) return (HttpStatusCode.BadRequest, "{\"error\":\"slow_down\"}");
                    return (HttpStatusCode.OK, "{\"access_token\":\"op_tok\",\"token_type\":\"Bearer\",\"expires_in\":3600}");
                }
                if (path == "/v1/oauth/token")
                {
                    if (body.Contains("client_credentials")) return (HttpStatusCode.OK, "{\"access_token\":\"atlas_live_at_partner\",\"token_type\":\"Bearer\",\"expires_in\":3600}");
                    return body.Contains("STEP")
                        ? (HttpStatusCode.Forbidden, "{\"errors\":[{\"code\":\"STEP_UP_REQUIRED\",\"message\":\"mfa\"}]}")
                        : (HttpStatusCode.OK, "{\"access_token\":\"atlas_live_at_xyz\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"issued_token_type\":\"urn:ietf:params:oauth:token-type:access_token\",\"resource\":\"ins_1\"}");
                }
                return (HttpStatusCode.NotFound, "{}");
            });
            var opts = new OperatorOptions
            {
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
                Delay = (d, ct) => { delays.Add(d); return Task.CompletedTask; },
            };

            var start = await OperatorClient.StartLoginAsync(null, opts);
            Assert.Equal("ABCD-EFGH", start.UserCode);
            Assert.Equal("atlas-cli", start.Config.ClientId);
            var token = await OperatorClient.PollLoginAsync(start, opts);
            Assert.Equal("op_tok", token.AccessToken);
            Assert.Equal(3, polls);
            Assert.Equal(new[] { 5.0, 5.0, 10.0 }, delays.Select(d => d.TotalSeconds).ToArray());

            var exchanged = await OperatorClient.ExchangeTokenAsync("op_tok", "ins_1", new[] { "users:read", "sessions:read" }, opts);
            Assert.Equal("atlas_live_at_xyz", exchanged.AccessToken);
            Assert.Equal("ins_1", exchanged.Resource);
            Assert.True(AtlasKeys.IsAccessToken(exchanged.AccessToken));
            var last = handler.Requests.Last();
            Assert.Contains("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Atoken-exchange", last.Body);
            Assert.Contains("scope=users%3Aread+sessions%3Aread", last.Body);
            Assert.Contains("resource=ins_1", last.Body);

            var step = await Assert.ThrowsAsync<OAuthFlowException>(() => OperatorClient.ExchangeTokenAsync("STEP", "ins_1", new[] { "keys:write" }, opts));
            Assert.Equal("STEP_UP_REQUIRED", step.Error);
            Assert.Equal(403, step.Status);

            var partner = await OperatorClient.PartnerLinkTokenAsync("pl1", "secret", "users:read", opts);
            Assert.Equal("atlas_live_at_partner", partner.AccessToken);
        }

        [Fact]
        public async Task OperatorLogin_DeniedAndExpired()
        {
            var handler = new MockHttpMessageHandler((req, _) => (HttpStatusCode.BadRequest, "{\"error\":\"access_denied\",\"error_description\":\"no\"}"));
            var opts = new OperatorOptions { HttpClient = new HttpClient(handler), Delay = (d, ct) => Task.CompletedTask };
            var start = new OperatorDeviceAuthorization { DeviceCode = "dc", Interval = 1, ExpiresIn = 600, Config = new OperatorConfiguration { TokenEndpoint = "https://t/oauth2/token", ClientId = "atlas-cli" } };
            var e = await Assert.ThrowsAsync<OAuthFlowException>(() => OperatorClient.PollLoginAsync(start, opts));
            Assert.Equal("access_denied", e.Error);

            var clock = DateTimeOffset.UtcNow;
            var pending = new MockHttpMessageHandler((req, _) => (HttpStatusCode.BadRequest, "{\"error\":\"authorization_pending\"}"));
            var opts2 = new OperatorOptions { HttpClient = new HttpClient(pending), Delay = (d, ct) => { clock = clock.Add(d).AddSeconds(1000); return Task.CompletedTask; }, Now = () => clock };
            var expired = await Assert.ThrowsAsync<OAuthFlowException>(() => OperatorClient.PollLoginAsync(start, opts2));
            Assert.Equal("expired_token", expired.Error);
        }

        [Fact]
        public async Task RevokeLeakedKeys_NeedsNoKey()
        {
            var handler = MockHttpMessageHandler.Json("{\"results\":[{\"index\":0,\"kind\":\"secret_key\",\"outcome\":\"revoked\"},{\"index\":1,\"kind\":\"unknown\",\"outcome\":\"not_found\"}]}");
            var opts = new OperatorOptions { ApiUrl = "https://api.test.example", HttpClient = new HttpClient(handler) };
            var results = await OperatorClient.RevokeLeakedKeysAsync(new[] { "atlas_live_sk_leak", "junk" }, "https://github.com/x/y", opts);
            Assert.Equal("revoked", results[0].Outcome);
            Assert.Equal("not_found", results[1].Outcome);
            Assert.Null(handler.Requests[0].Authorization);
            Assert.Contains("\"url\":\"https://github.com/x/y\"", handler.Requests[0].Body);
        }

        [Fact]
        public async Task NewClientFromAccessToken_DrivesBothApis()
        {
            var handler = MockHttpMessageHandler.Json("{}");
            using var client = OperatorClient.NewClientFromAccessToken("atlas_live_at_xyz", "https://api.test.example", new HttpClient(handler));
            await client.Whoami.GetAsync();
            await client.Platform.WhoamiAsync();
            Assert.All(handler.Requests, r => Assert.Equal("Bearer atlas_live_at_xyz", r.Authorization));
            Assert.Throws<ArgumentException>(() => OperatorClient.NewClientFromAccessToken(""));
        }

        // ---- partner-side request protocol ------------------------------------------------------------

        [Fact]
        public async Task PartnerLinkProtocol_CreatesAndReadsRequests()
        {
            var handler = new MockHttpMessageHandler((req, _) => req.Method == HttpMethod.Post
                ? (HttpStatusCode.Created, "{\"object\":\"partner_link_request\",\"protocol\":\"v1\",\"id\":\"plr1\",\"status\":\"pending\",\"resource\":\"ins_1\",\"scopes\":[\"users:read\"],\"request_secret\":\"rs_1\",\"expires_at\":9}")
                : (HttpStatusCode.OK, "{\"object\":\"partner_link_request\",\"id\":\"plr1\",\"status\":\"approved\",\"client_id\":\"pl9\",\"scopes\":[],\"expires_at\":9}"));
            var http = new HttpClient(handler);
            var created = await PartnerLinkProtocol.CreateRequestAsync(new CreatePartnerLinkRequestBody { Partner = "acme", Resource = "ins_1", Scopes = new List<string> { "users:read" }, CallbackUrl = "https://acme.test/cb" }, "https://api.test.example", http);
            Assert.Equal("rs_1", created.RequestSecret);
            Assert.Null(handler.Requests[0].Authorization);
            Assert.Contains("\"callback_url\":\"https://acme.test/cb\"", handler.Requests[0].Body);
            var read = await PartnerLinkProtocol.GetRequestAsync("plr1", "rs_1", "https://api.test.example", http);
            Assert.Equal("approved", read.Status);
            Assert.Equal("pl9", read.ClientId);
            Assert.Equal("Bearer rs_1", handler.Requests[1].Authorization);
        }

        // ---- idempotency + provisioning ----------------------------------------------------------------

        [Fact]
        public async Task Provisioning_BodiesCarryExternalRefAndMetadata()
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var client = Client(h);
            await client.OAuthClients.CreateAsync(new CreateOAuthClientBody { Name = "n", ExternalRef = "oc-1", Metadata = new Dictionary<string, object?> { ["product"] = "acme" }, RequirePkce = false });
            var body = Sent(h.Requests[0]);
            Assert.Equal("oc-1", body.GetProperty("external_ref").GetString());
            Assert.Equal("acme", body.GetProperty("metadata").GetProperty("product").GetString());
            Assert.False(body.GetProperty("require_pkce").GetBoolean());
        }
    }
}
