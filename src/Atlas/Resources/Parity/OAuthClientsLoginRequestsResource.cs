using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/oauth_login_requests/accept</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientsLoginRequestsAcceptBody
    {
        /// <summary>Required. The Atlas user id the visitor authenticated as.</summary>
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        /// <summary>Keep the Atlas session across browser restarts.</summary>
        [JsonPropertyName("remember")] public bool? Remember { get; set; }
        /// <summary>Authentication methods used, recorded on the session and echoed in the id_token.</summary>
        [JsonPropertyName("amr")] public List<string>? Amr { get; set; }
        /// <summary>Authentication context class reached.</summary>
        [JsonPropertyName("acr")] public string? Acr { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/oauth_login_requests/reject</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientsLoginRequestsRejectBody
    {
        /// <summary>The OAuth error returned to the application. Default access_denied.</summary>
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    /// <summary>Operations of <c>oauthClients.loginRequests</c>.</summary>
    public sealed partial class OAuthClientsLoginRequestsResource : ResourceBase
    {
        public OAuthClientsLoginRequestsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/oauth_login_requests</c> - Read a login challenge: which application is signing a user in, with what scopes, and who (if anyone) is already signed in on this browser.</summary>
        /// <remarks>Needs the <c>oauth_clients:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string loginChallenge, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/oauth_login_requests", query: Q(("login_challenge", loginChallenge)), cancellationToken: ct);

        /// <summary><c>PUT /v1/oauth_login_requests/accept</c> - Accept a login challenge: the visitor authenticated as `subject` (an Atlas user id). Returns redirect_to; when the browser follows it Atlas signs the user in and continues the authorization. The returned link is single-use and valid for five minutes.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> AcceptAsync(string loginChallenge, OAuthClientsLoginRequestsAcceptBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/oauth_login_requests/accept", body, query: Q(("dry_run", dryRun), ("login_challenge", loginChallenge)), cancellationToken: ct);

        /// <summary><c>PUT /v1/oauth_login_requests/reject</c> - Reject a login challenge. Following redirect_to returns the application to its redirect_uri with the OAuth error.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> RejectAsync(string loginChallenge, OAuthClientsLoginRequestsRejectBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/oauth_login_requests/reject", body, query: Q(("dry_run", dryRun), ("login_challenge", loginChallenge)), cancellationToken: ct);
    }
}
