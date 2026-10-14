using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Request-shape tests for the typed messaging provider config.</summary>
    public class MessagingTests
    {
        private const string Provider =
            "{\"object\":\"messaging_provider\",\"channel\":\"email\",\"transport\":\"smtp\",\"from_address\":null," +
            "\"status\":\"active\",\"config\":{\"host\":\"smtp.acme.test\"},\"secrets_set\":{\"password\":true},\"updated_at\":1}";

        private static AtlasClient Client(MockHttpMessageHandler handler) =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

        private static Dictionary<string, JsonElement> Config(JsonElement body)
        {
            var d = new Dictionary<string, JsonElement>();
            foreach (var p in body.GetProperty("config").EnumerateObject()) d[p.Name] = p.Value;
            return d;
        }

        [Fact]
        public async Task SetEmail_Typed_SendsTransportAndExactKeys()
        {
            var handler = MockHttpMessageHandler.Json(Provider);
            using var client = Client(handler);

            var result = await client.Messaging.SetEmailAsync(
                new EmailProviderConfig.Smtp { Host = "smtp.acme.test", Port = 465, Secure = true, Password = "p" },
                fromAddress: "Acme <no-reply@acme.test>");

            Assert.True(result.SecretsSet["password"]);
            var req = Assert.Single(handler.Requests);
            Assert.Equal("PUT", req.Method);
            Assert.Equal("/v1/messaging_providers/email", req.Path);
            using var doc = JsonDocument.Parse(req.Body);
            Assert.Equal("smtp", doc.RootElement.GetProperty("transport").GetString());
            Assert.Equal("Acme <no-reply@acme.test>", doc.RootElement.GetProperty("from_address").GetString());
            var config = Config(doc.RootElement);
            Assert.Equal(new[] { "host", "password", "port", "secure" }, Sorted(config.Keys));
            Assert.Equal(465, config["port"].GetInt32());
            Assert.True(config["secure"].GetBoolean());
        }

        [Fact]
        public async Task SetEmail_Update_OmitsUnsetKeys_SoStoredSecretIsKept()
        {
            var handler = MockHttpMessageHandler.Json(Provider);
            using var client = Client(handler);

            await client.Messaging.SetEmailAsync(new EmailProviderConfig.Mailgun { Region = MailgunRegion.Eu });

            using var doc = JsonDocument.Parse(handler.Requests[0].Body);
            Assert.Equal("mailgun", doc.RootElement.GetProperty("transport").GetString());
            Assert.False(doc.RootElement.TryGetProperty("from_address", out _));
            Assert.Equal(new[] { "region" }, Sorted(Config(doc.RootElement).Keys));
        }

        [Fact]
        public async Task SetSms_Typed_SendsToSmsPath()
        {
            var handler = MockHttpMessageHandler.Json(Provider);
            using var client = Client(handler);

            await client.Messaging.SetSmsAsync(
                new SmsProviderConfig.Twilio { AccountSid = "AC1", AuthToken = "t", From = "+15551234567" });

            var req = handler.Requests[0];
            Assert.Equal("/v1/messaging_providers/sms", req.Path);
            using var doc = JsonDocument.Parse(req.Body);
            Assert.Equal("twilio", doc.RootElement.GetProperty("transport").GetString());
            Assert.Equal(new[] { "account_sid", "auth_token", "from" }, Sorted(Config(doc.RootElement).Keys));
        }

        [Fact]
        public async Task SetEmail_UntypedInput_StillWorks()
        {
            var handler = MockHttpMessageHandler.Json(Provider);
            using var client = Client(handler);

            await client.Messaging.SetEmailAsync(new MessagingProviderInput
            {
                Transport = "resend",
                Config = new Dictionary<string, object?> { ["api_key"] = "re_x" },
            });

            using var doc = JsonDocument.Parse(handler.Requests[0].Body);
            Assert.Equal("resend", doc.RootElement.GetProperty("transport").GetString());
            Assert.Equal("re_x", Config(doc.RootElement)["api_key"].GetString());
        }

        private static string[] Sorted(IEnumerable<string> keys)
        {
            var list = new List<string>(keys);
            list.Sort(System.StringComparer.Ordinal);
            return list.ToArray();
        }
    }
}
