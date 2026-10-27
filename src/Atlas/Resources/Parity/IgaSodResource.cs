using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/iga/sod_rules</c>. Only the fields set are sent.</summary>
    public sealed class IgaSodCreateRuleBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("entitlement_a")] public string? EntitlementA { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("entitlement_b")] public string? EntitlementB { get; set; }
        [JsonPropertyName("mode")] public string? Mode { get; set; }
        [JsonPropertyName("approver")] public Dictionary<string, object?>? Approver { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/iga/sod_rules/{id}</c>. Only the fields set are sent.</summary>
    public sealed class IgaSodUpdateRuleBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("mode")] public string? Mode { get; set; }
        [JsonPropertyName("approver")] public Dictionary<string, object?>? Approver { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>Operations of <c>iga.sod</c>.</summary>
    public sealed partial class IgaSodResource : ResourceBase
    {
        public IgaSodResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/iga/sod_rules</c> - List separation-of-duties rules: pairs of entitlements one person should not hold together.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ListRulesAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/sod_rules", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/sod_rules</c> - Create a rule. `mode` is `block` (refuse) or `require_approval` (an extra approval step on a request; `approver` names who, default the owner).</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> CreateRuleAsync(IgaSodCreateRuleBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/iga/sod_rules", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PATCH /v1/iga/sod_rules/{id}</c> - Change a separation-of-duties rule.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> UpdateRuleAsync(string id, IgaSodUpdateRuleBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/iga/sod_rules/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/iga/sod_rules/{id}</c> - Delete a rule and its recorded violations.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> DeleteRuleAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/iga/sod_rules/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/iga/sod_violations</c> - The violations report: users who hold both sides of a rule (found by a scan, or accepted at request or assignment time).</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ViolationsAsync(string? status = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/sod_violations", query: Q(("status", status), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/sod_violations/scan</c> - Check every rule against what people hold right now: record new conflicts and resolve those that cleared.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> ScanAsync(bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/iga/sod_violations/scan", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
