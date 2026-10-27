using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/oauth_clients/{id}/exchange_policy</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientsSetExchangePolicyBody
    {
        [JsonPropertyName("allowed_audiences")] public List<string>? AllowedAudiences { get; set; }
        [JsonPropertyName("allowed_scopes")] public List<string>? AllowedScopes { get; set; }
        [JsonPropertyName("id_jag_audiences")] public List<string>? IdJagAudiences { get; set; }
        [JsonPropertyName("allow_impersonation")] public bool? AllowImpersonation { get; set; }
        [JsonPropertyName("require_may_act")] public bool? RequireMayAct { get; set; }
        [JsonPropertyName("max_delegation_depth")] public long? MaxDelegationDepth { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/oauth_clients/{id}/profile</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientsSetProfileBody
    {
        /// <summary>Required. A built-in or custom profile name; null clears.</summary>
        [JsonPropertyName("profile")] public string? Profile { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/oauth_clients/{id}/delegated_ui</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientsSetDelegatedUiBody
    {
        /// <summary>https URL of your sign-in page; null clears it. Omit to leave unchanged.</summary>
        [JsonPropertyName("login_url")] public string? LoginUrl { get; set; }
        /// <summary>https URL of your consent page; null clears it. Omit to leave unchanged.</summary>
        [JsonPropertyName("consent_url")] public string? ConsentUrl { get; set; }
    }

    /// <summary>More operations of <c>oauthClients</c>.</summary>
    public sealed partial class OAuthClientsResource
    {
        private OAuthClientsAppSettingsResource? _appSettings;
        /// <summary>The <c>oauthClients.appSettings</c> operations.</summary>
        public OAuthClientsAppSettingsResource AppSettings => _appSettings ??= new OAuthClientsAppSettingsResource(Transport);

        private OAuthClientsWorkloadIdentityResource? _workloadIdentity;
        /// <summary>The <c>oauthClients.workloadIdentity</c> operations.</summary>
        public OAuthClientsWorkloadIdentityResource WorkloadIdentity => _workloadIdentity ??= new OAuthClientsWorkloadIdentityResource(Transport);

        private OAuthClientsTrustedIssuersResource? _trustedIssuers;
        /// <summary>The <c>oauthClients.trustedIssuers</c> operations.</summary>
        public OAuthClientsTrustedIssuersResource TrustedIssuers => _trustedIssuers ??= new OAuthClientsTrustedIssuersResource(Transport);

        private OAuthClientsLoginRequestsResource? _loginRequests;
        /// <summary>The <c>oauthClients.loginRequests</c> operations.</summary>
        public OAuthClientsLoginRequestsResource LoginRequests => _loginRequests ??= new OAuthClientsLoginRequestsResource(Transport);

        private OAuthClientsConsentRequestsResource? _consentRequests;
        /// <summary>The <c>oauthClients.consentRequests</c> operations.</summary>
        public OAuthClientsConsentRequestsResource ConsentRequests => _consentRequests ??= new OAuthClientsConsentRequestsResource(Transport);

        /// <summary><c>GET /v1/oauth_clients/{id}/exchange_policy</c> - Read the token-exchange policy of an OAuth client (the defaults, with configured=false, when none is set).</summary>
        /// <remarks>Needs the <c>oauth_clients:read</c> scope.</remarks>
        public Task<JsonElement> GetExchangePolicyAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/oauth_clients/{Enc(id)}/exchange_policy", cancellationToken: ct);

        /// <summary><c>PUT /v1/oauth_clients/{id}/exchange_policy</c> - Set the token-exchange policy of an OAuth client: allowed audiences and scopes, whether it may impersonate (no actor_token), whether delegation needs may_act, and the maximum act-chain depth. Audited as oauth_exchange_policy.updated.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> SetExchangePolicyAsync(string id, OAuthClientsSetExchangePolicyBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/oauth_clients/{Enc(id)}/exchange_policy", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/oauth_clients/{id}/exchange_policy</c> - Remove the token-exchange policy of an OAuth client, restoring the defaults.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> DeleteExchangePolicyAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/oauth_clients/{Enc(id)}/exchange_policy", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>PUT /v1/oauth_clients/{id}/profile</c> - Assign a policy profile to an OAuth client (profile null clears it). The client must already conform (auth method, redirect URIs); the profile is then enforced at /oauth2/authorize, /oauth2/par and /oauth2/token.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> SetProfileAsync(string id, OAuthClientsSetProfileBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/oauth_clients/{Enc(id)}/profile", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>PUT /v1/oauth_clients/{id}/delegated_ui</c> - Set (or clear, with null) the URLs of your own sign-in and consent pages for an OAuth client. /oauth2/authorize then sends the browser there with a login_challenge / consent_challenge instead of Atlas’s pages. A client URL overrides the instance-wide oauthProvider.loginUrl / consentUrl.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> SetDelegatedUiAsync(string id, OAuthClientsSetDelegatedUiBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/oauth_clients/{Enc(id)}/delegated_ui", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
