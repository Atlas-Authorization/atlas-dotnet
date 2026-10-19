using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Atlas.Verification;
using Xunit;

namespace Atlas.Tests
{
    public class WebhookVerifierTests
    {
        private const string Secret = "whsec_test_secret";
        private const long Now = 1_757_937_600_000;
        private const string Body = "{\"id\":\"evt_1\",\"type\":\"user.updated\",\"data\":{\"id\":\"user_1\"}}";

        private static Dictionary<string, string> Sign(string id, long ts, string body, string secret = Secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var sig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{id}.{ts}.{body}")));
            return new Dictionary<string, string>
            {
                ["Atlas-Id"] = id,
                ["Atlas-Timestamp"] = ts.ToString(),
                ["Atlas-Signature"] = "v1," + sig,
            };
        }

        private static WebhookVerification Verify(string body, Dictionary<string, string> headers) =>
            new WebhookVerifier(Secret).Verify(Encoding.UTF8.GetBytes(body), headers, Now);

        [Fact]
        public void ReturnsTheDeliveryIdAndTimestamp()
        {
            var r = Verify(Body, Sign("evt_1", Now - 1000, Body));
            Assert.True(r.Verified);
            Assert.Equal("evt_1", r.DeliveryId);
            Assert.Equal(Now - 1000, r.Timestamp);
            Assert.Equal("user.updated", r.Event!.Value.GetProperty("type").GetString());
        }

        [Fact]
        public void Rejections()
        {
            var tampered = Verify(Body.Replace("user_1", "user_2"), Sign("evt_1", Now, Body));
            Assert.False(tampered.Verified);
            Assert.Equal("signature_mismatch", tampered.Reason);
            Assert.Null(tampered.DeliveryId);
            Assert.Equal("signature_mismatch", Verify(Body, Sign("evt_1", Now, Body, "whsec_x")).Reason);
            Assert.Equal("stale_timestamp", Verify(Body, Sign("evt_1", Now - 360_000, Body)).Reason);
            Assert.Equal("stale_timestamp", Verify(Body, Sign("evt_1", Now + 360_000, Body)).Reason);
            Assert.Equal("missing_headers", Verify(Body, new Dictionary<string, string>()).Reason);
            var bad = Sign("evt_1", Now, Body);
            bad["Atlas-Signature"] = "v2,abc";
            Assert.Equal("malformed_signature", Verify(Body, bad).Reason);
        }
    }
}
