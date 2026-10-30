using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Atlas.Verification
{
    /// <summary>
    /// Verifies an APPLICATION webhook (the events an app publishes to its customers' endpoints through
    /// <see cref="Atlas.Resources.AppEventsResource"/>): Standard Webhooks. Headers <c>webhook-id</c>,
    /// <c>webhook-timestamp</c> (epoch seconds) and <c>webhook-signature</c> (space-separated <c>v1,&lt;base64&gt;</c>
    /// entries, one per secret during a rotation); HMAC-SHA256 over <c>&lt;id&gt;.&lt;timestamp&gt;.&lt;raw body&gt;</c> keyed with
    /// the base64-decoded part of <c>whsec_&lt;base64&gt;</c>. Any Standard Webhooks library verifies the same deliveries.
    /// Returns the same <see cref="WebhookVerification"/> as <see cref="WebhookVerifier"/>, with <c>Timestamp</c> in seconds
    /// and <c>DeliveryId</c> the <c>webhook-id</c> (stable across retries: dedupe on it). Verify the RAW body.
    /// </summary>
    public sealed class StandardWebhookVerifier
    {
        /// <summary>The default accepted clock distance of a delivery, in seconds.</summary>
        public const long ToleranceSeconds = 300;

        private readonly byte[]? _key;
        private readonly long _toleranceSec;

        /// <summary>Create a verifier for an endpoint's <c>whsec_</c> signing secret.</summary>
        public StandardWebhookVerifier(string secret, long toleranceSec = ToleranceSeconds)
        {
            if (secret == null) throw new ArgumentNullException(nameof(secret));
            var encoded = secret.StartsWith("whsec_", StringComparison.Ordinal) ? secret.Substring(6) : secret;
            try
            {
                _key = Convert.FromBase64String(encoded);
            }
            catch (FormatException)
            {
                _key = null; // a key that is not base64 can never have produced a valid signature
            }
            _toleranceSec = toleranceSec;
        }

        /// <summary>Verify a delivery. Header names are matched case-insensitively.</summary>
        public WebhookVerification Verify(byte[] rawBody, IEnumerable<KeyValuePair<string, string>> headers, long? nowSec = null)
        {
            var id = WebhookVerifier.Header(headers, "webhook-id");
            var tsRaw = WebhookVerifier.Header(headers, "webhook-timestamp");
            var signature = WebhookVerifier.Header(headers, "webhook-signature");
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(tsRaw) || string.IsNullOrEmpty(signature))
                return WebhookVerification.Failure("missing_headers");

            var provided = WebhookVerifier.Signatures(signature!, "v1");
            if (provided.Count == 0) return WebhookVerification.Failure("malformed_signature");

            if (!long.TryParse(tsRaw!.Trim(), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var timestamp))
                return WebhookVerification.Failure("stale_timestamp");
            var now = nowSec ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - timestamp) > _toleranceSec) return WebhookVerification.Failure("stale_timestamp");
            if (_key == null) return WebhookVerification.Failure("signature_mismatch");

            var prefix = Encoding.UTF8.GetBytes(id + "." + timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
            var signed = new byte[prefix.Length + rawBody.Length];
            Buffer.BlockCopy(prefix, 0, signed, 0, prefix.Length);
            Buffer.BlockCopy(rawBody, 0, signed, prefix.Length, rawBody.Length);
            byte[] expected;
            using (var hmac = new HMACSHA256(_key)) expected = Encoding.UTF8.GetBytes(Convert.ToBase64String(hmac.ComputeHash(signed)));

            // Compare every entry (no early exit), each in constant time.
            var matched = false;
            foreach (var candidate in provided)
            {
                var b = Encoding.UTF8.GetBytes(candidate);
                if (expected.Length == b.Length && CryptographicOperations.FixedTimeEquals(expected, b)) matched = true;
            }
            if (!matched) return WebhookVerification.Failure("signature_mismatch");

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
    }
}
