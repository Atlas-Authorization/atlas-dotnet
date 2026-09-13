using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Per-signal score weights that feed the risk total. camelCase on the wire.</summary>
    public sealed class RiskWeights
    {
        [JsonPropertyName("newDevice")] public double NewDevice { get; set; }
        [JsonPropertyName("newSubnet")] public double NewSubnet { get; set; }
        [JsonPropertyName("newIp")] public double NewIp { get; set; }
        [JsonPropertyName("velocity")] public double Velocity { get; set; }
        [JsonPropertyName("impossibleTravel")] public double ImpossibleTravel { get; set; }
    }

    /// <summary>Per-signal on/off toggles. camelCase on the wire.</summary>
    public sealed class RiskSignalToggles
    {
        [JsonPropertyName("newDevice")] public bool NewDevice { get; set; }
        [JsonPropertyName("newSubnet")] public bool NewSubnet { get; set; }
        [JsonPropertyName("newIp")] public bool NewIp { get; set; }
        [JsonPropertyName("velocity")] public bool Velocity { get; set; }
        [JsonPropertyName("impossibleTravel")] public bool ImpossibleTravel { get; set; }
    }

    public sealed class RiskBasedMfa
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "risk_based_mfa";
        public bool Enabled { get; init; }
        /// <summary>The risk level at or above which a sign-in is stepped up.</summary>
        public string StepUpThreshold { get; init; } = "";
        /// <summary>What to do at high risk when the user has no enrolled factor.</summary>
        public string OnHighRiskNoFactor { get; init; } = "";
        public RiskWeights Weights { get; init; } = new RiskWeights();
        public RiskSignalToggles Signals { get; init; } = new RiskSignalToggles();
    }

    public sealed class UpdateRiskBasedMfaBody
    {
        public bool? Enabled { get; set; }
        public string? StepUpThreshold { get; set; }
        public string? OnHighRiskNoFactor { get; set; }
        public RiskWeights? Weights { get; set; }
        public RiskSignalToggles? Signals { get; set; }
    }

    /// <summary>The adaptive / risk-based MFA namespace (<c>/v1/risk_based_mfa</c>).</summary>
    public sealed class RiskBasedMfaResource : ResourceBase
    {
        public RiskBasedMfaResource(AtlasTransport transport) : base(transport) { }

        /// <summary>Read the current adaptive-MFA config.</summary>
        public Task<RiskBasedMfa> GetAsync(CancellationToken ct = default)
            => Req<RiskBasedMfa>(HttpVerb.Get, "/v1/risk_based_mfa", cancellationToken: ct);

        /// <summary>Change any subset of the config; omitted fields are left unchanged.</summary>
        public Task<RiskBasedMfa> UpdateAsync(UpdateRiskBasedMfaBody body, CancellationToken ct = default)
            => Req<RiskBasedMfa>(HttpVerb.Patch, "/v1/risk_based_mfa", body, cancellationToken: ct);
    }
}
