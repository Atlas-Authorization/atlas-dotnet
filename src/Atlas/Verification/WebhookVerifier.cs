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
    /// minutes from now, in either direction, is refused. The header may carry several space-separated
    /// <c>v1,&lt;base64&gt;</c> entries (one per secret while a secret rotates with an overlap); the delivery
    /// verifies when any one matches. Verify the RAW body, before any JSON parse.
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
            => VerifyCore(rawBody, headers, null, false, nowMs);

        /// <summary>
        /// Verify a delivery that may carry a v2 signature. An endpoint whose <c>signature_versions</c> include <c>v2</c> signs
        /// HMAC-SHA256 over <c>&lt;id&gt;.&lt;timestamp&gt;.&lt;url&gt;.&lt;body&gt;</c>, where <c>url</c> is the exact URL Atlas POSTed to (your
        /// endpoint's url), so a captured delivery cannot be replayed to another URL. A delivery verifies when any v1 entry (over
        /// <c>id.timestamp.body</c>) or any v2 entry matches; entries of unknown versions are ignored. With
        /// <paramref name="requireV2"/>, only a v2 entry is accepted.
        /// </summary>
        public WebhookVerification VerifyUrl(byte[] rawBody, IEnumerable<KeyValuePair<string, string>> headers, string endpointUrl, bool requireV2 = false, long? nowMs = null)
        {
            if (endpointUrl == null) throw new ArgumentNullException(nameof(endpointUrl));
            return VerifyCore(rawBody, headers, endpointUrl, requireV2, nowMs);
        }

        private WebhookVerification VerifyCore(byte[] rawBody, IEnumerable<KeyValuePair<string, string>> headers, string? endpointUrl, bool requireV2, long? nowMs)
        {
            var id = Header(headers, "atlas-id");
            var tsRaw = Header(headers, "atlas-timestamp");
            var signature = Header(headers, "atlas-signature");
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(tsRaw) || string.IsNullOrEmpty(signature))
                return WebhookVerification.Failure("missing_headers");

            var v1 = requireV2 ? new List<string>() : Signatures(signature!, "v1");
            var v2 = endpointUrl != null ? Signatures(signature!, "v2") : new List<string>();
            if (v1.Count == 0 && v2.Count == 0)
                return WebhookVerification.Failure("malformed_signature");

            if (!long.TryParse(tsRaw!.Trim(), System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture, out var timestamp))
                return WebhookVerification.Failure("stale_timestamp");
            var now = nowMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (Math.Abs(now - timestamp) > _toleranceMs) return WebhookVerification.Failure("stale_timestamp");

            var ts = timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var matched = false;
            if (v1.Count > 0) matched |= AnyMatches(Sign(id + "." + ts + ".", rawBody), v1);
            if (v2.Count > 0) matched |= AnyMatches(Sign(id + "." + ts + "." + endpointUrl + ".", rawBody), v2);
            if (!matched)
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

        private string Sign(string prefixText, byte[] rawBody)
        {
            var prefix = Encoding.UTF8.GetBytes(prefixText);
            var signed = new byte[prefix.Length + rawBody.Length];
            Buffer.BlockCopy(prefix, 0, signed, 0, prefix.Length);
            Buffer.BlockCopy(rawBody, 0, signed, prefix.Length, rawBody.Length);
            using var hmac = new HMACSHA256(_secret);
            return Convert.ToBase64String(hmac.ComputeHash(signed));
        }

        // Compare every entry (no early exit), each in constant time.
        private static bool AnyMatches(string expected, List<string> provided)
        {
            var a = Encoding.UTF8.GetBytes(expected);
            var matched = false;
            foreach (var candidate in provided)
            {
                var b = Encoding.UTF8.GetBytes(candidate);
                if (a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b)) matched = true;
            }
            return matched;
        }

        // The signatures of one version in an atlas-signature header: one "v1,<base64>" entry, or several separated by
        // spaces (while an endpoint secret rotates, Atlas signs with the old and the new secret). Entries of
        // another version are skipped.
        internal static List<string> Signatures(string header, string version)
        {
            var list = new List<string>();
            foreach (var entry in header.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var comma = entry.IndexOf(',');
                if (comma > 0 && comma < entry.Length - 1 && entry.Substring(0, comma) == version)
                    list.Add(entry.Substring(comma + 1));
            }
            return list;
        }

        internal static string? Header(IEnumerable<KeyValuePair<string, string>> headers, string name)
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
