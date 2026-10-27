using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/listen/sessions</c>. Only the fields set are sent.</summary>
    public sealed class ListenCreateSessionBody
    {
        /// <summary>Event types or patterns to forward, such as `user.created` or `user.*`. Omit for every event.</summary>
        [JsonPropertyName("events")] public List<string>? Events { get; set; }
        /// <summary>How long the session lives. Default 3600.</summary>
        [JsonPropertyName("ttl_seconds")] public long? TtlSeconds { get; set; }
    }

    /// <summary>Operations of <c>listen</c>.</summary>
    public sealed partial class ListenResource : ResourceBase
    {
        public ListenResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/listen/sessions</c> - Start a local webhook forwarding session (what `atlas listen` uses). Returns a session id, a signing secret for that session only (it is unrelated to any registered endpoint's secret) and the stream URL. The session needs no storage: it expires at `expires_at` (default 1 hour, `ttl_seconds` 60 to 43200).</summary>
        /// <remarks>Needs the <c>webhooks:write</c> scope.</remarks>
        public Task<JsonElement> CreateSessionAsync(ListenCreateSessionBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/listen/sessions", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/listen/sessions/{id}/stream</c> - Server-Sent Events stream of the instance's new events for a listen session. Each `webhook` event carries `{id, type, headers, body}`: the headers (`atlas-id`, `atlas-timestamp`, `atlas-signature`) and the exact JSON body of a would-be delivery, signed with the session secret. Starts at "now" unless resumed with `Last-Event-ID` or `after`. Query: `events` (comma-separated types or patterns), `max_events` and `max_wait_seconds` (end the stream early, for scripts and CI). A `: keep-alive` comment is sent every 15 seconds; an `expired` event ends the stream at the session expiry. 404 for an unknown session id, 400 once expired.</summary>
        /// <remarks>Needs the <c>webhooks:read</c> scope.</remarks>
        public Task<string> StreamSessionAsync(string id, string? events = null, string? after = null, long? maxEvents = null, long? pollMs = null, long? maxWaitSeconds = null, CancellationToken ct = default)
            => Req<string>(HttpVerb.Get, $"/v1/listen/sessions/{Enc(id)}/stream", query: Q(("events", events), ("after", after), ("max_events", maxEvents), ("poll_ms", pollMs), ("max_wait_seconds", maxWaitSeconds)), raw: true, headers: new Dictionary<string, string> { ["Accept"] = "text/event-stream" }, cancellationToken: ct);
    }
}
