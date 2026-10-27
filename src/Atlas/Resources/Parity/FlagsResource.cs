using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/organizations/{id}/entitlements</c>. Only the fields set are sent.</summary>
    public sealed class FlagsAssignOrganizationEntitlementsBody
    {
        [JsonPropertyName("plan")] public string? Plan { get; set; }
        [JsonPropertyName("features")] public List<string>? Features { get; set; }
        [JsonPropertyName("limits")] public Dictionary<string, object?>? Limits { get; set; }
        /// <summary>Unix ms or ISO 8601; null = no expiry.</summary>
        [JsonPropertyName("expires_at")] public object? ExpiresAt { get; set; }
    }

    /// <summary>The body of <c>POST /v1/feature_flags</c>. Only the fields set are sent.</summary>
    public sealed class FlagsCreateBody
    {
        /// <summary>Required. Lowercase letters, digits, - and _.</summary>
        [JsonPropertyName("key")] public string? Key { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>Required. A boolean, string, integer or JSON value matching the flag `type`.</summary>
        [JsonPropertyName("default_value")] public object? DefaultValue { get; set; }
        /// <summary>A boolean, string, integer or JSON value matching the flag `type`.</summary>
        [JsonPropertyName("off_value")] public object? OffValue { get; set; }
        /// <summary>Ordered targeting rules; the first match wins.</summary>
        [JsonPropertyName("rules")] public List<object?>? Rules { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/feature_flags/{key}</c>. Only the fields set are sent.</summary>
    public sealed class FlagsUpdateBody
    {
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>A boolean, string, integer or JSON value matching the flag `type`.</summary>
        [JsonPropertyName("default_value")] public object? DefaultValue { get; set; }
        /// <summary>A boolean, string, integer or JSON value matching the flag `type`.</summary>
        [JsonPropertyName("off_value")] public object? OffValue { get; set; }
        /// <summary>Ordered targeting rules; the first match wins.</summary>
        [JsonPropertyName("rules")] public List<object?>? Rules { get; set; }
    }

    /// <summary>The body of <c>POST /v1/feature_flags/evaluate</c>. Only the fields set are sent.</summary>
    public sealed class FlagsEvaluateBody
    {
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("org_id")] public string? OrgId { get; set; }
        [JsonPropertyName("plan")] public string? Plan { get; set; }
        [JsonPropertyName("keys")] public List<string>? Keys { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/users/{id}/entitlements</c>. Only the fields set are sent.</summary>
    public sealed class FlagsAssignUserEntitlementsBody
    {
        [JsonPropertyName("plan")] public string? Plan { get; set; }
        [JsonPropertyName("features")] public List<string>? Features { get; set; }
        [JsonPropertyName("limits")] public Dictionary<string, object?>? Limits { get; set; }
        /// <summary>Unix ms or ISO 8601; null = no expiry.</summary>
        [JsonPropertyName("expires_at")] public object? ExpiresAt { get; set; }
    }

    /// <summary>Operations of <c>flags</c>.</summary>
    public sealed partial class FlagsResource : ResourceBase
    {
        public FlagsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>PUT /v1/organizations/{id}/entitlements</c> - Assign an organization's entitlements (a plan, a feature list and/or limits, optionally expiring) without a billing subscription. Replaces whatever was assigned or overridden before; an empty body clears it and the organization falls back to its billing plan. Reaches the `pla`, `fea` and `ent` session claims on the next token.</summary>
        /// <remarks>Needs the <c>billing:write</c> scope.</remarks>
        public Task<JsonElement> AssignOrganizationEntitlementsAsync(string id, FlagsAssignOrganizationEntitlementsBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/organizations/{Enc(id)}/entitlements", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/feature_flags</c> - Create a feature flag: a typed value (`boolean`, `string`, `int` or `json`) with a `default_value`, an `off_value` (served while `enabled` is false; defaults to false for a boolean, else the default) and ordered targeting `rules` by org, user, plan and percentage. 409 when the key exists.</summary>
        /// <remarks>Needs the <c>flags:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(FlagsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/feature_flags", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/feature_flags</c> - List feature flags ordered by key. `starting_after` is the key of the last flag of the previous page.</summary>
        /// <remarks>Needs the <c>flags:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? startingAfter = null, long? limit = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/feature_flags", query: Q(("starting_after", startingAfter), ("limit", limit)), cancellationToken: ct);

        /// <summary><c>GET /v1/feature_flags/{key}</c> - Read one feature flag.</summary>
        /// <remarks>Needs the <c>flags:read</c> scope.</remarks>
        public Task<JsonElement> RetrieveAsync(string key, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/feature_flags/{Enc(key)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/feature_flags/{key}</c> - Change a flag's description, kill switch, default or off value, or replace its rules. The type and key are fixed.</summary>
        /// <remarks>Needs the <c>flags:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string key, FlagsUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/feature_flags/{Enc(key)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/feature_flags/{key}</c> - Delete a feature flag.</summary>
        /// <remarks>Needs the <c>flags:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string key, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/feature_flags/{Enc(key)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/feature_flags/evaluate</c> - Evaluate every flag (or the listed `keys`) for a subject (`user_id`, `org_id`, `plan`). When `plan` is omitted it is the organization's (else the user's) resolved plan. Each result carries the `value` and a `reason` (`off`, `default` or `rule:&lt;index&gt;`).</summary>
        /// <remarks>Needs the <c>flags:read</c> scope.</remarks>
        public Task<JsonElement> EvaluateAsync(FlagsEvaluateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/feature_flags/evaluate", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PUT /v1/users/{id}/entitlements</c> - Assign a user's entitlements the same way as for an organization.</summary>
        /// <remarks>Needs the <c>billing:write</c> scope.</remarks>
        public Task<JsonElement> AssignUserEntitlementsAsync(string id, FlagsAssignUserEntitlementsBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/users/{Enc(id)}/entitlements", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
