using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/oauth_consent_requests/accept</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientsConsentRequestsAcceptBody
    {
        /// <summary>The scopes to grant: a subset of requested_scope that includes openid. Default: all requested.</summary>
        [JsonPropertyName("grant_scope")] public List<string>? GrantScope { get; set; }
        /// <summary>Record the consent so the user is not asked again. Default true.</summary>
        [JsonPropertyName("remember")] public bool? Remember { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/oauth_consent_requests/reject</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientsConsentRequestsRejectBody
    {
        /// <summary>The OAuth error returned to the application. Default access_denied.</summary>
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    /// <summary>Operations of <c>oauthClients.consentRequests</c>.</summary>
    public sealed partial class OAuthClientsConsentRequestsResource : ResourceBase
    {
        public OAuthClientsConsentRequestsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/oauth_consent_requests</c> - Read a consent challenge: the signed-in user, the application and the scopes, audience, authorization details and organization it asks for.</summary>
        /// <remarks>Needs the <c>oauth_clients:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string consentChallenge, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/oauth_consent_requests", query: Q(("consent_challenge", consentChallenge)), cancellationToken: ct);

        /// <summary><c>PUT /v1/oauth_consent_requests/accept</c> - Accept a consent challenge, optionally narrowing the granted scopes. Following redirect_to (in the same browser that started the flow) records the consent unless remember is false, and returns the application to its redirect_uri with a code.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> AcceptAsync(string consentChallenge, OAuthClientsConsentRequestsAcceptBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/oauth_consent_requests/accept", body, query: Q(("dry_run", dryRun), ("consent_challenge", consentChallenge)), cancellationToken: ct);

        /// <summary><c>PUT /v1/oauth_consent_requests/reject</c> - Reject a consent challenge. Following redirect_to returns the application to its redirect_uri with the OAuth error.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> RejectAsync(string consentChallenge, OAuthClientsConsentRequestsRejectBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/oauth_consent_requests/reject", body, query: Q(("dry_run", dryRun), ("consent_challenge", consentChallenge)), cancellationToken: ct);
    }
}
