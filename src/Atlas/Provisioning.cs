using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Atlas
{
    /// <summary>
    /// Provisioning helpers: metadata list filters, dry runs and W3C trace context. The create bodies of
    /// provisioning resources (apps, instances, secret keys, OAuth clients, webhook endpoints, Sign in with
    /// Atlas apps, integrations) take <c>Metadata</c> and <c>ExternalRef</c>; their responses carry
    /// <c>Metadata</c>, <c>ExternalRef</c> and <c>IdempotentReplay</c>.
    /// </summary>
    public static class AtlasProvisioning
    {
        /// <summary>
        /// Turn a metadata filter into the <c>metadata[key]=value</c> query parameters the provisioning lists accept.
        /// Keys are sorted for stable URLs.
        /// </summary>
        public static List<KeyValuePair<string, object?>> MetadataQuery(IDictionary<string, object?>? filter)
        {
            var list = new List<KeyValuePair<string, object?>>();
            if (filter == null) return list;
            var keys = new List<string>(filter.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (var k in keys)
            {
                var v = filter[k];
                list.Add(new KeyValuePair<string, object?>("metadata[" + k + "]", v is bool b ? (b ? "true" : "false") : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)));
            }
            return list;
        }
    }

    /// <summary>One change a write would make (config-changelog shape).</summary>
    public sealed class DryRunChange
    {
        public string Section { get; init; } = "";
        public JsonElement? Before { get; init; }
        public JsonElement? After { get; init; }
    }

    /// <summary>What a write answers with <c>?dry_run=true</c>: the changes it would make, nothing changed.</summary>
    public sealed class DryRunResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "dry_run";
        public bool DryRun { get; init; }
        /// <summary>
        /// <c>native</c> (the endpoint's own dry run, validated in full), <c>rollback</c> (the real write ran in a transaction that
        /// was rolled back, every side effect held back, validated in full) or <c>plan</c> (validation could not run to the end;
        /// see <see cref="ValidationSkipped"/>).
        /// </summary>
        public string Mode { get; init; } = "";
        public bool Validated { get; init; }
        public string Method { get; init; } = "";
        public string Path { get; init; } = "";
        public List<DryRunChange> Changes { get; init; } = new List<DryRunChange>();
        public JsonElement? WouldReturn { get; init; }
        /// <summary>The same non-fatal warnings the real write returns (each has a <c>code</c>, e.g. <c>origin_still_referenced</c>).</summary>
        public List<JsonElement>? Warnings { get; init; }
        /// <summary>The status the real write would answer with (<c>rollback</c> mode).</summary>
        public int? WouldReturnStatus { get; init; }
        /// <summary>Side effects held back: <c>email</c>, <c>sms</c>, <c>voice</c>, <c>push</c>, <c>outbound_network</c>, <c>redis</c>, <c>cache</c>, <c>file</c>.</summary>
        public List<string>? Suppressed { get; init; }
        /// <summary><c>plan</c> mode: why validation did not run to the end.</summary>
        public DryRunValidationSkipped? ValidationSkipped { get; init; }
        public string Note { get; init; } = "";
    }

    /// <summary>Why a plan-mode dry run did not validate to the end: <c>Reason</c> is external_call, database or unsupported_response.</summary>
    public sealed class DryRunValidationSkipped
    {
        public string Reason { get; init; } = "";
        public string Detail { get; init; } = "";
    }

    /// <summary>W3C trace context: mint and read <c>traceparent</c> headers.</summary>
    public static class AtlasTrace
    {
        private static readonly Regex Pattern = new Regex("^[0-9a-f]{2}-([0-9a-f]{32})-([0-9a-f]{16})-([0-9a-f]{2})$", RegexOptions.Compiled);

        /// <summary>Mint a fresh sampled W3C traceparent (<c>00-&lt;32 hex&gt;-&lt;16 hex&gt;-01</c>).</summary>
        public static string NewTraceparent()
        {
            var buf = new byte[24];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(buf);
            // All-zero ids are invalid; nudge the last byte of each.
            if (AllZero(buf, 0, 16)) buf[15] = 1;
            if (AllZero(buf, 16, 8)) buf[23] = 1;
            return "00-" + Hex(buf, 0, 16) + "-" + Hex(buf, 16, 8) + "-01";
        }

        /// <summary>Split a W3C traceparent into its trace id, span id and flags; false when it is malformed.</summary>
        public static bool TryParseTraceparent(string? traceparent, out string traceId, out string spanId, out string flags)
        {
            traceId = spanId = flags = "";
            if (traceparent == null) return false;
            var m = Pattern.Match(traceparent.Trim().ToLowerInvariant());
            if (!m.Success) return false;
            if (m.Groups[1].Value == new string('0', 32) || m.Groups[2].Value == new string('0', 16)) return false;
            traceId = m.Groups[1].Value;
            spanId = m.Groups[2].Value;
            flags = m.Groups[3].Value;
            return true;
        }

        /// <summary>
        /// Read the traceparent Atlas sent (an executor or orchestrator receiver) from request headers, matched
        /// case-insensitively; null when absent or malformed.
        /// </summary>
        public static string? TraceparentFromHeaders(IEnumerable<KeyValuePair<string, string>>? headers)
        {
            if (headers == null) return null;
            foreach (var h in headers)
            {
                if (!string.Equals(h.Key, "traceparent", StringComparison.OrdinalIgnoreCase)) continue;
                var v = (h.Value ?? "").Trim();
                return TryParseTraceparent(v, out _, out _, out _) ? v : null;
            }
            return null;
        }

        private static bool AllZero(byte[] b, int offset, int count)
        {
            for (var i = 0; i < count; i++) if (b[offset + i] != 0) return false;
            return true;
        }

        private static string Hex(byte[] b, int offset, int count)
        {
            var sb = new System.Text.StringBuilder(count * 2);
            for (var i = 0; i < count; i++) sb.Append(b[offset + i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
