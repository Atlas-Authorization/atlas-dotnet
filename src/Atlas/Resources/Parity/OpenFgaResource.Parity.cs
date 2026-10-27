using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/openfga/stores/{store}/list-users</c>. Only the fields set are sent.</summary>
    public sealed class OpenFgaListUsersBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("object")] public Dictionary<string, object?>? Object { get; set; }
        /// <summary>Required. e.g. viewer</summary>
        [JsonPropertyName("relation")] public string? Relation { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("user_filters")] public List<object?>? UserFilters { get; set; }
        [JsonPropertyName("authorization_model_id")] public string? AuthorizationModelId { get; set; }
        /// <summary>The OpenFGA tuple-list wrapper.</summary>
        [JsonPropertyName("contextual_tuples")] public Dictionary<string, object?>? ContextualTuples { get; set; }
        /// <summary>Request-time values for tuple-condition parameters. A tuple's own context wins on a clash.</summary>
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        /// <summary>MINIMIZE_LATENCY (and UNSPECIFIED) may be answered from a short-lived result cache; HIGHER_CONSISTENCY bypasses it and reads the primary.</summary>
        [JsonPropertyName("consistency")] public string? Consistency { get; set; }
        /// <summary>A token returned by a write (or any read): evaluate at least that fresh, so a check can never be answered from before that write. Also accepted as the `Atlas-Consistency-Token` request header.</summary>
        [JsonPropertyName("consistency_token")] public string? ConsistencyToken { get; set; }
        /// <summary>Defaults to 1000.</summary>
        [JsonPropertyName("page_size")] public long? PageSize { get; set; }
        /// <summary>The `continuation_token` of the previous page.</summary>
        [JsonPropertyName("continuation_token")] public string? ContinuationToken { get; set; }
    }

    /// <summary>More operations of <c>openfga</c>.</summary>
    public sealed partial class OpenFgaResource
    {
        /// <summary><c>POST /v1/openfga/stores/{store}/list-users</c> - List the users that have a relation on an object (OpenFGA-wire ListUsers): concrete users, usersets (`user_filters[0].relation`), and the `type:*` wildcard with `excluded_users`. Honours contextual tuples and tuple conditions; pages with `page_size` / `continuation_token`.</summary>
        /// <remarks>Needs the <c>fga:read</c> scope.</remarks>
        public Task<JsonElement> ListUsersAsync(string store, OpenFgaListUsersBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/openfga/stores/{Enc(store)}/list-users", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/openfga/stores/{store}/changes</c> - Read the tuple changelog (OpenFGA-wire ReadChanges): every tuple write and delete in order, resumable with `continuation_token` (the token is returned even on the last page — pass it later to receive only newer changes).</summary>
        /// <remarks>Needs the <c>fga:read</c> scope.</remarks>
        public Task<JsonElement> ReadChangesAsync(string store, string? type = null, long? pageSize = null, string? continuationToken = null, string? startTime = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/openfga/stores/{Enc(store)}/changes", query: Q(("type", type), ("page_size", pageSize), ("continuation_token", continuationToken), ("start_time", startTime)), cancellationToken: ct);
    }
}
