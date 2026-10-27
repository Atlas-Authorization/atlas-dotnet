using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/groups</c>. Only the fields set are sent.</summary>
    public sealed class GroupsCreateBody
    {
        /// <summary>Required. Lowercase letters, digits, - and _ (max 63). Unique per instance; usable in place of the id in paths.</summary>
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>Nest under another group (up to 8 levels, no cycles). Members of a child are also members of its ancestors for the token claim and role mapping.</summary>
        [JsonPropertyName("parent_id")] public string? ParentId { get; set; }
        /// <summary>Free attributes (max 8 KB).</summary>
        [JsonPropertyName("attributes")] public Dictionary<string, object?>? Attributes { get; set; }
        /// <summary>App roles every member (and member of a descendant) holds, merged into app_roles / app_permissions on the token.</summary>
        [JsonPropertyName("app_role_keys")] public List<string>? AppRoleKeys { get; set; }
        /// <summary>Add the slug to the `groups` token claim (default true).</summary>
        [JsonPropertyName("include_in_token")] public bool? IncludeInToken { get; set; }
        /// <summary>A user-search filter tree. POST …/reapply (and each SSO sign-in) makes membership match it.</summary>
        [JsonPropertyName("dynamic_filter")] public Dictionary<string, object?>? DynamicFilter { get; set; }
        /// <summary>{ claim, equals | contains | matches }: join at SSO sign-in when the IdP claim (a string or list, e.g. "groups") matches; leave when it stops matching.</summary>
        [JsonPropertyName("sso_match")] public Dictionary<string, object?>? SsoMatch { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/groups/{id}</c>. Only the fields set are sent.</summary>
    public sealed class GroupsUpdateBody
    {
        /// <summary>Lowercase letters, digits, - and _ (max 63). Unique per instance; usable in place of the id in paths.</summary>
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>Nest under another group (up to 8 levels, no cycles). Members of a child are also members of its ancestors for the token claim and role mapping.</summary>
        [JsonPropertyName("parent_id")] public string? ParentId { get; set; }
        /// <summary>Free attributes (max 8 KB).</summary>
        [JsonPropertyName("attributes")] public Dictionary<string, object?>? Attributes { get; set; }
        /// <summary>App roles every member (and member of a descendant) holds, merged into app_roles / app_permissions on the token.</summary>
        [JsonPropertyName("app_role_keys")] public List<string>? AppRoleKeys { get; set; }
        /// <summary>Add the slug to the `groups` token claim (default true).</summary>
        [JsonPropertyName("include_in_token")] public bool? IncludeInToken { get; set; }
        /// <summary>A user-search filter tree. POST …/reapply (and each SSO sign-in) makes membership match it.</summary>
        [JsonPropertyName("dynamic_filter")] public Dictionary<string, object?>? DynamicFilter { get; set; }
        /// <summary>{ claim, equals | contains | matches }: join at SSO sign-in when the IdP claim (a string or list, e.g. "groups") matches; leave when it stops matching.</summary>
        [JsonPropertyName("sso_match")] public Dictionary<string, object?>? SsoMatch { get; set; }
    }

    /// <summary>The body of <c>POST /v1/groups/{id}/members</c>. Only the fields set are sent.</summary>
    public sealed class GroupsAddMembersBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("user_ids")] public List<string>? UserIds { get; set; }
    }

    /// <summary>The body of <c>POST /v1/groups/{id}/members/remove</c>. Only the fields set are sent.</summary>
    public sealed class GroupsRemoveMembersBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("user_ids")] public List<string>? UserIds { get; set; }
    }

    /// <summary>Operations of <c>groups</c>.</summary>
    public sealed partial class GroupsResource : ResourceBase
    {
        public GroupsResource(AtlasTransport t) : base(t) { }

        private GroupsRulesResource? _rules;
        /// <summary>The <c>groups.rules</c> operations.</summary>
        public GroupsRulesResource Rules => _rules ??= new GroupsRulesResource(Transport);

        /// <summary><c>GET /v1/groups</c> - Every instance group with its member count, by slug.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/groups", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/groups</c> - Create an instance group. Emits group.created. -&gt; 201.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(GroupsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/groups", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/groups/{id}</c> - One group, by id or slug.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/groups/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/groups/{id}</c> - Change a group. Emits group.updated.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, GroupsUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/groups/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/groups/{id}</c> - Delete a group: its memberships go, its children move to the top level. Emits group.deleted.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/groups/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/groups/{id}/members</c> - The members of one group (direct members only), with how each joined (manual, dynamic, sso, rule). Cursor paged by user id.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListMembersAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/groups/{Enc(id)}/members", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/groups/{id}/members</c> - Add up to 500 users by hand. A member who joined by filter, SSO or rule becomes manual, so a re-apply leaves them. Emits group.member_added per new member.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task AddMembersAsync(string id, GroupsAddMembersBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, $"/v1/groups/{Enc(id)}/members", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/groups/{id}/members/remove</c> - Remove up to 500 users from a group. Emits group.member_removed.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task RemoveMembersAsync(string id, GroupsRemoveMembersBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, $"/v1/groups/{Enc(id)}/members/remove", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/groups/{id}/reapply</c> - Re-evaluate a dynamic group now: users matching dynamic_filter are added, those who no longer match are removed; manual members are never touched (400 when the group has no dynamic_filter, or the filter matches over 50,000 users).</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> ReapplyAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/groups/{Enc(id)}/reapply", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/groups</c> - A user's direct groups and how they joined each.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ForUserAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/users/{Enc(id)}/groups", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
