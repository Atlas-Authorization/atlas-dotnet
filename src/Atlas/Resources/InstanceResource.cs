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
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        public Metadata AuthConfig { get; init; } = new Metadata();
        public long CreatedAt { get; init; }
    }

    public sealed class UpdateInstanceBody
    {
        /// <summary>Exact origins only — a wildcard or a path is rejected (§13.1).</summary>
        public List<string>? AllowedOrigins { get; set; }
        public Dictionary<string, object?>? AuthConfig { get; set; }
    }

    /// <summary><c>PATCH /v1/instance</c> echoes only the mutable fields, not a full Instance.</summary>
    public sealed class InstanceUpdateResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "instance";
        public string Id { get; init; } = "";
        public List<string> AllowedOrigins { get; init; } = new List<string>();
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
    }

    public sealed class KerberosSecretResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "kerberos_secret";
        public bool Set { get; init; }
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

        /// <summary>Store the captcha provider secret. Refused while the provider is <c>none</c>.</summary>
        public Task<CaptchaSecretResult> SetCaptchaSecretAsync(string secret, CancellationToken ct = default)
            => Req<CaptchaSecretResult>(HttpVerb.Put, "/v1/instance/captcha_secret", body: Body(("secret", secret)), cancellationToken: ct);

        public Task<CaptchaSecretResult> DeleteCaptchaSecretAsync(CancellationToken ct = default)
            => Req<CaptchaSecretResult>(HttpVerb.Delete, "/v1/instance/captcha_secret", cancellationToken: ct);

        /// <summary>Store the Kerberos/IWA trusted-proxy secret. Refused while the strategy is off.</summary>
        public Task<KerberosSecretResult> SetKerberosSecretAsync(string secret, CancellationToken ct = default)
            => Req<KerberosSecretResult>(HttpVerb.Put, "/v1/instance/kerberos_secret", body: Body(("secret", secret)), cancellationToken: ct);

        public Task<KerberosSecretResult> DeleteKerberosSecretAsync(CancellationToken ct = default)
            => Req<KerberosSecretResult>(HttpVerb.Delete, "/v1/instance/kerberos_secret", cancellationToken: ct);

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
    }
}
