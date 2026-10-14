using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class WaitlistEntry
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "waitlist_entry";
        public string Id { get; init; } = "";
        public string EmailAddress { get; init; } = "";
        public string Status { get; init; } = "";
        public string? Note { get; init; }
        public string? DecidedBy { get; init; }
        public long? DecidedAt { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class ListWaitlistParams : CursorParams
    {
        public string? Status { get; set; }
        public string? Query { get; set; }

        public override IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            foreach (var pair in base.ToQuery()) yield return pair;
            yield return new KeyValuePair<string, object?>("status", Status);
            yield return new KeyValuePair<string, object?>("query", Query);
        }
    }

    public sealed class ListWaitlistResponse
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<WaitlistEntry> Data { get; init; } = new List<WaitlistEntry>();
        public Dictionary<string, int> Counts { get; init; } = new Dictionary<string, int>();
        public bool HasMore { get; init; }
        public string? NextCursor { get; init; }
    }

    /// <summary>The waitlist namespace (<c>/v1/waitlist_entries</c>).</summary>
    public sealed class WaitlistResource : ResourceBase
    {
        public WaitlistResource(AtlasTransport transport) : base(transport) { }

        public Task<ListWaitlistResponse> ListAsync(ListWaitlistParams? @params = null, CancellationToken ct = default)
            => Req<ListWaitlistResponse>(HttpVerb.Get, "/v1/waitlist_entries", query: (@params ?? new ListWaitlistParams()).ToQuery(), cancellationToken: ct);

        /// <summary>Approve or deny a waitlist entry.</summary>
        public Task<WaitlistEntry> DecideAsync(string id, string status, string? note = null, CancellationToken ct = default)
            => Req<WaitlistEntry>(HttpVerb.Post, $"/v1/waitlist_entries/{Enc(id)}/decide",
                body: Body(("status", status), ("note", note)), cancellationToken: ct);

        /// <summary><c>POST /v1/waitlist_entries</c> — Admin-create a waitlist entry (idempotent on address).</summary>
        public Task<WaitlistEntry> CreateAsync(string emailAddress, CancellationToken ct = default)
            => Req<WaitlistEntry>(HttpVerb.Post, "/v1/waitlist_entries", Body(("email_address", emailAddress)), cancellationToken: ct);

        /// <summary><c>POST /v1/waitlist_entries/bulk</c> — Bulk-create waitlist entries (≤100 addresses); per-entry errors returned.</summary>
        public Task<DataList<WaitlistEntry>> CreateBulkAsync(List<string> emailAddresses, CancellationToken ct = default)
            => Req<DataList<WaitlistEntry>>(HttpVerb.Post, "/v1/waitlist_entries/bulk", Body(("email_addresses", emailAddresses)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/waitlist_entries/{id}</c> — Delete a waitlist entry.</summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/waitlist_entries/{Enc(id)}", cancellationToken: ct);
    }
}
