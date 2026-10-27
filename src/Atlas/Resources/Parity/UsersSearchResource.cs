using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/users/search</c>. Only the fields set are sent.</summary>
    public sealed class UsersSearchQueryBody
    {
        /// <summary>A filter tree. A group is exactly one of {"and":[…]}, {"or":[…]}, {"not":{…}} (nest up to 6 levels, 50 conditions). A condition is {"field","op","value"}. Fields: id, username, first_name, last_name, external_id, locale, email, phone, created_at, updated_at, last_sign_in_at, password_updated_at, ban</summary>
        [JsonPropertyName("filter")] public Dictionary<string, object?>? Filter { get; set; }
        /// <summary>Free text over id, names, username, emails and phones, ANDed with the filter.</summary>
        [JsonPropertyName("query")] public string? Query { get; set; }
        /// <summary>created_at (default -created_at), updated_at, last_sign_in_at; prefix - for descending.</summary>
        [JsonPropertyName("order_by")] public string? OrderBy { get; set; }
        [JsonPropertyName("include_deleted")] public bool? IncludeDeleted { get; set; }
        [JsonPropertyName("limit")] public long? Limit { get; set; }
        [JsonPropertyName("starting_after")] public string? StartingAfter { get; set; }
        [JsonPropertyName("offset")] public long? Offset { get; set; }
        [JsonPropertyName("include_total_count")] public bool? IncludeTotalCount { get; set; }
    }

    /// <summary>The body of <c>POST /v1/users/search/export</c>. Only the fields set are sent.</summary>
    public sealed class UsersSearchExportBody
    {
        /// <summary>A filter tree. A group is exactly one of {"and":[…]}, {"or":[…]}, {"not":{…}} (nest up to 6 levels, 50 conditions). A condition is {"field","op","value"}. Fields: id, username, first_name, last_name, external_id, locale, email, phone, created_at, updated_at, last_sign_in_at, password_updated_at, ban</summary>
        [JsonPropertyName("filter")] public Dictionary<string, object?>? Filter { get; set; }
        /// <summary>Free text over id, names, username, emails and phones, ANDed with the filter.</summary>
        [JsonPropertyName("query")] public string? Query { get; set; }
        /// <summary>created_at (default -created_at), updated_at, last_sign_in_at; prefix - for descending.</summary>
        [JsonPropertyName("order_by")] public string? OrderBy { get; set; }
        [JsonPropertyName("include_deleted")] public bool? IncludeDeleted { get; set; }
        [JsonPropertyName("format")] public string? Format { get; set; }
        [JsonPropertyName("fields")] public List<string>? Fields { get; set; }
    }

    /// <summary>Operations of <c>users.search</c>.</summary>
    public sealed partial class UsersSearchResource : ResourceBase
    {
        public UsersSearchResource(AtlasTransport t) : base(t) { }

        private UsersSearchViewsResource? _views;
        /// <summary>The <c>users.search.views</c> operations.</summary>
        public UsersSearchViewsResource Views => _views ??= new UsersSearchViewsResource(Transport);

        /// <summary><c>POST /v1/users/search</c> - Search users with a boolean filter tree (and/or/not) over profile columns, traits and metadata keys, organization and org role, MFA enrolled/strategy, sign-in strategy, last sign-in country or IP, and date ranges. Sort with order_by; page with starting_after (creation/update order) or offset (last_sign_in_at order). include_total_count adds total_count. Containment tests on public metadata and traits are answered by an index. Same user objects as GET /v1/users.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> QueryAsync(UsersSearchQueryBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/users/search", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/users/search/export</c> - Export every user a search matches (up to 10,000; x-export-truncated says when more matched) as CSV (default) or NDJSON. fields picks columns: id, primary_email, username, first_name, last_name, external_id, created_at, last_sign_in_at, banned, mfa_enrolled, plus traits.&lt;key&gt; / public_metadata.&lt;path&gt;. CSV cells that start with = + - @ are prefixed with an apostrophe so a spreadsheet never runs them as formulas.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task ExportAsync(UsersSearchExportBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, "/v1/users/search/export", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
