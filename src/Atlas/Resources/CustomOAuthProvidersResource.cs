using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The URLs to register at an upstream OAuth / OIDC provider.</summary>
    public sealed class UpstreamUrls
    {
        /// <summary>The client JWKS URL (private_key_jwt).</summary>
        public string ClientJwksUri { get; init; } = "";
        public string BackchannelLogoutUri { get; init; } = "";
        public string FrontchannelLogoutUri { get; init; } = "";
        public string PostLogoutRedirectUri { get; init; } = "";
        public string InitiateLoginUri { get; init; } = "";
    }

    /// <summary>A private_key_jwt signing key Atlas holds for an upstream provider.</summary>
    public sealed class UpstreamSigningKey
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "upstream_signing_key";
        public string Kid { get; init; } = "";
        /// <summary><c>RS256</c>, <c>PS256</c> or <c>ES256</c>.</summary>
        public string Alg { get; init; } = "";
        /// <summary><c>active</c>, <c>retiring</c> or <c>retired</c>.</summary>
        public string Status { get; init; } = "";
        public Dictionary<string, JsonElement>? PublicJwk { get; init; }
        public long CreatedAt { get; init; }
        public long? RetiredAt { get; init; }
    }

    /// <summary>A custom upstream OAuth 2.0 / OpenID Connect provider. The client secret is never returned.</summary>
    public sealed class CustomOAuthProvider
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "custom_oauth_provider";
        public string Id { get; init; } = "";
        public string ProviderKey { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string? IconUrl { get; init; }
        /// <summary><c>oidc</c> or <c>oauth2</c>.</summary>
        public string Protocol { get; init; } = "";
        public string? Issuer { get; init; }
        public string? DiscoveryUrl { get; init; }
        public string? AuthorizationEndpoint { get; init; }
        public string? TokenEndpoint { get; init; }
        public string? UserinfoEndpoint { get; init; }
        public string? JwksUri { get; init; }
        public string? EndSessionEndpoint { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        public string ScopeSeparator { get; init; } = " ";
        public bool Pkce { get; init; }
        public Dictionary<string, string>? AuthorizationParams { get; init; }
        public Dictionary<string, string>? TokenParams { get; init; }
        public string UserinfoAuth { get; init; } = "header";
        public string TokenEndpointAuthMethod { get; init; } = "";
        public string? ClientAssertionAudience { get; init; }
        public string ProfileSource { get; init; } = "merged";
        public JsonElement? ProfileMapping { get; init; }
        public JsonElement? Logout { get; init; }
        public JsonElement? IdpInitiatedLogin { get; init; }
        public string? ClientId { get; init; }
        public bool HasSecret { get; init; }
        public bool Enabled { get; init; }
        public bool AllowSignIn { get; init; }
        public bool AllowSignUp { get; init; }
        /// <summary>Register at the provider as the redirect / callback URI.</summary>
        public string RedirectUri { get; init; } = "";
        public string ClientJwksUri { get; init; } = "";
        public string BackchannelLogoutUri { get; init; } = "";
        public string FrontchannelLogoutUri { get; init; } = "";
        public string PostLogoutRedirectUri { get; init; } = "";
        public string InitiateLoginUri { get; init; } = "";
        public List<UpstreamSigningKey> SigningKeys { get; init; } = new List<UpstreamSigningKey>();
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    /// <summary>
    /// Body for creating (<see cref="ProviderKey"/> and <see cref="DisplayName"/> required) or patching a custom
    /// provider. <see cref="ClientSecret"/> is write-only; <see cref="Set"/> sends an explicit null to clear a setting.
    /// </summary>
    public sealed class CustomOAuthProviderBody
    {
        public string? ProviderKey { get; set; }
        public string? DisplayName { get; set; }
        public string? IconUrl { get; set; }
        /// <summary><c>oidc</c> (default) or <c>oauth2</c>.</summary>
        public string? Protocol { get; set; }
        public string? Issuer { get; set; }
        public string? DiscoveryUrl { get; set; }
        public string? AuthorizationEndpoint { get; set; }
        public string? TokenEndpoint { get; set; }
        public string? UserinfoEndpoint { get; set; }
        public string? JwksUri { get; set; }
        public string? EndSessionEndpoint { get; set; }
        public List<string>? Scopes { get; set; }
        /// <summary><c>" "</c>, <c>","</c> or <c>"+"</c>.</summary>
        public string? ScopeSeparator { get; set; }
        public bool? Pkce { get; set; }
        public Dictionary<string, string>? AuthorizationParams { get; set; }
        public Dictionary<string, string>? TokenParams { get; set; }
        /// <summary><c>header</c> or <c>query</c>.</summary>
        public string? UserinfoAuth { get; set; }
        /// <summary>client_secret_basic, client_secret_post, client_secret_jwt, private_key_jwt or none.</summary>
        public string? TokenEndpointAuthMethod { get; set; }
        public string? ClientAssertionAudience { get; set; }
        /// <summary><c>merged</c>, <c>userinfo</c> or <c>id_token</c>.</summary>
        public string? ProfileSource { get; set; }
        /// <summary><c>fields</c>, <c>public_metadata</c>, <c>private_metadata</c>, <c>update_on_sign_in</c>.</summary>
        public Dictionary<string, object?>? ProfileMapping { get; set; }
        public Dictionary<string, object?>? Logout { get; set; }
        public Dictionary<string, object?>? IdpInitiatedLogin { get; set; }
        public string? ClientId { get; set; }
        /// <summary>Write-only; never returned.</summary>
        public string? ClientSecret { get; set; }
        public bool? Enabled { get; set; }
        public bool? AllowSignIn { get; set; }
        public bool? AllowSignUp { get; set; }

        /// <summary>Extra wire fields, including explicit <c>null</c>s that clear a stored value.</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name (snake_case), including <c>null</c> to clear it.</summary>
        public CustomOAuthProviderBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>A profile mapping run against a sample: the mapped profile, a per-target trace and warnings.</summary>
    public sealed class ProfileMappingPreview
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "profile_mapping_preview";
        public Dictionary<string, JsonElement> Profile { get; init; } = new Dictionary<string, JsonElement>();
        public Dictionary<string, JsonElement> Trace { get; init; } = new Dictionary<string, JsonElement>();
        public List<string> Warnings { get; init; } = new List<string>();
    }

    /// <summary>
    /// <c>/v1/custom_oauth_providers</c> — your own upstream OAuth 2.0 / OpenID Connect login providers, with Atlas
    /// as the relying party: endpoints (or just an OIDC issuer), token-endpoint client authentication (incl.
    /// private_key_jwt with an Atlas-held key), a declarative profile mapping, upstream logout and third-party
    /// initiated login. The provider then signs users in like any built-in one, under its provider key.
    /// </summary>
    public sealed class CustomOAuthProvidersResource : ResourceBase
    {
        public CustomOAuthProvidersResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<CustomOAuthProvider>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<CustomOAuthProvider>>(HttpVerb.Get, "/v1/custom_oauth_providers", cancellationToken: ct);

        public Task<CustomOAuthProvider> GetAsync(string id, CancellationToken ct = default)
            => Req<CustomOAuthProvider>(HttpVerb.Get, $"/v1/custom_oauth_providers/{Enc(id)}", cancellationToken: ct);

        public Task<CustomOAuthProvider> CreateAsync(CustomOAuthProviderBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<CustomOAuthProvider>(HttpVerb.Post, "/v1/custom_oauth_providers", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<CustomOAuthProvider> UpdateAsync(string id, CustomOAuthProviderBody body, CancellationToken ct = default)
            => Req<CustomOAuthProvider>(HttpVerb.Patch, $"/v1/custom_oauth_providers/{Enc(id)}", body, cancellationToken: ct);

        public Task<JsonElement> DeleteAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/custom_oauth_providers/{Enc(id)}", cancellationToken: ct);

        /// <summary>Fetch and validate an issuer's OpenID configuration (for autofill).</summary>
        public Task<JsonElement> DiscoverAsync(string issuer, string? discoveryUrl = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/custom_oauth_providers/discover", Body(("issuer", issuer), ("discovery_url", discoveryUrl)), cancellationToken: ct);

        /// <summary>Run a mapping (<c>profile_mapping</c>) against a pasted sample (<c>userinfo</c>, <c>id_token_claims</c>, <c>id_token</c>).</summary>
        public Task<ProfileMappingPreview> PreviewProfileMappingAsync(Dictionary<string, object?> body, CancellationToken ct = default)
            => Req<ProfileMappingPreview>(HttpVerb.Post, "/v1/custom_oauth_providers/profile_mapping/preview", body, cancellationToken: ct);

        /// <summary>Run a provider's stored mapping (or the body's) against a pasted sample.</summary>
        public Task<ProfileMappingPreview> PreviewProfileMappingAsync(string id, Dictionary<string, object?> body, CancellationToken ct = default)
            => Req<ProfileMappingPreview>(HttpVerb.Post, $"/v1/custom_oauth_providers/{Enc(id)}/profile_mapping/preview", body, cancellationToken: ct);

        /// <summary>The provider's private_key_jwt signing keys.</summary>
        public Task<ListPage<UpstreamSigningKey>> ListSigningKeysAsync(string id, CancellationToken ct = default)
            => Req<ListPage<UpstreamSigningKey>>(HttpVerb.Get, $"/v1/custom_oauth_providers/{Enc(id)}/signing_keys", cancellationToken: ct);

        /// <summary>Generate a new active key (<c>RS256</c>, <c>PS256</c> or <c>ES256</c>); the previous one turns <c>retiring</c>.</summary>
        public Task<UpstreamSigningKey> RotateSigningKeyAsync(string id, string? alg = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<UpstreamSigningKey>(HttpVerb.Post, $"/v1/custom_oauth_providers/{Enc(id)}/signing_keys", Body(("alg", alg)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Retire a non-active key (it leaves the JWKS).</summary>
        public Task<UpstreamSigningKey> RetireSigningKeyAsync(string id, string kid, CancellationToken ct = default)
            => Req<UpstreamSigningKey>(HttpVerb.Post, $"/v1/custom_oauth_providers/{Enc(id)}/signing_keys/{Enc(kid)}/retire", cancellationToken: ct);
    }
}
