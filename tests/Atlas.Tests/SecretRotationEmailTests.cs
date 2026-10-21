using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>
    /// OAuth client secret rotation with overlap, the development email sender, managed email on your own
    /// provider account, origin integrations, issuer fields and the new error codes.
    /// </summary>
    public class SecretRotationEmailTests
    {
        private static AtlasClient Client(MockHttpMessageHandler handler) =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

        private static JsonElement Body(RecordedRequest req) => JsonDocument.Parse(req.Body).RootElement;

        [Fact]
        public async Task RotateSecret_SendsOverlap_AndDecodesExpiry()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"oauth_client\",\"id\":\"oc_1\",\"client_secret\":\"cs_new\",\"overlap_seconds\":0,\"previous_secret_expires_at\":null}");
            using var client = Client(handler);
            var out1 = await client.OAuthClients.RotateSecretAsync("oc_1", new RotateOAuthClientSecretBody { OverlapSeconds = 0 }, "idem_1");
            var req = handler.Requests[0];
            Assert.Equal("POST", req.Method);
            Assert.Equal("/v1/oauth_clients/oc_1/rotate_secret", req.Path);
            Assert.Equal("{\"overlap_seconds\":0}", req.Body);
            Assert.Equal("idem_1", req.IdempotencyKey);
            Assert.Equal("cs_new", out1.ClientSecret);
            Assert.Equal(0, out1.OverlapSeconds);
            Assert.Null(out1.PreviousSecretExpiresAt);

            var c2 = MockHttpMessageHandler.Json("{\"object\":\"oauth_client\",\"id\":\"oc_1\",\"overlap_seconds\":86400,\"previous_secret_expires_at\":1800000000000}");
            using var client2 = Client(c2);
            var dflt = await client2.OAuthClients.RotateSecretAsync("oc_1");
            Assert.Equal(1_800_000_000_000L, dflt.PreviousSecretExpiresAt);
        }

        [Fact]
        public async Task DevelopmentEmail_GetEnableDisable()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"development_email\",\"provider_id\":\"mp_1\",\"enabled\":true,\"active\":true,"
                + "\"transport\":\"resend\",\"from_address\":\"onboarding@resend.dev\",\"subject_prefix\":\"[Atlas test] \",\"footer\":\"f\","
                + "\"templates\":[\"magic_link\"],\"limits\":{\"daily\":100,\"per_recipient_hourly\":10},\"updated_at\":5,\"deleted\":true,\"mode\":\"standard\"}");
            using var client = Client(handler);

            var dev = await client.Messaging.GetDevelopmentEmailAsync();
            Assert.Equal("GET", handler.Requests[0].Method);
            Assert.Equal("/v1/messaging_providers/email/development", handler.Requests[0].Path);
            Assert.Equal(10, dev.Limits!.PerRecipientHourly);
            Assert.Equal("[Atlas test] ", dev.SubjectPrefix);

            await client.Messaging.EnableDevelopmentEmailAsync(new DevelopmentEmailBody { ProviderId = "mp_1" });
            Assert.Equal("POST", handler.Requests[1].Method);
            Assert.Equal("{\"provider_id\":\"mp_1\"}", handler.Requests[1].Body);

            var gone = await client.Messaging.DisableDevelopmentEmailAsync();
            Assert.Equal("DELETE", handler.Requests[2].Method);
            Assert.Equal("/v1/messaging_providers/email/development", handler.Requests[2].Path);
            Assert.Equal("standard", gone.Mode);
        }

        [Fact]
        public async Task ManagedEmail_OwnAccount_AndProviderMode()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"managed_email\",\"backend\":\"brevo\",\"provider_id\":\"mp_2\",\"limits\":null,"
                + "\"reputation\":\"your_provider_account\",\"feedback_webhook\":\"registered\",\"id\":\"mp_1\",\"mode\":\"development\",\"channel\":\"email\",\"transport\":\"resend\"}");
            using var client = Client(handler);
            var me = await client.Messaging.CreateManagedEmailAsync(new ManagedEmailBody
            {
                FromAddress = "hi@acme.test",
                Transport = "brevo",
                Config = new ManagedEmailProviderKey { ApiKey = "xkeysib-x" },
            });
            var body = Body(handler.Requests[0]);
            Assert.Equal("brevo", body.GetProperty("transport").GetString());
            Assert.Equal("xkeysib-x", body.GetProperty("config").GetProperty("api_key").GetString());
            Assert.False(body.TryGetProperty("provider_id", out _));
            Assert.Equal("mp_2", me.ProviderId);
            Assert.Equal("brevo", me.Backend);
            Assert.Equal("registered", me.FeedbackWebhook);
            Assert.Null(me.Limits);

            var p = await client.Messaging.SetEmailAsync(new EmailProviderConfig.Resend { ApiKey = "re_x" }, null, "development");
            var put = Body(handler.Requests[1]);
            Assert.Equal("PUT", handler.Requests[1].Method);
            Assert.Equal("development", put.GetProperty("mode").GetString());
            Assert.Equal("mp_1", p.Id);
            Assert.Equal("development", p.Mode);
        }

        [Fact]
        public async Task OriginEntries_AndIssuerFields()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"instance\",\"issuer\":\"https://auth.acme.test\","
                + "\"discovery_url\":\"https://auth.acme.test/.well-known/openid-configuration\",\"jwks_uri\":\"https://auth.acme.test/.well-known/jwks.json\","
                + "\"allowed_origins\":[\"https://app.acme.test\"],\"allowed_origin_integrations\":{\"https://app.acme.test\":\"redirect\"}}");
            using var client = Client(handler);
            var ins = await client.Instance.GetAsync();
            Assert.Equal("https://auth.acme.test", ins.Issuer);
            Assert.EndsWith("jwks.json", ins.JwksUri);
            Assert.Equal("redirect", ins.AllowedOriginIntegrations!["https://app.acme.test"]);

            await client.Instance.UpdateAsync(new UpdateInstanceBody().WithAllowedOriginEntries(new[]
            {
                new AllowedOriginEntry("https://a.acme.test"),
                new AllowedOriginEntry("https://app.acme.test", AllowedOriginEntry.Redirect),
            }));
            Assert.Equal("{\"allowed_origins\":[\"https://a.acme.test\",{\"origin\":\"https://app.acme.test\",\"integration\":\"redirect\"}]}",
                handler.Requests[1].Body);

            var platformBody = AtlasJson.Serialize(new UpdatePlatformAppBody { Name = "x" }
                .WithAllowedOriginEntries(new[] { new AllowedOriginEntry("https://a.acme.test", AllowedOriginEntry.Auto) }));
            Assert.Equal("{\"name\":\"x\",\"allowed_origins\":[{\"origin\":\"https://a.acme.test\",\"integration\":\"auto\"}]}", platformBody);

            var app = AtlasJson.Deserialize<PlatformApp>("{\"frontend_api_host\":\"auth.acme.test\",\"issuer\":\"https://i\",\"discovery_url\":\"https://d\","
                + "\"jwks_uri\":\"https://j\",\"allowed_origin_integrations\":{\"https://a\":\"embedded\"}}")!;
            Assert.Equal("auth.acme.test", app.FrontendApiHost);
            Assert.Equal("embedded", app.AllowedOriginIntegrations!["https://a"]);
        }

        [Fact]
        public void LoginReadiness_Verification_Audiences_AndCodes()
        {
            var lr = AtlasJson.Deserialize<LoginReadiness>("{\"object\":\"login_readiness\",\"ready\":true,"
                + "\"integration\":{\"assumed\":\"redirect\",\"source\":\"declared\",\"reason\":\"r\"},\"checks\":[{\"name\":\"cors\",\"pass\":false,\"applies\":false}]}")!;
            Assert.Equal("redirect", lr.Integration!.Assumed);
            Assert.False(lr.Checks[0].Applies);

            var ea = AtlasJson.Deserialize<EmailAddress>("{\"verified\":false,\"verification\":{\"status\":\"unverified\",\"strategy\":\"skipped\"}}")!;
            Assert.Equal("skipped", ea.Verification!.Strategy);

            var branding = AtlasJson.Serialize(new Branding
            {
                Audiences = new Dictionary<string, BrandingAudience?> { ["partners"] = new BrandingAudience { Heading = "P" }, ["old"] = null },
            });
            Assert.Equal("{\"audiences\":{\"partners\":{\"heading\":\"P\"},\"old\":null}}", branding);

            Assert.Equal("READ_ONLY_FIELD", ErrorCodes.ReadOnlyField);
            Assert.Equal("DEVELOPMENT_EMAIL_NOT_ALLOWED", ErrorCodes.DevelopmentEmailNotAllowed);
            Assert.Equal("DEVELOPMENT_EMAIL_UNAVAILABLE", ErrorCodes.DevelopmentEmailUnavailable);
            Assert.Equal("DEVELOPMENT_EMAIL_LIMIT_REACHED", ErrorCodes.DevelopmentEmailLimitReached);
            Assert.Equal("DEVELOPMENT_EMAIL_TEMPLATE_NOT_ALLOWED", ErrorCodes.DevelopmentEmailTemplateNotAllowed);
        }
    }
}
