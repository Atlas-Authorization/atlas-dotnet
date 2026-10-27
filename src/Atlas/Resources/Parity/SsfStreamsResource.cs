using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/ssf/streams</c>. Only the fields set are sent.</summary>
    public sealed class SsfStreamsCreateBody
    {
        [JsonPropertyName("delivery_method")] public string? DeliveryMethod { get; set; }
        [JsonPropertyName("endpoint_url")] public string? EndpointUrl { get; set; }
        [JsonPropertyName("authorization_header")] public string? AuthorizationHeader { get; set; }
        [JsonPropertyName("audience")] public string? Audience { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("events_requested")] public List<string>? EventsRequested { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/ssf/streams/{id}</c>. Only the fields set are sent.</summary>
    public sealed class SsfStreamsUpdateBody
    {
        [JsonPropertyName("endpoint_url")] public string? EndpointUrl { get; set; }
        [JsonPropertyName("authorization_header")] public string? AuthorizationHeader { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("events_requested")] public List<string>? EventsRequested { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    /// <summary>The body of <c>POST /v1/ssf/streams/{id}/verify</c>. Only the fields set are sent.</summary>
    public sealed class SsfStreamsVerifyBody
    {
        [JsonPropertyName("state")] public string? State { get; set; }
    }

    /// <summary>The body of <c>POST /v1/ssf/streams/{id}/status</c>. Only the fields set are sent.</summary>
    public sealed class SsfStreamsUpdateStatusBody
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    /// <summary>Operations of <c>ssf.streams</c>.</summary>
    public sealed partial class SsfStreamsResource : ResourceBase
    {
        public SsfStreamsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/ssf/streams</c> - List the Shared Signals streams this instance transmits on (push and poll), with their delivery method, requested events and status.</summary>
        /// <remarks>Needs the <c>ssf:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/ssf/streams", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/ssf/streams</c> - Create a stream. `delivery_method` is `push` (we POST each signed SET to `endpoint_url`, with the optional `authorization_header`) or `poll` (the receiver pulls from the returned `poll_endpoint` with the one-time `poll_token`). `events_requested` limits the events sent (empty = all supported). SETs are signed with the instance signing key.</summary>
        /// <remarks>Needs the <c>ssf:write</c> scope.</remarks>
        public Task CreateAsync(SsfStreamsCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, "/v1/ssf/streams", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/ssf/streams/{id}</c> - Read a stream, including how many events are waiting to be delivered or acknowledged.</summary>
        /// <remarks>Needs the <c>ssf:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/ssf/streams/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/ssf/streams/{id}</c> - Change a stream: its events, endpoint, authorization header, description, or status (`enabled`, `paused`, `disabled`). A paused stream stops receiving new events.</summary>
        /// <remarks>Needs the <c>ssf:write</c> scope.</remarks>
        public Task UpdateAsync(string id, SsfStreamsUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Patch, $"/v1/ssf/streams/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/ssf/streams/{id}</c> - Delete a stream and its queued events.</summary>
        /// <remarks>Needs the <c>ssf:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/ssf/streams/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/ssf/streams/{id}/verify</c> - Queue an SSF verification event carrying `state`, so a receiver can confirm the stream works end to end.</summary>
        /// <remarks>Needs the <c>ssf:write</c> scope.</remarks>
        public Task VerifyAsync(string id, SsfStreamsVerifyBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, $"/v1/ssf/streams/{Enc(id)}/verify", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/ssf/streams/{id}/status</c> - The stream's delivery status: enabled, paused or disabled (SSF stream status).</summary>
        /// <remarks>Needs the <c>ssf:read</c> scope.</remarks>
        public Task<JsonElement> GetStatusAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/ssf/streams/{Enc(id)}/status", cancellationToken: ct);

        /// <summary><c>POST /v1/ssf/streams/{id}/status</c> - Set the stream's delivery status to enabled, paused or disabled. A paused stream collects nothing.</summary>
        /// <remarks>Needs the <c>ssf:write</c> scope.</remarks>
        public Task<JsonElement> UpdateStatusAsync(string id, SsfStreamsUpdateStatusBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/ssf/streams/{Enc(id)}/status", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/ssf/streams/{id}/events</c> - The stream's recent events with delivery status, attempts and the last error (the dashboard activity view).</summary>
        /// <remarks>Needs the <c>ssf:read</c> scope.</remarks>
        public Task<JsonElement> EventsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/ssf/streams/{Enc(id)}/events", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
