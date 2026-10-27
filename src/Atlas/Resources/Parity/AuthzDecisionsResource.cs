using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Operations of <c>authzDecisions</c>.</summary>
    public sealed partial class AuthzDecisionsResource : ResourceBase
    {
        public AuthzDecisionsResource(AtlasTransport t) : base(t) { }

        private AuthzDecisionsSettingsResource? _settings;
        /// <summary>The <c>authzDecisions.settings</c> operations.</summary>
        public AuthzDecisionsSettingsResource Settings => _settings ??= new AuthzDecisionsSettingsResource(Transport);

        /// <summary><c>GET /v1/authz_decisions</c> - Query the authorization decision log, newest first: ABAC, AuthZEN, FGA-check and access-policy decisions with their redacted input, result, the policy revision in force and latency. Only decisions that passed sampling are stored.</summary>
        /// <remarks>Needs the <c>authz_decisions:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? source = null, string? decision = null, string? principal = null, string? action = null, string? resource = null, string? policyRevision = null, string? since = null, string? until = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/authz_decisions", query: Q(("source", source), ("decision", decision), ("principal", principal), ("action", action), ("resource", resource), ("policy_revision", policyRevision), ("since", since), ("until", until), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/authz_decisions/stats</c> - Decision counts per engine and outcome over a window (the last 24 hours by default), with a sampling-corrected estimate of the true volume and latency percentiles of the recorded decisions.</summary>
        /// <remarks>Needs the <c>authz_decisions:read</c> scope.</remarks>
        public Task<JsonElement> StatsAsync(string? since = null, string? until = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/authz_decisions/stats", query: Q(("since", since), ("until", until)), cancellationToken: ct);

        /// <summary><c>GET /v1/authz_decisions/{id}</c> - Read one logged decision.</summary>
        /// <remarks>Needs the <c>authz_decisions:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/authz_decisions/{Enc(id)}", cancellationToken: ct);
    }
}
