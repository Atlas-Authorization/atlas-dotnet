using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Atlas.Verification
{
    /// <summary>
    /// The outcome of <see cref="WebhookVerifier.Verify(byte[], IEnumerable{KeyValuePair{string, string}}, long?)"/>.
    /// Never thrown: check <see cref="Verified"/>.
    /// </summary>
    public sealed class WebhookVerification
    {
        /// <summary>True only when the signature and timestamp both check out.</summary>
        public bool Verified { get; }

        /// <summary>
        /// Why verification failed: <c>missing_headers</c>, <c>malformed_signature</c>,
        /// <c>stale_timestamp</c> or <c>signature_mismatch</c>. Null when verified.
        /// </summary>
        public string? Reason { get; }

        /// <summary>The parsed envelope <c>{ id, type, timestamp, instance_id, data }</c>. Null unless verified.</summary>
        public JsonElement? Event { get; }

        /// <summary>
        /// The signed <c>atlas-id</c> header. The same on every retry of one event, so it is
        /// the key to deduplicate deliveries on. Null unless verified.
        /// </summary>
        public string? DeliveryId { get; }

        /// <summary>The signed <c>atlas-timestamp</c> header (epoch ms). Null unless verified.</summary>
        public long? Timestamp { get; }

        internal WebhookVerification(bool verified, string? reason, JsonElement? evt, string? deliveryId, long? timestamp)
        {
            Verified = verified;
            Reason = reason;
            Event = evt;
            DeliveryId = deliveryId;
            Timestamp = timestamp;
        }

        internal static WebhookVerification Failure(string reason) => new WebhookVerification(false, reason, null, null, null);
    }

    /// <summary>
    /// Verifies an inbound Atlas webhook, for the customer consuming events. Atlas signs every
    /// delivery: <c>atlas-signature: v1,&lt;base64&gt;</c> is HMAC-SHA256 over
    /// <c>&lt;id&gt;.&lt;timestamp&gt;.&lt;raw body&gt;</c> keyed with the endpoint secret (<c>whsec_…</c>),
    /// sent with <c>atlas-id</c> and <c>atlas-timestamp</c> (epoch ms). A delivery more than five
    /// minutes from now, in either direction, is refused. Verify the RAW body, before any JSON parse.
    /// </summary>
    public sealed class WebhookVerifier
    {
        /// <summary>The default accepted clock distance of a delivery, in milliseconds.</summary>
        public const long ReplayToleranceMs = 5 * 60_000;

        private readonly byte[] _secret;
        private readonly long _toleranceMs;

        /// <summary>Create a verifier for an endpoint's signing secret.</summary>
        public WebhookVerifier(string secret, long toleranceMs = ReplayToleranceMs)
        {
            _secret = Encoding.UTF8.GetBytes(secret ?? throw new ArgumentNullException(nameof(secret)));
            _toleranceMs = toleranceMs;
        }

        /// <summary>Verify a delivery. Header names are matched case-insensitively.</summary>
        public WebhookVerification Verify(byte[] rawBody, IEnumerable<KeyValuePair<string, string>> headers, long? nowMs = null)
        {
            var id = Header(headers, "atlas-id");
            var tsRaw = Header(headers, "atlas-timestamp");
            var signature = Header(headers, "atlas-signature");
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(tsRaw) || string.IsNullOrEmpty(signature))
                return WebhookVerification.Failure("missing_headers");

            var comma = signature!.IndexOf(',');
            if (comma < 0 || signature.Substring(0, comma) != "v1" || comma == signature.Length - 1)
                return WebhookVerification.Failure("malformed_signature");
            var provided = signature.Substring(comma + 1);

            if (!long.TryParse(tsRaw!.Trim(), System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture, out var timestamp))
                return WebhookVerification.Failure("stale_timestamp");
            var now = nowMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (Math.Abs(now - timestamp) > _toleranceMs) return WebhookVerification.Failure("stale_timestamp");

            var prefix = Encoding.UTF8.GetBytes(id + "." + timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
            var signed = new byte[prefix.Length + rawBody.Length];
            Buffer.BlockCopy(prefix, 0, signed, 0, prefix.Length);
            Buffer.BlockCopy(rawBody, 0, signed, prefix.Length, rawBody.Length);
            string expected;
            using (var hmac = new HMACSHA256(_secret)) expected = Convert.ToBase64String(hmac.ComputeHash(signed));
            var a = Encoding.UTF8.GetBytes(expected);
            var b = Encoding.UTF8.GetBytes(provided);
            if (a.Length != b.Length || !CryptographicOperations.FixedTimeEquals(a, b))
                return WebhookVerification.Failure("signature_mismatch");

            JsonElement? evt = null;
            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                if (doc.RootElement.ValueKind == JsonValueKind.Object) evt = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // A valid signature over a non-JSON body: verified, with no event.
            }
            return new WebhookVerification(true, null, evt, id, timestamp);
        }

        private static string? Header(IEnumerable<KeyValuePair<string, string>> headers, string name)
        {
            if (headers == null) return null;
            foreach (var pair in headers)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)) return pair.Value;
            }
            return null;
        }
    }
}
