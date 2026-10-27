using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/oauth_clients/{id}/trusted_issuers</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientsTrustedIssuersCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("issuer")] public string? Issuer { get; set; }
        /// <summary>Inline JWK Set. Provide exactly one of jwks or jwks_uri.</summary>
        [JsonPropertyName("jwks")] public Dictionary<string, object?>? Jwks { get; set; }
        /// <summary>https URL of the issuer JWK Set. Provide exactly one of jwks or jwks_uri.</summary>
        [JsonPropertyName("jwks_uri")] public string? JwksUri { get; set; }
        [JsonPropertyName("subject_mapping")] public string? SubjectMapping { get; set; }
        /// <summary>Fills in issuer and jwks_uri for github_actions, gcp and azure when you omit them.</summary>
        [JsonPropertyName("workload_kind")] public string? WorkloadKind { get; set; }
        /// <summary>Required for workload_kind azure (without an explicit issuer).</summary>
        [JsonPropertyName("tenant_id")] public string? TenantId { get; set; }
        [JsonPropertyName("purpose")] public string? Purpose { get; set; }
        /// <summary>Required when purpose is client_auth or both. A glob with * wildcards; a bare * is refused.</summary>
        [JsonPropertyName("subject_pattern")] public string? SubjectPattern { get; set; }
        /// <summary>Extra claims that must match exactly.</summary>
        [JsonPropertyName("required_claims")] public Dictionary<string, object?>? RequiredClaims { get; set; }
        /// <summary>Accepted aud values. Defaults to the token endpoint, the PAR endpoint and the issuer.</summary>
        [JsonPropertyName("audiences")] public List<string>? Audiences { get; set; }
    }

    /// <summary>Operations of <c>oauthClients.trustedIssuers</c>.</summary>
    public sealed partial class OAuthClientsTrustedIssuersResource : ResourceBase
    {
        public OAuthClientsTrustedIssuersResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/oauth_clients/{id}/trusted_issuers</c> - List the external issuers this OAuth client trusts to assert users through the RFC 7523 jwt-bearer grant.</summary>
        /// <remarks>Needs the <c>oauth_clients:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/oauth_clients/{Enc(id)}/trusted_issuers", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/oauth_clients/{id}/trusted_issuers</c> - Trust an external issuer for this client. With purpose user_assertion (default) its signed JWTs (verified against the inline jwks or the https jwks_uri) can then be presented to POST /oauth2/token with grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer. subject_mapping says how the JWT sub names an Atlas user. With purpose client_auth or both (PRO-06 workload identity) a client registered with token_endpoint_auth_method workload_jwt authenticates by presenting a token from this issuer as its client_assertion; subject_pattern is then required and must name the workload. workload_kind github_actions, gcp and azure fill in the issuer and JWKS URL. Audited as oauth_trusted_issuer.created.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(string id, OAuthClientsTrustedIssuersCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/oauth_clients/{Enc(id)}/trusted_issuers", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/oauth_clients/{id}/trusted_issuers/{issuerId}</c> - Stop trusting an issuer: its assertions are refused at once. Audited as oauth_trusted_issuer.deleted.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, string issuerId, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/oauth_clients/{Enc(id)}/trusted_issuers/{Enc(issuerId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
