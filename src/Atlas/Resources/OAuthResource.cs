using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    // ---------------- OAuth clients ----------------

    public sealed class OAuthClient
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_client";
        public string Id { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? LogoUrl { get; init; }
        public List<string> RedirectUris { get; init; } = new List<string>();
        public List<string> AllowedScopes { get; init; } = new List<string>();
        public List<string> GrantTypes { get; init; } = new List<string>();
        public string TokenEndpointAuthMethod { get; init; } = "";
        public string? SecretPrefix { get; init; }
        /// <summary>After a rotation with overlap: when the previous secret stops working (epoch ms); null when only the current secret is accepted.</summary>
        public long? PreviousSecretExpiresAt { get; init; }
        public bool IsPublic { get; init; }
        public bool FirstParty { get; init; }
        /// <summary>
        /// When true, RP-initiated logout (<c>/oauth2/logout</c>) with this client's <c>client_id</c> and an exactly
        /// registered <c>post_logout_redirect_uri</c> ends the session and redirects straight back (with <c>state</c>)
        /// without the confirmation page. First-party clients only. Defaults to false.
        /// </summary>
        public bool SkipLogoutConfirmation { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>Every OAuth flow for the client is refused while true.</summary>
        public bool Disabled { get; init; }
        /// <summary>PKCE is required for this confidential client too.</summary>
        public bool RequirePkce { get; init; }
        /// <summary><c>public</c> (sub = the user id) or <c>pairwise</c> (a per-sector sub).</summary>
        public string? SubjectType { get; init; }
        public string? SectorIdentifierUri { get; init; }
        /// <summary>When the current secret stops authenticating (epoch ms); null = never.</summary>
        public long? ClientSecretExpiresAt { get; init; }
    }

    /// <summary>Create/rotate reveal the secret exactly once, alongside the client.</summary>
    public sealed class OAuthClientWithSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_client";
        public string Id { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? LogoUrl { get; init; }
        public List<string> RedirectUris { get; init; } = new List<string>();
        public List<string> AllowedScopes { get; init; } = new List<string>();
        public List<string> GrantTypes { get; init; } = new List<string>();
        public string TokenEndpointAuthMethod { get; init; } = "";
        public string? SecretPrefix { get; init; }
        /// <summary>After a rotation with overlap: when the previous secret stops working (epoch ms); null when only the current secret is accepted.</summary>
        public long? PreviousSecretExpiresAt { get; init; }
        public bool IsPublic { get; init; }
        public bool FirstParty { get; init; }
        /// <summary>
        /// When true, RP-initiated logout (<c>/oauth2/logout</c>) with this client's <c>client_id</c> and an exactly
        /// registered <c>post_logout_redirect_uri</c> ends the session and redirects straight back (with <c>state</c>)
        /// without the confirmation page. First-party clients only. Defaults to false.
        /// </summary>
        public bool SkipLogoutConfirmation { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        public string? ClientSecret { get; init; }
        public string Note { get; init; } = "";
        /// <summary>On a rotation: how long the previous secret keeps working, in seconds (echoed).</summary>
        public int? OverlapSeconds { get; init; }
    }

    /// <summary>The body of <c>POST /v1/oauth_clients/{id}/rotate_secret</c>.</summary>
    public sealed class RotateOAuthClientSecretBody
    {
        /// <summary>How long the previous secret keeps working, in seconds. Null uses the default, 86400 (24 hours); 0 revokes it now; at most 604800 (7 days).</summary>
        public int? OverlapSeconds { get; set; }
    }

    public sealed class ClientGrant
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "client_grant";
        public string Id { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string ResourceServerId { get; init; } = "";
        public List<string> Scopes { get; init; } = new List<string>();
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class CreateOAuthClientBody
    {
        public string Name { get; set; } = "";
        public List<string> RedirectUris { get; set; } = new List<string>();
        public List<string>? AllowedScopes { get; set; }
        public List<string>? GrantTypes { get; set; }
        public string? TokenEndpointAuthMethod { get; set; }
        public string? LogoUrl { get; set; }
        public bool? FirstParty { get; set; }
        /// <summary>
        /// Skip the logout confirmation page for RP-initiated logout (<c>/oauth2/logout</c>) with this client's
        /// <c>client_id</c> and an exactly registered <c>post_logout_redirect_uri</c>: the session ends and the user is
        /// redirected straight back with <c>state</c>. First-party clients only — on a third-party client the API answers
        /// 422 <c>VALIDATION_FAILED</c> (param <c>skip_logout_confirmation</c>).
        /// </summary>
        public bool? SkipLogoutConfirmation { get; set; }
        /// <summary>Lifetime of the issued secret (reported as <c>client_secret_expires_at</c>); null = never expires.</summary>
        public long? SecretExpiresInSeconds { get; set; }
        /// <summary><c>public</c> (default) or <c>pairwise</c>.</summary>
        public string? SubjectType { get; set; }
        /// <summary>An https URL listing every redirect_uri; required for a pairwise client spanning hosts.</summary>
        public string? SectorIdentifierUri { get; set; }
    }

    public sealed class UpdateOAuthClientBody
    {
        public string? Name { get; set; }
        public List<string>? RedirectUris { get; set; }
        public List<string>? AllowedScopes { get; set; }
        public string? TokenEndpointAuthMethod { get; set; }
        public string? LogoUrl { get; set; }
        public bool? FirstParty { get; set; }
        /// <summary>
        /// Skip the logout confirmation page for RP-initiated logout (<c>/oauth2/logout</c>) with this client's
        /// <c>client_id</c> and an exactly registered <c>post_logout_redirect_uri</c>: the session ends and the user is
        /// redirected straight back with <c>state</c>. First-party clients only — on a third-party client the API answers
        /// 422 <c>VALIDATION_FAILED</c> (param <c>skip_logout_confirmation</c>).
        /// </summary>
        public bool? SkipLogoutConfirmation { get; set; }
        /// <summary>Lifetime of the issued secret (reported as <c>client_secret_expires_at</c>); null = never expires.</summary>
        public long? SecretExpiresInSeconds { get; set; }
        /// <summary><c>public</c> (default) or <c>pairwise</c>.</summary>
        public string? SubjectType { get; set; }
        /// <summary>An https URL listing every redirect_uri; required for a pairwise client spanning hosts.</summary>
        public string? SectorIdentifierUri { get; set; }
        /// <summary>Refuse (true) or re-allow (false) every OAuth flow for the client.</summary>
        public bool? Disabled { get; set; }
        /// <summary>Require PKCE for this confidential client too.</summary>
        public bool? RequirePkce { get; set; }
    }

    public sealed class DeleteOAuthClientResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_client";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
        public string Note { get; init; } = "";
    }

    public sealed class DeletedClientGrant
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "client_grant";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary>Nested OAuth-client grants namespace.</summary>
    public sealed class OAuthClientGrantsResource : ResourceBase
    {
        public OAuthClientGrantsResource(AtlasTransport t) : base(t) { }

        public Task<ListPage<ClientGrant>> ListAsync(string clientId, CancellationToken ct = default)
            => Req<ListPage<ClientGrant>>(HttpVerb.Get, $"/v1/oauth_clients/{Enc(clientId)}/grants", cancellationToken: ct);

        public Task<ClientGrant> CreateAsync(string clientId, string resourceServerId, IEnumerable<string>? scopes = null, CancellationToken ct = default)
            => Req<ClientGrant>(HttpVerb.Post, $"/v1/oauth_clients/{Enc(clientId)}/grants",
                body: Body(("resource_server_id", resourceServerId), ("scopes", scopes == null ? null : new List<string>(scopes))), cancellationToken: ct);

        public Task<DeletedClientGrant> DeleteAsync(string clientId, string grantId, CancellationToken ct = default)
            => Req<DeletedClientGrant>(HttpVerb.Delete, $"/v1/oauth_clients/{Enc(clientId)}/grants/{Enc(grantId)}", cancellationToken: ct);
    }

    /// <summary>The OAuth-clients namespace (<c>/v1/oauth_clients</c>).</summary>
    /// <summary>One back-channel logout attempt. Atlas never follows redirects: a 3xx fails with error <c>backchannel_redirect_not_followed</c> and its Location.</summary>
    public sealed class BackchannelLogoutDelivery
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "backchannel_logout_delivery";
        public string Id { get; init; } = "";
        public string? OauthClientId { get; init; }
        /// <summary>The ended session (<c>sid_test_…</c> for a test); null for "sign out everywhere".</summary>
        public string? Sid { get; init; }
        /// <summary>Sent by the test call; never retried.</summary>
        public bool Test { get; init; }
        /// <summary>pending, success or failed.</summary>
        public string Status { get; init; } = "";
        public int AttemptNumber { get; init; }
        public int MaxAttempts { get; init; }
        public int? HttpStatus { get; init; }
        public int? LatencyMs { get; init; }
        public string? Error { get; init; }
        public string? Location { get; init; }
        public string? ResponseSnippet { get; init; }
        public long? NextRetryAt { get; init; }
        public long? AttemptedAt { get; init; }
        public long? DeliveredAt { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>The result of a test back-channel logout, recorded in the delivery log with Test true.</summary>
    public sealed class BackchannelLogoutTest
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "backchannel_logout_test";
        public string DeliveryId { get; init; } = "";
        /// <summary>True when the receiver answered 2xx.</summary>
        public bool Ok { get; init; }
        public string Sid { get; init; } = "";
        public int? Status { get; init; }
        public int? LatencyMs { get; init; }
        public string? Error { get; init; }
        public string? Location { get; init; }
        /// <summary>Set when nothing was sent (private, loopback or non-https address).</summary>
        public string? BlockedReason { get; init; }
        public string? BodyExcerpt { get; init; }
    }

    public sealed class OAuthClientsResource : ResourceBase
    {
        public OAuthClientsResource(AtlasTransport transport) : base(transport)
        {
            Grants = new OAuthClientGrantsResource(transport);
        }

        public OAuthClientGrantsResource Grants { get; }

        public Task<ListPage<OAuthClient>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<OAuthClient>>(HttpVerb.Get, "/v1/oauth_clients", cancellationToken: ct);

        public Task<OAuthClient> GetAsync(string id, CancellationToken ct = default)
            => Req<OAuthClient>(HttpVerb.Get, $"/v1/oauth_clients/{Enc(id)}", cancellationToken: ct);

        public Task<OAuthClientWithSecret> CreateAsync(CreateOAuthClientBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OAuthClientWithSecret>(HttpVerb.Post, "/v1/oauth_clients", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<OAuthClient> UpdateAsync(string id, UpdateOAuthClientBody body, CancellationToken ct = default)
            => Req<OAuthClient>(HttpVerb.Patch, $"/v1/oauth_clients/{Enc(id)}", body, cancellationToken: ct);

        /// <summary>Rotate the client secret (revealed once). The previous secret keeps working for the default overlap (24 hours).</summary>
        public Task<OAuthClientWithSecret> RotateSecretAsync(string id, CancellationToken ct = default)
            => Req<OAuthClientWithSecret>(HttpVerb.Post, $"/v1/oauth_clients/{Enc(id)}/rotate_secret", cancellationToken: ct);

        /// <summary>
        /// Rotate the client secret with a chosen overlap: the previous secret keeps working for <c>OverlapSeconds</c>
        /// (0 revokes it now, e.g. after a leak). <see cref="OAuthClientWithSecret.PreviousSecretExpiresAt"/> says when it stops.
        /// </summary>
        public Task<OAuthClientWithSecret> RotateSecretAsync(string id, RotateOAuthClientSecretBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OAuthClientWithSecret>(HttpVerb.Post, $"/v1/oauth_clients/{Enc(id)}/rotate_secret", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/oauth_clients/{id}/backchannel_deliveries</c> — The client's back-channel logout delivery log, newest first.</summary>
        public Task<CursorPage<BackchannelLogoutDelivery>> ListBackchannelDeliveriesAsync(string id, int? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<CursorPage<BackchannelLogoutDelivery>>(HttpVerb.Get, $"/v1/oauth_clients/{Enc(id)}/backchannel_deliveries",
                query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/oauth_clients/{id}/backchannel_logout/test</c> — Send a test back-channel logout now and return what the receiver answered.</summary>
        public Task<BackchannelLogoutTest> TestBackchannelLogoutAsync(string id, CancellationToken ct = default)
            => Req<BackchannelLogoutTest>(HttpVerb.Post, $"/v1/oauth_clients/{Enc(id)}/backchannel_logout/test", cancellationToken: ct);

        public Task<DeleteOAuthClientResult> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeleteOAuthClientResult>(HttpVerb.Delete, $"/v1/oauth_clients/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/oauth_clients/import</c> — Bulk-import OAuth clients for a migration, optionally with a pre-hashed client secret.</summary>
        public Task<ListPage<ImportedOAuthClient>> ImportClientsAsync(ImportOAuthClientsBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ListPage<ImportedOAuthClient>>(HttpVerb.Post, "/v1/oauth_clients/import", body, idempotencyKey: idempotencyKey, cancellationToken: ct);
    }

    // ---------------- Resource servers ----------------

    public sealed class ResourceServerScope
    {
        public string Value { get; set; } = "";
        public string? Description { get; set; }
    }

    public sealed class ResourceServer
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "resource_server";
        public string Id { get; init; } = "";
        public string Identifier { get; init; } = "";
        public string Name { get; init; } = "";
        public List<ResourceServerScope> Scopes { get; init; } = new List<ResourceServerScope>();
        public int TokenTtlSeconds { get; init; }
        public string SigningAlg { get; init; } = "";
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>Atlas serves the RFC 9728 protected-resource metadata document for this API.</summary>
        public bool PublishResourceMetadata { get; init; }
    }

    public sealed class CreateResourceServerBody
    {
        public string Identifier { get; set; } = "";
        public string Name { get; set; } = "";
        /// <summary>Each scope is a bare string or a <see cref="ResourceServerScope"/>.</summary>
        public List<object>? Scopes { get; set; }
        public int? TokenTtlSeconds { get; set; }
        public string? SigningAlg { get; set; }
        /// <summary>Serve the RFC 9728 protected-resource metadata document for this API.</summary>
        public bool? PublishResourceMetadata { get; set; }
    }

    public sealed class UpdateResourceServerBody
    {
        public string? Name { get; set; }
        public List<object>? Scopes { get; set; }
        public int? TokenTtlSeconds { get; set; }
        public string? SigningAlg { get; set; }
        /// <summary>Immutable — supplying a different value is rejected.</summary>
        public string? Identifier { get; set; }
        /// <summary>Serve the RFC 9728 protected-resource metadata document for this API.</summary>
        public bool? PublishResourceMetadata { get; set; }
    }

    public sealed class DeletedResourceServer
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "resource_server";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary>The resource-servers namespace (<c>/v1/resource_servers</c>).</summary>
    public sealed class ResourceServersResource : ResourceBase
    {
        public ResourceServersResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<ResourceServer>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<ResourceServer>>(HttpVerb.Get, "/v1/resource_servers", cancellationToken: ct);

        public Task<ResourceServer> GetAsync(string id, CancellationToken ct = default)
            => Req<ResourceServer>(HttpVerb.Get, $"/v1/resource_servers/{Enc(id)}", cancellationToken: ct);

        public Task<ResourceServer> CreateAsync(CreateResourceServerBody body, CancellationToken ct = default)
            => Req<ResourceServer>(HttpVerb.Post, "/v1/resource_servers", body, cancellationToken: ct);

        public Task<ResourceServer> UpdateAsync(string id, UpdateResourceServerBody body, CancellationToken ct = default)
            => Req<ResourceServer>(HttpVerb.Patch, $"/v1/resource_servers/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedResourceServer> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedResourceServer>(HttpVerb.Delete, $"/v1/resource_servers/{Enc(id)}", cancellationToken: ct);
    }

    // ---------------- OAuth providers (social sign-in) ----------------

    public sealed class OAuthProviderCredentialField
    {
        public string Key { get; init; } = "";
        public string Label { get; init; } = "";
        public bool? Secret { get; init; }
        public bool? Multiline { get; init; }
        public bool? Optional { get; init; }
        public string? Help { get; init; }
    }

    public sealed class OAuthProviderSetting
    {
        public string Key { get; init; } = "";
        public string Label { get; init; } = "";
        public string Type { get; init; } = "";
        public List<string>? Options { get; init; }
        public string? Help { get; init; }
    }

    public sealed class OAuthProvider
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_provider";
        public string Provider { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string Category { get; init; } = "";
        public string Tier { get; init; } = "";
        public string? SetupDocsUrl { get; init; }
        public List<string> DefaultScopes { get; init; } = new List<string>();
        public List<OAuthProviderCredentialField> CredentialFields { get; init; } = new List<OAuthProviderCredentialField>();
        public List<OAuthProviderSetting> Settings { get; init; } = new List<OAuthProviderSetting>();
        public bool Native { get; init; }
        public string RedirectUri { get; init; } = "";
        public bool Configured { get; init; }
        public string? ClientId { get; init; }
        public bool HasSecret { get; init; }
        public bool Enabled { get; init; }
        public bool AllowSignIn { get; init; }
        public bool AllowSignUp { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        public long? UpdatedAt { get; init; }
        public List<string>? ApiScopes { get; init; }
        /// <summary>The stored non-secret settings (for the wallet providers, the verifier settings such as <c>domain</c> and <c>chain_ids</c>).</summary>
        public Dictionary<string, string>? SettingValues { get; init; }
        /// <summary><c>native</c> or <c>redirect</c>.</summary>
        public string? Mode { get; init; }
        public List<string>? JavascriptOrigins { get; init; }
        public string? AtlasCallback { get; init; }
        public string? CustomRedirectUri { get; init; }
        /// <summary>The button icon override, when set.</summary>
        public string? IconUrl { get; init; }
    }

    /// <summary>
    /// Body for <c>PUT /v1/oauth_providers/:provider</c>. Display-only fields
    /// (<see cref="DisplayName"/>, <see cref="IconUrl"/>, <see cref="Mode"/>) can be sent on
    /// their own to re-brand a provider's button. The credential-less wallet providers
    /// (<c>siwe</c>, <c>siws</c>, <c>siwf</c>) take only their verifier <see cref="Settings"/> plus
    /// display fields — see <see cref="Siwe"/>, <see cref="Siws"/>, <see cref="Siwf"/>. A setting
    /// set to <c>null</c> (see <see cref="ClearSetting"/>) or <c>""</c> clears it.
    /// </summary>
    public sealed class UpsertOAuthProviderBody
    {
        public string? ClientId { get; set; }
        /// <summary>Write-only; stored encrypted, never returned. Required for redirect-only providers.</summary>
        public string? ClientSecret { get; set; }
        public List<string>? Scopes { get; set; }
        public Dictionary<string, string>? Values { get; set; }
        public Dictionary<string, string?>? Settings { get; set; }
        /// <summary>Override the redirect URI registered with the provider.</summary>
        public string? CustomRedirectUri { get; set; }
        public bool? Native { get; set; }
        /// <summary><c>native</c>, <c>redirect</c>, or <c>""</c> to clear the override.</summary>
        public string? Mode { get; set; }
        /// <summary>The brand shown on the button (e.g. <c>Discord</c>); empty clears.</summary>
        public string? DisplayName { get; set; }
        /// <summary>An http(s) or <c>data:image</c> icon URL for the button; empty clears.</summary>
        public string? IconUrl { get; set; }

        /// <summary>Set one provider setting (a <c>null</c> value clears it).</summary>
        public UpsertOAuthProviderBody Setting(string key, string? value)
        {
            (Settings ??= new Dictionary<string, string?>())[key] = value;
            return this;
        }

        /// <summary>Clear one stored provider setting (sends it as <c>null</c>).</summary>
        public UpsertOAuthProviderBody ClearSetting(string key) => Setting(key, null);

        /// <summary>Sign in with Ethereum verifier settings: the expected <paramref name="domain"/>, allowed EIP-155 <paramref name="chainIds"/> (comma-separated), and an https <paramref name="rpcUrl"/> for contract wallets. Null leaves one unchanged.</summary>
        public static UpsertOAuthProviderBody Siwe(string? domain = null, string? chainIds = null, string? rpcUrl = null)
            => new UpsertOAuthProviderBody().SetIf("domain", domain).SetIf("chain_ids", chainIds).SetIf("rpc_url", rpcUrl);

        /// <summary>Sign in with Solana verifier settings: the expected <paramref name="domain"/> and allowed <paramref name="networks"/> (comma-separated: mainnet, devnet, testnet).</summary>
        public static UpsertOAuthProviderBody Siws(string? domain = null, string? networks = null)
            => new UpsertOAuthProviderBody().SetIf("domain", domain).SetIf("networks", networks);

        /// <summary>Sign in with Farcaster verifier settings: <paramref name="domain"/>, <paramref name="chainIds"/>, an https <paramref name="optimismRpcUrl"/> (required to verify FID ownership) and the <paramref name="idRegistryAddress"/>.</summary>
        public static UpsertOAuthProviderBody Siwf(string? domain = null, string? chainIds = null, string? optimismRpcUrl = null, string? idRegistryAddress = null)
            => new UpsertOAuthProviderBody().SetIf("domain", domain).SetIf("chain_ids", chainIds)
                .SetIf("optimism_rpc_url", optimismRpcUrl).SetIf("id_registry_address", idRegistryAddress);

        private UpsertOAuthProviderBody SetIf(string key, string? value) => value == null ? this : Setting(key, value);
    }

    public sealed class OAuthConnectionTest
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_connection_test";
        public string Provider { get; init; } = "";
        public bool Ok { get; init; }
        public string Reason { get; init; } = "";
        public string Message { get; init; } = "";
        public List<string> Checked { get; init; } = new List<string>();
        public List<string> NotChecked { get; init; } = new List<string>();
    }

    public sealed class UpsertOAuthProviderResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_provider";
        public string Provider { get; init; } = "";
        public bool Configured { get; init; }
        public bool? Enabled { get; init; }
        public Dictionary<string, string>? SettingValues { get; init; }
        public List<string>? UpdatedFields { get; init; }
        public string? Warning { get; init; }
    }

    public sealed class DeleteOAuthProviderResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_provider";
        public string Provider { get; init; } = "";
        public bool Deleted { get; init; }
        public string Note { get; init; } = "";
    }

    public sealed class OAuthProviderEnabledResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_provider";
        public string Provider { get; init; } = "";
        public bool Enabled { get; init; }
    }

    public sealed class OAuthProviderScopeResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_provider";
        public string Provider { get; init; } = "";
        public bool AllowSignIn { get; init; }
        public bool AllowSignUp { get; init; }
    }

    /// <summary>The social sign-in providers namespace (<c>/v1/oauth_providers</c>).</summary>
    public sealed class OAuthProvidersResource : ResourceBase
    {
        public OAuthProvidersResource(AtlasTransport transport) : base(transport) { }

        /// <summary>The whole catalog, configured or not.</summary>
        public Task<ListPage<OAuthProvider>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<OAuthProvider>>(HttpVerb.Get, "/v1/oauth_providers", cancellationToken: ct);

        /// <summary>The catalog narrowed to the given provider keys and/or configured state (null skips a filter).</summary>
        public Task<ListPage<OAuthProvider>> ListAsync(IEnumerable<string>? providers, bool? configured = null, CancellationToken ct = default)
            => Req<ListPage<OAuthProvider>>(HttpVerb.Get, "/v1/oauth_providers",
                query: Q(("provider", providers), ("configured", configured)), cancellationToken: ct);

        public Task<OAuthProvider> GetAsync(string provider, CancellationToken ct = default)
            => Req<OAuthProvider>(HttpVerb.Get, $"/v1/oauth_providers/{Enc(provider)}", cancellationToken: ct);

        /// <summary>Set (or replace) this instance's own credentials for a provider.</summary>
        public Task<UpsertOAuthProviderResult> UpsertAsync(string provider, UpsertOAuthProviderBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<UpsertOAuthProviderResult>(HttpVerb.Put, $"/v1/oauth_providers/{Enc(provider)}", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<DeleteOAuthProviderResult> DeleteAsync(string provider, CancellationToken ct = default)
            => Req<DeleteOAuthProviderResult>(HttpVerb.Delete, $"/v1/oauth_providers/{Enc(provider)}", cancellationToken: ct);

        /// <summary>Turn a configured provider on or off. Refused if it has no usable credentials.</summary>
        public Task<OAuthProviderEnabledResult> SetEnabledAsync(string provider, bool enabled, CancellationToken ct = default)
            => Req<OAuthProviderEnabledResult>(HttpVerb.Post, $"/v1/oauth_providers/{Enc(provider)}/enabled",
                body: Body(("enabled", enabled)), cancellationToken: ct);

        /// <summary>§6.5 which screens this provider serves. At least one must be true.</summary>
        public Task<OAuthProviderScopeResult> SetScopeAsync(string provider, bool allowSignIn, bool allowSignUp, CancellationToken ct = default)
            => Req<OAuthProviderScopeResult>(HttpVerb.Post, $"/v1/oauth_providers/{Enc(provider)}/scope",
                body: Body(("allow_sign_in", allowSignIn), ("allow_sign_up", allowSignUp)), cancellationToken: ct);

        /// <summary>Verify the stored credentials against the provider's token endpoint, without enabling it.</summary>
        public Task<OAuthConnectionTest> TestAsync(string provider, CancellationToken ct = default)
            => Req<OAuthConnectionTest>(HttpVerb.Post, $"/v1/oauth_providers/{Enc(provider)}/test", cancellationToken: ct);
    }

    public sealed class ImportOAuthClient
    {
        /// <summary>Preserve the existing client_id for a migration.</summary>
        public string? ClientId { get; set; }
        /// <summary>Plaintext secret; hashed on import. Write-only.</summary>
        public string? ClientSecret { get; set; }
        /// <summary>A pre-computed secret hash to import verbatim (in place of client_secret).</summary>
        public string? ClientSecretHash { get; set; }
        public string? Name { get; set; }
        public List<string>? RedirectUris { get; set; }
        public List<string>? AllowedScopes { get; set; }
        public List<string>? GrantTypes { get; set; }
        public string? TokenEndpointAuthMethod { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public ImportOAuthClient Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class ImportOAuthClientsBody
    {
        /// <summary>The clients to import; each entry is an OAuth client registration.</summary>
        public List<ImportOAuthClient>? Clients { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public ImportOAuthClientsBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class ImportedOAuthClient
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public string? ClientId { get; init; }
        public string? Name { get; init; }
        public string? LogoUrl { get; init; }
        public List<string>? RedirectUris { get; init; }
        public List<string>? AllowedScopes { get; init; }
        public List<string>? GrantTypes { get; init; }
        public string? TokenEndpointAuthMethod { get; init; }
        public string? SecretPrefix { get; init; }
        public bool? IsPublic { get; init; }
        public bool? FirstParty { get; init; }
        public List<string>? PostLogoutRedirectUris { get; init; }
        public string? BackchannelLogoutUri { get; init; }
        public bool? BackchannelLogoutSessionRequired { get; init; }
        public string? FrontchannelLogoutUri { get; init; }
        public bool? FrontchannelLogoutSessionRequired { get; init; }
        /// <summary>The client secret — returned ONCE on create (absent when auth method is `none`).</summary>
        public string? ClientSecret { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long? CreatedAt { get; init; }
        /// <summary>Updated (epoch ms).</summary>
        public long? UpdatedAt { get; init; }
    }
}
