using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/iga/birthright_rules</c>. Only the fields set are sent.</summary>
    public sealed class IgaLifecycleCreateBirthrightRuleBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("match")] public Dictionary<string, object?>? Match { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("entitlement_ids")] public List<string>? EntitlementIds { get; set; }
        [JsonPropertyName("revoke_on_unmatch")] public bool? RevokeOnUnmatch { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/iga/birthright_rules/{id}</c>. Only the fields set are sent.</summary>
    public sealed class IgaLifecycleUpdateBirthrightRuleBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("match")] public Dictionary<string, object?>? Match { get; set; }
        [JsonPropertyName("entitlement_ids")] public List<string>? EntitlementIds { get; set; }
        [JsonPropertyName("revoke_on_unmatch")] public bool? RevokeOnUnmatch { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>POST /v1/iga/users/{id}/offboard</c>. Only the fields set are sent.</summary>
    public sealed class IgaLifecycleOffboardBody
    {
        [JsonPropertyName("close_account")] public bool? CloseAccount { get; set; }
        [JsonPropertyName("grace_seconds")] public long? GraceSeconds { get; set; }
        [JsonPropertyName("reason")] public string? Reason { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/iga/settings</c>. Only the fields set are sent.</summary>
    public sealed class IgaLifecycleUpdateSettingsBody
    {
        [JsonPropertyName("jml_enabled")] public bool? JmlEnabled { get; set; }
        [JsonPropertyName("leaver_action")] public string? LeaverAction { get; set; }
        [JsonPropertyName("leaver_close_grace_seconds")] public long? LeaverCloseGraceSeconds { get; set; }
    }

    /// <summary>Operations of <c>iga.lifecycle</c>.</summary>
    public sealed partial class IgaLifecycleResource : ResourceBase
    {
        public IgaLifecycleResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/iga/birthright_rules</c> - List birthright rules.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ListBirthrightRulesAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/birthright_rules", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/birthright_rules</c> - Create a birthright rule: everyone matching `match` (a group, organization and role, app role or verified email domain; all conditions must hold) holds the listed entitlements. When someone stops matching, the grants the rule gave are revoked unless `revoke_on_unmatch` is false.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> CreateBirthrightRuleAsync(IgaLifecycleCreateBirthrightRuleBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/iga/birthright_rules", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PATCH /v1/iga/birthright_rules/{id}</c> - Change a birthright rule.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> UpdateBirthrightRuleAsync(string id, IgaLifecycleUpdateBirthrightRuleBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/iga/birthright_rules/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/iga/birthright_rules/{id}</c> - Delete a birthright rule (grants it already gave stay until revoked).</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> DeleteBirthrightRuleAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/iga/birthright_rules/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/users/{id}/reconcile</c> - Re-evaluate one user's birthright access now (joiner or mover).</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> ReconcileAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/iga/users/{Enc(id)}/reconcile", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/iga/users/{id}/offboard</c> - Offboard a leaver: revoke every grant, cancel pending requests and delegations, and with `close_account` schedule the account closure through the data-rights path (so downstream erasure webhooks and any DSAR orchestrator run as for any deletion). `grace_seconds` sets the cancellation window.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> OffboardAsync(string id, IgaLifecycleOffboardBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/iga/users/{Enc(id)}/offboard", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/iga/settings</c> - Joiner/mover/leaver settings.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> SettingsAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/settings", cancellationToken: ct);

        /// <summary><c>PUT /v1/iga/settings</c> - Turn the joiner/mover/leaver automation on or off and choose what a deactivated account triggers: `revoke` (end the grants) or `revoke_and_close` (also schedule the closure after `leaver_close_grace_seconds`).</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> UpdateSettingsAsync(IgaLifecycleUpdateSettingsBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/iga/settings", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
