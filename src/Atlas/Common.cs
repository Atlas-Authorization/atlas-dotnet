using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Atlas
{
    /// <summary>
    /// A free-form metadata bag. The BAPI stores arbitrary JSON objects here;
    /// values arrive as <see cref="System.Text.Json.JsonElement"/> on read.
    /// </summary>
    public sealed class Metadata : Dictionary<string, object?> { }

    /// <summary>
    /// The <c>GET /v1/users</c>-style cursor page: no total, a boolean
    /// <see cref="HasMore"/>, and an opaque <see cref="NextCursor"/> to pass back
    /// as <c>starting_after</c>.
    /// </summary>
    public sealed class CursorPage<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; init; } = new List<T>();

        [JsonPropertyName("has_more")]
        public bool HasMore { get; init; }

        [JsonPropertyName("next_cursor")]
        public string? NextCursor { get; init; }
    }

    /// <summary>The <c>{ object: 'list', data, has_more }</c> envelope some list routes use.</summary>
    public sealed class ListPage<T>
    {
        [JsonPropertyName("object")]
        public string ObjectType { get; init; } = "list";

        [JsonPropertyName("data")]
        public List<T> Data { get; init; } = new List<T>();

        [JsonPropertyName("has_more")]
        public bool? HasMore { get; init; }
    }

    /// <summary>A bare <c>{ data }</c> list with no pagination metadata.</summary>
    public sealed class DataList<T>
    {
        [JsonPropertyName("object")]
        public string? ObjectType { get; init; }

        [JsonPropertyName("data")]
        public List<T> Data { get; init; } = new List<T>();
    }

    /// <summary>Common cursor-pagination params.</summary>
    public class CursorParams
    {
        /// <summary>Page size. Server clamps to its own maximum.</summary>
        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        /// <summary>Opaque cursor: the id after which to continue (from <c>next_cursor</c>).</summary>
        [JsonPropertyName("starting_after")]
        public string? StartingAfter { get; set; }

        /// <summary>Flatten to query key/value pairs (snake_case), dropping unset fields.</summary>
        public virtual IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
        }
    }

    /// <summary>A minimal deletion acknowledgement several mutation routes return.</summary>
    public sealed class DeletedObject
    {
        [JsonPropertyName("object")]
        public string ObjectType { get; init; } = "";

        [JsonPropertyName("id")]
        public string Id { get; init; } = "";

        [JsonPropertyName("deleted")]
        public bool Deleted { get; init; }
    }

    /// <summary>A minimal revocation acknowledgement several routes return (<c>{ object, id, revoked }</c>).</summary>
    public sealed class RevokedObject
    {
        [JsonPropertyName("object")]
        public string ObjectType { get; init; } = "";

        [JsonPropertyName("id")]
        public string Id { get; init; } = "";

        [JsonPropertyName("revoked")]
        public bool Revoked { get; init; }

        /// <summary>A human-readable note some revoke routes attach (e.g. SCIM tokens).</summary>
        [JsonPropertyName("note")]
        public string? Note { get; init; }
    }
}
