using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Atlas
{
    /// <summary>
    /// OpenTelemetry-compatible tracing and metrics with no OpenTelemetry dependency: the SDK emits through the
    /// platform's own <see cref="ActivitySource"/> and <see cref="Meter"/> (both named <see cref="Name"/>), which cost
    /// nothing until something listens. Opt in from your host:
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithTracing(t => t.AddSource(AtlasTelemetry.Name))
    ///     .WithMetrics(m => m.AddMeter(AtlasTelemetry.Name));
    /// </code>
    /// Spans: one client span per API call (<c>Atlas GET</c>), tagged <c>http.request.method</c>, <c>url.path</c>,
    /// <c>http.response.status_code</c>, <c>atlas.retry.count</c>, with the error type on failure. Metrics:
    /// <c>atlas.client.requests</c> (count by method and status), <c>atlas.client.request.duration</c> (seconds) and
    /// <c>atlas.client.retries</c>. Keys, bodies and headers are never recorded.
    /// </summary>
    public static class AtlasTelemetry
    {
        /// <summary>The name of the <see cref="ActivitySource"/> and the <see cref="Meter"/>.</summary>
        public const string Name = "Atlas.Sdk";

        private const string Version = "1.0.0";

        public static readonly ActivitySource Source = new ActivitySource(Name, Version);
        public static readonly Meter Meter = new Meter(Name, Version);

        internal static readonly Counter<long> Requests = Meter.CreateCounter<long>("atlas.client.requests", "{request}", "API calls made by the SDK.");
        internal static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("atlas.client.request.duration", "s", "API call duration including retries.");
        internal static readonly Counter<long> Retries = Meter.CreateCounter<long>("atlas.client.retries", "{retry}", "Retries after a 429, 503 or network failure.");

        internal static Activity? Start(RequestOptions options)
        {
            var activity = Source.StartActivity("Atlas " + options.Method.ToString().ToUpperInvariant(), ActivityKind.Client);
            if (activity == null) return null;
            activity.SetTag("http.request.method", options.Method.ToString().ToUpperInvariant());
            activity.SetTag("url.path", options.Path);
            activity.SetTag("peer.service", "atlas");
            return activity;
        }

        internal static void Finish(Activity? activity, RequestOptions options, int status, int retries, double seconds, string? errorType)
        {
            var method = options.Method.ToString().ToUpperInvariant();
            var tags = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("http.request.method", method),
                new KeyValuePair<string, object?>("http.response.status_code", status),
            };
            Requests.Add(1, tags);
            Duration.Record(seconds, tags);
            if (retries > 0) Retries.Add(retries, new KeyValuePair<string, object?>("http.request.method", method));
            if (activity == null) return;
            activity.SetTag("http.response.status_code", status);
            activity.SetTag("atlas.retry.count", retries);
            if (errorType != null)
            {
                activity.SetTag("error.type", errorType);
                activity.SetStatus(ActivityStatusCode.Error);
            }
        }
    }
}
