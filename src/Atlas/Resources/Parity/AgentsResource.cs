using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/agents/{id}/claim</c>. Only the fields set are sent.</summary>
    public sealed class AgentsClaimBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/agents/{id}/owner</c>. Only the fields set are sent.</summary>
    public sealed class AgentsSetOwnerBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
    }

    /// <summary>The body of <c>POST /v1/agents/{id}/revoke_access</c>. Only the fields set are sent.</summary>
    public sealed class AgentsRevokeAccessBody
    {
        [JsonPropertyName("disable")] public bool? Disable { get; set; }
    }

    /// <summary>Operations of <c>agents</c>.</summary>
    public sealed partial class AgentsResource : ResourceBase
    {
        public AgentsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/agents/{id}/claim</c> - Backend-assisted claim: bind an agent to a user you already know and promote it to trusted. Audited as agent.claimed.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task ClaimAsync(string id, AgentsClaimBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, $"/v1/agents/{Enc(id)}/claim", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/agents</c> - The agent registry: every registered agent with its tier, owner and status. Filter by owner_user_id or tier; page with limit and starting_after.</summary>
        /// <remarks>Needs the <c>machines:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? ownerUserId = null, string? tier = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/agents", query: Q(("owner_user_id", ownerUserId), ("tier", tier), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/agents/{id}</c> - One agent with its currently valid access grants.</summary>
        /// <remarks>Needs the <c>machines:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/agents/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PUT /v1/agents/{id}/owner</c> - Set the owning user of an agent, or null to remove the owner (which also drops the agent back to untrusted).</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task SetOwnerAsync(string id, AgentsSetOwnerBody body, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Put, $"/v1/agents/{Enc(id)}/owner", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/agents/{id}/revoke_access</c> - Revoke every grant and cancel every pending request of an agent. With disable: true the agent itself is revoked and can no longer get tokens. Emits agent.access_revoked.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task RevokeAccessAsync(string id, AgentsRevokeAccessBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, $"/v1/agents/{Enc(id)}/revoke_access", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
