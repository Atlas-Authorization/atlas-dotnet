using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/authz_decision_settings</c>. Only the fields set are sent.</summary>
    public sealed class AuthzDecisionsSettingsUpdateBody
    {
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("sample_rate_allow")] public double? SampleRateAllow { get; set; }
        [JsonPropertyName("sample_rate_deny")] public double? SampleRateDeny { get; set; }
        [JsonPropertyName("input_mode")] public string? InputMode { get; set; }
        [JsonPropertyName("redact")] public List<string>? Redact { get; set; }
        [JsonPropertyName("ship_to_events")] public bool? ShipToEvents { get; set; }
        [JsonPropertyName("retention_days")] public long? RetentionDays { get; set; }
    }

    /// <summary>Operations of <c>authzDecisions.settings</c>.</summary>
    public sealed partial class AuthzDecisionsSettingsResource : ResourceBase
    {
        public AuthzDecisionsSettingsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/authz_decision_settings</c> - How authorization decisions are captured: on or off, sampling per outcome, how much of each request is kept, redaction paths, event shipping and retention.</summary>
        /// <remarks>Needs the <c>authz_decisions:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/authz_decision_settings", cancellationToken: ct);

        /// <summary><c>PUT /v1/authz_decision_settings</c> - Change how decisions are captured. Fields you omit keep their value. Capture is off by default; `input_mode` defaults to `ids` (identifiers only) so nothing personal is stored until you ask for it. With `ship_to_events` each recorded decision is also an `authz.decision` event for your log streams and webhooks.</summary>
        /// <remarks>Needs the <c>authz_decisions:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(AuthzDecisionsSettingsUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/authz_decision_settings", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
