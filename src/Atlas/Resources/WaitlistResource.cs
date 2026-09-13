using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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
    }
}
