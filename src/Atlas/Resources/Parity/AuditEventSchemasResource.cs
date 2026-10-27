using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/audit_event_schemas</c>. Only the fields set are sent.</summary>
    public sealed class AuditEventSchemasRegisterBody
    {
        /// <summary>Required. e.g. invoice.sent</summary>
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("version")] public long? Version { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("target_types")] public List<string>? TargetTypes { get; set; }
        [JsonPropertyName("actor_types")] public List<string>? ActorTypes { get; set; }
        [JsonPropertyName("metadata_schema")] public Dictionary<string, object?>? MetadataSchema { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/audit_event_schemas/{id}</c>. Only the fields set are sent.</summary>
    public sealed class AuditEventSchemasUpdateBody
    {
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    /// <summary>The body of <c>POST /v1/audit_events/{id}/redact</c>. Only the fields set are sent.</summary>
    public sealed class AuditEventSchemasRedactBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("fields")] public List<string>? Fields { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("reason")] public string? Reason { get; set; }
    }

    /// <summary>Operations of <c>auditEventSchemas</c>.</summary>
    public sealed partial class AuditEventSchemasResource : ResourceBase
    {
        public AuditEventSchemasResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/audit_event_schemas</c> - List the registered audit event schemas, by action then version (newest first).</summary>
        /// <remarks>Needs the <c>audit_events:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/audit_event_schemas", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/audit_event_schemas</c> - Register a schema for an action: the target types and actor types it allows and a JSON Schema its metadata must satisfy. `version` defaults to the next number for that action; a version is immutable (registering it again is a 409) - change a schema by registering a new version. From then on events with this action are validated on write.</summary>
        /// <remarks>Needs the <c>audit_events:write</c> scope.</remarks>
        public Task<JsonElement> RegisterAsync(AuditEventSchemasRegisterBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/audit_event_schemas", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/audit_event_schemas/{id}</c> - One audit event schema.</summary>
        /// <remarks>Needs the <c>audit_events:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/audit_event_schemas/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/audit_event_schemas/{id}</c> - Change a schema version's description or status. A deprecated version is no longer applied by default but still validates events that name it with `schema_version`.</summary>
        /// <remarks>Needs the <c>audit_events:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, AuditEventSchemasUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/audit_event_schemas/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/audit_event_schemas/{id}</c> - Remove a schema version. Events already recorded keep their `schema_version` number.</summary>
        /// <remarks>Needs the <c>audit_events:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/audit_event_schemas/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/audit_events/{id}/redact</c> - Blank named fields of one audit event (replaced by [redacted]) without deleting it: `actor_id`, `target_id`, `target_name` or `metadata.&lt;dotted.path&gt;`. A `reason` is required. The redaction is recorded in its own log (GET /v1/audit_events/:id/redactions) and in Atlas's audit log as `audit_event.redacted` - the fields and the reason, never the removed values.</summary>
        /// <remarks>Needs the <c>audit_events:write</c> scope.</remarks>
        public Task<JsonElement> RedactAsync(string id, AuditEventSchemasRedactBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/audit_events/{Enc(id)}/redact", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/audit_events/{id}/redactions</c> - An audit event's redaction history: which fields were blanked, why, and by which key.</summary>
        /// <remarks>Needs the <c>audit_events:read</c> scope.</remarks>
        public Task<JsonElement> RedactionsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/audit_events/{Enc(id)}/redactions", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
