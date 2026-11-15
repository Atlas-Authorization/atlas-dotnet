using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Verification;

namespace Atlas
{
    public enum DoctorStatus { Ok, Warn, Fail, Skipped }

    public sealed class DoctorCheck
    {
        public string Name { get; init; } = "";
        public DoctorStatus Status { get; init; }
        public string Message { get; init; } = "";
        /// <summary>What to do about it, for a Warn or Fail.</summary>
        public string? Fix { get; init; }
    }

    public sealed class DoctorReport
    {
        public List<DoctorCheck> Checks { get; init; } = new List<DoctorCheck>();
        /// <summary>No check failed (warnings are allowed).</summary>
        public bool Ok => Checks.All(c => c.Status != DoctorStatus.Fail);

        public override string ToString() =>
            string.Join("\n", Checks.Select(c => $"[{c.Status.ToString().ToUpperInvariant()}] {c.Name}: {c.Message}" + (c.Fix != null ? $" ({c.Fix})" : "")));
    }

    public sealed class DoctorOptions
    {
        /// <summary>The issuer (frontend API origin); with it the JWKS check runs against <c>{issuer}/.well-known/jwks.json</c>.</summary>
        public string? Issuer { get; set; }
        /// <summary>Overrides the JWKS URL.</summary>
        public string? JwksUrl { get; set; }
        /// <summary>Clock skew above this many seconds is a warning (session tokens tolerate 5). Default 5.</summary>
        public double WarnSkewSeconds { get; set; } = 5;
        /// <summary>Clock skew above this many seconds is a failure. Default 60.</summary>
        public double FailSkewSeconds { get; set; } = 60;
        /// <summary>Clock for tests.</summary>
        public Func<DateTimeOffset>? Now { get; set; }
    }

    public sealed partial class AtlasClient
    {
        /// <summary>
        /// Check this client's setup and say what is wrong: the keys' format and environment, that the API answers and
        /// accepts the secret key, the clock skew against the API (a skewed clock breaks token checks), the JWKS (when
        /// <see cref="DoctorOptions.Issuer"/> is set) and the instance's service status. Never throws.
        /// </summary>
        public async Task<DoctorReport> DoctorAsync(DoctorOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new DoctorOptions();
            var now = options.Now ?? (() => DateTimeOffset.UtcNow);
            var checks = new List<DoctorCheck>();
            void Add(string name, DoctorStatus status, string message, string? fix = null) =>
                checks.Add(new DoctorCheck { Name = name, Status = status, Message = message, Fix = fix });

            // 1. keys
            if (!string.IsNullOrEmpty(_options.SecretKey))
            {
                if (AtlasKeys.TryParse(_options.SecretKey, out var parsed) && parsed != null && parsed.Kind == KeyKind.Secret)
                    Add("secret key", DoctorStatus.Ok, "A secret key" + (parsed.Environment != null ? " for " + parsed.Environment : "") + ".");
                else
                    Add("secret key", DoctorStatus.Fail, "SecretKey is not a secret key.", "Use the sk_ key from the dashboard; a publishable key cannot call this API.");
            }
            else Add("secret key", DoctorStatus.Skipped, "No SecretKey configured.");
            if (!string.IsNullOrEmpty(_options.PlatformKey))
                Add("platform key", AtlasKeys.IsPlatformKey(_options.PlatformKey) ? DoctorStatus.Ok : DoctorStatus.Fail,
                    AtlasKeys.IsPlatformKey(_options.PlatformKey) ? "A platform key." : "PlatformKey is not a platform key.");
            var url = _options.ApiUrl.TrimEnd('/');
            Add("api url", url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? DoctorStatus.Ok : DoctorStatus.Warn,
                url, url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? null : "Use https outside local development.");

            // 2. status endpoint + clock skew (the Date header of any answer is the server clock)
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url + "/v1/status");
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.Headers.Date.HasValue)
                {
                    var skew = Math.Abs((response.Headers.Date.Value - now()).TotalSeconds);
                    Add("clock skew", skew > options.FailSkewSeconds ? DoctorStatus.Fail : skew > options.WarnSkewSeconds ? DoctorStatus.Warn : DoctorStatus.Ok,
                        $"{skew:0.#} s from the API clock.", skew > options.WarnSkewSeconds ? "Sync this host's clock (NTP); token expiry checks use it." : null);
                }
                else Add("clock skew", DoctorStatus.Skipped, "The API sent no Date header.");
                if (response.IsSuccessStatusCode)
                {
                    var text =
#if NET8_0_OR_GREATER
                        await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                    using var doc = JsonDocument.Parse(text);
                    var overall = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : "unknown";
                    Add("service status", overall == "operational" ? DoctorStatus.Ok : DoctorStatus.Warn, "Atlas reports " + overall + ".",
                        overall == "operational" ? null : "Retries with backoff are the right response; see the status page.");
                }
                else Add("service status", DoctorStatus.Warn, "GET /v1/status answered " + (int)response.StatusCode + ".");
            }
            catch (Exception e) when (!cancellationToken.IsCancellationRequested)
            {
                Add("api reachable", DoctorStatus.Fail, "Could not reach " + url + ": " + e.Message, "Check DNS, proxy and firewall egress to the API host.");
            }

            // 3. the key is accepted
            if (!string.IsNullOrEmpty(_options.SecretKey))
            {
                try
                {
                    await _transport.SendAsync<JsonElement>(new RequestOptions { Method = HttpVerb.Get, Path = "/v1/instance" }, cancellationToken).ConfigureAwait(false);
                    Add("key accepted", DoctorStatus.Ok, "The API accepted the secret key.");
                }
                catch (AtlasException e) when (e.Status == 401)
                {
                    Add("key accepted", DoctorStatus.Fail, "The API rejected the secret key (401).", "The key is revoked, mistyped or from another instance.");
                }
                catch (AtlasException e) when (e.Status == 403)
                {
                    Add("key accepted", DoctorStatus.Warn, "The key is valid but lacks the instance:read scope (403).", "Add the scope, or ignore if this key is deliberately narrow.");
                }
                catch (Exception e) when (!cancellationToken.IsCancellationRequested)
                {
                    Add("key accepted", DoctorStatus.Warn, "Could not confirm the key: " + e.Message);
                }
            }

            // 4. JWKS
            var jwksUrl = options.JwksUrl ?? (options.Issuer != null ? options.Issuer.TrimEnd('/') + "/.well-known/jwks.json" : null);
            if (jwksUrl == null) Add("jwks", DoctorStatus.Skipped, "No Issuer or JwksUrl given.");
            else
            {
                var cache = new JwksCache(jwksUrl, _http);
                var jwks = await cache.GetAsync(null, cancellationToken).ConfigureAwait(false);
                if (jwks != null && jwks.Count > 0) Add("jwks", DoctorStatus.Ok, jwks.Count + " signing key(s) at " + jwksUrl + ".");
                else Add("jwks", DoctorStatus.Fail, "No signing keys at " + jwksUrl + ".", "Check the issuer; local token verification cannot work without keys.");
            }
            return new DoctorReport { Checks = checks };
        }
    }
}
