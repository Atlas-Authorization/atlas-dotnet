using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PATCH /v1/oauth_clients/{id}/app_settings</c>. Only the fields set are sent.</summary>
    public sealed class OAuthClientsAppSettingsUpdateBody
    {
        /// <summary>The sign-in methods a session must have used for this app: password, email_code, email_link, passkey, phone_code, siwe, siws, siwf, a provider such as oauth_google, or the families oauth (any social provider) and enterprise_sso (any enterprise connection). The hosted pages offer only these when the</summary>
        [JsonPropertyName("allowed_sign_in_methods")] public List<string>? AllowedSignInMethods { get; set; }
        /// <summary>The heading, sub-heading, logo and primary colour of the hosted sign-in and sign-up pages when the sign-in is for this app.</summary>
        [JsonPropertyName("branding")] public Dictionary<string, object?>? Branding { get; set; }
        /// <summary>The user claims this app receives in its ID token and from userinfo (for example email, name, org_id). Protocol claims (iss, sub, aud, exp, iat, nonce, auth_time, acr, amr, azp, sid) always pass. [] = protocol claims only.</summary>
        [JsonPropertyName("claims_allowlist")] public List<string>? ClaimsAllowlist { get; set; }
        /// <summary>The identity traits a session that came from this app (a native app that exchanged its token for a session) may change through PATCH /v1/client/me. Others are refused with 403.</summary>
        [JsonPropertyName("writable_traits")] public List<string>? WritableTraits { get; set; }
        /// <summary>Overrides of the instance's riskBasedMfa for sign-ins to this app (a sign-in started from this app's /oauth2/authorize request). A field left null keeps the instance value; weights, signals and reputation feeds stay instance-wide.</summary>
        [JsonPropertyName("risk")] public Dictionary<string, object?>? Risk { get; set; }
    }

    /// <summary>Operations of <c>oauthClients.appSettings</c>.</summary>
    public sealed partial class OAuthClientsAppSettingsResource : ResourceBase
    {
        public OAuthClientsAppSettingsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/oauth_clients/{id}/app_settings</c> - The per-application settings of an OAuth client: allowed sign-in methods, hosted-page branding, the user-claim allow-list for ID tokens and userinfo, and the traits a session from this app may write. Null = no restriction.</summary>
        /// <remarks>Needs the <c>oauth_clients:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/oauth_clients/{Enc(id)}/app_settings", cancellationToken: ct);

        /// <summary><c>PATCH /v1/oauth_clients/{id}/app_settings</c> - Change the per-application settings. Each field sent replaces the stored one and null clears it; fields left out keep their value. Unknown fields and malformed values are refused.</summary>
        /// <remarks>Needs the <c>oauth_clients:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, OAuthClientsAppSettingsUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/oauth_clients/{Enc(id)}/app_settings", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
