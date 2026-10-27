using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/users/search/views</c>. Only the fields set are sent.</summary>
    public sealed class UsersSearchViewsCreateBody
    {
        /// <summary>Required. Unique per instance (max 100 characters).</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>A filter tree. A group is exactly one of {"and":[…]}, {"or":[…]}, {"not":{…}} (nest up to 6 levels, 50 conditions). A condition is {"field","op","value"}. Fields: id, username, first_name, last_name, external_id, locale, email, phone, created_at, updated_at, last_sign_in_at, password_updated_at, ban</summary>
        [JsonPropertyName("filter")] public Dictionary<string, object?>? Filter { get; set; }
        /// <summary>One of created_at, updated_at, last_sign_in_at, optionally prefixed with - for descending.</summary>
        [JsonPropertyName("order_by")] public string? OrderBy { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/users/search/views/{id}</c>. Only the fields set are sent.</summary>
    public sealed class UsersSearchViewsUpdateBody
    {
        /// <summary>Unique per instance (max 100 characters).</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>A filter tree. A group is exactly one of {"and":[…]}, {"or":[…]}, {"not":{…}} (nest up to 6 levels, 50 conditions). A condition is {"field","op","value"}. Fields: id, username, first_name, last_name, external_id, locale, email, phone, created_at, updated_at, last_sign_in_at, password_updated_at, ban</summary>
        [JsonPropertyName("filter")] public Dictionary<string, object?>? Filter { get; set; }
        /// <summary>One of created_at, updated_at, last_sign_in_at, optionally prefixed with - for descending.</summary>
        [JsonPropertyName("order_by")] public string? OrderBy { get; set; }
    }

    /// <summary>Operations of <c>users.search.views</c>.</summary>
    public sealed partial class UsersSearchViewsResource : ResourceBase
    {
        public UsersSearchViewsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/users/search/views</c> - The saved user-search views of the instance, by name.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/users/search/views", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/users/search/views</c> - Save a named filter (and sort) to re-run. A filter that could never run is refused; a duplicate name is a 400. -&gt; 201.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(UsersSearchViewsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/users/search/views", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PATCH /v1/users/search/views/{id}</c> - Rename a saved view or change its filter or sort.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, UsersSearchViewsUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/users/search/views/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/search/views/{id}</c> - Delete a saved user-search view.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/users/search/views/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
