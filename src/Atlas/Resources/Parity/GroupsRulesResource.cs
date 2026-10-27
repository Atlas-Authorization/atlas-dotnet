using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/group_rules</c>. Only the fields set are sent.</summary>
    public sealed class GroupsRulesCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>Lower runs first (default 100).</summary>
        [JsonPropertyName("priority")] public long? Priority { get; set; }
        /// <summary>Required. A user-search filter tree (see POST /v1/users/search).</summary>
        [JsonPropertyName("condition")] public Dictionary<string, object?>? Condition { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("actions")] public List<object?>? Actions { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/group_rules/{id}</c>. Only the fields set are sent.</summary>
    public sealed class GroupsRulesUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>Lower runs first (default 100).</summary>
        [JsonPropertyName("priority")] public long? Priority { get; set; }
        /// <summary>A user-search filter tree (see POST /v1/users/search).</summary>
        [JsonPropertyName("condition")] public Dictionary<string, object?>? Condition { get; set; }
        [JsonPropertyName("actions")] public List<object?>? Actions { get; set; }
    }

    /// <summary>Operations of <c>groups.rules</c>.</summary>
    public sealed partial class GroupsRulesResource : ResourceBase
    {
        public GroupsRulesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/group_rules</c> - Group rules, lowest priority number first.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/group_rules", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/group_rules</c> - Create a rule: when a user matches condition, run the actions (add to a group, grant an app role, set a trait). -&gt; 201.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(GroupsRulesCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/group_rules", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/group_rules/apply</c> - Run every enabled rule in priority order. One result per rule.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> ApplyAllAsync(bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/group_rules/apply", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/group_rules/{id}</c> - One group rule, with its condition and actions.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/group_rules/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/group_rules/{id}</c> - Change a group rule, its condition or actions.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, GroupsRulesUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/group_rules/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/group_rules/{id}</c> - Delete a rule. Memberships it created stay until removed.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/group_rules/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/group_rules/{id}/apply</c> - Run one rule against every matching user now. Returns the match count and what changed.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> ApplyAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/group_rules/{Enc(id)}/apply", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
