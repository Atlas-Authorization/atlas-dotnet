using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/agent_access_requests/{id}/approve</c>. Only the fields set are sent.</summary>
    public sealed class AgentAccessRequestsApproveBody
    {
        [JsonPropertyName("expires_in_seconds")] public long? ExpiresInSeconds { get; set; }
        [JsonPropertyName("decided_by")] public string? DecidedBy { get; set; }
    }

    /// <summary>The body of <c>POST /v1/agent_access_requests/{id}/deny</c>. Only the fields set are sent.</summary>
    public sealed class AgentAccessRequestsDenyBody
    {
        [JsonPropertyName("decided_by")] public string? DecidedBy { get; set; }
    }

    /// <summary>Operations of <c>agentAccessRequests</c>.</summary>
    public sealed partial class AgentAccessRequestsResource : ResourceBase
    {
        public AgentAccessRequestsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/agent_access_requests/{id}/approve</c> - Approve a pending request, optionally for a limited time (expires_in_seconds). 409 when it is no longer pending. Emits agent.access_decided.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task ApproveAsync(string id, AgentAccessRequestsApproveBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, $"/v1/agent_access_requests/{Enc(id)}/approve", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/agent_access_requests/{id}/deny</c> - Deny a pending request. 409 when it is no longer pending. Emits agent.access_decided.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task DenyAsync(string id, AgentAccessRequestsDenyBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, $"/v1/agent_access_requests/{Enc(id)}/deny", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/agent_access_requests</c> - List agent access requests, newest first. Filter by status (pending, approved, denied, revoked) or agent_id.</summary>
        /// <remarks>Needs the <c>machines:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? status = null, string? agentId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/agent_access_requests", query: Q(("status", status), ("agent_id", agentId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
