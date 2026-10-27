using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/access_policies</c>. Only the fields set are sent.</summary>
    public sealed class AccessPoliciesCreateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("priority")] public long? Priority { get; set; }
        [JsonPropertyName("stages")] public List<string>? Stages { get; set; }
        [JsonPropertyName("conditions")] public Dictionary<string, object?>? Conditions { get; set; }
        [JsonPropertyName("grants")] public List<object?>? Grants { get; set; }
    }

    /// <summary>The body of <c>POST /v1/access_policies/simulate</c>. Only the fields set are sent.</summary>
    public sealed class AccessPoliciesSimulateBody
    {
        [JsonPropertyName("policy_id")] public string? PolicyId { get; set; }
        [JsonPropertyName("stage")] public string? Stage { get; set; }
        [JsonPropertyName("ip")] public string? Ip { get; set; }
        [JsonPropertyName("application_id")] public string? ApplicationId { get; set; }
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("groups")] public List<string>? Groups { get; set; }
        [JsonPropertyName("orgs")] public List<string>? Orgs { get; set; }
        [JsonPropertyName("roles")] public List<string>? Roles { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("risk_level")] public string? RiskLevel { get; set; }
        [JsonPropertyName("device_trust")] public string? DeviceTrust { get; set; }
        [JsonPropertyName("auth_methods")] public List<string>? AuthMethods { get; set; }
        [JsonPropertyName("mfa_verified")] public bool? MfaVerified { get; set; }
        [JsonPropertyName("acr")] public List<string>? Acr { get; set; }
        [JsonPropertyName("now")] public long? Now { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/access_policies/{id}</c>. Only the fields set are sent.</summary>
    public sealed class AccessPoliciesUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("priority")] public long? Priority { get; set; }
        [JsonPropertyName("stages")] public List<string>? Stages { get; set; }
        [JsonPropertyName("conditions")] public Dictionary<string, object?>? Conditions { get; set; }
        [JsonPropertyName("grants")] public List<object?>? Grants { get; set; }
    }

    /// <summary>Operations of <c>accessPolicies</c>.</summary>
    public sealed partial class AccessPoliciesResource : ResourceBase
    {
        public AccessPoliciesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/access_policies</c> - List conditional-access policies, in evaluation order.</summary>
        /// <remarks>Needs the <c>access_policies:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/access_policies", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/access_policies</c> - Create a conditional-access policy. `conditions` (applications, userIds, groups, orgs, roles, networkZones {include, exclude}, countries, riskLevels, deviceTrust, authMethods, timeWindow) are ANDed; an empty condition matches anything. `grants` are allow, block, require_mfa, require_acr, require_policy_acceptance or require_action. `stages` limits it to sign_in, authorize and/or token (default all). A matching block wins; step-up grants are unioned.</summary>
        /// <remarks>Needs the <c>access_policies:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(AccessPoliciesCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/access_policies", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/access_policies/simulate</c> - What-if: describe a request (stage, ip, application, orgs, roles, country, risk level, device trust, auth methods, whether MFA was verified) and see the decision, the policies that matched and the unmet step-up requirements. Pass `policy_id` to test one policy even if it is disabled.</summary>
        /// <remarks>Needs the <c>access_policies:read</c> scope.</remarks>
        public Task<JsonElement> SimulateAsync(AccessPoliciesSimulateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/access_policies/simulate", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/access_policies/{id}</c> - Read one access policy.</summary>
        /// <remarks>Needs the <c>access_policies:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/access_policies/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/access_policies/{id}</c> - Update an access policy. Sent fields replace the stored value.</summary>
        /// <remarks>Needs the <c>access_policies:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, AccessPoliciesUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/access_policies/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/access_policies/{id}</c> - Delete an access policy.</summary>
        /// <remarks>Needs the <c>access_policies:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/access_policies/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
