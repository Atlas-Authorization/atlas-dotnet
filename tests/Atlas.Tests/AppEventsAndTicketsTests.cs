using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>The 2026-10-30 surface (1.20.0): application webhooks, browser tickets, test sign-in URLs, instance settings.</summary>
    public class AppEventsAndTicketsTests
    {
        private static (AtlasClient, MockHttpMessageHandler) Client(string json = "{}")
        {
            var handler = MockHttpMessageHandler.Json(json);
            var client = new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });
            return (client, handler);
        }

        private static JsonElement Sent(MockHttpMessageHandler h) => JsonDocument.Parse(h.Requests[h.Requests.Count - 1].Body).RootElement;

        [Fact]
        public async Task BrowserTicket_SendsModeAndParsesTheTicket()
        {
            var (c, h) = Client("{\"object\":\"browser_ticket\",\"id\":\"sit_1\",\"user_id\":\"user_1\",\"ticket\":\"t\",\"url\":\"https://app.example.com/?__atlas_ticket=t\",\"redirect_url\":\"https://app.example.com/\",\"expires_in\":120,\"expires_at\":\"2026-10-30T12:00:00.000Z\"}");
            var t = await c.Sessions.CreateBrowserTicketAsync("user_1", "https://app.example.com/", 120);
            var r = h.Requests[0];
            Assert.Equal("POST", r.Method);
            Assert.Equal("/v1/sessions", r.Path);
            var body = Sent(h);
            Assert.Equal("browser_ticket", body.GetProperty("mode").GetString());
            Assert.Equal("user_1", body.GetProperty("user_id").GetString());
            Assert.Equal(120, body.GetProperty("expires_in_seconds").GetInt32());
            Assert.Equal("t", t.Ticket);
            Assert.Equal(120, t.ExpiresIn);
            Assert.StartsWith("https://app.example.com/", t.Url);
        }

        [Fact]
        public async Task TestingSignInUrl_PostsTheBody()
        {
            var (c, h) = Client("{\"object\":\"testing_sign_in_url\",\"url\":\"http://localhost:3000/\"}");
            var t = await c.Testing.CreateSignInUrlAsync("user_1", "http://localhost:3000/");
            Assert.Equal("/v1/testing/sign_in_url", h.Requests[0].Path);
            var body = Sent(h);
            Assert.Equal("http://localhost:3000/", body.GetProperty("redirect_url").GetString());
            Assert.False(body.TryGetProperty("expires_in_seconds", out _));
            Assert.Equal("testing_sign_in_url", t.ObjectType);
        }

        [Fact]
        public async Task AppEvents_PublishCarriesTheIdempotencyKey_AndParsesTheReplayFields()
        {
            var (c, h) = Client("{\"object\":\"app_event\",\"id\":\"aev_1\",\"type\":\"call.completed\",\"organization_id\":\"org_1\",\"idempotent_replay\":false,\"deliveries_queued\":2,\"endpoint_ids\":[\"we_1\",\"we_2\"]}");
            var e = await c.AppEvents.PublishAsync(new PublishAppEventBody { OrganizationId = "org_1", Type = "call.completed", Data = new Dictionary<string, object?> { ["call_id"] = "c1" } }, "c1");
            Assert.Equal("/v1/app_events", h.Requests[0].Path);
            Assert.Equal("c1", h.Requests[0].IdempotencyKey);
            Assert.Equal("c1", Sent(h).GetProperty("data").GetProperty("call_id").GetString());
            Assert.Equal(2, e.DeliveriesQueued);
            Assert.Equal(new List<string> { "we_1", "we_2" }, e.EndpointIds);
        }

        [Fact]
        public async Task AppWebhookEndpoints_Routes()
        {
            var (c, h) = Client();
            await c.AppEvents.Endpoints.EventTypesAsync("org_1");
            Assert.Equal("/v1/organizations/org_1/app_event_types", h.Requests[0].Path);
            await c.AppEvents.Endpoints.TestAsync("org_1", "we_1", "10s");
            Assert.Equal("/v1/organizations/org_1/app_webhook_endpoints/we_1/test", h.Requests[1].Path);
            Assert.Equal("?wait=10s", h.Requests[1].Query);
            await c.AppEvents.Endpoints.CreateAsync("org_1", new AppWebhookEndpointBody { Url = "https://hooks.example.com/", Active = false });
            Assert.False(Sent(h).GetProperty("active").GetBoolean());
            await c.AppEvents.Types.GetAsync("call.completed");
            Assert.Equal("/v1/app_event_types/call.completed", h.Requests[3].Path);
        }

        [Fact]
        public async Task HostedPages_ClearsAUrlWithAnExplicitNull()
        {
            var (c, h) = Client();
            await c.Instance.UpdateHostedPagesAsync(new UpdateHostedPagesBody { SignInUrl = "https://app.example.com/sign-in" }.Set("user_url", null));
            var body = Sent(h);
            Assert.Equal("https://app.example.com/sign-in", body.GetProperty("sign_in_url").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("user_url").ValueKind);
            Assert.False(body.TryGetProperty("sign_up_url", out _));
        }

        [Fact]
        public void Models_ReadTheNewFields()
        {
            var user = JsonSerializer.Deserialize<User>("{\"id\":\"user_1\",\"external_accounts\":[{\"object\":\"external_account\",\"id\":\"ea_1\",\"provider\":\"google\",\"provider_user_id\":\"g1\",\"email_verified\":true,\"connected_at\":5}]}", AtlasJson.Options)!;
            Assert.Equal("google", user.ExternalAccounts![0].Provider);
            Assert.Equal(5, user.ExternalAccounts[0].ConnectedAt);
            var v = JsonSerializer.Deserialize<ApiKeyVerification>("{\"valid\":true,\"effective_scopes\":[\"calls:read\",\"calls:write\"]}", AtlasJson.Options)!;
            Assert.Equal(2, v.EffectiveScopes!.Count);
            var m = JsonSerializer.Deserialize<ManagedEmail>("{\"dns\":{\"provider\":\"cloudflare\",\"token_set\":true,\"status\":\"verifying\",\"attempts\":2,\"last_error\":\"x\"},\"feedback_webhook_error\":\"refused\"}", AtlasJson.Options)!;
            Assert.Equal("verifying", m.Dns!.Status);
            Assert.Equal(2, m.Dns.Attempts);
            Assert.Equal("refused", m.FeedbackWebhookError);
        }
    }
}
