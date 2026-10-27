using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/agent_registration</c>. Only the fields set are sent.</summary>
    public sealed class AgentRegistrationReplaceBody
    {
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("allow_anonymous")] public bool? AllowAnonymous { get; set; }
        [JsonPropertyName("allow_service_auth")] public bool? AllowServiceAuth { get; set; }
        [JsonPropertyName("allow_refresh")] public bool? AllowRefresh { get; set; }
        [JsonPropertyName("untrusted_scopes")] public List<string>? UntrustedScopes { get; set; }
        [JsonPropertyName("trusted_scopes")] public List<string>? TrustedScopes { get; set; }
        [JsonPropertyName("claim_ttl_seconds")] public long? ClaimTtlSeconds { get; set; }
        /// <summary>Access requests from a trusted agent matching a rule are approved automatically.</summary>
        [JsonPropertyName("auto_approve_rules")] public List<object?>? AutoApproveRules { get; set; }
    }

    /// <summary>Operations of <c>agentRegistration</c>.</summary>
    public sealed partial class AgentRegistrationResource : ResourceBase
    {
        public AgentRegistrationResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/agent_registration</c> - Read the agent registration settings (enabled, allowed identity kinds, untrusted and trusted scopes, claim code lifetime).</summary>
        /// <remarks>Needs the <c>machines:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/agent_registration", cancellationToken: ct);

        /// <summary><c>PUT /v1/agent_registration</c> - Update the agent registration settings. Partial: only the fields sent change.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task ReplaceAsync(AgentRegistrationReplaceBody body, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Put, "/v1/agent_registration", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
