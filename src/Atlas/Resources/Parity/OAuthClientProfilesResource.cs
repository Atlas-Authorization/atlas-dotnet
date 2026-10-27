using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/oauth_client_profiles/{name}</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientProfilesSetBody
    {
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("require_pkce")] public bool? RequirePkce { get; set; }
        /// <summary>Authorization requests must be pushed (RFC 9126).</summary>
        [JsonPropertyName("require_par")] public bool? RequirePar { get; set; }
        /// <summary>The token request must prove DPoP, mutual TLS, or either (any).</summary>
        [JsonPropertyName("sender_constraint")] public string? SenderConstraint { get; set; }
        /// <summary>Authorization requests must be a signed request object (RFC 9101), pushed or direct.</summary>
        [JsonPropertyName("require_signed_request")] public bool? RequireSignedRequest { get; set; }
        /// <summary>Allowed client authentication methods; null = any.</summary>
        [JsonPropertyName("token_endpoint_auth_methods")] public List<string>? TokenEndpointAuthMethods { get; set; }
        /// <summary>JWS algorithms accepted for request objects and client assertions; null = any asymmetric.</summary>
        [JsonPropertyName("signing_algs")] public List<string>? SigningAlgs { get; set; }
        [JsonPropertyName("https_redirects_only")] public bool? HttpsRedirectsOnly { get; set; }
        /// <summary>Claim names a `claims` request may ask for; null = any.</summary>
        [JsonPropertyName("allowed_claims")] public List<string>? AllowedClaims { get; set; }
    }

    /// <summary>Operations of <c>oauthClientProfiles</c>.</summary>
    public sealed partial class OAuthClientProfilesResource : ResourceBase
    {
        public OAuthClientProfilesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/oauth_client_profiles</c> - List the OAuth client policy profiles: the built-ins (fapi2_security, fapi2_message_signing, pkce_required, par_required, dpop_required) and your custom ones.</summary>
        /// <remarks>Needs the <c>oauth_clients:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/oauth_client_profiles", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>PUT /v1/oauth_client_profiles/{name}</c> - Create or replace a custom client policy profile (rules: PKCE, PAR, sender constraint, signed request object, allowed auth methods and signing algorithms, https-only redirects, allowed claims). Built-in names are reserved.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> SetAsync(string name, OAuthClientProfilesSetBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/oauth_client_profiles/{Enc(name)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/oauth_client_profiles/{name}</c> - Delete a custom client policy profile. 409 while any client is assigned it.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string name, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/oauth_client_profiles/{Enc(name)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
