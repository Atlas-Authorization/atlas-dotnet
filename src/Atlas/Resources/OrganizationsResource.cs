using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class Organization
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Slug { get; init; } = "";
        public string? ImageUrl { get; init; }
        public Metadata PublicMetadata { get; init; } = new Metadata();
        public int MaxAllowedMemberships { get; init; }
        public string CreatedBy { get; init; } = "";
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class OrganizationMembership
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_membership";
        public string Id { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public string UserId { get; init; } = "";
        public string Role { get; init; } = "";
        public long CreatedAt { get; init; }
    }

    public sealed class OrganizationInvitation
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_invitation";
        public string Id { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public string Email { get; init; } = "";
        public string Role { get; init; } = "";
        public string Status { get; init; } = "";
        public string InviterUserId { get; init; } = "";
        public long ExpiresAt { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class OrgDomainVerification
    {
        public string RecordName { get; init; } = "";
        public string RecordType { get; init; } = "TXT";
        public string RecordValue { get; init; } = "";
    }

    public sealed class OrgDomain
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "org_domain";
        public string Id { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public string Domain { get; init; } = "";
        public string Status { get; init; } = "";
        public bool AutoJoin { get; init; }
        public string? DefaultRoleId { get; init; }
        public OrgDomainVerification? Verification { get; init; }
        public long? VerifiedAt { get; init; }
        public long? LastCheckedAt { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class OrganizationPolicy
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_policy";
        public string OrganizationId { get; init; } = "";
        public Dictionary<string, object?> Policy { get; init; } = new Dictionary<string, object?>();
    }

    public sealed class OrganizationHierarchyNode
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Slug { get; init; } = "";
    }

    public sealed class OrganizationHierarchy
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_hierarchy";
        public string OrganizationId { get; init; } = "";
        public List<OrganizationHierarchyNode> Ancestors { get; init; } = new List<OrganizationHierarchyNode>();
        public List<OrganizationHierarchyNode> Children { get; init; } = new List<OrganizationHierarchyNode>();
    }

    public sealed class OrganizationEntitlements
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_entitlements";
        public string OrganizationId { get; init; } = "";
        public string? Plan { get; init; }
        public Dictionary<string, object?> Features { get; init; } = new Dictionary<string, object?>();
        public Dictionary<string, object?> Catalog { get; init; } = new Dictionary<string, object?>();
    }

    public sealed class CreateOrganizationBody
    {
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public int? MaxAllowedMemberships { get; set; }
    }

    public sealed class UpdateOrganizationBody
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? ImageUrl { get; set; }
        public int? MaxAllowedMemberships { get; set; }
        public Metadata? PublicMetadata { get; set; }
        public Metadata? PrivateMetadata { get; set; }
    }

    public sealed class ReplaceOrganizationMetadataBody
    {
        public Metadata? PublicMetadata { get; set; }
        public Metadata? PrivateMetadata { get; set; }
    }

    /// <summary>The <c>setParent</c> body. The field is always serialized, so null clears the parent.</summary>
    public sealed class SetParentBody
    {
        [JsonPropertyName("parent_organization_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? ParentOrganizationId { get; set; }
    }

    /// <summary>The org security policy patch (§4.4). Fields are camelCase on this route.</summary>
    public sealed class OrganizationPolicyPatch
    {
        [JsonPropertyName("requireMfa")] public bool? RequireMfa { get; set; }
        [JsonPropertyName("ssoRequired")] public bool? SsoRequired { get; set; }
        [JsonPropertyName("sessionIdleOverrideMs")] public long? SessionIdleOverrideMs { get; set; }
        /// <summary>Any further policy keys, written verbatim at the top level.</summary>
        [JsonExtensionData] public Dictionary<string, object?>? Extra { get; set; }
    }

    // --- Sub-namespace result shapes ---

    public sealed class AddMembershipResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_membership";
        public string OrganizationId { get; init; } = "";
        public string UserId { get; init; } = "";
        public string Role { get; init; } = "";
    }

    public sealed class UpdateMembershipResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_membership";
        public string Role { get; init; } = "";
        public bool Updated { get; init; }
    }

    public sealed class RemoveMembershipResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_membership";
        public bool Deleted { get; init; }
    }

    public sealed class CreateOrgInvitationResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_invitation";
        public string Id { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public string Email { get; init; } = "";
        public string Status { get; init; } = "";
        public long ExpiresAt { get; init; }
    }

    public sealed class RevokeOrgInvitationResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "organization_invitation";
        public string Id { get; init; } = "";
        public string Status { get; init; } = "";
    }

    public sealed class DeletedOrgDomain
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "org_domain";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
    }

    public sealed class GroupRoleGrant
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "group_role_grant";
        public string OrganizationId { get; init; } = "";
        public string GroupId { get; init; } = "";
        public string RoleId { get; init; } = "";
        public bool Granted { get; init; }
        public bool Deleted { get; init; }
    }

    public sealed class MembershipListPage
    {
        public List<OrganizationMembership> Data { get; init; } = new List<OrganizationMembership>();
        public bool HasMore { get; init; }
    }

    /// <summary>Nested memberships namespace.</summary>
    public sealed class OrganizationMembershipsResource : ResourceBase
    {
        public OrganizationMembershipsResource(AtlasTransport t) : base(t) { }

        public Task<MembershipListPage> ListAsync(string orgId, CancellationToken ct = default)
            => Req<MembershipListPage>(HttpVerb.Get, $"/v1/organizations/{Enc(orgId)}/memberships", cancellationToken: ct);

        public Task<AddMembershipResult> AddAsync(string orgId, string userId, string? role = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<AddMembershipResult>(HttpVerb.Post, $"/v1/organizations/{Enc(orgId)}/memberships",
                body: Body(("user_id", userId), ("role", role)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<UpdateMembershipResult> UpdateAsync(string orgId, string userId, string role, CancellationToken ct = default)
            => Req<UpdateMembershipResult>(HttpVerb.Patch, $"/v1/organizations/{Enc(orgId)}/memberships/{Enc(userId)}",
                body: Body(("role", role)), cancellationToken: ct);

        public Task<RemoveMembershipResult> RemoveAsync(string orgId, string userId, CancellationToken ct = default)
            => Req<RemoveMembershipResult>(HttpVerb.Delete, $"/v1/organizations/{Enc(orgId)}/memberships/{Enc(userId)}", cancellationToken: ct);
    }

    /// <summary>Nested organization-invitations namespace.</summary>
    public sealed class OrganizationInvitationsResource : ResourceBase
    {
        public OrganizationInvitationsResource(AtlasTransport t) : base(t) { }

        public Task<ListPage<OrganizationInvitation>> ListAsync(string orgId, CancellationToken ct = default)
            => Req<ListPage<OrganizationInvitation>>(HttpVerb.Get, $"/v1/organizations/{Enc(orgId)}/invitations", cancellationToken: ct);

        public Task<CreateOrgInvitationResult> CreateAsync(string orgId, string email, string role, string inviterUserId, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<CreateOrgInvitationResult>(HttpVerb.Post, $"/v1/organizations/{Enc(orgId)}/invitations",
                body: Body(("email", email), ("role", role), ("inviter_user_id", inviterUserId)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<RevokeOrgInvitationResult> RevokeAsync(string orgId, string invitationId, CancellationToken ct = default)
            => Req<RevokeOrgInvitationResult>(HttpVerb.Post, $"/v1/organizations/{Enc(orgId)}/invitations/{Enc(invitationId)}/revoke", cancellationToken: ct);
    }

    /// <summary>Nested organization-domains namespace.</summary>
    public sealed class OrganizationDomainsResource : ResourceBase
    {
        public OrganizationDomainsResource(AtlasTransport t) : base(t) { }

        public Task<DataList<OrgDomain>> ListAsync(string orgId, CancellationToken ct = default)
            => Req<DataList<OrgDomain>>(HttpVerb.Get, $"/v1/organizations/{Enc(orgId)}/domains", cancellationToken: ct);

        public Task<OrgDomain> CreateAsync(string orgId, string domain, bool? autoJoin = null, string? defaultRoleId = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OrgDomain>(HttpVerb.Post, $"/v1/organizations/{Enc(orgId)}/domains",
                body: Body(("domain", domain), ("auto_join", autoJoin), ("default_role_id", defaultRoleId)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<OrgDomain> VerifyAsync(string orgId, string domainId, CancellationToken ct = default)
            => Req<OrgDomain>(HttpVerb.Post, $"/v1/organizations/{Enc(orgId)}/domains/{Enc(domainId)}/verify", cancellationToken: ct);

        public Task<DeletedOrgDomain> DeleteAsync(string orgId, string domainId, CancellationToken ct = default)
            => Req<DeletedOrgDomain>(HttpVerb.Delete, $"/v1/organizations/{Enc(orgId)}/domains/{Enc(domainId)}", cancellationToken: ct);
    }

    /// <summary>Nested directory-group → role grant namespace.</summary>
    public sealed class OrganizationGroupRolesResource : ResourceBase
    {
        public OrganizationGroupRolesResource(AtlasTransport t) : base(t) { }

        public Task<GroupRoleGrant> GrantAsync(string orgId, string groupId, string roleId, CancellationToken ct = default)
            => Req<GroupRoleGrant>(HttpVerb.Put, $"/v1/organizations/{Enc(orgId)}/groups/{Enc(groupId)}/roles/{Enc(roleId)}", cancellationToken: ct);

        public Task<GroupRoleGrant> RevokeAsync(string orgId, string groupId, string roleId, CancellationToken ct = default)
            => Req<GroupRoleGrant>(HttpVerb.Delete, $"/v1/organizations/{Enc(orgId)}/groups/{Enc(groupId)}/roles/{Enc(roleId)}", cancellationToken: ct);
    }

    /// <summary>The organizations namespace (<c>/v1/organizations</c>) and its sub-namespaces.</summary>
    public sealed class OrganizationsResource : ResourceBase
    {
        public OrganizationsResource(AtlasTransport transport) : base(transport)
        {
            Memberships = new OrganizationMembershipsResource(transport);
            Invitations = new OrganizationInvitationsResource(transport);
            Domains = new OrganizationDomainsResource(transport);
            GroupRoles = new OrganizationGroupRolesResource(transport);
        }

        public OrganizationMembershipsResource Memberships { get; }
        public OrganizationInvitationsResource Invitations { get; }
        public OrganizationDomainsResource Domains { get; }
        public OrganizationGroupRolesResource GroupRoles { get; }

        public Task<CursorPage<Organization>> ListAsync(CursorParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<Organization>>(HttpVerb.Get, "/v1/organizations", query: (@params ?? new CursorParams()).ToQuery(), cancellationToken: ct);

        public Task<Organization> GetAsync(string id, CancellationToken ct = default)
            => Req<Organization>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}", cancellationToken: ct);

        public Task<Organization> CreateAsync(CreateOrganizationBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Organization>(HttpVerb.Post, "/v1/organizations", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<Organization> UpdateAsync(string id, UpdateOrganizationBody body, CancellationToken ct = default)
            => Req<Organization>(HttpVerb.Patch, $"/v1/organizations/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/organizations/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PUT /v1/organizations/:id/metadata</c> — replaces the named bags wholesale.</summary>
        public Task<Organization> UpdateMetadataAsync(string id, ReplaceOrganizationMetadataBody body, CancellationToken ct = default)
            => Req<Organization>(HttpVerb.Put, $"/v1/organizations/{Enc(id)}/metadata", body, cancellationToken: ct);

        public Task<OrganizationPolicy> UpdatePolicyAsync(string id, OrganizationPolicyPatch body, CancellationToken ct = default)
            => Req<OrganizationPolicy>(HttpVerb.Patch, $"/v1/organizations/{Enc(id)}/policy", body, cancellationToken: ct);

        /// <summary>
        /// <c>PUT /v1/organizations/:id/parent</c> — set or clear (pass null) the
        /// org's parent. The field is always sent, so a null genuinely re-parents
        /// to top level rather than being omitted.
        /// </summary>
        public Task<Organization> SetParentAsync(string id, string? parentOrganizationId, CancellationToken ct = default)
            => Req<Organization>(HttpVerb.Put, $"/v1/organizations/{Enc(id)}/parent",
                body: new SetParentBody { ParentOrganizationId = parentOrganizationId }, cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/:id/hierarchy</c> — ancestors (nearest first) + direct children.</summary>
        public Task<OrganizationHierarchy> HierarchyAsync(string id, CancellationToken ct = default)
            => Req<OrganizationHierarchy>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/hierarchy", cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/:id/entitlements</c> — the org's resolved feature set.</summary>
        public Task<OrganizationEntitlements> EntitlementsAsync(string id, CancellationToken ct = default)
            => Req<OrganizationEntitlements>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/entitlements", cancellationToken: ct);
    }
}
