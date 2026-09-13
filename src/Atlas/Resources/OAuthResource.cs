using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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
        public bool IsPublic { get; init; }
        public bool FirstParty { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
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
        public bool IsPublic { get; init; }
        public bool FirstParty { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        public string? ClientSecret { get; init; }
        public string Note { get; init; } = "";
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
    }

    public sealed class UpdateOAuthClientBody
    {
        public string? Name { get; set; }
        public List<string>? RedirectUris { get; set; }
        public List<string>? AllowedScopes { get; set; }
        public string? TokenEndpointAuthMethod { get; set; }
        public string? LogoUrl { get; set; }
        public bool? FirstParty { get; set; }
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

        public Task<OAuthClientWithSecret> RotateSecretAsync(string id, CancellationToken ct = default)
            => Req<OAuthClientWithSecret>(HttpVerb.Post, $"/v1/oauth_clients/{Enc(id)}/rotate_secret", cancellationToken: ct);

        public Task<DeleteOAuthClientResult> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeleteOAuthClientResult>(HttpVerb.Delete, $"/v1/oauth_clients/{Enc(id)}", cancellationToken: ct);
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
    }

    public sealed class CreateResourceServerBody
    {
        public string Identifier { get; set; } = "";
        public string Name { get; set; } = "";
        /// <summary>Each scope is a bare string or a <see cref="ResourceServerScope"/>.</summary>
        public List<object>? Scopes { get; set; }
        public int? TokenTtlSeconds { get; set; }
        public string? SigningAlg { get; set; }
    }

    public sealed class UpdateResourceServerBody
    {
        public string? Name { get; set; }
        public List<object>? Scopes { get; set; }
        public int? TokenTtlSeconds { get; set; }
        public string? SigningAlg { get; set; }
        /// <summary>Immutable — supplying a different value is rejected.</summary>
        public string? Identifier { get; set; }
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
    }

    public sealed class UpsertOAuthProviderBody
    {
        public string? ClientId { get; set; }
        /// <summary>Write-only; stored encrypted, never returned. Required for redirect-only providers.</summary>
        public string? ClientSecret { get; set; }
        public List<string>? Scopes { get; set; }
        public Dictionary<string, string>? Values { get; set; }
        public Dictionary<string, string>? Settings { get; set; }
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
}
