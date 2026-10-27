using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/risk_based_mfa/reputation_secret</c>. Only the fields set are sent.</summary>
    public sealed class RiskBasedMfaSetReputationSecretBody
    {
        [JsonPropertyName("secret")] public string? Secret { get; set; }
        [JsonPropertyName("provider")] public string? Provider { get; set; }
    }

    /// <summary>More operations of <c>riskBasedMfa</c>.</summary>
    public sealed partial class RiskBasedMfaResource
    {
        /// <summary><c>PUT /v1/risk_based_mfa/reputation_secret</c> - Store (write-only, encrypted) the API key of your IP-reputation provider (ipqs, spur or minfraud; minFraud uses "&lt;account id&gt;:&lt;license key&gt;"). Then set `reputation.provider` on PATCH /v1/risk_based_mfa. Lookups are cached and fail open: a provider outage never blocks a sign-in.</summary>
        /// <remarks>Needs the <c>risk_based_mfa:write</c> scope.</remarks>
        public Task<JsonElement> SetReputationSecretAsync(RiskBasedMfaSetReputationSecretBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/risk_based_mfa/reputation_secret", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/risk_based_mfa/reputation_secret</c> - Remove a stored IP-reputation provider key (default: the configured provider). IP lookups for that provider stop; sign-in is unaffected.</summary>
        /// <remarks>Needs the <c>risk_based_mfa:write</c> scope.</remarks>
        public Task<JsonElement> RemoveReputationSecretAsync(bool? dryRun = null, string? provider = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, "/v1/risk_based_mfa/reputation_secret", query: Q(("dry_run", dryRun), ("provider", provider)), cancellationToken: ct);
    }
}
