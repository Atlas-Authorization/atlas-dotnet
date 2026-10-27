using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/iga/requests</c>. Only the fields set are sent.</summary>
    public sealed class IgaRequestsCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("entitlement")] public string? Entitlement { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("requester_user_id")] public string? RequesterUserId { get; set; }
        [JsonPropertyName("justification")] public string? Justification { get; set; }
        [JsonPropertyName("duration_seconds")] public long? DurationSeconds { get; set; }
    }

    /// <summary>The body of <c>POST /v1/iga/requests/{id}/decide</c>. Only the fields set are sent.</summary>
    public sealed class IgaRequestsDecideBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("decision")] public string? Decision { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
        [JsonPropertyName("approver_user_id")] public string? ApproverUserId { get; set; }
    }

    /// <summary>Operations of <c>iga.requests</c>.</summary>
    public sealed partial class IgaRequestsResource : ResourceBase
    {
        public IgaRequestsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/iga/requests</c> - List access requests, newest first.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? status = null, string? userId = null, string? requesterUserId = null, string? entitlement = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/requests", query: Q(("status", status), ("user_id", userId), ("requester_user_id", requesterUserId), ("entitlement", entitlement), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/requests</c> - File an access request on behalf of a user. It follows the entitlement's approval steps (and any extra step a separation-of-duties rule adds); with no steps it is fulfilled at once. A blocking separation-of-duties conflict, a user who already holds the access, or a pending duplicate is a 409. `duration_seconds` makes the grant time-bound (default and maximum: the entitlement's limit).</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(IgaRequestsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/iga/requests", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/iga/requests/{id}</c> - Read one request with its steps and the approvals recorded so far.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/iga/requests/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/iga/requests/{id}/decide</c> - Decide the open step. With `approver_user_id` the decision is recorded as that person (who must be an approver of the step or hold a delegation); without it the call is an administrator override that settles the step. A denial ends the request. 409 when it is no longer pending.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> DecideAsync(string id, IgaRequestsDecideBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/iga/requests/{Enc(id)}/decide", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/iga/requests/{id}/cancel</c> - Cancel a pending request.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> CancelAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/iga/requests/{Enc(id)}/cancel", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/iga/approvals</c> - The requests waiting on one person's decision, including those delegated to them.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> InboxAsync(string approverUserId, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/approvals", query: Q(("approver_user_id", approverUserId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
