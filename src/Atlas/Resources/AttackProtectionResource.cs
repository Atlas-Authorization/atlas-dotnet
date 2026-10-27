using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class BruteForceTier
    {
        public int Threshold { get; init; }
        public long WindowMs { get; init; }
        public long LockMs { get; init; }
        public bool NotifyUser { get; init; }
        public bool FlagForAdmin { get; init; }
    }

    public sealed class BruteForceConfig
    {
        public bool Enabled { get; init; }
        public List<BruteForceTier> Tiers { get; init; } = new List<BruteForceTier>();
    }

    public sealed class BreachedPasswordConfig
    {
        public bool Enabled { get; init; }
    }

    public sealed class SuspiciousIpConfig
    {
        public bool Enabled { get; init; }
        public List<string> IpAllowlist { get; init; } = new List<string>();
        public string ManagedBy { get; init; } = "";
    }

    public sealed class CaptchaConfig
    {
        public string Provider { get; init; } = "";
        public bool SiteKeySet { get; init; }
        public bool SecretSet { get; init; }
        public string ManagedBy { get; init; } = "";
    }

    public sealed class AttackProtection
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "attack_protection";
        public BruteForceConfig BruteForce { get; init; } = new BruteForceConfig();
        public BreachedPasswordConfig BreachedPassword { get; init; } = new BreachedPasswordConfig();
        public SuspiciousIpConfig SuspiciousIp { get; init; } = new SuspiciousIpConfig();
        public CaptchaConfig Captcha { get; init; } = new CaptchaConfig();
        /// <summary>Dormant-account reactivation alerts (writable).</summary>
        public DormantAccountsSettings? DormantAccounts { get; init; }
    }

    public sealed class ToggleConfig
    {
        public bool? Enabled { get; set; }
    }

    /// <summary>Only the two flags are writable; the rest of the object is read-only.</summary>
    public sealed class UpdateAttackProtectionBody
    {
        public ToggleConfig? BruteForce { get; set; }
        public ToggleConfig? BreachedPassword { get; set; }
        /// <summary>The dormant-account reactivation policy (any subset).</summary>
        public DormantAccountsSettings? DormantAccounts { get; set; }
    }

    /// <summary>The attack-protection namespace (<c>/v1/attack_protection</c>).</summary>
    public sealed partial class AttackProtectionResource : ResourceBase
    {
        public AttackProtectionResource(AtlasTransport transport) : base(transport) { }

        public Task<AttackProtection> GetAsync(CancellationToken ct = default)
            => Req<AttackProtection>(HttpVerb.Get, "/v1/attack_protection", cancellationToken: ct);

        public Task<AttackProtection> UpdateAsync(UpdateAttackProtectionBody body, CancellationToken ct = default)
            => Req<AttackProtection>(HttpVerb.Patch, "/v1/attack_protection", body, cancellationToken: ct);
    }

    /// <summary>
    /// When an account idle for <see cref="ThresholdDays"/> (1–3650, default 90) signs in again: an audit entry, the
    /// <c>user.reactivated</c> webhook, an optional notice to the user and, with <see cref="RequireMfa"/>, a
    /// second-factor step-up (<see cref="OnNoFactor"/>: <c>allow</c>, <c>require_enroll</c> or <c>block</c>).
    /// On an update, unset fields are left unchanged.
    /// </summary>
    public sealed class DormantAccountsSettings
    {
        public bool? Enabled { get; set; }
        public int? ThresholdDays { get; set; }
        public bool? RequireMfa { get; set; }
        public string? OnNoFactor { get; set; }
        public bool? NotifyUser { get; set; }
    }
}
