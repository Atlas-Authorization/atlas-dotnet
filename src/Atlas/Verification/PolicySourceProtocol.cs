using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Atlas.Verification
{
    /// <summary>Why a policy feed request was refused.</summary>
    public enum PolicyFeedFailure
    {
        MissingHeaders,
        MalformedSignature,
        StaleTimestamp,
        SignatureMismatch,
        WrongMethod,
    }

    /// <summary>
    /// The outcome of <see cref="PolicySourceProtocol.VerifyFeedRequest"/>. When <see cref="Verified"/>, <see cref="Key"/>
    /// is the <c>key</c> query parameter (null for the full feed) and <see cref="ProtocolVersion"/> the
    /// <c>atlas-policies-version</c> header; otherwise <see cref="Failure"/> says why.
    /// </summary>
    public sealed class PolicyFeedVerifyResult
    {
        public bool Verified { get; init; }
        public PolicyFeedFailure? Failure { get; init; }
        public string? DeliveryId { get; init; }
        public long? Timestamp { get; init; }
        public string? Key { get; init; }
        public string? ProtocolVersion { get; init; }
    }

    /// <summary>A signed event: POST <see cref="Body"/> byte-for-byte with <see cref="Headers"/>.</summary>
    public sealed class SignedPolicyEvent
    {
        public string Body { get; init; } = "";
        public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Atlas policy source protocol v1: the helpers for the system that SERVES an instance's policy documents (a CMS,
    /// a docs platform, your own service).
    /// <para>Atlas to you: <c>GET {endpoint_url}/policies</c> (the full feed) or <c>GET {endpoint_url}/policies?key=&lt;key&gt;</c>
    /// (one policy, after your event), signed with the source's signing secret: <c>atlas-id</c>, <c>atlas-timestamp</c>
    /// (epoch milliseconds) and <c>atlas-signature: v1,&lt;base64 HMAC-SHA256&gt;</c> over
    /// <c>{atlas-id}.{atlas-timestamp}.GET\n{path+query}</c>. Verify it with <see cref="VerifyFeedRequest"/>, then answer
    /// <c>{"object":"policy_list","policies":[...]}</c> (at most 512 KB per version).</para>
    /// <para>You to Atlas: <c>POST {webhook_url}</c> (from <c>PolicySource.GetAsync()</c>), an event signed with the same
    /// secret: <see cref="SignPublished"/> / <see cref="SignRetired"/> (Atlas-style headers) or any Standard Webhooks
    /// library. Atlas then re-fetches that policy; it never stores content from the event.</para>
    /// </summary>
    public static class PolicySourceProtocol
    {
        public const string ProtocolVersionValue = "2026-10-24";
        public const string VersionHeader = "atlas-policies-version";
        public const long ToleranceMs = 5 * 60_000L;
        public const string EventPolicyPublished = "policy.published";
        public const string EventPolicyRetired = "policy.retired";

        /// <summary>
        /// Verify Atlas's signed GET of your feed. Pass the request's path AND query exactly as received and the method;
        /// <paramref name="headers"/> looks a header up by lower-case name (null when absent).
        /// </summary>
        public static PolicyFeedVerifyResult VerifyFeedRequest(
            string? method, string pathAndQuery, Func<string, string?> headers, string secret,
            long? nowMs = null, long toleranceMs = ToleranceMs)
        {
            if (method != null && !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)) return Fail(PolicyFeedFailure.WrongMethod);
            var id = headers("atlas-id");
            var ts = headers("atlas-timestamp");
            var sig = headers("atlas-signature");
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(sig)) return Fail(PolicyFeedFailure.MissingHeaders);
            var provided = new List<byte[]>();
            foreach (var entry in sig!.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!entry.StartsWith("v1,", StringComparison.Ordinal)) continue;
                try { provided.Add(Convert.FromBase64String(entry.Substring(3))); }
                catch (FormatException) { provided.Add(new byte[0]); }
            }
            if (provided.Count == 0) return Fail(PolicyFeedFailure.MalformedSignature);
            if (!long.TryParse(ts!.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var timestamp))
                return Fail(PolicyFeedFailure.StaleTimestamp);
            var now = nowMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (Math.Abs(now - timestamp) > toleranceMs) return Fail(PolicyFeedFailure.StaleTimestamp);
            var expected = Mac(secret, id!, ts, "GET\n" + pathAndQuery);
            var matched = false;
            foreach (var p in provided)
            {
                // Compare every entry (no early exit), each in constant time.
                if (p.Length == expected.Length && CryptographicOperations.FixedTimeEquals(p, expected)) matched = true;
            }
            if (!matched) return Fail(PolicyFeedFailure.SignatureMismatch);
            return new PolicyFeedVerifyResult
            {
                Verified = true,
                DeliveryId = id,
                Timestamp = timestamp,
                Key = QueryParam(pathAndQuery, "key"),
                ProtocolVersion = headers(VersionHeader),
            };
        }

        /// <summary>Sign a <c>policy.published</c> event for <c>POST {webhook_url}</c>.</summary>
        public static SignedPolicyEvent SignPublished(string key, string version, string secret, string? id = null, long? timestampMs = null)
            => SignEvent(EventPolicyPublished, key, version, secret, id, timestampMs);

        /// <summary>Sign a <c>policy.retired</c> event for <c>POST {webhook_url}</c>.</summary>
        public static SignedPolicyEvent SignRetired(string key, string secret, string? id = null, long? timestampMs = null)
            => SignEvent(EventPolicyRetired, key, null, secret, id, timestampMs);

        /// <summary>
        /// Sign an event (Atlas-style headers). <paramref name="type"/> is <see cref="EventPolicyPublished"/> (needs a
        /// version) or <see cref="EventPolicyRetired"/>. <paramref name="id"/> defaults to <c>evt_&lt;guid&gt;</c>.
        /// </summary>
        public static SignedPolicyEvent SignEvent(string type, string key, string? version, string secret, string? id = null, long? timestampMs = null)
        {
            if (type != EventPolicyPublished && type != EventPolicyRetired)
                throw new ArgumentException("Unknown policy source event type: " + type, nameof(type));
            if (type == EventPolicyPublished && string.IsNullOrEmpty(version))
                throw new ArgumentException("A policy.published event needs a version.", nameof(version));
            var sb = new StringBuilder("{");
            sb.Append("\"occurred_at\":").Append(JsonSerializer.Serialize(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)));
            sb.Append(",\"type\":").Append(JsonSerializer.Serialize(type));
            sb.Append(",\"key\":").Append(JsonSerializer.Serialize(key));
            if (type == EventPolicyPublished) sb.Append(",\"version\":").Append(JsonSerializer.Serialize(version));
            sb.Append('}');
            return SignBody(sb.ToString(), secret, id, timestampMs);
        }

        /// <summary>Sign an already serialised JSON body.</summary>
        public static SignedPolicyEvent SignBody(string body, string secret, string? id = null, long? timestampMs = null)
        {
            var eventId = id ?? "evt_" + Guid.NewGuid().ToString();
            var ts = (timestampMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).ToString(CultureInfo.InvariantCulture);
            return new SignedPolicyEvent
            {
                Body = body,
                Headers = new Dictionary<string, string>
                {
                    ["atlas-id"] = eventId,
                    ["atlas-timestamp"] = ts,
                    ["atlas-signature"] = "v1," + Convert.ToBase64String(Mac(secret, eventId, ts, body)),
                },
            };
        }

        private static PolicyFeedVerifyResult Fail(PolicyFeedFailure f) => new PolicyFeedVerifyResult { Verified = false, Failure = f };

        private static byte[] Mac(string secret, string id, string timestamp, string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(id + "." + timestamp + "." + payload));
        }

        private static string? QueryParam(string pathAndQuery, string name)
        {
            var q = pathAndQuery.IndexOf('?');
            if (q == -1) return null;
            foreach (var pair in pathAndQuery.Substring(q + 1).Split('&'))
            {
                var eq = pair.IndexOf('=');
                var k = Uri.UnescapeDataString((eq == -1 ? pair : pair.Substring(0, eq)).Replace('+', ' '));
                if (k == name) return eq == -1 ? "" : Uri.UnescapeDataString(pair.Substring(eq + 1).Replace('+', ' '));
            }
            return null;
        }
    }
}
