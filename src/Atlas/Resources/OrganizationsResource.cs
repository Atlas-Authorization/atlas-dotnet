using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

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

        /// <summary>The policy overrides as a typed <see cref="OrgPolicy"/> (unknown keys land in <see cref="OrgPolicy.Extra"/>).</summary>
        public OrgPolicy Typed() =>
            AtlasJson.Deserialize<OrgPolicy>(AtlasJson.Serialize(Policy)) ?? new OrgPolicy();
    }

    /// <summary>
    /// An organization's security policy overrides, typed. Only the overrides that are
    /// set appear (unset fields are null). The wire keys are camelCase.
    /// </summary>
    public sealed class OrgPolicy
    {
        /// <summary>Members must have MFA enrolled.</summary>
        [JsonPropertyName("requireMfa")] public bool? RequireMfa { get; init; }
        /// <summary>Members must sign in through the organization's SSO connection.</summary>
        [JsonPropertyName("ssoRequired")] public bool? SsoRequired { get; init; }
        /// <summary>Session idle timeout for members, in milliseconds.</summary>
        [JsonPropertyName("sessionIdleOverrideMs")] public long? SessionIdleOverrideMs { get; init; }
        /// <summary>The sign-in methods members may use (e.g. <c>password</c>, <c>passkey</c>, <c>oauth:google</c>, <c>saml</c>). Empty or absent means all.</summary>
        [JsonPropertyName("allowedSignInMethods")] public List<string>? AllowedSignInMethods { get; init; }
        /// <summary>CIDR allow-list gating member sign-in.</summary>
        [JsonPropertyName("ipAllowlist")] public List<string>? IpAllowlist { get; init; }
        /// <summary>Absolute session lifetime cap for members, in seconds.</summary>
        [JsonPropertyName("maxSessionAgeSeconds")] public long? MaxSessionAgeSeconds { get; init; }
        /// <summary>The organization's end-user (reader) pool settings.</summary>
        [JsonPropertyName("endUserPool")] public EndUserPoolPolicy? EndUserPool { get; init; }
        /// <summary>Policy keys this SDK version does not model.</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>An organization's end-user (reader) pool policy. The wire keys are camelCase.</summary>
    public sealed class EndUserPoolPolicy
    {
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>New end users wait for approval before they can sign in.</summary>
        [JsonPropertyName("requireApproval")] public bool? RequireApproval { get; set; }
        /// <summary>End users may sign up on the hosted pages.</summary>
        [JsonPropertyName("allowHostedSignup")] public bool? AllowHostedSignup { get; set; }
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
        /// <summary>The feature keys the organization is entitled to.</summary>
        public List<string> Features { get; init; } = new List<string>();
        public Dictionary<string, object?> Catalog { get; init; } = new Dictionary<string, object?>();
        /// <summary>Numeric limits by key (e.g. seats).</summary>
        public Dictionary<string, JsonElement> Limits { get; init; } = new Dictionary<string, JsonElement>();
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
        /// <summary>e.g. <c>password</c>, <c>passkey</c>, <c>oauth:google</c>, <c>saml</c>; an unknown method is a 400.</summary>
        [JsonPropertyName("allowedSignInMethods")] public List<string>? AllowedSignInMethods { get; set; }
        /// <summary>CIDR ranges members may sign in from.</summary>
        [JsonPropertyName("ipAllowlist")] public List<string>? IpAllowlist { get; set; }
        [JsonPropertyName("maxSessionAgeSeconds")] public long? MaxSessionAgeSeconds { get; set; }
        [JsonPropertyName("endUserPool")] public EndUserPoolPolicy? EndUserPool { get; set; }
        /// <summary>Any further policy keys, written verbatim at the top level.</summary>
        [JsonExtensionData] public Dictionary<string, object?>? Extra { get; set; }

        /// <summary>Remove a stored override (sends <c>"key": null</c>), e.g. <c>Clear("ipAllowlist")</c>.</summary>
        public OrganizationPolicyPatch Clear(string key)
        {
            (Extra ??= new Dictionary<string, object?>())[key] = null;
            return this;
        }
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

        /// <summary>Resend a pending invitation: same id and role, a fresh expiry.</summary>
        public Task<JsonElement> ResendAsync(string orgId, string invitationId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/organizations/{Enc(orgId)}/invitations/{Enc(invitationId)}/resend", cancellationToken: ct);
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
            Teams = new OrganizationTeamsResource(transport);
            Roles = new OrganizationRolesResource(transport);
        }

        public OrganizationMembershipsResource Memberships { get; }
        public OrganizationInvitationsResource Invitations { get; }
        public OrganizationDomainsResource Domains { get; }
        public OrganizationGroupRolesResource GroupRoles { get; }
        /// <summary>Teams inside an organization.</summary>
        public OrganizationTeamsResource Teams { get; }
        /// <summary>The roles an organization can assign, and its own custom roles.</summary>
        public OrganizationRolesResource Roles { get; }

        /// <summary>Fetch an organization by slug (case-insensitive).</summary>
        public Task<Organization> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Req<Organization>(HttpVerb.Get, $"/v1/organizations/by_slug/{Enc(slug)}", cancellationToken: ct);

        /// <summary>Whether a slug is valid and free: <c>{ slug, valid, available }</c>.</summary>
        public Task<JsonElement> CheckSlugAsync(string slug, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/organizations/check_slug", query: Q(("slug", slug)), cancellationToken: ct);

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

        /// <summary><c>GET /v1/organizations/{id}/branding</c> — Read an organization's hosted-page branding overlay (display name, logo, primary colour, support URL, custom domain).</summary>
        public Task<OrganizationBranding> GetBrandingAsync(string id, CancellationToken ct = default)
            => Req<OrganizationBranding>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/branding", cancellationToken: ct);

        /// <summary><c>PUT /v1/organizations/{id}/branding</c> — Set an organization's hosted-page branding overlay — a partial merge: a provided field overrides the instance brand, null (or empty string) clears it back.</summary>
        public Task<OrganizationBranding> ReplaceBrandingAsync(string id, OrganizationBrandingBody body, CancellationToken ct = default)
            => Req<OrganizationBranding>(HttpVerb.Put, $"/v1/organizations/{Enc(id)}/branding", body, cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/{orgId}/reader_domain</c> — List the organization's reader domain(s) — the host(s) whose sign-ups join this org's end-user pool, with DNS/certificate status.</summary>
        public Task<ListPage<ReaderDomain>> ListReaderDomainsAsync(string orgId, CancellationToken ct = default)
            => Req<ListPage<ReaderDomain>>(HttpVerb.Get, $"/v1/organizations/{Enc(orgId)}/reader_domain", cancellationToken: ct);

        /// <summary><c>POST /v1/organizations/{orgId}/reader_domain</c> — Bind a host to this organization's reader pool: a sign-up on it joins the pool.</summary>
        public Task<ReaderDomainSetup> SetReaderDomainAsync(string orgId, string? host, CancellationToken ct = default)
            => Req<ReaderDomainSetup>(HttpVerb.Post, $"/v1/organizations/{Enc(orgId)}/reader_domain", Body(("host", host)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/organizations/{orgId}/reader_domain</c> — Remove the organization's reader domain binding and drop its edge router.</summary>
        public Task<DeletedObject> RemoveReaderDomainAsync(string orgId, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/organizations/{Enc(orgId)}/reader_domain", cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/{orgId}/end_users</c> — List an organization's end-user (reader) pool — the B2B2C tier of external users attached to the org, distinct from its staff members.</summary>
        public Task<CursorPage<OrganizationEndUser>> ListEndUsersAsync(string orgId, ListEndUsersOrganizationsParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<OrganizationEndUser>>(HttpVerb.Get, $"/v1/organizations/{Enc(orgId)}/end_users", query: (@params ?? new ListEndUsersOrganizationsParams()).ToQuery(), cancellationToken: ct);

        /// <summary><c>POST /v1/organizations/{orgId}/end_users</c> — Attach an instance user to an organization's end-user pool as a reader (the token-exchange path).</summary>
        public Task<OrganizationEndUser> AddEndUserAsync(string orgId, AddOrganizationEndUserBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OrganizationEndUser>(HttpVerb.Post, $"/v1/organizations/{Enc(orgId)}/end_users", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PATCH /v1/organizations/{orgId}/end_users/{userId}</c> — Approve (active), gate (pending) or block a reader in an organization end-user pool.</summary>
        public Task<OrganizationEndUser> UpdateEndUserAsync(string orgId, string userId, string? status, CancellationToken ct = default)
            => Req<OrganizationEndUser>(HttpVerb.Patch, $"/v1/organizations/{Enc(orgId)}/end_users/{Enc(userId)}", Body(("status", status)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/organizations/{orgId}/end_users/{userId}</c> — Remove a reader from an organization's end-user pool.</summary>
        public Task<DeletedObject> RemoveEndUserAsync(string orgId, string userId, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/organizations/{Enc(orgId)}/end_users/{Enc(userId)}", cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/{id}/audit_streams</c> — List an organization's registered audit stream destinations.</summary>
        public Task<ListPage<OrganizationAuditStream>> ListAuditStreamsAsync(string id, CancellationToken ct = default)
            => Req<ListPage<OrganizationAuditStream>>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/audit_streams", cancellationToken: ct);

        /// <summary><c>POST /v1/organizations/{id}/audit_streams</c> — Register an audit stream destination for one organization: an HTTPS SIEM/collector URL plus a shared HMAC signing secret (write-only, stored encrypted, min 16 chars) and an optional format hint.</summary>
        public Task<OrganizationAuditStream> CreateAuditStreamAsync(string id, CreateOrganizationAuditStreamBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OrganizationAuditStream>(HttpVerb.Post, $"/v1/organizations/{Enc(id)}/audit_streams", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/organizations/{id}/audit_streams/{streamId}</c> — Delete one of an organization's audit stream destinations.</summary>
        public Task<DeletedObject> DeleteAuditStreamAsync(string id, string streamId, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/organizations/{Enc(id)}/audit_streams/{Enc(streamId)}", cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/{id}/settings</c> — Fetch one organization's settings values.</summary>
        public Task<OrganizationSettings> GetSettingsAsync(string id, CancellationToken ct = default)
            => Req<OrganizationSettings>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/settings", cancellationToken: ct);

        /// <summary><c>PUT /v1/organizations/{id}/settings</c> — Write one organization's settings values.</summary>
        public Task<OrganizationSettings> ReplaceSettingsAsync(string id, Dictionary<string, object?>? values, CancellationToken ct = default)
            => Req<OrganizationSettings>(HttpVerb.Put, $"/v1/organizations/{Enc(id)}/settings", Body(("values", values)), cancellationToken: ct);

        /// <summary><c>PATCH /v1/organizations/{id}/settings</c> — Merge a partial object into an organization's settings values (RFC 7396 shallow merge): a key set to null is REMOVED, any other value sets/overwrites it, untouched keys are preserved — so a single settings key can be removed or changed without…</summary>
        public Task<OrganizationSettings> UpdateSettingsAsync(string id, Dictionary<string, object?>? values, CancellationToken ct = default)
            => Req<OrganizationSettings>(HttpVerb.Patch, $"/v1/organizations/{Enc(id)}/settings", Body(("values", values)), cancellationToken: ct);
    }

    public sealed class OrganizationBrandingBody
    {
        public string? DisplayName { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SupportUrl { get; set; }
        public string? CustomDomain { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OrganizationBrandingBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AddOrganizationEndUserBody
    {
        /// <summary>An instance user id to attach as a reader.</summary>
        public string? UserId { get; set; }
        /// <summary>Defaults to active. Use pending when the org gates readers behind approval.</summary>
        public string? Status { get; set; }
        /// <summary>How the reader entered the pool. Defaults to token_exchange for this API.</summary>
        public string? Source { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AddOrganizationEndUserBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class CreateOrganizationAuditStreamBody
    {
        /// <summary>Public HTTPS endpoint. SSRF-validated at save and at delivery.</summary>
        public string? Url { get; set; }
        /// <summary>Shared HMAC-SHA256 signing secret (min 16 chars). Write-only; stored encrypted and never returned.</summary>
        public string? Secret { get; set; }
        /// <summary>Optional shape hint for the collector. Default `json`.</summary>
        public string? Format { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateOrganizationAuditStreamBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OrganizationBranding
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public string? DisplayName { get; init; }
        public string? LogoUrl { get; init; }
        public string? PrimaryColor { get; init; }
        public string? SupportUrl { get; init; }
        public string? CustomDomain { get; init; }
        public long? UpdatedAt { get; init; }
    }

    /// <summary>A reader-pool custom domain (its `object` const is 'custom_domain').</summary>
    public sealed class ReaderDomain
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Role { get; init; } = "";
        public string Host { get; init; } = "";
        public string Status { get; init; } = "";
        public string? Action { get; init; }
        public bool Live { get; init; }
        public string? CnameTarget { get; init; }
        public long? LastCheckedAt { get; init; }
        public string? LastObservedTarget { get; init; }
        public string? FailureReason { get; init; }
        public long? CertificateExpiresAt { get; init; }
        public string? CookieDomain { get; init; }
    }

    public sealed class DnsInstruction
    {
        public string Role { get; init; } = "";
        public string Host { get; init; } = "";
        public string Type { get; init; } = "";
        public string Target { get; init; } = "";
        public string Purpose { get; init; } = "";
    }

    public sealed class ReaderDomainSetup
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Role { get; init; } = "";
        public string Host { get; init; } = "";
        public string Status { get; init; } = "";
        public string? Action { get; init; }
        public bool Live { get; init; }
        public string? CnameTarget { get; init; }
        public long? LastCheckedAt { get; init; }
        public string? LastObservedTarget { get; init; }
        public string? FailureReason { get; init; }
        public long? CertificateExpiresAt { get; init; }
        public string? CookieDomain { get; init; }
        public List<DnsInstruction> Instructions { get; init; } = new List<DnsInstruction>();
        public string Note { get; init; } = "";
    }

    public sealed class OrganizationEndUser
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public string? OrganizationId { get; init; }
        public string? UserId { get; init; }
        public string? Status { get; init; }
        public string? Source { get; init; }
        public Dictionary<string, object?>? PublicMetadata { get; init; }
        public long? ApprovedAt { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long? CreatedAt { get; init; }
        /// <summary>Updated (epoch ms).</summary>
        public long? UpdatedAt { get; init; }
    }

    public sealed class OrganizationAuditStream
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public string? OrganizationId { get; init; }
        /// <summary>The organization's HTTPS SIEM/collector endpoint.</summary>
        public string? Url { get; init; }
        /// <summary>Shape hint for the collector, e.g. `json`.</summary>
        public string? Format { get; init; }
        public bool? Active { get; init; }
        /// <summary>Whether a signing secret is set — never the secret itself.</summary>
        public bool? HasSecret { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long? CreatedAt { get; init; }
    }

    public sealed class OrganizationSettings
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? OrganizationId { get; init; }
        public Dictionary<string, object?>? Values { get; init; }
        /// <summary>Monotonic per-org version; 0 when nothing is set yet.</summary>
        public long? Version { get; init; }
        public string? UpdatedBy { get; init; }
        /// <summary>Last write (epoch ms), or null if never set.</summary>
        public long? UpdatedAt { get; init; }
    }
}
