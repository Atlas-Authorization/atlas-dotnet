using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Request shaping and decoding for the origin move, connections, integrations, logout fan-out, managed email, back-channel logs and the Sign in with Atlas publisher.</summary>
    public class AddedOperationsTests
    {
        private const string Reply = "{\"object\":\"x\",\"data\":[],\"changes\":[],\"problems\":[]}";

        private static AtlasClient Client(MockHttpMessageHandler handler) =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                PlatformKey = "psk_test_456",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

        private static async Task Expect(string method, string pathAndQuery, string? body, Func<AtlasClient, Task> call)
        {
            var h = MockHttpMessageHandler.Json(Reply);
            using var c = Client(h);
            await call(c);
            var r = h.Requests[0];
            Assert.Equal(method, r.Method);
            Assert.Equal(pathAndQuery, r.Path + r.Query);
            if (body == null)
            {
                Assert.True(string.IsNullOrEmpty(r.Body), $"{method} {pathAndQuery} should send no body");
            }
            else
            {
                Assert.True(JsonElementEquals(JsonDocument.Parse(body).RootElement, JsonDocument.Parse(r.Body).RootElement),
                    $"{method} {pathAndQuery} body {r.Body}");
            }
        }

        private static bool JsonElementEquals(JsonElement a, JsonElement b)
        {
            if (a.ValueKind != b.ValueKind) return false;
            switch (a.ValueKind)
            {
                case JsonValueKind.Object:
                    var props = new Dictionary<string, JsonElement>();
                    foreach (var p in b.EnumerateObject()) props[p.Name] = p.Value;
                    var count = 0;
                    foreach (var p in a.EnumerateObject())
                    {
                        count++;
                        if (!props.TryGetValue(p.Name, out var v) || !JsonElementEquals(p.Value, v)) return false;
                    }
                    return count == props.Count;
                case JsonValueKind.Array:
                    var ea = a.EnumerateArray().GetEnumerator();
                    var eb = b.EnumerateArray().GetEnumerator();
                    while (true)
                    {
                        var na = ea.MoveNext();
                        var nb = eb.MoveNext();
                        if (na != nb) return false;
                        if (!na) return true;
                        if (!JsonElementEquals(ea.Current, eb.Current)) return false;
                    }
                default:
                    return a.GetRawText() == b.GetRawText();
            }
        }

        private static readonly OriginMoveBody Move = new OriginMoveBody { From = "https://old.example.com", To = "https://new.example.com", DryRun = true };
        private const string MoveJson = "{\"from\":\"https://old.example.com\",\"to\":\"https://new.example.com\",\"dry_run\":true}";

        [Fact]
        public async Task AddedBackendOperations_HitTheirRoutes()
        {
            await Expect("POST", "/v1/instance/move_origin", MoveJson, c => c.Instance.MoveOriginAsync(Move));
            await Expect("GET", "/v1/instance/connections", null, c => c.Instance.ConnectionsAsync());
            await Expect("GET", "/v1/instance/integrations/chatly", null, c => c.Instance.GetChatlyIntegrationAsync());
            await Expect("PUT", "/v1/instance/integrations/chatly", "{\"channel\":\"support\"}", c => c.Instance.SetChatlyIntegrationAsync("support"));
            await Expect("DELETE", "/v1/instance/integrations/chatly", null, c => c.Instance.DeleteChatlyIntegrationAsync());
            await Expect("GET", "/v1/instance/integrations/forumulate", null, c => c.Instance.GetForumulateIntegrationAsync());
            await Expect("PUT", "/v1/instance/integrations/forumulate", "{\"org_id\":\"org_1\",\"spaces\":[\"sp_1\"]}",
                c => c.Instance.SetForumulateIntegrationAsync(new ForumulateIntegrationBody { OrgId = "org_1", Spaces = new List<object> { "sp_1" } }));
            await Expect("DELETE", "/v1/instance/integrations/forumulate", null, c => c.Instance.DeleteForumulateIntegrationAsync());
            await Expect("GET", "/v1/logout_notify_endpoints", null, c => c.LogoutNotifyEndpoints.ListAsync());
            await Expect("POST", "/v1/logout_notify_endpoints", "{\"url\":\"https://hooks.example.com/logout\"}",
                c => c.LogoutNotifyEndpoints.CreateAsync(new LogoutNotifyEndpointBody { Url = "https://hooks.example.com/logout" }));
            await Expect("GET", "/v1/logout_notify_endpoints/lne_1", null, c => c.LogoutNotifyEndpoints.GetAsync("lne_1"));
            await Expect("PATCH", "/v1/logout_notify_endpoints/lne_1", "{\"description\":\"x\"}",
                c => c.LogoutNotifyEndpoints.UpdateAsync("lne_1", new LogoutNotifyEndpointBody { Description = "x" }));
            await Expect("DELETE", "/v1/logout_notify_endpoints/lne_1", null, c => c.LogoutNotifyEndpoints.DeleteAsync("lne_1"));
            await Expect("GET", "/v1/logout_notify_endpoints/lne_1/deliveries?limit=5", null, c => c.LogoutNotifyEndpoints.ListDeliveriesAsync("lne_1", 5));
            await Expect("GET", "/v1/messaging_providers/email/managed", null, c => c.Messaging.GetManagedEmailAsync());
            await Expect("POST", "/v1/messaging_providers/email/managed", "{\"from_address\":\"no-reply@example.com\"}",
                c => c.Messaging.CreateManagedEmailAsync(new ManagedEmailBody { FromAddress = "no-reply@example.com" }));
            await Expect("POST", "/v1/messaging_providers/email/managed/verify", null, c => c.Messaging.VerifyManagedEmailAsync());
            await Expect("DELETE", "/v1/messaging_providers/email/managed?force=true", null, c => c.Messaging.DeleteManagedEmailAsync(true));
            await Expect("GET", "/v1/oauth_clients/oac_1/backchannel_deliveries?limit=10", null, c => c.OAuthClients.ListBackchannelDeliveriesAsync("oac_1", 10));
            await Expect("POST", "/v1/oauth_clients/oac_1/backchannel_logout/test", null, c => c.OAuthClients.TestBackchannelLogoutAsync("oac_1"));
        }

        [Fact]
        public async Task AddedPlatformOperations_HitTheirRoutes()
        {
            await Expect("GET", "/v1/platform/atlas_id_apps/app_1/backchannel_deliveries?starting_after=d_1", null,
                c => c.Platform.AtlasIdApps.ListBackchannelDeliveriesAsync("app_1", startingAfter: "d_1"));
            await Expect("POST", "/v1/platform/atlas_id_apps/app_1/backchannel_logout/test", null, c => c.Platform.AtlasIdApps.TestBackchannelLogoutAsync("app_1"));
            await Expect("GET", "/v1/platform/atlas_id_apps/app_1/preflight", null, c => c.Platform.AtlasIdApps.PreflightAsync("app_1"));
            await Expect("GET", "/v1/platform/atlas_id_apps/app_1/users?email=a%40example.com", null, c => c.Platform.AtlasIdApps.FindUsersAsync("app_1", "a@example.com"));
            await Expect("DELETE", "/v1/platform/atlas_id_apps/app_1/grants/gr_1", null, c => c.Platform.AtlasIdApps.RevokeGrantAsync("app_1", "gr_1"));
            await Expect("POST", "/v1/platform/atlas_id_apps/app_1/move_origin", MoveJson, c => c.Platform.AtlasIdApps.MoveOriginAsync("app_1", Move));
            await Expect("POST", "/v1/platform/atlas_id_apps/app_1/webhook/rotate_secret", null, c => c.Platform.AtlasIdApps.RotateWebhookSecretAsync("app_1"));
            await Expect("GET", "/v1/platform/atlas_id_publisher", null, c => c.Platform.AtlasIdPublisher.GetAsync());
            await Expect("PUT", "/v1/platform/atlas_id_publisher", "{\"name\":\"Acme\",\"domain\":\"example.com\"}",
                c => c.Platform.AtlasIdPublisher.SetAsync(new AtlasIdPublisherBody { Name = "Acme", Domain = "example.com" }));
            await Expect("PATCH", "/v1/platform/atlas_id_publisher", "{\"skip_consent\":true}", c => c.Platform.AtlasIdPublisher.SetSkipConsentAsync(true));
            await Expect("DELETE", "/v1/platform/atlas_id_publisher", null, c => c.Platform.AtlasIdPublisher.DeleteAsync());
            await Expect("POST", "/v1/platform/atlas_id_publisher/verify", null, c => c.Platform.AtlasIdPublisher.VerifyAsync());
            await Expect("GET", "/v1/platform/config_changelog", null, c => c.Platform.ConfigChangelogAsync());
        }

        [Fact]
        public async Task DecodesOriginMoveAndCookieInventory()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"origin_move\",\"from\":\"https://old.example.com\",\"to\":\"https://new.example.com\",\"dry_run\":false,\"applied\":true,"
                + "\"changes\":[{\"object\":\"oauth_client\",\"id\":\"oac_1\",\"field\":\"redirect_uris\",\"before\":[\"https://old.example.com/cb\"],\"after\":[\"https://new.example.com/cb\"]}],\"problems\":[]}");
            using (var c = Client(h))
            {
                var m = await c.Instance.MoveOriginAsync(Move);
                Assert.True(m.Applied);
                Assert.Equal("https://new.example.com/cb", m.Changes[0].After!.Value[0].GetString());
            }

            var h2 = MockHttpMessageHandler.Json("{\"object\":\"cookie_consent\",\"enabled\":true,\"culture\":\"page\",\"declared_cookies\":[{\"name\":\"__session\",\"pattern\":null,\"store\":\"cookie\","
                + "\"domain\":\".example.com\",\"lifetime_seconds\":null,\"lifetime\":\"session\",\"http_only\":true,\"category\":\"necessary\",\"purpose\":\"p\",\"written_when\":\"w\",\"set_by\":\"atlas_frontend_api\"}]}");
            using var c2 = Client(h2);
            var cc = await c2.Instance.GetCookieConsentAsync();
            Assert.Equal("page", cc.Culture);
            Assert.Equal("cookie", cc.DeclaredCookies![0].Store);
            Assert.True(cc.DeclaredCookies[0].HttpOnly);
        }
    }
}
