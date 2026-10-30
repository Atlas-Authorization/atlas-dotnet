using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Atlas.Verification;
using Xunit;

namespace Atlas.Tests
{
    public class StandardWebhookVerifierTests
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("0123456789abcdefghijklmnop");
        private static readonly string Secret = "whsec_" + Convert.ToBase64String(Key);
        private const long Now = 1_757_937_600;
        private const string Body = "{\"id\":\"evt_1\",\"type\":\"user.updated\",\"data\":{\"id\":\"user_1\"}}";

        private static string Sig(byte[] key, long ts, string body)
        {
            using var hmac = new HMACSHA256(key);
            return "v1," + Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes($"msg_1.{ts}.{body}")));
        }

        private static Dictionary<string, string> Headers(string sig, string? ts = null) => new Dictionary<string, string>
        {
            ["Webhook-Id"] = "msg_1",
            ["Webhook-Timestamp"] = ts ?? Now.ToString(),
            ["Webhook-Signature"] = sig,
        };

        private static WebhookVerification Verify(string body, Dictionary<string, string> headers, long now = Now) =>
            new StandardWebhookVerifier(Secret).Verify(Encoding.UTF8.GetBytes(body), headers, now);

        [Fact]
        public void VerifiesAndReturnsIdTimestampAndEvent()
        {
            var r = Verify(Body, Headers(Sig(Key, Now, Body)));
            Assert.True(r.Verified);
            Assert.Equal("msg_1", r.DeliveryId);
            Assert.Equal(Now, r.Timestamp);
            Assert.Equal("user.updated", r.Event!.Value.GetProperty("type").GetString());
        }

        [Fact]
        public void AnyEntryOfARotatedListVerifies()
        {
            var old = Sig(Encoding.UTF8.GetBytes("old-key"), Now, Body);
            Assert.True(Verify(Body, Headers(old + " " + Sig(Key, Now, Body))).Verified);
            Assert.Equal("signature_mismatch", Verify(Body, Headers(old)).Reason);
        }

        [Fact]
        public void Rejections()
        {
            var good = Sig(Key, Now, Body);
            Assert.Equal("signature_mismatch", Verify(Body + " ", Headers(good)).Reason);
            Assert.Equal("stale_timestamp", Verify(Body, Headers(good), Now + 301).Reason);
            Assert.Equal("stale_timestamp", Verify(Body, Headers(good, "abc")).Reason);
            Assert.Equal("missing_headers", Verify(Body, new Dictionary<string, string>()).Reason);
            Assert.Equal("malformed_signature", Verify(Body, Headers("v2,abc")).Reason);
            Assert.Equal("signature_mismatch", new StandardWebhookVerifier("whsec_!!notbase64").Verify(Encoding.UTF8.GetBytes(Body), Headers(good), Now).Reason);
        }
    }
}
