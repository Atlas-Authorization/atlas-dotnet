using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    // ---------------- SSO connections ----------------

    /// <summary>An IdP-claim → role mapping. <c>roleKey</c> is camelCase on the wire.</summary>
    public sealed class ClaimRoleMapping
    {
        public string Claim { get; set; } = "";
        public string Value { get; set; } = "";
        [JsonPropertyName("roleKey")] public string RoleKey { get; set; } = "";
    }

    public sealed class SsoConnection
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "sso_connection";
        public string Id { get; init; } = "";
        public string? OrganizationId { get; init; }
        public string Type { get; init; } = "";
        public string Status { get; init; } = "";
        public string? OidcIssuer { get; init; }
        public string? OidcClientId { get; init; }
        public bool HasSecret { get; init; }
        public string? SamlIdpEntityId { get; init; }
        public string? SamlIdpSsoUrl { get; init; }
        public string? SamlSpEntityId { get; init; }
        public bool HasSamlCertificate { get; init; }
        public bool SamlAllowIdpInitiated { get; init; }
        public bool SamlSignAuthnRequests { get; init; }
        public bool SamlWantResponseSigned { get; init; }
        public bool HasDiscourseSecret { get; init; }
        public string? DiscourseProviderUrl { get; init; }
        public List<string> AllowedDomains { get; init; } = new List<string>();
        public List<ClaimRoleMapping> ClaimRoleMappings { get; init; } = new List<ClaimRoleMapping>();
        public string? DefaultRoleId { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    /// <summary>SP SAML metadata: the EntityDescriptor XML carried as a string in a JSON envelope.</summary>
    public sealed class SamlMetadata
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "sso_saml_metadata";
        public string SpEntityId { get; init; } = "";
        public string AcsUrl { get; init; } = "";
        public string MetadataXml { get; init; } = "";
    }

    public sealed class CreateSsoConnectionBody
    {
        public string? OrganizationId { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? OidcIssuer { get; set; }
        public string? OidcClientId { get; set; }
        /// <summary>Write-only; stored encrypted, never returned.</summary>
        public string? OidcClientSecret { get; set; }
        public string? SamlIdpEntityId { get; set; }
        public string? SamlIdpSsoUrl { get; set; }
        /// <summary>Write-only; only <c>has_saml_certificate</c> is ever returned.</summary>
        public string? SamlIdpCertificate { get; set; }
        public string? SamlSpEntityId { get; set; }
        public bool? SamlAllowIdpInitiated { get; set; }
        public bool? SamlSignAuthnRequests { get; set; }
        public bool? SamlWantResponseSigned { get; set; }
        public string? DiscourseSecret { get; set; }
        public string? DiscourseProviderUrl { get; set; }
        public List<string>? AllowedDomains { get; set; }
        public List<ClaimRoleMapping>? ClaimRoleMappings { get; set; }
        public string? DefaultRoleId { get; set; }
    }

    /// <summary>PATCH accepts everything create does except <c>type</c>, which is immutable.</summary>
    public sealed class UpdateSsoConnectionBody
    {
        public string? OrganizationId { get; set; }
        public string? Status { get; set; }
        public string? OidcIssuer { get; set; }
        public string? OidcClientId { get; set; }
        public string? OidcClientSecret { get; set; }
        public string? SamlIdpEntityId { get; set; }
        public string? SamlIdpSsoUrl { get; set; }
        public string? SamlIdpCertificate { get; set; }
        public string? SamlSpEntityId { get; set; }
        public bool? SamlAllowIdpInitiated { get; set; }
        public bool? SamlSignAuthnRequests { get; set; }
        public bool? SamlWantResponseSigned { get; set; }
        public string? DiscourseSecret { get; set; }
        public string? DiscourseProviderUrl { get; set; }
        public List<string>? AllowedDomains { get; set; }
        public List<ClaimRoleMapping>? ClaimRoleMappings { get; set; }
        public string? DefaultRoleId { get; set; }
    }

    /// <summary>The SSO-connections namespace (<c>/v1/sso_connections</c>).</summary>
    public sealed class SsoConnectionsResource : ResourceBase
    {
        public SsoConnectionsResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<SsoConnection>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<SsoConnection>>(HttpVerb.Get, "/v1/sso_connections", cancellationToken: ct);

        public Task<SsoConnection> GetAsync(string id, CancellationToken ct = default)
            => Req<SsoConnection>(HttpVerb.Get, $"/v1/sso_connections/{Enc(id)}", cancellationToken: ct);

        public Task<SsoConnection> CreateAsync(CreateSsoConnectionBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<SsoConnection>(HttpVerb.Post, "/v1/sso_connections", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<SsoConnection> UpdateAsync(string id, UpdateSsoConnectionBody body, CancellationToken ct = default)
            => Req<SsoConnection>(HttpVerb.Patch, $"/v1/sso_connections/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/sso_connections/{Enc(id)}", cancellationToken: ct);

        /// <summary>SP SAML metadata for a connection (JSON envelope carrying the XML).</summary>
        public Task<SamlMetadata> SamlMetadataAsync(string id, CancellationToken ct = default)
            => Req<SamlMetadata>(HttpVerb.Get, $"/v1/sso_connections/{Enc(id)}/saml_metadata", cancellationToken: ct);
    }

    // ---------------- SCIM tokens (inbound) ----------------

    public sealed class ScimToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "scim_token";
        public string Id { get; init; } = "";
        public string? Name { get; init; }
        public string OrganizationId { get; init; } = "";
        public string? ConnectionId { get; init; }
        public string Prefix { get; init; } = "";
        public long? LastUsedAt { get; init; }
        public long? ExpiresAt { get; init; }
        public long? RevokedAt { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>Create reveals the usable secret exactly once.</summary>
    public sealed class ScimTokenWithSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "scim_token";
        public string Id { get; init; } = "";
        public string? Name { get; init; }
        public string OrganizationId { get; init; } = "";
        public string? ConnectionId { get; init; }
        public string Prefix { get; init; } = "";
        public long? LastUsedAt { get; init; }
        public long? ExpiresAt { get; init; }
        public long? RevokedAt { get; init; }
        public long CreatedAt { get; init; }
        public string Secret { get; init; } = "";
        public string Note { get; init; } = "";
    }

    public sealed class CreateScimTokenBody
    {
        public string OrganizationId { get; set; } = "";
        public string? Name { get; set; }
        public string? ConnectionId { get; set; }
    }

    /// <summary>The inbound SCIM tokens namespace (<c>/v1/scim_tokens</c>).</summary>
    public sealed class ScimTokensResource : ResourceBase
    {
        public ScimTokensResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<ScimToken>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<ScimToken>>(HttpVerb.Get, "/v1/scim_tokens", cancellationToken: ct);

        public Task<ScimTokenWithSecret> CreateAsync(CreateScimTokenBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ScimTokenWithSecret>(HttpVerb.Post, "/v1/scim_tokens", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<RevokedObject> RevokeAsync(string id, CancellationToken ct = default)
            => Req<RevokedObject>(HttpVerb.Post, $"/v1/scim_tokens/{Enc(id)}/revoke", cancellationToken: ct);
    }

    // ---------------- SSO onboarding (self-service) ----------------

    public sealed class SsoOnboardingProfile
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "sso_onboarding_profile";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public List<string> AllowedConnectionTypes { get; init; } = new List<string>();
        public string? OrganizationId { get; init; }
        public string? CompanyName { get; init; }
        public bool AllowScim { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class SsoOnboardingTicket
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "sso_onboarding_ticket";
        public string Id { get; init; } = "";
        public string ProfileId { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public string? SsoConnectionId { get; init; }
        public string Status { get; init; } = "";
        public long ExpiresAt { get; init; }
        public long CreatedAt { get; init; }
        public long? CompletedAt { get; init; }
    }

    /// <summary>The create response: the ticket plus the once-only token and hosted URL.</summary>
    public sealed class IssuedSsoOnboardingTicket
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "sso_onboarding_ticket";
        public string Id { get; init; } = "";
        public string ProfileId { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public string? SsoConnectionId { get; init; }
        public string Status { get; init; } = "";
        public long ExpiresAt { get; init; }
        public long CreatedAt { get; init; }
        public long? CompletedAt { get; init; }
        /// <summary>Revealed exactly once; only its hash is stored.</summary>
        public string Token { get; init; } = "";
        /// <summary>The hosted URL the end-customer's IT admin opens.</summary>
        public string Url { get; init; } = "";
        public int ExpiresIn { get; init; }
    }

    public sealed class CreateSsoOnboardingProfileBody
    {
        public string Name { get; set; } = "";
        public List<string>? AllowedConnectionTypes { get; set; }
        public string? OrganizationId { get; set; }
        public string? CompanyName { get; set; }
        public bool? AllowScim { get; set; }
    }

    public sealed class CreateSsoOnboardingTicketBody
    {
        public string? OrganizationId { get; set; }
        public int? ExpiresInSeconds { get; set; }
    }

    /// <summary>The self-service SSO onboarding namespace (<c>/v1/sso_onboarding_profiles</c>).</summary>
    public sealed class SsoOnboardingResource : ResourceBase
    {
        public SsoOnboardingResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<SsoOnboardingProfile>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<SsoOnboardingProfile>>(HttpVerb.Get, "/v1/sso_onboarding_profiles", cancellationToken: ct);

        public Task<SsoOnboardingProfile> GetAsync(string id, CancellationToken ct = default)
            => Req<SsoOnboardingProfile>(HttpVerb.Get, $"/v1/sso_onboarding_profiles/{Enc(id)}", cancellationToken: ct);

        public Task<SsoOnboardingProfile> CreateAsync(CreateSsoOnboardingProfileBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<SsoOnboardingProfile>(HttpVerb.Post, "/v1/sso_onboarding_profiles", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/sso_onboarding_profiles/{Enc(id)}", cancellationToken: ct);

        /// <summary>Issue a one-time ticket for a profile. The token is returned once, here.</summary>
        public Task<IssuedSsoOnboardingTicket> CreateTicketAsync(string profileId, CreateSsoOnboardingTicketBody? body = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<IssuedSsoOnboardingTicket>(HttpVerb.Post, $"/v1/sso_onboarding_profiles/{Enc(profileId)}/tickets",
                body ?? new CreateSsoOnboardingTicketBody(), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Kill a still-live ticket. 404 once it is used, expired, or already revoked.</summary>
        public Task<RevokedObject> RevokeTicketAsync(string ticketId, CancellationToken ct = default)
            => Req<RevokedObject>(HttpVerb.Post, $"/v1/sso_onboarding_tickets/{Enc(ticketId)}/revoke", cancellationToken: ct);
    }

    // ---------------- SCIM provisioning (outbound) ----------------

    public sealed class ScimProvisioningTarget
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "scim_provisioning_target";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string BaseUrl { get; init; } = "";
        public bool HasBearerToken { get; init; }
        public bool Enabled { get; init; }
        public Dictionary<string, object?> AttributeMapping { get; init; } = new Dictionary<string, object?>();
        public string DeprovisionAction { get; init; } = "";
        public string Status { get; init; } = "";
        public string? Cursor { get; init; }
        public int ConsecutiveFailures { get; init; }
        public string? LastError { get; init; }
        public long? LastSyncedAt { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class CreateScimProvisioningTargetBody
    {
        public string Name { get; set; } = "";
        /// <summary>Must be an https:// URL so the bearer never travels in cleartext.</summary>
        public string BaseUrl { get; set; } = "";
        /// <summary>Write-only; stored encrypted, never returned.</summary>
        public string BearerToken { get; set; } = "";
        public Dictionary<string, object?>? AttributeMapping { get; set; }
        public string? DeprovisionAction { get; set; }
        public bool? Enabled { get; set; }
    }

    public sealed class UpdateScimProvisioningTargetBody
    {
        public string? Name { get; set; }
        public string? BaseUrl { get; set; }
        /// <summary>Write-only; omit to leave the stored bearer unchanged.</summary>
        public string? BearerToken { get; set; }
        public Dictionary<string, object?>? AttributeMapping { get; set; }
        public string? DeprovisionAction { get; set; }
        public bool? Enabled { get; set; }
        public string? Status { get; set; }
    }

    public sealed class ScimProvisioningTestResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "scim_provisioning_test_result";
        public string Id { get; init; } = "";
        public bool Ok { get; init; }
        public int? Status { get; init; }
        public string? Error { get; init; }
    }

    public sealed class ScimProvisioningSyncResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "scim_provisioning_sync_result";
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        public string Action { get; init; } = "";
        public bool Ok { get; init; }
        public int? Status { get; init; }
        public string? RemoteId { get; init; }
        public string? Error { get; init; }
    }

    public sealed class ScimProvisioningGroupSyncResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "scim_provisioning_group_sync_result";
        public string Id { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public string Action { get; init; } = "";
        public bool Ok { get; init; }
        public int? Status { get; init; }
        public string? RemoteId { get; init; }
        public int MemberCount { get; init; }
        public string? Error { get; init; }
    }

    /// <summary>The outbound SCIM provisioning namespace (<c>/v1/scim_provisioning_targets</c>).</summary>
    public sealed class ScimProvisioningResource : ResourceBase
    {
        public ScimProvisioningResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<ScimProvisioningTarget>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<ScimProvisioningTarget>>(HttpVerb.Get, "/v1/scim_provisioning_targets", cancellationToken: ct);

        public Task<ScimProvisioningTarget> GetAsync(string id, CancellationToken ct = default)
            => Req<ScimProvisioningTarget>(HttpVerb.Get, $"/v1/scim_provisioning_targets/{Enc(id)}", cancellationToken: ct);

        public Task<ScimProvisioningTarget> CreateAsync(CreateScimProvisioningTargetBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ScimProvisioningTarget>(HttpVerb.Post, "/v1/scim_provisioning_targets", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<ScimProvisioningTarget> UpdateAsync(string id, UpdateScimProvisioningTargetBody body, CancellationToken ct = default)
            => Req<ScimProvisioningTarget>(HttpVerb.Patch, $"/v1/scim_provisioning_targets/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/scim_provisioning_targets/{Enc(id)}", cancellationToken: ct);

        /// <summary>Probe connectivity + auth to the downstream with the real bearer. Writes nothing.</summary>
        public Task<ScimProvisioningTestResult> TestAsync(string id, CancellationToken ct = default)
            => Req<ScimProvisioningTestResult>(HttpVerb.Post, $"/v1/scim_provisioning_targets/{Enc(id)}/test", cancellationToken: ct);

        /// <summary>Force one user's sync now (backfill / re-push), out of band from the worker.</summary>
        public Task<ScimProvisioningSyncResult> SyncUserAsync(string id, string userId, CancellationToken ct = default)
            => Req<ScimProvisioningSyncResult>(HttpVerb.Post, $"/v1/scim_provisioning_targets/{Enc(id)}/sync_user",
                body: Body(("user_id", userId)), cancellationToken: ct);

        /// <summary>Force one org's (group) sync now; only already-provisioned members are sent.</summary>
        public Task<ScimProvisioningGroupSyncResult> SyncGroupAsync(string id, string organizationId, CancellationToken ct = default)
            => Req<ScimProvisioningGroupSyncResult>(HttpVerb.Post, $"/v1/scim_provisioning_targets/{Enc(id)}/sync_group",
                body: Body(("organization_id", organizationId)), cancellationToken: ct);
    }
}
