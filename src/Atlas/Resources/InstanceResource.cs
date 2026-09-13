using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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
}
