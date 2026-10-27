using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/relay_agents</c>. Only the fields set are sent.</summary>
    public sealed class RelayAgentsCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("organization_id")] public string? OrganizationId { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        /// <summary>host:port destinations the agent may reach.</summary>
        [JsonPropertyName("allowed_destinations")] public List<string>? AllowedDestinations { get; set; }
        /// <summary>Client-certificate pins.</summary>
        [JsonPropertyName("cert_fingerprints")] public List<string>? CertFingerprints { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/relay_agents/{id}</c>. Only the fields set are sent.</summary>
    public sealed class RelayAgentsUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("organization_id")] public string? OrganizationId { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        /// <summary>host:port destinations the agent may reach.</summary>
        [JsonPropertyName("allowed_destinations")] public List<string>? AllowedDestinations { get; set; }
        /// <summary>Client-certificate pins.</summary>
        [JsonPropertyName("cert_fingerprints")] public List<string>? CertFingerprints { get; set; }
    }

    /// <summary>Operations of <c>relayAgents</c>.</summary>
    public sealed partial class RelayAgentsResource : ResourceBase
    {
        public RelayAgentsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/relay_agents</c> - List the on-prem relay agents with their allowed destinations and whether each is online. Secrets are never returned.</summary>
        /// <remarks>Needs the <c>relay_agents:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/relay_agents", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/relay_agents</c> - Register an outbound-only relay agent. The response carries the agent token once; it cannot be read back. An agent can reach only the destinations in allowed_destinations (host, host:port, *.domain).</summary>
        /// <remarks>Needs the <c>relay_agents:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(RelayAgentsCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/relay_agents", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/relay_agents/{id}</c> - Read one relay agent, including when it last polled.</summary>
        /// <remarks>Needs the <c>relay_agents:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/relay_agents/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/relay_agents/{id}</c> - Rename an agent, change its allowed destinations or pinned client-certificate thumbprints, or revoke it (status: revoked).</summary>
        /// <remarks>Needs the <c>relay_agents:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, RelayAgentsUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/relay_agents/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/relay_agents/{id}</c> - Delete a relay agent and its queued jobs.</summary>
        /// <remarks>Needs the <c>relay_agents:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/relay_agents/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/relay_agents/{id}/rotate_secret</c> - Issue a new agent token (returned once). The old token stops working immediately.</summary>
        /// <remarks>Needs the <c>relay_agents:write</c> scope.</remarks>
        public Task<JsonElement> RotateSecretAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/relay_agents/{Enc(id)}/rotate_secret", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
