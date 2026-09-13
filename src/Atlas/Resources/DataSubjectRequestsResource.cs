using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class DataSubjectRequest
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "data_subject_request";
        public string Id { get; init; } = "";
        public string Type { get; init; } = "";
        public string Status { get; init; } = "";
        public string UserId { get; init; } = "";
        public string? RequestedByType { get; init; }
        public string? RequestedById { get; init; }
        public string? Reason { get; init; }
        public long? ScheduledFor { get; init; }
        public long RequestedAt { get; init; }
        public long UpdatedAt { get; init; }
        public long? CompletedAt { get; init; }
        /// <summary>The export package — present on <c>get</c> and on a fulfilled export.</summary>
        public object? Result { get; init; }
    }

    public sealed class ListDataSubjectRequestsParams : CursorParams
    {
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? UserId { get; set; }

        public override IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            foreach (var pair in base.ToQuery()) yield return pair;
            yield return new KeyValuePair<string, object?>("type", Type);
            yield return new KeyValuePair<string, object?>("status", Status);
            yield return new KeyValuePair<string, object?>("user_id", UserId);
        }
    }

    /// <summary>The GDPR/DSAR admin namespace (<c>/v1/data_subject_requests</c>).</summary>
    public sealed class DataSubjectRequestsResource : ResourceBase
    {
        public DataSubjectRequestsResource(AtlasTransport transport) : base(transport) { }

        public Task<CursorPage<DataSubjectRequest>> ListAsync(ListDataSubjectRequestsParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<DataSubjectRequest>>(HttpVerb.Get, "/v1/data_subject_requests", query: (@params ?? new ListDataSubjectRequestsParams()).ToQuery(), cancellationToken: ct);

        public Task<DataSubjectRequest> GetAsync(string id, CancellationToken ct = default)
            => Req<DataSubjectRequest>(HttpVerb.Get, $"/v1/data_subject_requests/{Enc(id)}", cancellationToken: ct);

        /// <summary>Fulfil a request now — build the export package, or run the erasure — ahead of schedule.</summary>
        public Task<DataSubjectRequest> FulfillAsync(string id, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<DataSubjectRequest>(HttpVerb.Post, $"/v1/data_subject_requests/{Enc(id)}/fulfill", idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Reject a request with a recorded reason. Terminal — it cannot be re-actioned.</summary>
        public Task<DataSubjectRequest> RejectAsync(string id, string? reason = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<DataSubjectRequest>(HttpVerb.Post, $"/v1/data_subject_requests/{Enc(id)}/reject",
                body: Body(("reason", reason)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
