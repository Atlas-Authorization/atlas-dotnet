using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class BotSignal
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "bot_signal";
        public string Id { get; init; } = "";
        public string? AttemptId { get; init; }
        public string? UserId { get; init; }
        public string Kind { get; init; } = "";
        public string Source { get; init; } = "";
        public string? IpHash { get; init; }
        public string? IpSubnet { get; init; }
        public long? Asn { get; init; }
        public string? GeoCountry { get; init; }
        public string? GeoCity { get; init; }
        public bool? IsDatacenter { get; init; }
        public bool? IsAnonymizer { get; init; }
        public string? HeaderFingerprint { get; init; }
        public string? AcceptLanguage { get; init; }
        public string? DeviceId { get; init; }
        public string? DeviceFingerprint { get; init; }
        public Dictionary<string, object?> DeviceFeatures { get; init; } = new Dictionary<string, object?>();
        public Dictionary<string, object?> BehaviorFeatures { get; init; } = new Dictionary<string, object?>();
        public string? CaptchaProvider { get; init; }
        public double? CaptchaScore { get; init; }
        public string? RiskLevel { get; init; }
        public double? RiskScore { get; init; }
        public Dictionary<string, object?> RiskSignals { get; init; } = new Dictionary<string, object?>();
        public double? HeuristicScore { get; init; }
        public List<object> HeuristicReasons { get; init; } = new List<object>();
        public string? Outcome { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class BotLabel
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "bot_label";
        public string Id { get; init; } = "";
        public string SubjectType { get; init; } = "";
        public string SubjectId { get; init; } = "";
        public string Label { get; init; } = "";
        public string Source { get; init; } = "";
        public double Confidence { get; init; }
        public string? Note { get; init; }
        public string? LabeledBy { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>
    /// A newest-first export page: <see cref="NextBefore"/> is the created-at
    /// epoch-ms cursor to pass back as <c>before</c> for the next (older) page, or
    /// null at the end.
    /// </summary>
    public sealed class BotExportPage<T>
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<T> Data { get; init; } = new List<T>();
        public long? NextBefore { get; init; }
    }

    public sealed class BotExportParams
    {
        /// <summary>1..500, default 100.</summary>
        public int? Limit { get; set; }
        /// <summary>created-at epoch-ms cursor; returns rows OLDER than this.</summary>
        public long? Before { get; set; }

        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("before", Before);
        }
    }

    public sealed class CreateBotLabelBody
    {
        public string SubjectType { get; set; } = "";
        public string SubjectId { get; set; } = "";
        public string Label { get; set; } = "";
        /// <summary>Clamped to 0..1; defaults to 1.</summary>
        public double? Confidence { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>The anti-bot signal-lake namespace (<c>/v1/bot_signals</c>, <c>/v1/bot_labels</c>).</summary>
    public sealed class BotSignalsResource : ResourceBase
    {
        public BotSignalsResource(AtlasTransport transport) : base(transport) { }

        /// <summary>Page the signal lake, newest first, for an incremental pull.</summary>
        public Task<BotExportPage<BotSignal>> ListAsync(BotExportParams? @params = null, CancellationToken ct = default)
            => Req<BotExportPage<BotSignal>>(HttpVerb.Get, "/v1/bot_signals", query: (@params ?? new BotExportParams()).ToQuery(), cancellationToken: ct);

        /// <summary>Page the training labels, newest first.</summary>
        public Task<BotExportPage<BotLabel>> ListLabelsAsync(BotExportParams? @params = null, CancellationToken ct = default)
            => Req<BotExportPage<BotLabel>>(HttpVerb.Get, "/v1/bot_labels", query: (@params ?? new BotExportParams()).ToQuery(), cancellationToken: ct);

        /// <summary>Attach a training label to a user / device / ip_hash / attempt.</summary>
        public Task<BotLabel> CreateLabelAsync(CreateBotLabelBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<BotLabel>(HttpVerb.Post, "/v1/bot_labels", body, idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
