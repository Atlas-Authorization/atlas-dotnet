using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class Instance
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "instance";
        public string Id { get; init; } = "";
        public string Environment { get; init; } = "";
        public string PublishableKey { get; init; } = "";
        public string FrontendApiHost { get; init; } = "";
        /// <summary>The <c>iss</c> the instance's session tokens carry. It changes when a custom domain goes active, so read it rather than hard-coding it.</summary>
        public string? Issuer { get; init; }
        /// <summary>The OpenID discovery document whose <c>issuer</c> equals <see cref="Issuer"/>.</summary>
        public string? DiscoveryUrl { get; init; }
        /// <summary>The JWKS holding the instance's signing keys.</summary>
        public string? JwksUri { get; init; }
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        /// <summary>The declared integration per allowed origin (<c>redirect</c> or <c>embedded</c>). An origin not listed is inferred.</summary>
        public Dictionary<string, string>? AllowedOriginIntegrations { get; init; }
        public Metadata AuthConfig { get; init; } = new Metadata();
        public long CreatedAt { get; init; }
    }

    /// <summary>
    /// One <c>allowed_origins</c> entry that may also declare how the origin signs in. With no
    /// integration it is sent as the plain origin string (keeping the hint the origin already had);
    /// otherwise as <c>{ origin, integration }</c>.
    /// </summary>
    public sealed class AllowedOriginEntry
    {
        /// <summary>Hosted-redirect OIDC: the app never runs the embedded components.</summary>
        public const string Redirect = "redirect";
        /// <summary>The sign-in components run on the origin.</summary>
        public const string Embedded = "embedded";
        /// <summary>Clear the declared integration; Atlas infers it.</summary>
        public const string Auto = "auto";

        public AllowedOriginEntry(string origin, string? integration = null)
        {
            Origin = origin;
            Integration = integration;
        }

        public string Origin { get; }
        /// <summary><see cref="Redirect"/>, <see cref="Embedded"/>, <see cref="Auto"/>, or null to keep the current hint.</summary>
        public string? Integration { get; }

        internal static List<object> ToWire(IEnumerable<AllowedOriginEntry> entries)
        {
            var list = new List<object>();
            foreach (var e in entries)
            {
                if (e.Integration == null) list.Add(e.Origin);
                else list.Add(new Dictionary<string, object?> { ["origin"] = e.Origin, ["integration"] = e.Integration });
            }
            return list;
        }
    }

    public sealed class UpdateInstanceBody
    {
        /// <summary>Exact origins only — a wildcard or a path is rejected (§13.1).</summary>
        public List<string>? AllowedOrigins { get; set; }
        /// <summary>Also takes <c>strictRequestBodies</c>, <c>dangerouslyReportSkippedEmailsAsVerified</c> and <c>branding.audiences</c>.</summary>
        public Dictionary<string, object?>? AuthConfig { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is.</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name, including one this SDK does not model yet.</summary>
        public UpdateInstanceBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }

        /// <summary>Send <c>allowed_origins</c> as entries that may also declare how each origin signs in; replaces <see cref="AllowedOrigins"/>.</summary>
        public UpdateInstanceBody WithAllowedOriginEntries(IEnumerable<AllowedOriginEntry> entries)
        {
            AllowedOrigins = null;
            return Set("allowed_origins", AllowedOriginEntry.ToWire(entries));
        }
    }

    /// <summary><c>PATCH /v1/instance</c> echoes only the mutable fields, not a full Instance.</summary>
    public sealed class InstanceUpdateResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "instance";
        public string Id { get; init; } = "";
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        public Dictionary<string, string>? AllowedOriginIntegrations { get; init; }
        public Metadata AuthConfig { get; init; } = new Metadata();
    }

    /// <summary>The instance-config namespace (<c>/v1/instance</c>).</summary>
    public sealed class InstanceResource : ResourceBase
    {
        public InstanceResource(AtlasTransport transport) : base(transport) { }

        public Task<Instance> GetAsync(CancellationToken ct = default)
            => Req<Instance>(HttpVerb.Get, "/v1/instance", cancellationToken: ct);

        public Task<InstanceUpdateResult> UpdateAsync(UpdateInstanceBody body, CancellationToken ct = default)
            => Req<InstanceUpdateResult>(HttpVerb.Patch, "/v1/instance", body, cancellationToken: ct);

        /// <summary><c>GET /v1/instance/preflight</c> — Check your setup in one call.</summary>
        public Task<Preflight> PreflightAsync(CancellationToken ct = default)
            => Req<Preflight>(HttpVerb.Get, "/v1/instance/preflight", cancellationToken: ct);

        /// <summary><c>POST /v1/instance/move_origin</c> — Move every URL that points at one origin to another (a domain move), across the instance, its OAuth clients, webhooks, redirect URLs and Sign in with Atlas apps. DryRun writes nothing.</summary>
        public Task<OriginMove> MoveOriginAsync(OriginMoveBody body, CancellationToken ct = default)
            => Req<OriginMove>(HttpVerb.Post, "/v1/instance/move_origin", body, cancellationToken: ct);

        /// <summary><c>GET /v1/instance/connections</c> — The instance's links to other platforms and their health. Read-only.</summary>
        public Task<InstanceConnections> ConnectionsAsync(CancellationToken ct = default)
            => Req<InstanceConnections>(HttpVerb.Get, "/v1/instance/connections", cancellationToken: ct);

        /// <summary><c>GET /v1/instance/integrations/chatly</c> — The Chatly connection.</summary>
        public Task<ChatlyIntegration> GetChatlyIntegrationAsync(CancellationToken ct = default)
            => Req<ChatlyIntegration>(HttpVerb.Get, "/v1/instance/integrations/chatly", cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/integrations/chatly</c> — Connect a Chatly channel (its public id).</summary>
        public Task<ChatlyIntegration> SetChatlyIntegrationAsync(string channel, CancellationToken ct = default)
            => Req<ChatlyIntegration>(HttpVerb.Put, "/v1/instance/integrations/chatly", Body(("channel", channel)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/integrations/chatly</c> — Remove the Chatly connection.</summary>
        public Task<ChatlyIntegration> DeleteChatlyIntegrationAsync(CancellationToken ct = default)
            => Req<ChatlyIntegration>(HttpVerb.Delete, "/v1/instance/integrations/chatly", cancellationToken: ct);

        /// <summary><c>GET /v1/instance/integrations/forumulate</c> — The Forumulate connection.</summary>
        public Task<ForumulateIntegration> GetForumulateIntegrationAsync(CancellationToken ct = default)
            => Req<ForumulateIntegration>(HttpVerb.Get, "/v1/instance/integrations/forumulate", cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/integrations/forumulate</c> — Connect Forumulate spaces (replaces the whole connection).</summary>
        public Task<ForumulateIntegration> SetForumulateIntegrationAsync(ForumulateIntegrationBody body, CancellationToken ct = default)
            => Req<ForumulateIntegration>(HttpVerb.Put, "/v1/instance/integrations/forumulate", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/integrations/forumulate</c> — Remove the Forumulate connection.</summary>
        public Task<ForumulateIntegration> DeleteForumulateIntegrationAsync(CancellationToken ct = default)
            => Req<ForumulateIntegration>(HttpVerb.Delete, "/v1/instance/integrations/forumulate", cancellationToken: ct);

        /// <summary><c>GET /v1/instance/cookie_consent</c> — Read the cookie-consent CMP injected into the hosted sign-in, sign-up and account pages (enabled:false when none).</summary>
        public Task<CookieConsent> GetCookieConsentAsync(CancellationToken ct = default)
            => Req<CookieConsent>(HttpVerb.Get, "/v1/instance/cookie_consent", cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/cookie_consent</c> — Set the cookie-consent CMP for the hosted pages (any provider).</summary>
        public Task<CookieConsent> ReplaceCookieConsentAsync(CookieConsentBody body, CancellationToken ct = default)
            => Req<CookieConsent>(HttpVerb.Put, "/v1/instance/cookie_consent", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/cookie_consent</c> — Turn off the hosted-page cookie-consent CMP: no script is injected and the CSP goes back to normal.</summary>
        public Task<CookieConsent> DeleteCookieConsentAsync(CancellationToken ct = default)
            => Req<CookieConsent>(HttpVerb.Delete, "/v1/instance/cookie_consent", cancellationToken: ct);
    }

    // ---------------- Instance security ----------------

    /// <summary>Per-tenant kill switches. camelCase on the wire.</summary>
    public sealed class InstanceKillSwitches
    {
        [JsonPropertyName("disableSignUps")] public bool? DisableSignUps { get; set; }
        [JsonPropertyName("disableSignIns")] public bool? DisableSignIns { get; set; }
        [JsonPropertyName("forceChallenge")] public bool? ForceChallenge { get; set; }
        [JsonPropertyName("disabledProviders")] public List<string>? DisabledProviders { get; set; }
    }

    public sealed class InstanceSecurity
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "instance_security";
        public InstanceKillSwitches KillSwitches { get; init; } = new InstanceKillSwitches();
        public List<string> IpAllowlist { get; init; } = new List<string>();
    }

    public sealed class UpdateInstanceSecurityBody
    {
        public InstanceKillSwitches? KillSwitches { get; set; }
        /// <summary>CIDRs / IPs; <c>[]</c> clears the allowlist. Every entry is validated.</summary>
        public List<string>? IpAllowlist { get; set; }
        /// <summary>
        /// Required to set an allowlist that omits the caller's own IP — which would
        /// lock THIS management API out irreversibly. Otherwise such a list is a 422.
        /// </summary>
        public bool? ConfirmLockout { get; set; }
    }

    /// <summary>A confirmed self-lockout returns the config plus a plain-language <c>warning</c>.</summary>
    public sealed class InstanceSecurityResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "instance_security";
        public InstanceKillSwitches KillSwitches { get; init; } = new InstanceKillSwitches();
        public List<string> IpAllowlist { get; init; } = new List<string>();
        public string? Warning { get; init; }
    }

    public sealed class CaptchaSecretResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "captcha_secret";
        public string Provider { get; init; } = "";
        public bool Set { get; init; }
        public bool Deleted { get; init; }
        /// <summary>The captcha flows a forced delete switched off.</summary>
        [JsonPropertyName("flows_turned_off")] public List<string>? FlowsTurnedOff { get; init; }
    }

    public sealed class KerberosSecretResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "kerberos_secret";
        public bool Set { get; init; }
        public bool Deleted { get; init; }
    }

    /// <summary>
    /// The trusted reverse proxy secret's state, never the secret. A proxy that serves the Frontend API from your own origin
    /// sends the secret in <c>X-Atlas-Proxy-Secret</c> and the browser's IP in <c>X-Atlas-Client-IP</c>; Atlas then uses that
    /// IP for rate limits, lockout, risk signals, geo, audit logs and session metadata.
    /// </summary>
    public class ProxySecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "proxy_secret";
        /// <summary>Whether a proxy secret is set.</summary>
        public bool Set { get; init; }
        /// <summary>When the current secret was created (epoch ms), or null.</summary>
        public long? CreatedAt { get; init; }
        /// <summary>After a rotation: when the replaced secret stops working (epoch ms), or null.</summary>
        public long? PreviousExpiresAt { get; init; }
        /// <summary><c>X-Atlas-Proxy-Secret</c>.</summary>
        public string SecretHeader { get; init; } = "";
        /// <summary><c>X-Atlas-Client-IP</c>.</summary>
        [JsonPropertyName("client_ip_header")] public string ClientIpHeader { get; init; } = "";
    }

    /// <summary>Create / rotate result: the secret, shown only here.</summary>
    public sealed class ProxySecretCreated : ProxySecret
    {
        /// <summary><c>aps_...</c>, returned once. Store it in your proxy's environment; Atlas keeps only a hash.</summary>
        public string Secret { get; init; } = "";
        /// <summary>true when an existing secret was replaced.</summary>
        public bool Rotated { get; init; }
    }

    /// <summary>The body of <c>POST /v1/instance/proxy_secret</c>.</summary>
    public sealed class CreateProxySecretBody
    {
        /// <summary>On a rotation: how long the replaced secret keeps working, in seconds. Null uses the default, 86400 (24 hours); 0 stops it now; at most 604800 (7 days).</summary>
        public int? OverlapSeconds { get; set; }
    }

    public sealed class ProxySecretDeleted
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "proxy_secret";
        public bool Deleted { get; init; }
    }

    public sealed class LdapBindPasswordResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "ldap_bind_password";
        public string ConnectionId { get; init; } = "";
        public bool Set { get; init; }
        public bool Deleted { get; init; }
    }

    /// <summary>The instance-security namespace (<c>/v1/instance/security</c>) and write-only secrets.</summary>
    public sealed class InstanceSecurityResource : ResourceBase
    {
        public InstanceSecurityResource(AtlasTransport transport) : base(transport) { }

        public Task<InstanceSecurity> GetAsync(CancellationToken ct = default)
            => Req<InstanceSecurity>(HttpVerb.Get, "/v1/instance/security", cancellationToken: ct);

        public Task<InstanceSecurityResult> UpdateAsync(UpdateInstanceSecurityBody body, CancellationToken ct = default)
            => Req<InstanceSecurityResult>(HttpVerb.Patch, "/v1/instance/security", body, cancellationToken: ct);

        /// <summary>Store the captcha secret for the configured provider.</summary>
        public Task<CaptchaSecretResult> SetCaptchaSecretAsync(string secret, CancellationToken ct = default)
            => SetCaptchaSecretAsync(secret, null, ct);

        /// <summary>
        /// Store a captcha provider secret. <paramref name="provider"/> (null: the configured one) lets you store it
        /// BEFORE turning captcha on: captcha is bring-your-own-keys, and enabling it without a site key and secret is
        /// refused with 422 <c>CAPTCHA_KEYS_REQUIRED</c> (<see cref="ErrorCodes.CaptchaKeysRequired"/>).
        /// </summary>
        public Task<CaptchaSecretResult> SetCaptchaSecretAsync(string secret, string? provider, CancellationToken ct = default)
            => Req<CaptchaSecretResult>(HttpVerb.Put, "/v1/instance/captcha_secret", body: Body(("secret", secret), ("provider", provider)), cancellationToken: ct);

        /// <summary>Remove the configured provider's captcha secret.</summary>
        public Task<CaptchaSecretResult> DeleteCaptchaSecretAsync(CancellationToken ct = default)
            => DeleteCaptchaSecretAsync(null, false, ct);

        /// <summary>
        /// Remove a captcha secret. Refused with 409 <c>CAPTCHA_IN_USE</c> (<see cref="ErrorCodes.CaptchaInUse"/>) while a
        /// flow uses it (<c>meta.flows</c>); <paramref name="force"/> removes it anyway and turns those flows off
        /// (<see cref="CaptchaSecretResult.FlowsTurnedOff"/>).
        /// </summary>
        public Task<CaptchaSecretResult> DeleteCaptchaSecretAsync(string? provider, bool force, CancellationToken ct = default)
            => Req<CaptchaSecretResult>(HttpVerb.Delete, "/v1/instance/captcha_secret",
                query: Q(("provider", provider), ("force", force ? true : null)), cancellationToken: ct);

        /// <summary>Store the Kerberos/IWA trusted-proxy secret. Refused while the strategy is off.</summary>
        public Task<KerberosSecretResult> SetKerberosSecretAsync(string secret, CancellationToken ct = default)
            => Req<KerberosSecretResult>(HttpVerb.Put, "/v1/instance/kerberos_secret", body: Body(("secret", secret)), cancellationToken: ct);

        public Task<KerberosSecretResult> DeleteKerberosSecretAsync(CancellationToken ct = default)
            => Req<KerberosSecretResult>(HttpVerb.Delete, "/v1/instance/kerberos_secret", cancellationToken: ct);

        /// <summary><c>GET /v1/instance/proxy_secret</c> — Whether a trusted reverse proxy secret is set, and since when (never the secret).</summary>
        public Task<ProxySecret> GetProxySecretAsync(CancellationToken ct = default)
            => Req<ProxySecret>(HttpVerb.Get, "/v1/instance/proxy_secret", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/instance/proxy_secret</c> — Create, or rotate, the trusted reverse proxy secret; it is returned once. On a
        /// rotation the replaced secret keeps working for <c>OverlapSeconds</c> (default 86400, 0 stops it now, at most 604800).
        /// </summary>
        public Task<ProxySecretCreated> CreateProxySecretAsync(CreateProxySecretBody? body = null, CancellationToken ct = default)
            => Req<ProxySecretCreated>(HttpVerb.Post, "/v1/instance/proxy_secret", body ?? new CreateProxySecretBody(), cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/proxy_secret</c> — Remove the proxy secret: <c>X-Atlas-Client-IP</c> is ignored again.</summary>
        public Task<ProxySecretDeleted> DeleteProxySecretAsync(CancellationToken ct = default)
            => Req<ProxySecretDeleted>(HttpVerb.Delete, "/v1/instance/proxy_secret", cancellationToken: ct);

        /// <summary>Store an LDAP connection's service-account bind password (connection must already exist).</summary>
        public Task<LdapBindPasswordResult> SetLdapBindPasswordAsync(string connectionId, string bindPassword, CancellationToken ct = default)
            => Req<LdapBindPasswordResult>(HttpVerb.Put, $"/v1/instance/ldap_connections/{Enc(connectionId)}/bind_password",
                body: Body(("bind_password", bindPassword)), cancellationToken: ct);

        public Task<LdapBindPasswordResult> DeleteLdapBindPasswordAsync(string connectionId, CancellationToken ct = default)
            => Req<LdapBindPasswordResult>(HttpVerb.Delete, $"/v1/instance/ldap_connections/{Enc(connectionId)}/bind_password", cancellationToken: ct);
    }

    public sealed class CookieConsentBody
    {
        /// <summary>Your CMP script URL. Must be https; its origin is added to the page CSP.</summary>
        public string? ScriptUrl { get; set; }
        /// <summary>The &lt;script id&gt; some CMPs need.</summary>
        public string? ScriptId { get; set; }
        /// <summary>Your CMP site id (sent as data-cbid).</summary>
        public string? SiteId { get; set; }
        /// <summary>The CMP prior-blocking mode, e.g. "auto" (sent as data-blockingmode).</summary>
        public string? BlockingMode { get; set; }
        /// <summary>Other https URLs the CMP loads from or calls; each origin is added to the page CSP.</summary>
        public List<string>? ExtraOrigins { get; set; }
        /// <summary>
        /// A "Cookie settings" link on the hosted pages that reopens the CMP. Omitted (or null) = no link; to remove a
        /// stored link send an explicit null with <c>Set("settings_link", null)</c>.
        /// </summary>
        public CookieConsentSettingsLinkInput? SettingsLink { get; set; }
        /// <summary><c>page</c> (default: the script tag carries the page language) or <c>browser</c>.</summary>
        public string? Culture { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CookieConsentBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>Known values of a cookie-consent settings link <c>preset</c> (the CMP whose trigger the link carries).</summary>
    public static class CookieConsentSettingsLinkPresets
    {
        public const string Cookiemunch = "cookiemunch";
        public const string Cookiebot = "cookiebot";
        public const string OneTrust = "onetrust";
        /// <summary>No built-in trigger: the link carries only your own attributes.</summary>
        public const string Custom = "custom";
    }

    /// <summary>
    /// The "Cookie settings" link to render on the hosted pages. It needs a <see cref="Preset"/> or at least one
    /// attribute (the API answers 400 otherwise).
    /// </summary>
    public sealed class CookieConsentSettingsLinkInput
    {
        /// <summary>The link text (1-60 characters).</summary>
        public string Label { get; set; } = "";
        /// <summary>The CMP trigger to attach, one of <see cref="CookieConsentSettingsLinkPresets"/>; null for none.</summary>
        public string? Preset { get; set; }
        /// <summary>
        /// Extra attributes for the link (at most 10, values up to 200 characters). Allowed names: <c>data-*</c>,
        /// <c>aria-*</c>, <c>id</c>, <c>class</c> and <c>href</c> (https or a <c>#fragment</c>).
        /// </summary>
        public Dictionary<string, string>? Attributes { get; set; }
    }

    /// <summary>The stored "Cookie settings" link.</summary>
    public sealed class CookieConsentSettingsLink
    {
        public string Label { get; init; } = "";
        /// <summary>The CMP trigger preset (see <see cref="CookieConsentSettingsLinkPresets"/>), or null.</summary>
        public string? Preset { get; init; }
        /// <summary>The attributes you set.</summary>
        public Dictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();
        /// <summary>The attributes the rendered link carries: yours plus the preset's trigger.</summary>
        public Dictionary<string, string> RenderedAttributes { get; init; } = new Dictionary<string, string>();
    }

    public sealed class PreflightFinding
    {
        public string Severity { get; init; } = "";
        /// <summary>Stable snake_case identifier, e.g. oauth_provider_missing_credentials.</summary>
        public string Code { get; init; } = "";
        /// <summary>What is wrong, in a plain sentence.</summary>
        public string Message { get; init; } = "";
        /// <summary>What to do about it, in a plain sentence.</summary>
        public string Fix { get; init; } = "";
        /// <summary>Ids of what the finding is about (provider, id/client_id, domain_id/host, …).</summary>
        public Dictionary<string, string>? Subject { get; init; }
    }

    public sealed class Preflight
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>True when there are no error-severity findings.</summary>
        public bool Ok { get; init; }
        /// <summary>When the check ran (epoch ms).</summary>
        public long CheckedAt { get; init; }
        /// <summary>Ordered error, then warning, then info.</summary>
        public List<PreflightFinding> Findings { get; init; } = new List<PreflightFinding>();
        /// <summary>Atlas browser SDK versions seen in the last 7 days, with their advisories.</summary>
        public List<SdkVersionSeen>? SdkVersionsSeen { get; init; }
    }

    public sealed class SdkVersionSeen
    {
        public string Package { get; init; } = "";
        public string Version { get; init; } = "";
        public long LastSeenAt { get; init; }
        public List<SdkAdvisory> Advisories { get; init; } = new List<SdkAdvisory>();
    }

    public sealed class SdkAdvisory
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "sdk_advisory";
        public string Package { get; init; } = "";
        public string Affected { get; init; } = "";
        public string FixedIn { get; init; } = "";
        /// <summary>Stable code, e.g. <c>email_code_sign_in</c>.</summary>
        public string Reason { get; init; } = "";
        /// <summary>critical, high, medium or low.</summary>
        public string Severity { get; init; } = "";
        public string Summary { get; init; } = "";
        public string PublishedAt { get; init; } = "";
    }

    /// <summary>The consent-management platform (CMP) injected into the hosted pages. enabled:false (and no other fields) when none is set.</summary>
    public sealed class CookieConsent
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public bool Enabled { get; init; }
        /// <summary>The CMP script URL (https).</summary>
        public string? ScriptUrl { get; init; }
        public string? ScriptId { get; init; }
        public string? SiteId { get; init; }
        public string? BlockingMode { get; init; }
        public List<string>? ExtraOrigins { get; init; }
        /// <summary>The "Cookie settings" link on the hosted pages, or null when there is none.</summary>
        public CookieConsentSettingsLink? SettingsLink { get; init; }
        /// <summary><c>page</c> or <c>browser</c>.</summary>
        public string? Culture { get; init; }
        /// <summary>Every cookie and browser-storage key Atlas writes for this instance, for your cookie policy.</summary>
        public List<DeclaredCookie>? DeclaredCookies { get; init; }
    }

    /// <summary>A cookie or browser-storage key Atlas writes (all strictly necessary).</summary>
    public sealed class DeclaredCookie
    {
        /// <summary>The exact name, or null when <see cref="Pattern"/> describes a family of keys.</summary>
        public string? Name { get; init; }
        public string? Pattern { get; init; }
        /// <summary>cookie, localStorage, sessionStorage or indexedDB.</summary>
        public string Store { get; init; } = "";
        public string Domain { get; init; } = "";
        /// <summary>Null = browser session (cookie) or until cleared (storage).</summary>
        public long? LifetimeSeconds { get; init; }
        public string Lifetime { get; init; } = "";
        public bool HttpOnly { get; init; }
        public string Category { get; init; } = "";
        public string Purpose { get; init; } = "";
        public string WrittenWhen { get; init; } = "";
        /// <summary>atlas_frontend_api or atlas_sdk.</summary>
        public string SetBy { get; init; } = "";
    }

    /// <summary>The body of an origin move: <c>from</c> and <c>to</c> are <c>scheme://host[:port]</c> origins.</summary>
    public sealed class OriginMoveBody
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        /// <summary>true reports the diff and any problems and writes nothing.</summary>
        public bool? DryRun { get; set; }
    }

    /// <summary>One field an origin move rewrites; Before/After are a URL, a list of URLs, or null.</summary>
    public sealed class OriginMoveChange
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Field { get; init; } = "";
        public JsonElement? Before { get; init; }
        public JsonElement? After { get; init; }
    }

    public sealed class OriginMoveProblem
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public string? Field { get; init; }
        public string? Url { get; init; }
        public string? Message { get; init; }
    }

    /// <summary>The before → after diff of an origin move. A repeated run returns no changes.</summary>
    public sealed class OriginMove
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "origin_move";
        public string From { get; init; } = "";
        public string To { get; init; } = "";
        public bool DryRun { get; init; }
        /// <summary>True when this run wrote changes.</summary>
        public bool Applied { get; init; }
        public List<OriginMoveChange> Changes { get; init; } = new List<OriginMoveChange>();
        /// <summary>Values the field would refuse; a real run with problems is a 422 and writes nothing.</summary>
        public List<OriginMoveProblem> Problems { get; init; } = new List<OriginMoveProblem>();
        /// <summary>Platform app twin only: the app after the move.</summary>
        public JsonElement? App { get; init; }
    }

    public sealed class InstanceConnection
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "connection";
        /// <summary>privacy_webhook, cmp, atlas_id_app or jwt_template.</summary>
        public string Kind { get; init; } = "";
        public string Check { get; init; } = "";
        public bool Ok { get; init; }
        /// <summary>error, warning or info.</summary>
        public string Severity { get; init; } = "";
        public Dictionary<string, string> Subject { get; init; } = new Dictionary<string, string>();
        public string? FixHint { get; init; }
        public long LastCheckedAt { get; init; }
        /// <summary>Kind-specific facts.</summary>
        public JsonElement? Details { get; init; }
    }

    /// <summary>The instance's links to other platforms. Ok is false when a warning- or error-severity row fails.</summary>
    public sealed class InstanceConnections
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "instance_connections";
        public bool Ok { get; init; }
        public long CheckedAt { get; init; }
        public List<InstanceConnection> Data { get; init; } = new List<InstanceConnection>();
    }

    /// <summary>The Chatly connection (Enabled false and nothing else when none is set).</summary>
    public sealed class ChatlyIntegration
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "chatly_integration";
        public bool Enabled { get; init; }
        public string? Channel { get; init; }
        /// <summary>The template name to pass to getToken({ template: 'chatly' }).</summary>
        public string? Template { get; init; }
        public string? Audience { get; init; }
        public string? Issuer { get; init; }
        public string? JwksUrl { get; init; }
        public int? TokenTtlSeconds { get; init; }
        public Dictionary<string, string>? IdentityVerification { get; init; }
    }

    public sealed class ForumulateSpace
    {
        public string Id { get; init; } = "";
        public string? RedeemUrl { get; init; }
    }

    /// <summary>The Forumulate connection (Enabled false and nothing else when none is set).</summary>
    public sealed class ForumulateIntegration
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "forumulate_integration";
        public bool Enabled { get; init; }
        public string? OrgId { get; init; }
        public List<ForumulateSpace>? Spaces { get; init; }
        public string? Audience { get; init; }
        public string? Issuer { get; init; }
        public string? JwksUrl { get; init; }
        public int? TokenTtlSeconds { get; init; }
        /// <summary>The external_jwt provider to store on each space.</summary>
        public JsonElement? ReaderAuthProvider { get; init; }
    }

    /// <summary>The body of <c>PUT /v1/instance/integrations/forumulate</c>; replaces the whole connection.</summary>
    public sealed class ForumulateIntegrationBody
    {
        public string OrgId { get; set; } = "";
        /// <summary>1-50 space ids (strings), or objects with <c>id</c> and <c>redeem_url</c>.</summary>
        public List<object> Spaces { get; set; } = new List<object>();
        /// <summary>The token aud. Default <c>forumulate:&lt;org_id&gt;</c>.</summary>
        public string? Audience { get; set; }
    }
}
