using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class AuditLog
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "audit_log";
        public string Id { get; init; } = "";
        public string ActorType { get; init; } = "";
        public string? ActorId { get; init; }
        public string Action { get; init; } = "";
        public string? TargetType { get; init; }
        public string? TargetId { get; init; }
        public Dictionary<string, object?>? Metadata { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class ListAuditLogsParams : CursorParams
    {
        public string? ActorId { get; set; }
        public string? Action { get; set; }

        public override IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            foreach (var pair in base.ToQuery()) yield return pair;
            yield return new KeyValuePair<string, object?>("actor_id", ActorId);
            yield return new KeyValuePair<string, object?>("action", Action);
        }
    }

    /// <summary>The audit-logs namespace (<c>/v1/audit_logs</c>).</summary>
    public sealed class AuditLogsResource : ResourceBase
    {
        public AuditLogsResource(AtlasTransport transport) : base(transport) { }

        public Task<CursorPage<AuditLog>> ListAsync(ListAuditLogsParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<AuditLog>>(HttpVerb.Get, "/v1/audit_logs", query: (@params ?? new ListAuditLogsParams()).ToQuery(), cancellationToken: ct);
    }
}
