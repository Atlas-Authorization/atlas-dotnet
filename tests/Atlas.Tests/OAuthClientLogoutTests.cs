using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>The 2026-11-02 surface (1.22.0): client logout, refresh_touches_session, add_redirect_origins and the captcha site key.</summary>
    public class OAuthClientLogoutTests
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
        public async Task Logout_SendsTheSid_AndParsesTheResult()
        {
            var (c, h) = Client("{\"object\":\"oauth_client_logout\",\"client_id\":\"atlas_client_1\",\"sid\":\"sess_1\",\"session\":\"revoked\",\"grants_revoked\":2,\"backchannel_logouts\":3}");
            var out1 = await c.OAuthClients.LogoutAsync("oac_1", "sess_1", "idem_1");
            Assert.Equal("POST", h.Requests[0].Method);
            Assert.Equal("/v1/oauth_clients/oac_1/logout", h.Requests[0].Path);
            Assert.Equal("sess_1", Sent(h).GetProperty("sid").GetString());
            Assert.Equal("idem_1", h.Requests[0].IdempotencyKey);
            Assert.Equal("revoked", out1.Session);
            Assert.Equal(2, out1.GrantsRevoked);
            Assert.Equal(3, out1.BackchannelLogouts);
            Assert.Equal("atlas_client_1", out1.ClientId);
        }

        [Fact]
        public async Task Create_SendsTheNewFlags_AndParsesAddedOrigins()
        {
            var (c, h) = Client("{\"object\":\"oauth_client\",\"id\":\"oac_1\",\"refresh_touches_session\":true,\"added_origins\":[\"https://app.example.com\"],\"client_secret\":\"cs_x\"}");
            var created = await c.OAuthClients.CreateAsync(new CreateOAuthClientBody
            {
                Name = "App",
                RedirectUris = new List<string> { "https://app.example.com/cb" },
                FirstParty = true,
                RefreshTouchesSession = true,
                AddRedirectOrigins = true,
            });
            var body = Sent(h);
            Assert.True(body.GetProperty("refresh_touches_session").GetBoolean());
            Assert.True(body.GetProperty("add_redirect_origins").GetBoolean());
            Assert.True(created.RefreshTouchesSession);
            Assert.Equal(new List<string> { "https://app.example.com" }, created.AddedOrigins);
        }

        [Fact]
        public async Task Update_SendsTheFlags_AndOmitsThemWhenUnset()
        {
            var (c, h) = Client("{\"object\":\"oauth_client\",\"id\":\"oac_1\"}");
            var updated = await c.OAuthClients.UpdateAsync("oac_1", new UpdateOAuthClientBody { RefreshTouchesSession = false, AddRedirectOrigins = true });
            Assert.Equal("PATCH", h.Requests[0].Method);
            Assert.False(Sent(h).GetProperty("refresh_touches_session").GetBoolean());
            Assert.True(Sent(h).GetProperty("add_redirect_origins").GetBoolean());
            Assert.False(updated.RefreshTouchesSession);
            Assert.Null(updated.AddedOrigins);

            await c.OAuthClients.UpdateAsync("oac_1", new UpdateOAuthClientBody { Name = "App 2" });
            Assert.False(Sent(h).TryGetProperty("refresh_touches_session", out _));
            Assert.False(Sent(h).TryGetProperty("add_redirect_origins", out _));
        }

        [Fact]
        public async Task CaptchaSecret_WithASiteKey_SwitchesTheProvider()
        {
            var (c, h) = Client("{\"object\":\"captcha_secret\",\"provider\":\"turnstile\",\"set\":true,\"switched\":true,\"captcha\":{\"provider\":\"turnstile\",\"siteKey\":\"0x4BBB\"}}");
            var res = await c.InstanceSecurity.SetCaptchaSecretAsync("0x4AAA", "turnstile", "0x4BBB");
            Assert.Equal("PUT", h.Requests[0].Method);
            Assert.Equal("/v1/instance/captcha_secret", h.Requests[0].Path);
            Assert.Equal("0x4BBB", Sent(h).GetProperty("site_key").GetString());
            Assert.True(res.Switched);
            Assert.Equal("0x4BBB", res.Captcha!["siteKey"].GetString());

            await c.InstanceSecurity.SetCaptchaSecretAsync("0x4AAA", "turnstile");
            Assert.False(Sent(h).TryGetProperty("site_key", out _));
        }
    }
}
