using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class AuditEventTarget
    {
        public string? Type { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AuditEventTarget Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AuditEventBody
    {
        public AuditEventActor? Actor { get; set; }
        public string? Action { get; set; }
        public AuditEventTarget? Target { get; set; }
        public string? OrganizationId { get; set; }
        public double? OccurredAt { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AuditEventBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AuditEventBatchBody
    {
        public List<AuditEventBody>? Events { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AuditEventBatchBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AuditEvent
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        /// <summary>The org this event is scoped to, if any. Org-scoped events are streamed to that org's SIEM.</summary>
        public string? OrganizationId { get; init; }
        public AuditEventActor? Actor { get; init; }
        /// <summary>The tenant's own action string, e.g. `invoice.sent`.</summary>
        public string Action { get; init; } = "";
        public AuditEventTarget? Target { get; init; }
        /// <summary>When the event happened in the tenant's system (epoch ms).</summary>
        public long OccurredAt { get; init; }
        /// <summary>Arbitrary non-secret structured context the tenant attached.</summary>
        public JsonElement? Metadata { get; init; }
        /// <summary>When Atlas received the event (epoch ms).</summary>
        public long CreatedAt { get; init; }
    }

    public sealed class AuditEventsDeleted
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>How many events were removed.</summary>
        public long Deleted { get; init; }
    }

    /// <summary>Your own custom audit events (<c>/v1/audit_events</c>).</summary>
    public sealed class AuditEventsResource : ResourceBase
    {
        public AuditEventsResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>POST /v1/audit_events</c> — Write one of the tenant's OWN custom audit events (actor, action, optional target, optional metadata).</summary>
        public Task<AuditEvent> CreateAsync(AuditEventBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<AuditEvent>(HttpVerb.Post, "/v1/audit_events", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/audit_events</c> — Query the tenant's custom audit events, filterable by organization_id, actor_id, action, target_type, target_id and an occurred-at window (from/to), cursor-paginated with limit + starting_after.</summary>
        public Task<CursorPage<AuditEvent>> ListAsync(ListAuditEventsParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<AuditEvent>>(HttpVerb.Get, "/v1/audit_events", query: (@params ?? new ListAuditEventsParams()).ToQuery(), cancellationToken: ct);

        /// <summary><c>DELETE /v1/audit_events</c> — Bulk-delete the instance's custom audit events — a TEST-MODE reset, allowed ONLY on a development instance (a production audit trail cannot be bulk-erased: 403).</summary>
        public Task<AuditEventsDeleted> DeleteAllAsync(string? organizationId = null, string? action = null, CancellationToken ct = default)
            => Req<AuditEventsDeleted>(HttpVerb.Delete, "/v1/audit_events", query: Q(("organization_id", organizationId), ("action", action)), cancellationToken: ct);

        /// <summary><c>POST /v1/audit_events/batch</c> — Write up to 100 custom audit events in one call.</summary>
        public Task<ListPage<AuditEvent>> CreateBatchAsync(AuditEventBatchBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ListPage<AuditEvent>>(HttpVerb.Post, "/v1/audit_events/batch", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/audit_events/export</c> — Bulk-export every matching custom audit event in one streamed response — `format=jsonl` (newline-delimited JSON, default) or `format=csv`.</summary>
        public Task<string> ExportAsync(ExportAuditEventsParams? @params = null, CancellationToken ct = default)
            => Req<string>(HttpVerb.Get, "/v1/audit_events/export", query: (@params ?? new ExportAuditEventsParams()).ToQuery(), raw: true, cancellationToken: ct);
    }
}
