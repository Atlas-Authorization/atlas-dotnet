using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

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
        /// <summary>The canonical request type: access, deletion, closure, restriction or rectification.</summary>
        public string? CanonicalType { get; init; }
        /// <summary>Where the request came from.</summary>
        public string? Source { get; init; }
        public string? Detail { get; init; }
        /// <summary>The case id in the data-rights orchestrator, when one was opened.</summary>
        public string? CaseId { get; init; }
        public JsonElement? ExternalCase { get; init; }
        /// <summary>The W3C trace id of the operation that caused the request.</summary>
        public string? TraceId { get; init; }
        public List<PrivacyStep>? Steps { get; init; }
        public List<PrivacyRetention>? Retention { get; init; }
        /// <summary>When the deletion takes effect (epoch ms), after the grace window.</summary>
        public long? DeletionEffectiveAt { get; init; }
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

        /// <summary><c>POST /v1/data_subject_requests</c> — Create a DSAR (export or erasure) for a user from the backend — the twin of the self-service /client/me flows, for a DSAR orchestrator.</summary>
        public Task<DataSubjectRequest> CreateAsync(CreateDataSubjectRequestBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<DataSubjectRequest>(HttpVerb.Post, "/v1/data_subject_requests", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/data_subject_requests/{id}/cancel</c> — Cancel a scheduled erasure within its grace window — the sibling to /fulfill (which expedites).</summary>
        public Task<DataSubjectRequest> CancelAsync(string id, CancellationToken ct = default)
            => Req<DataSubjectRequest>(HttpVerb.Post, $"/v1/data_subject_requests/{Enc(id)}/cancel", cancellationToken: ct);
    }

    public sealed class CreateDataSubjectRequestBody
    {
        /// <summary>The subject's Atlas id.</summary>
        public string? UserId { get; set; }
        /// <summary>The subject's email address (verified, else unverified). Resolves to the user so a rights orchestrator needn't translate email→id itself.</summary>
        public string? SubjectEmail { get; set; }
        public string? Type { get; set; }
        public string? Reason { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateDataSubjectRequestBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }
}
