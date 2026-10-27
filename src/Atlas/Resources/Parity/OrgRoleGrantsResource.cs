using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/org_role_grants</c>. Only the fields set are sent.</summary>
    public sealed class OrgRoleGrantsCreateBody
    {
        [JsonPropertyName("grantor_org_id")] public string? GrantorOrgId { get; set; }
        [JsonPropertyName("grantee_org_id")] public string? GranteeOrgId { get; set; }
        [JsonPropertyName("role_keys")] public List<string>? RoleKeys { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/org_role_grants/{id}</c>. Only the fields set are sent.</summary>
    public sealed class OrgRoleGrantsUpdateBody
    {
        [JsonPropertyName("role_keys")] public List<string>? RoleKeys { get; set; }
    }

    /// <summary>The body of <c>POST /v1/org_role_grants/{id}/delegations</c>. Only the fields set are sent.</summary>
    public sealed class OrgRoleGrantsDelegateBody
    {
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("role_key")] public string? RoleKey { get; set; }
    }

    /// <summary>Operations of <c>orgRoleGrants</c>.</summary>
    public sealed partial class OrgRoleGrantsResource : ResourceBase
    {
        public OrgRoleGrantsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/org_role_grants</c> - List cross-organization role grants, filtered by grantor_org_id, grantee_org_id or status=active.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? grantorOrgId = null, string? granteeOrgId = null, string? status = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/org_role_grants", query: Q(("grantor_org_id", grantorOrgId), ("grantee_org_id", granteeOrgId), ("status", status), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/org_role_grants</c> - Let a grantee organization hand out a bounded set of the grantor organization’s roles (role_keys) to its own members. One active grant per pair.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(OrgRoleGrantsCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/org_role_grants", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/org_role_grants/{id}</c> - Fetch one cross-organization role grant.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/org_role_grants/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/org_role_grants/{id}</c> - Replace the granted role_keys. Delegations of roles no longer granted are removed.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, OrgRoleGrantsUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/org_role_grants/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/org_role_grants/{id}</c> - Revoke a grant. Every delegation under it stops taking effect immediately.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> RevokeAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/org_role_grants/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/org_role_grants/{id}/delegations</c> - List the members holding roles under a grant.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> ListDelegationsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/org_role_grants/{Enc(id)}/delegations", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/org_role_grants/{id}/delegations</c> - Delegate one granted role_key to a member (user_id) of the grantee organization.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> DelegateAsync(string id, OrgRoleGrantsDelegateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/org_role_grants/{Enc(id)}/delegations", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/org_role_grants/{id}/delegations/{delegationId}</c> - Remove a delegation of a granted role.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task UndelegateAsync(string id, string delegationId, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/org_role_grants/{Enc(id)}/delegations/{Enc(delegationId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/org_role_grants/effective</c> - The roles and permissions a user (user_id) holds in a grantor organization (organization_id, optional) through active delegations.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> EffectiveAsync(string userId, string? organizationId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/org_role_grants/effective", query: Q(("user_id", userId), ("organization_id", organizationId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
