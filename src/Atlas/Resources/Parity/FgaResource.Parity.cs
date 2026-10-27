using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/fga/stores/{id}/list-users</c>. Only the fields set are sent.</summary>
    public sealed class FgaListUsersBody
    {
        [JsonPropertyName("object")] public object? Object { get; set; }
        [JsonPropertyName("relation")] public string? Relation { get; set; }
        [JsonPropertyName("user_filters")] public List<object?>? UserFilters { get; set; }
        [JsonPropertyName("authorization_model_id")] public string? AuthorizationModelId { get; set; }
        [JsonPropertyName("contextual_tuples")] public object? ContextualTuples { get; set; }
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        [JsonPropertyName("page_size")] public long? PageSize { get; set; }
        [JsonPropertyName("continuation_token")] public string? ContinuationToken { get; set; }
        [JsonPropertyName("consistency")] public string? Consistency { get; set; }
        [JsonPropertyName("consistency_token")] public string? ConsistencyToken { get; set; }
    }

    /// <summary>More operations of <c>fga</c>.</summary>
    public sealed partial class FgaResource
    {
        /// <summary><c>POST /v1/fga/stores/{id}/list-users</c> - List the users that have a relation on an object, filtered to one user type (or to `type#relation` usersets via `user_filters[0].relation`). Exact for wildcards (`type:*` plus `excluded_users` under `but not`), nested groups and contextual tuples; tuple conditions are evaluated against `context`. Pages with `page_size` / `continuation_token`; `consistency` / `consistency_token` behave as on check.</summary>
        /// <remarks>Needs the <c>fga:read</c> scope.</remarks>
        public Task<JsonElement> ListUsersAsync(string id, FgaListUsersBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/fga/stores/{Enc(id)}/list-users", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/fga/stores/{id}/changes</c> - Read the store’s tuple changelog: every tuple write and delete in commit order, resumable with `continuation_token` (returned even on the last page — pass it later to receive only newer changes). Filter by object `type` and `start_time`.</summary>
        /// <remarks>Needs the <c>fga:read</c> scope.</remarks>
        public Task<JsonElement> ReadChangesAsync(string id, string? type = null, long? pageSize = null, string? continuationToken = null, string? startTime = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/fga/stores/{Enc(id)}/changes", query: Q(("type", type), ("page_size", pageSize), ("continuation_token", continuationToken), ("start_time", startTime), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
