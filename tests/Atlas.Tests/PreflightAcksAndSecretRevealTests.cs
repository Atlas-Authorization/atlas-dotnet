using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>
    /// The second 2026-11-02 surface (1.22.0): preflight acknowledgements, the OAuth client secret reveal and
    /// require_verified_email, invitation expiry, platform app filters, user status, hook fail_closed, test-send outcomes.
    /// </summary>
    public class PreflightAcksAndSecretRevealTests
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
        public async Task PreflightAcks_SetListDelete()
        {
            var (c, h) = Client("{\"object\":\"preflight_ack\",\"code\":\"pkce_not_required\",\"reason\":\"legacy app\",\"by\":\"ak_1\",\"at\":1,\"expires_at\":2,\"active\":true}");
            var ack = await c.Instance.AcknowledgePreflightAsync("pkce_not_required", "legacy app", 2L);
            Assert.Equal("PUT", h.Requests[0].Method);
            Assert.Equal("/v1/instance/preflight/acks/pkce_not_required", h.Requests[0].Path);
            Assert.Equal("legacy app", Sent(h).GetProperty("reason").GetString());
            Assert.Equal(2, Sent(h).GetProperty("expires_at").GetInt64());
            Assert.True(ack.Active);
            Assert.Equal(2L, ack.ExpiresAt);
            Assert.Equal("ak_1", ack.By);

            await c.Instance.AcknowledgePreflightAsync("pkce_not_required", "legacy app");
            Assert.False(Sent(h).TryGetProperty("expires_at", out _));

            await c.Instance.ListPreflightAcksAsync();
            Assert.Equal("GET", h.Requests[2].Method);
            Assert.Equal("/v1/instance/preflight/acks", h.Requests[2].Path);

            await c.Instance.DeletePreflightAckAsync("pkce_not_required");
            Assert.Equal("DELETE", h.Requests[3].Method);
            Assert.Equal("/v1/instance/preflight/acks/pkce_not_required", h.Requests[3].Path);
        }

        [Fact]
        public async Task Preflight_FindingCarriesAcknowledged()
        {
            var (c, _) = Client("{\"object\":\"preflight\",\"ok\":true,\"checked_at\":1,\"findings\":[{\"severity\":\"warning\",\"code\":\"pkce_not_required\",\"message\":\"m\",\"fix\":\"f\",\"acknowledged\":{\"reason\":\"legacy\",\"by\":null,\"at\":5,\"expires_at\":null}}]}");
            var report = await c.Instance.PreflightAsync();
            var ack = report.Findings[0].Acknowledged!;
            Assert.Equal("legacy", ack.Reason);
            Assert.Null(ack.By);
            Assert.Null(ack.ExpiresAt);
        }

        [Fact]
        public async Task OAuthClient_ChosenSecret_RequireVerifiedEmail_AndReveal()
        {
            var (c, h) = Client("{\"object\":\"oauth_client\",\"id\":\"oac_1\",\"require_verified_email\":true,\"client_secret\":\"cs_x\",\"client_secret_reveal\":{\"path\":\"/v1/oauth_clients/oac_1/secret/reveal\",\"expires_at\":9}}");
            var created = await c.OAuthClients.CreateAsync(new CreateOAuthClientBody
            {
                Name = "App",
                RedirectUris = new List<string> { "https://app.example.com/cb" },
                ClientSecret = "s3cr3t-s3cr3t-s3cr3t-s3cr3t-s3cr3t-0",
                RequireVerifiedEmail = true,
            });
            Assert.Equal("s3cr3t-s3cr3t-s3cr3t-s3cr3t-s3cr3t-0", Sent(h).GetProperty("client_secret").GetString());
            Assert.True(Sent(h).GetProperty("require_verified_email").GetBoolean());
            Assert.True(created.RequireVerifiedEmail);
            Assert.Equal("/v1/oauth_clients/oac_1/secret/reveal", created.ClientSecretReveal!.Path);
            Assert.Equal(9, created.ClientSecretReveal.ExpiresAt);

            await c.OAuthClients.UpdateAsync("oac_1", new UpdateOAuthClientBody { RequireVerifiedEmail = false });
            Assert.False(Sent(h).GetProperty("require_verified_email").GetBoolean());

            var (c2, h2) = Client("{\"object\":\"oauth_client_secret\",\"id\":\"oac_1\",\"client_id\":\"atlas_client_1\",\"client_secret\":\"cs_x\",\"secret_prefix\":\"cs_\",\"client_secret_expires_at\":null,\"note\":\"n\"}");
            var revealed = await c2.OAuthClients.RevealSecretAsync("oac_1");
            Assert.Equal("POST", h2.Requests[0].Method);
            Assert.Equal("/v1/oauth_clients/oac_1/secret/reveal", h2.Requests[0].Path);
            Assert.Equal("cs_x", revealed.ClientSecret);
            Assert.Null(revealed.ClientSecretExpiresAt);
        }

        [Fact]
        public async Task Invitation_Expiry_AndPlatformAppFilters()
        {
            var (c, h) = Client("{\"object\":\"invitation\",\"id\":\"inv_1\"}");
            await c.Invitations.CreateAsync(new CreateInvitationBody { EmailAddress = "a@example.com", ExpiresInDays = 30 });
            Assert.Equal(30, Sent(h).GetProperty("expires_in_days").GetInt32());
            Assert.False(Sent(h).TryGetProperty("expires_at", out _));
            await c.Invitations.CreateAsync(new CreateInvitationBody { EmailAddress = "a@example.com", ExpiresAt = "2027-01-01T00:00:00Z" });
            Assert.Equal("2027-01-01T00:00:00Z", Sent(h).GetProperty("expires_at").GetString());

            await c.Platform.Apps.ListAsync(new ListPlatformAppsParams { Name = "Acme", NamePrefix = "Ac", ExternalRef = "acme/prod", Limit = 10, StartingAfter = "ins_9" });
            var q = h.Requests[2].Query;
            Assert.Contains("name=Acme", q);
            Assert.Contains("name_prefix=Ac", q);
            Assert.Contains("external_ref=acme%2Fprod", q);
            Assert.Contains("limit=10", q);
            Assert.Contains("starting_after=ins_9", q);
        }

        [Fact]
        public async Task UserStatus_HookFailClosed_TestSendAndWebhookTestUrl()
        {
            var (c, _) = Client("{\"object\":\"user\",\"id\":\"user_1\",\"status\":\"suspended\"}");
            Assert.Equal("suspended", (await c.Users.GetAsync("user_1")).Status);

            var (c2, h2) = Client("{\"object\":\"before_user_create_hook\",\"enabled\":true,\"fail_closed\":true}");
            var hook = await c2.UserHooks.UpdateBeforeUserCreateAsync(new UpdateBeforeUserCreateHookBody { FailClosed = true });
            Assert.True(Sent(h2).GetProperty("fail_closed").GetBoolean());
            Assert.True(hook.FailClosed);

            var (c3, _) = Client("{\"object\":\"messaging_test\",\"channel\":\"email\",\"sent_to\":\"a@example.com\",\"ok\":true,\"accepted\":true,\"provider\":\"resend\",\"provider_message_id\":\"msg_1\"}");
            var sent = await c3.Messaging.TestAsync("email", "a@example.com");
            Assert.True(sent.Accepted);
            Assert.Equal("resend", sent.Provider);
            Assert.Equal("msg_1", sent.ProviderMessageId);

            var (c4, _) = Client("{\"object\":\"webhook_test_result\",\"delivery_id\":\"d_1\",\"event_id\":\"e_1\",\"url\":\"https://hooks.example.com/atlas\",\"status\":\"success\"}");
            Assert.Equal("https://hooks.example.com/atlas", (await c4.Webhooks.Endpoints.TestNowAsync("we_1")).Url);

            Assert.Equal("session.expired", WebhookEvents.SessionExpired);
            Assert.Equal("invitation.accepted", WebhookEvents.InvitationAccepted);
        }
    }
}
