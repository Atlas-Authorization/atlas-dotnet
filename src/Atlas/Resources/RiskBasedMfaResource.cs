using System.Collections.Generic;
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
        /// <summary>The account was idle past the dormant-account threshold before this sign-in.</summary>
        [JsonPropertyName("dormantAccount")] public double DormantAccount { get; set; }
    }

    /// <summary>Per-signal on/off toggles. camelCase on the wire.</summary>
    public sealed class RiskSignalToggles
    {
        [JsonPropertyName("newDevice")] public bool NewDevice { get; set; }
        [JsonPropertyName("newSubnet")] public bool NewSubnet { get; set; }
        [JsonPropertyName("newIp")] public bool NewIp { get; set; }
        [JsonPropertyName("velocity")] public bool Velocity { get; set; }
        [JsonPropertyName("impossibleTravel")] public bool ImpossibleTravel { get; set; }
        [JsonPropertyName("dormantAccount")] public bool DormantAccount { get; set; }
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
        /// <summary><c>enforce</c>, or <c>audit</c> (score and record without acting).</summary>
        public string? Mode { get; init; }
        /// <summary>What each risk level does (<c>medium</c>, <c>high</c>): default, allow, notify, mfa or block.</summary>
        public Dictionary<string, object?>? LevelActions { get; init; }
        /// <summary>External IP-reputation / email-risk feeds.</summary>
        public Dictionary<string, object?>? Reputation { get; init; }
        /// <summary>Fraud rules and sanctioned countries used by <c>POST /v1/fraud/lookup</c>.</summary>
        public Dictionary<string, object?>? Fraud { get; init; }
        /// <summary>Per-user baseline: <c>enabled</c> and <c>weight</c>.</summary>
        public Dictionary<string, object?>? Baseline { get; init; }
        /// <summary>Re-evaluate a session on refresh: <c>enabled</c> and <c>action</c> (revoke, notify or audit).</summary>
        public Dictionary<string, object?>? MidSession { get; init; }
    }

    public sealed class UpdateRiskBasedMfaBody
    {
        public bool? Enabled { get; set; }
        public string? StepUpThreshold { get; set; }
        public string? OnHighRiskNoFactor { get; set; }
        public RiskWeights? Weights { get; set; }
        public RiskSignalToggles? Signals { get; set; }
        public string? Mode { get; set; }
        public Dictionary<string, object?>? LevelActions { get; set; }
        public Dictionary<string, object?>? Reputation { get; set; }
        public Dictionary<string, object?>? Fraud { get; set; }
        public Dictionary<string, object?>? Baseline { get; set; }
        public Dictionary<string, object?>? MidSession { get; set; }
    }

    /// <summary>One risk-scored sign-in on the audit / shadow-mode timeline.</summary>
    public sealed class RiskEvent
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "risk_event";
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        /// <summary><c>low</c>, <c>medium</c> or <c>high</c>.</summary>
        public string Level { get; init; } = "";
        public double Score { get; init; }
        /// <summary>Names of the signals that fired.</summary>
        public List<string> Signals { get; init; } = new List<string>();
        /// <summary>The action decided; in audit mode, what would have happened.</summary>
        public string Action { get; init; } = "";
        /// <summary><c>enforce</c> or <c>audit</c>.</summary>
        public string Mode { get; init; } = "";
        public string? Ip { get; init; }
        /// <summary><c>legit</c>, <c>fraud</c> or null.</summary>
        public string? Feedback { get; init; }
        public string? FeedbackAt { get; init; }
        public string CreatedAt { get; init; } = "";
    }

    public sealed class RiskEventList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<RiskEvent> Data { get; init; } = new List<RiskEvent>();
    }

    /// <summary>Filters for the risk-event timeline.</summary>
    public sealed class ListRiskEventsParams
    {
        public string? UserId { get; set; }
        /// <summary><c>low</c>, <c>medium</c> or <c>high</c>.</summary>
        public string? Level { get; set; }
        /// <summary>1..200, default 50.</summary>
        public int? Limit { get; set; }
        /// <summary>ISO timestamp cursor on <c>created_at</c>; returns rows OLDER than this.</summary>
        public string? Before { get; set; }

        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("user_id", UserId);
            yield return new KeyValuePair<string, object?>("level", Level);
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("before", Before);
        }
    }

    public sealed class RiskEventMetricsBucket
    {
        public string Level { get; init; } = "";
        public string Action { get; init; } = "";
        public string Mode { get; init; } = "";
        public int Count { get; init; }
    }

    /// <summary>Risk-event counts per level/action/mode over the last <see cref="Days"/>.</summary>
    public sealed class RiskEventMetrics
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "risk_event_metrics";
        public int Days { get; init; }
        public int Total { get; init; }
        public List<RiskEventMetricsBucket> Buckets { get; init; } = new List<RiskEventMetricsBucket>();
    }

    public sealed class RiskEventFeedbackBody
    {
        /// <summary><c>legit</c> or <c>fraud</c>.</summary>
        public string Verdict { get; set; } = "";
    }

    /// <summary>Body of <c>POST /v1/fraud/lookup</c>; <see cref="Ip"/> is required.</summary>
    public sealed class FraudLookupBody
    {
        public string Ip { get; set; } = "";
        public string? UserAgent { get; set; }
        /// <summary>The stable id from the JS SDK.</summary>
        public string? DeviceId { get; set; }
        public string? Email { get; set; }
        /// <summary>A user of this instance, for user-based rules.</summary>
        public string? UserId { get; set; }
        public string? OrgId { get; set; }
        /// <summary>Device features reported by the SDK.</summary>
        public Dictionary<string, object?>? Device { get; set; }
    }

    /// <summary>The allow / challenge / block verdict of a fraud lookup.</summary>
    public sealed class FraudLookup
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fraud_lookup";
        /// <summary><c>allow</c>, <c>challenge</c> or <c>block</c>.</summary>
        public string Verdict { get; init; } = "";
        public List<string> Reasons { get; init; } = new List<string>();
        public List<string> MatchedRules { get; init; } = new List<string>();
        public string? DeviceId { get; init; }
        public Dictionary<string, object?> Signals { get; init; } = new Dictionary<string, object?>();
    }

    /// <summary>The adaptive / risk-based MFA namespace (<c>/v1/risk_based_mfa</c>).</summary>
    public sealed partial class RiskBasedMfaResource : ResourceBase
    {
        public RiskBasedMfaResource(AtlasTransport transport) : base(transport) { }

        /// <summary>Read the current adaptive-MFA config.</summary>
        public Task<RiskBasedMfa> GetAsync(CancellationToken ct = default)
            => Req<RiskBasedMfa>(HttpVerb.Get, "/v1/risk_based_mfa", cancellationToken: ct);

        /// <summary>Change any subset of the config; omitted fields are left unchanged.</summary>
        public Task<RiskBasedMfa> UpdateAsync(UpdateRiskBasedMfaBody body, CancellationToken ct = default)
            => Req<RiskBasedMfa>(HttpVerb.Patch, "/v1/risk_based_mfa", body, cancellationToken: ct);

        /// <summary>The risk-event timeline (every scored sign-in), newest first. <c>GET /v1/risk_events</c>.</summary>
        public Task<RiskEventList> ListEventsAsync(ListRiskEventsParams? @params = null, CancellationToken ct = default)
            => Req<RiskEventList>(HttpVerb.Get, "/v1/risk_events", query: (@params ?? new ListRiskEventsParams()).ToQuery(), cancellationToken: ct);

        /// <summary>Counts per level/action/mode over the last <paramref name="days"/> (1..90, default 7).</summary>
        public Task<RiskEventMetrics> EventMetricsAsync(int? days = null, CancellationToken ct = default)
            => Req<RiskEventMetrics>(HttpVerb.Get, "/v1/risk_events/metrics", query: new[] { new KeyValuePair<string, object?>("days", days) }, cancellationToken: ct);

        /// <summary>Mark a scored sign-in <c>legit</c> or <c>fraud</c>; the weight of each signal that fired moves accordingly.</summary>
        public Task<RiskEvent> EventFeedbackAsync(string id, RiskEventFeedbackBody body, CancellationToken ct = default)
            => Req<RiskEvent>(HttpVerb.Post, $"/v1/risk_events/{Enc(id)}/feedback", body, cancellationToken: ct);

        /// <summary>Allow / challenge / block verdict for a sign-up, sign-in or checkout. <c>POST /v1/fraud/lookup</c>.</summary>
        public Task<FraudLookup> FraudLookupAsync(FraudLookupBody body, CancellationToken ct = default)
            => Req<FraudLookup>(HttpVerb.Post, "/v1/fraud/lookup", body, cancellationToken: ct);
    }
}
