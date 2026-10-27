using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/analytics/alert_rules</c>. Only the fields set are sent.</summary>
    public sealed class AnalyticsAlertRulesCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>Required. Percent metrics are 0-100; failed_sign_ins is a count; sign_in_p95_seconds is seconds; api_usage_spike is the ratio of a key or organization's request rate in the window to its average hourly rate over the 7 days before (for example 10 = ten times its usual volume), and min_sample is the fewest requ</summary>
        [JsonPropertyName("metric")] public string? Metric { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("comparator")] public string? Comparator { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("threshold")] public double? Threshold { get; set; }
        /// <summary>Trailing window, 5 to 1440 (default 60).</summary>
        [JsonPropertyName("window_minutes")] public long? WindowMinutes { get; set; }
        /// <summary>Ratios and latency are not evaluated on fewer observations (default 20).</summary>
        [JsonPropertyName("min_sample")] public long? MinSample { get; set; }
        /// <summary>Minimum minutes between notifications while the rule stays firing (default 60).</summary>
        [JsonPropertyName("cooldown_minutes")] public long? CooldownMinutes { get; set; }
        [JsonPropertyName("email_contacts")] public List<string>? EmailContacts { get; set; }
        /// <summary>A public HTTPS endpoint. The body is signed like any Atlas webhook; the signing secret is returned once, on create.</summary>
        [JsonPropertyName("webhook_url")] public string? WebhookUrl { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>api_usage_spike only: watch secret keys, end-user API keys or organizations (null = all).</summary>
        [JsonPropertyName("subject_type")] public string? SubjectType { get; set; }
        /// <summary>api_usage_spike only: watch one key or organization (needs subject_type).</summary>
        [JsonPropertyName("subject_id")] public string? SubjectId { get; set; }
        /// <summary>Also raise a Shared Signals event (an opaque subject naming the key or organization) on every enabled stream that asks for it.</summary>
        [JsonPropertyName("ssf")] public bool? Ssf { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/analytics/alert_rules/{id}</c>. Only the fields set are sent.</summary>
    public sealed class AnalyticsAlertRulesUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>Percent metrics are 0-100; failed_sign_ins is a count; sign_in_p95_seconds is seconds; api_usage_spike is the ratio of a key or organization's request rate in the window to its average hourly rate over the 7 days before (for example 10 = ten times its usual volume), and min_sample is the fewest requ</summary>
        [JsonPropertyName("metric")] public string? Metric { get; set; }
        [JsonPropertyName("comparator")] public string? Comparator { get; set; }
        [JsonPropertyName("threshold")] public double? Threshold { get; set; }
        /// <summary>Trailing window, 5 to 1440 (default 60).</summary>
        [JsonPropertyName("window_minutes")] public long? WindowMinutes { get; set; }
        /// <summary>Ratios and latency are not evaluated on fewer observations (default 20).</summary>
        [JsonPropertyName("min_sample")] public long? MinSample { get; set; }
        /// <summary>Minimum minutes between notifications while the rule stays firing (default 60).</summary>
        [JsonPropertyName("cooldown_minutes")] public long? CooldownMinutes { get; set; }
        [JsonPropertyName("email_contacts")] public List<string>? EmailContacts { get; set; }
        /// <summary>A public HTTPS endpoint. The body is signed like any Atlas webhook; the signing secret is returned once, on create.</summary>
        [JsonPropertyName("webhook_url")] public string? WebhookUrl { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>api_usage_spike only: watch secret keys, end-user API keys or organizations (null = all).</summary>
        [JsonPropertyName("subject_type")] public string? SubjectType { get; set; }
        /// <summary>api_usage_spike only: watch one key or organization (needs subject_type).</summary>
        [JsonPropertyName("subject_id")] public string? SubjectId { get; set; }
        /// <summary>Also raise a Shared Signals event (an opaque subject naming the key or organization) on every enabled stream that asks for it.</summary>
        [JsonPropertyName("ssf")] public bool? Ssf { get; set; }
    }

    /// <summary>The body of <c>POST /v1/analytics/alert_rules/{id}/test</c>. Only the fields set are sent.</summary>
    public sealed class AnalyticsAlertRulesTestBody
    {
        [JsonPropertyName("notify")] public bool? Notify { get; set; }
    }

    /// <summary>Operations of <c>analytics.alertRules</c>.</summary>
    public sealed partial class AnalyticsAlertRulesResource : ResourceBase
    {
        public AnalyticsAlertRulesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/analytics/alert_rules</c> - List analytics alert rules with their state and last measured value. The metric catalog is included.</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/alert_rules", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/analytics/alert_rules</c> - Create an alert rule: tell me when a sign-in health metric, or a key or organization's API usage against its baseline, crosses a threshold over a trailing window. It must have email_contacts, a webhook_url or ssf. A webhook signing secret is returned once.</summary>
        /// <remarks>Needs the <c>analytics:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(AnalyticsAlertRulesCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/analytics/alert_rules", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/analytics/alert_rules/{id}</c> - One analytics alert rule.</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/analytics/alert_rules/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/analytics/alert_rules/{id}</c> - Edit an alert rule. Editing re-arms it.</summary>
        /// <remarks>Needs the <c>analytics:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, AnalyticsAlertRulesUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/analytics/alert_rules/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/analytics/alert_rules/{id}</c> - Delete an alert rule.</summary>
        /// <remarks>Needs the <c>analytics:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/analytics/alert_rules/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/analytics/alert_rules/{id}/test</c> - Measure the rule now. With `notify: true` it also sends the rule's email and webhook, whether or not the rule is breaching, to prove the delivery path.</summary>
        /// <remarks>Needs the <c>analytics:write</c> scope.</remarks>
        public Task<JsonElement> TestAsync(string id, AnalyticsAlertRulesTestBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/analytics/alert_rules/{Enc(id)}/test", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
