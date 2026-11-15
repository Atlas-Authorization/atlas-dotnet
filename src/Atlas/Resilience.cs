using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas
{
    /// <summary>What one retry is about to do; handed to <see cref="ResilienceOptions.OnRetry"/>.</summary>
    public sealed class RetryInfo
    {
        /// <summary>1 for the first retry.</summary>
        public int Attempt { get; init; }
        /// <summary>The status that caused it (0 for a network failure).</summary>
        public int Status { get; init; }
        public TimeSpan Delay { get; init; }
        /// <summary>True when <see cref="Delay"/> came from the server's <c>Retry-After</c>.</summary>
        public bool FromRetryAfter { get; init; }
        /// <summary>The major outage Atlas reports, when <see cref="ResilienceOptions.StatusAware"/> found one.</summary>
        public string? Incident { get; init; }
        public string Method { get; init; } = "";
        public string Path { get; init; } = "";
    }

    /// <summary>
    /// Status-aware retries, opt-in (<c>AtlasClientOptions.Resilience</c>). A 429 is always retried (the call was not
    /// processed); a 503 and a network failure are retried for reads and for writes carrying an idempotency key (so a
    /// replay cannot double-apply), or for every write with <see cref="RetryWrites"/>. The wait is the server's
    /// <c>Retry-After</c> when sent, else exponential backoff with full jitter, never above <see cref="MaxDelay"/>.
    /// With <see cref="StatusAware"/>, a 503 with no <c>Retry-After</c> reads the public status endpoint: while Atlas
    /// reports a major outage the backoff is longer and the outage description is passed to <see cref="OnRetry"/>.
    /// </summary>
    public sealed class ResilienceOptions
    {
        public int MaxRetries { get; set; } = 3;
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(250);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
        public bool RetryWrites { get; set; }
        public bool StatusAware { get; set; } = true;
        public Action<RetryInfo>? OnRetry { get; set; }
        /// <summary>Test hook: replaces <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</summary>
        public Func<TimeSpan, CancellationToken, Task>? Delay { get; set; }
        /// <summary>Test hook: a random fraction in [0, 1) for jitter.</summary>
        public Func<double>? Random { get; set; }
    }

    internal sealed class AttemptInfo
    {
        public int Status;
        public TimeSpan? RetryAfter;
        public DateTimeOffset? DateHeader;
    }

    internal static class Resilient
    {
        private static readonly Random Jitter = new Random();
        private static (long At, string? Incident)? _status;

        public static TimeSpan? ParseRetryAfter(HttpResponseMessage response)
        {
            var ra = response.Headers.RetryAfter;
            if (ra == null) return null;
            if (ra.Delta.HasValue) return ra.Delta.Value < TimeSpan.Zero ? TimeSpan.Zero : ra.Delta.Value;
            if (ra.Date.HasValue)
            {
                var d = ra.Date.Value - DateTimeOffset.UtcNow;
                return d < TimeSpan.Zero ? TimeSpan.Zero : d;
            }
            return null;
        }

        public static async Task<T?> RunAsync<T>(RequestOptions options, ResilienceOptions? policy, string baseUrl, HttpClient http,
            Func<AttemptInfo, CancellationToken, Task<T?>> send, CancellationToken ct)
        {
            using var activity = AtlasTelemetry.Start(options);
            var started = Stopwatch.GetTimestamp();
            var retries = 0;
            var status = 0;
            string? errorType = null;
            try
            {
                while (true)
                {
                    var info = new AttemptInfo();
                    try
                    {
                        var result = await send(info, ct).ConfigureAwait(false);
                        status = info.Status;
                        return result;
                    }
                    catch (Exception e) when (!ct.IsCancellationRequested)
                    {
                        status = e is AtlasException ae ? ae.Status : info.Status;
                        errorType = e.GetType().Name;
                        var transient = e is AtlasException a ? (a.Status == 429 || a.Status == 503) : e is HttpRequestException || e is TimeoutException;
                        if (policy == null || !transient || retries >= policy.MaxRetries || !CanRetry(options, policy, status)) throw;
                        var (delay, fromHeader, incident) = await NextDelayAsync(policy, info, status, retries, baseUrl, http, ct).ConfigureAwait(false);
                        retries++;
                        policy.OnRetry?.Invoke(new RetryInfo
                        {
                            Attempt = retries, Status = status, Delay = delay, FromRetryAfter = fromHeader, Incident = incident,
                            Method = options.Method.ToString().ToUpperInvariant(), Path = options.Path,
                        });
                        await (policy.Delay != null ? policy.Delay(delay, ct) : Task.Delay(delay, ct)).ConfigureAwait(false);
                        errorType = null;
                    }
                }
            }
            finally
            {
                var seconds = (Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency;
                AtlasTelemetry.Finish(activity, options, status, retries, seconds, errorType);
            }
        }

        private static bool CanRetry(RequestOptions o, ResilienceOptions p, int status)
        {
            if (status == 429) return true;
            if (o.Method == HttpVerb.Get) return true;
            return p.RetryWrites || !string.IsNullOrEmpty(o.IdempotencyKey);
        }

        private static async Task<(TimeSpan Delay, bool FromHeader, string? Incident)> NextDelayAsync(ResilienceOptions p, AttemptInfo info,
            int status, int retried, string baseUrl, HttpClient http, CancellationToken ct)
        {
            if (info.RetryAfter.HasValue) return (Min(info.RetryAfter.Value, p.MaxDelay), true, null);
            string? incident = null;
            var factor = 1.0;
            if (status == 503 && p.StatusAware)
            {
                incident = await ReadIncidentAsync(baseUrl, http, ct).ConfigureAwait(false);
                if (incident != null) factor = 4; // an acknowledged incident: do not hammer it
            }
            var ceiling = Math.Min(p.MaxDelay.TotalMilliseconds, p.BaseDelay.TotalMilliseconds * factor * Math.Pow(2, retried));
            var rnd = p.Random != null ? p.Random() : Jitter.NextDouble();
            return (TimeSpan.FromMilliseconds(ceiling * (0.5 + rnd / 2)), false, incident);
        }

        private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;

        /// <summary>A description of a major outage from the public <c>GET /v1/status</c>, or null. Cached 15 s; never throws.</summary>
        internal static async Task<string?> ReadIncidentAsync(string baseUrl, HttpClient http, CancellationToken ct)
        {
            var now = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
            if (_status is { } cached && now - cached.At < 15_000) return cached.Incident;
            string? incident = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/v1/status");
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
#if NET8_0_OR_GREATER
                    var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
                    var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                    using var doc = JsonDocument.Parse(text);
                    var root = doc.RootElement;
                    // Only a major outage counts (top level, or the backend_api component); other states are not guessed at.
                    if (root.TryGetProperty("status", out var overall) && overall.ValueKind == JsonValueKind.String && overall.GetString() == "major_outage")
                        incident = "Atlas reports a major outage";
                    else if (root.TryGetProperty("components", out var comps) && comps.ValueKind == JsonValueKind.Array)
                        foreach (var c in comps.EnumerateArray())
                            if (c.TryGetProperty("id", out var id) && id.GetString() == "backend_api"
                                && c.TryGetProperty("status", out var cs) && cs.GetString() == "major_outage")
                            { incident = "Atlas reports a backend API outage"; break; }
                }
            }
            catch (Exception) when (!ct.IsCancellationRequested) { }
            _status = (now, incident);
            return incident;
        }
    }
}
