using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class Usage
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public UsageWindow? Window { get; init; }
        public UsagePreviousWindow? PreviousWindow { get; init; }
        public List<UsageMetrics> Metrics { get; init; } = new List<UsageMetrics>();
        public UsageMonthlyActiveUsers? MonthlyActiveUsers { get; init; }
    }

    public sealed class UsageWindow
    {
        public string? From { get; init; }
        public string? To { get; init; }
        public long? Days { get; init; }
    }

    public sealed class UsagePreviousWindow
    {
        public string? From { get; init; }
        public string? To { get; init; }
    }

    public sealed class UsageMetrics
    {
        public string? Metric { get; init; }
        public string? Label { get; init; }
        public string? Description { get; init; }
        public bool? Additive { get; init; }
        public List<JsonElement?>? Points { get; init; }
        public JsonElement? Peak { get; init; }
        public double? Total { get; init; }
        public double? Trend { get; init; }
    }

    public sealed class UsageMonthlyActiveUsers
    {
        public long? Value { get; init; }
        public long? WindowDays { get; init; }
        public double? Trend { get; init; }
    }

    public sealed class Event
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Type { get; init; } = "";
        public WebhookPayload? Payload { get; init; }
        /// <summary>Occurred (epoch ms).</summary>
        public long CreatedAt { get; init; }
    }

    public sealed class WebhookPayload
    {
        public string Id { get; init; } = "";
        public string Type { get; init; } = "";
        /// <summary>Emitted (epoch ms).</summary>
        public long Timestamp { get; init; }
        public string InstanceId { get; init; } = "";
        public JsonElement? Data { get; init; }
    }

    /// <summary>The page of <c>GET /v1/events</c> with how long events are kept.</summary>
    public sealed class EventPage
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public List<Event> Data { get; init; } = new List<Event>();
        public bool HasMore { get; init; }
        public string? NextCursor { get; init; }
        public List<string>? AvailableTypes { get; init; }
        /// <summary>How long events are kept.</summary>
        public JsonElement? Retention { get; init; }
    }

    /// <summary>Usage analytics (<c>/v1/usage</c>) and the product activity feed (<c>/v1/events</c>).</summary>
    public sealed class AnalyticsResource : ResourceBase
    {
        public AnalyticsResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/usage</c> — Read usage analytics: daily sign-in/active-user series with peaks and trends over a window, plus monthly active users.</summary>
        public Task<Usage> UsageAsync(int? days = null, CancellationToken ct = default)
            => Req<Usage>(HttpVerb.Get, "/v1/usage", query: Q(("days", days)), cancellationToken: ct);

        /// <summary><c>GET /v1/events</c> — Read the product activity feed (events), filterable by type and time and cursor-paginated, with the list of event types this instance has actually emitted.</summary>
        public Task<CursorPage<Event>> EventsAsync(ListEventsParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<Event>>(HttpVerb.Get, "/v1/events", query: (@params ?? new ListEventsParams()).ToQuery(), cancellationToken: ct);

        /// <summary><c>GET /v1/events</c> with the whole page: <c>available_types</c> and the <c>retention</c> window.</summary>
        public Task<EventPage> EventsPageAsync(ListEventsParams? @params = null, CancellationToken ct = default)
            => Req<EventPage>(HttpVerb.Get, "/v1/events", query: (@params ?? new ListEventsParams()).ToQuery(), cancellationToken: ct);
    }
}
