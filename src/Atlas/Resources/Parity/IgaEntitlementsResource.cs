using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/iga/entitlements</c>. Only the fields set are sent.</summary>
    public sealed class IgaEntitlementsCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("key")] public string? Key { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("kind")] public string? Kind { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("target")] public Dictionary<string, object?>? Target { get; set; }
        [JsonPropertyName("owner_user_id")] public string? OwnerUserId { get; set; }
        [JsonPropertyName("risk_level")] public string? RiskLevel { get; set; }
        [JsonPropertyName("approval_rules")] public Dictionary<string, object?>? ApprovalRules { get; set; }
        [JsonPropertyName("max_duration_seconds")] public long? MaxDurationSeconds { get; set; }
        [JsonPropertyName("requestable")] public bool? Requestable { get; set; }
        [JsonPropertyName("require_justification")] public bool? RequireJustification { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/iga/entitlements/{id}</c>. Only the fields set are sent.</summary>
    public sealed class IgaEntitlementsUpdateBody
    {
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("kind")] public string? Kind { get; set; }
        [JsonPropertyName("target")] public Dictionary<string, object?>? Target { get; set; }
        [JsonPropertyName("owner_user_id")] public string? OwnerUserId { get; set; }
        [JsonPropertyName("risk_level")] public string? RiskLevel { get; set; }
        [JsonPropertyName("approval_rules")] public Dictionary<string, object?>? ApprovalRules { get; set; }
        [JsonPropertyName("max_duration_seconds")] public long? MaxDurationSeconds { get; set; }
        [JsonPropertyName("requestable")] public bool? Requestable { get; set; }
        [JsonPropertyName("require_justification")] public bool? RequireJustification { get; set; }
    }

    /// <summary>Operations of <c>iga.entitlements</c>.</summary>
    public sealed partial class IgaEntitlementsResource : ResourceBase
    {
        public IgaEntitlementsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/iga/entitlements</c> - List the governed entitlement catalog.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? kind = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/entitlements", query: Q(("kind", kind), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/entitlements</c> - Add an entitlement to the catalog: a role, organization membership, group, resource role, installed-app assignment or custom item, with an owner, a risk level and approval rules. The target must exist. 409 for a key already in use.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(IgaEntitlementsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/iga/entitlements", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/iga/entitlements/{id}</c> - Read one entitlement by id or key.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/iga/entitlements/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/iga/entitlements/{id}</c> - Change an entitlement (its kind cannot change). A change to the approval rules applies to requests filed afterwards.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, IgaEntitlementsUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/iga/entitlements/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/iga/entitlements/{id}</c> - Remove an entitlement. 409 while it still has active grants.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/iga/entitlements/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
