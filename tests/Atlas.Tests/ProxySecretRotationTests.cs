using System.Net.Http;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>
    /// The trusted reverse proxy secret, and secret rotation with an overlap for webhook endpoints, per-instance
    /// platform keys and Sign in with Atlas apps.
    /// </summary>
    public class ProxySecretRotationTests
    {
        private static AtlasClient Client(MockHttpMessageHandler handler) =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                PlatformKey = "psk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

        [Fact]
        public async Task ProxySecret_GetCreateRotateDelete()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"proxy_secret\",\"set\":true,\"created_at\":1800000000000,"
                + "\"previous_expires_at\":1800003600000,\"secret_header\":\"X-Atlas-Proxy-Secret\",\"client_ip_header\":\"X-Atlas-Client-IP\","
                + "\"secret\":\"aps_1\",\"rotated\":true,\"deleted\":true}");
            using var client = Client(handler);

            var view = await client.InstanceSecurity.GetProxySecretAsync();
            Assert.Equal("GET", handler.Requests[0].Method);
            Assert.Equal("/v1/instance/proxy_secret", handler.Requests[0].Path);
            Assert.True(view.Set);
            Assert.Equal(1_800_000_000_000L, view.CreatedAt);
            Assert.Equal("X-Atlas-Client-IP", view.ClientIpHeader);

            var created = await client.InstanceSecurity.CreateProxySecretAsync();
            Assert.Equal("POST", handler.Requests[1].Method);
            Assert.Equal("{}", handler.Requests[1].Body);
            Assert.Equal("aps_1", created.Secret);

            var rotated = await client.InstanceSecurity.CreateProxySecretAsync(new CreateProxySecretBody { OverlapSeconds = 3600 });
            Assert.Equal("{\"overlap_seconds\":3600}", handler.Requests[2].Body);
            Assert.True(rotated.Rotated);
            Assert.Equal(1_800_003_600_000L, rotated.PreviousExpiresAt);

            var gone = await client.InstanceSecurity.DeleteProxySecretAsync();
            Assert.Equal("DELETE", handler.Requests[3].Method);
            Assert.Equal("/v1/instance/proxy_secret", handler.Requests[3].Path);
            Assert.True(gone.Deleted);
        }

        [Fact]
        public async Task WebhookRotateSecret_WithOverlap()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"webhook_secret\",\"secret\":\"whsec_new\",\"overlap_seconds\":600,\"previous_secret_expires_at\":1800000600000}");
            using var client = Client(handler);
            var out1 = await client.Webhooks.Endpoints.RotateSecretAsync("whep_1", new RotateWebhookSecretBody { OverlapSeconds = 600 });
            Assert.Equal("/v1/webhook_endpoints/whep_1/rotate_secret", handler.Requests[0].Path);
            Assert.Equal("{\"overlap_seconds\":600}", handler.Requests[0].Body);
            Assert.Equal(600, out1.OverlapSeconds);
            Assert.Equal(1_800_000_600_000L, out1.PreviousSecretExpiresAt);
        }

        [Fact]
        public async Task PlatformInstanceRotation_WithOverlap()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"app_secret\",\"app_id\":\"app_1\",\"instance_id\":\"ins_1\",\"secret\":\"sk_x\","
                + "\"overlap_seconds\":0,\"previous_keys_expire_at\":null,\"note\":\"n\"}");
            using var client = Client(handler);
            var out1 = await client.Platform.Apps.RotateInstanceSecretAsync("app_1", "ins_1", new RotatePlatformSecretBody { OverlapSeconds = 0 }, "idem_1");
            var req = handler.Requests[0];
            Assert.Equal("/v1/platform/apps/app_1/instances/ins_1/rotate_secret", req.Path);
            Assert.Equal("{\"overlap_seconds\":0}", req.Body);
            Assert.Equal("idem_1", req.IdempotencyKey);
            Assert.Equal("ins_1", out1.InstanceId);
            Assert.Equal("app_1", out1.AppId);
            Assert.Null(out1.PreviousKeysExpireAt);
        }

        [Fact]
        public async Task AtlasIdAppRotations_WithOverlap()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"atlas_id_app\",\"id\":\"aia_1\",\"client_secret\":\"cs_new\",\"overlap_seconds\":3600,"
                + "\"previous_secret_expires_at\":1800003600000,\"secret_issued_at\":1800000000000,\"webhook_secret\":\"whsec_new\","
                + "\"webhook_previous_secret_expires_at\":1800000600000}");
            using var client = Client(handler);
            var app = await client.Platform.AtlasIdApps.RotateSecretAsync("aia_1", new RotateAtlasIdAppSecretBody { OverlapSeconds = 3600 });
            Assert.Equal("/v1/platform/atlas_id_apps/aia_1/rotate_secret", handler.Requests[0].Path);
            Assert.Equal("{\"overlap_seconds\":3600}", handler.Requests[0].Body);
            Assert.Equal(1_800_000_000_000L, app.SecretIssuedAt);
            Assert.Equal(3600, app.OverlapSeconds);

            var hook = await client.Platform.AtlasIdApps.RotateWebhookSecretAsync("aia_1", new RotateAtlasIdAppSecretBody { OverlapSeconds = 600 });
            Assert.Equal("/v1/platform/atlas_id_apps/aia_1/webhook/rotate_secret", handler.Requests[1].Path);
            Assert.Equal("{\"overlap_seconds\":600}", handler.Requests[1].Body);
            Assert.Equal(1_800_000_600_000L, hook.WebhookPreviousSecretExpiresAt);
        }
    }
}
