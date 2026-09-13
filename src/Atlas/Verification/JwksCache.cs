using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Verification
{
    /// <summary>A parsed JWKS: the raw JSON (to build signing keys from) and the key ids it holds.</summary>
    public sealed class Jwks
    {
        /// <summary>The verbatim JWKS JSON, kept so a verifier can build key material from it.</summary>
        public string Raw { get; }

        /// <summary>The <c>kid</c> of every key in the set (entries with no kid contribute an empty id).</summary>
        public IReadOnlyList<string?> Kids { get; }

        public Jwks(string raw, IReadOnlyList<string?> kids)
        {
            Raw = raw;
            Kids = kids;
        }

        public int Count => Kids.Count;
    }

    /// <summary>The result of the last <see cref="JwksCache.GetAsync"/> — exposed so a test can assert the throttle bit.</summary>
    public enum FetchOutcome { Fresh, Cached, Refetched, Throttled, Failed }

    /// <summary>
    /// §7.3 in-process JWKS cache with kid-miss refetch (at most one per minute).
    ///
    /// The rate limit on the refetch is the security property, not politeness:
    /// without it an attacker sends tokens carrying random <c>kid</c> values and
    /// every one forces an outbound request to the JWKS endpoint, turning any
    /// unauthenticated caller into a traffic amplifier. The cache's job is as much
    /// to refuse to fetch as it is to fetch.
    /// </summary>
    public sealed class JwksCache
    {
        /// <summary>§7.3: at most one refetch per minute, however many misses arrive.</summary>
        public const long RefetchIntervalMs = 60_000;

        /// <summary>§7.3 serves <c>Cache-Control: max-age=3600</c>; honoured rather than ignored.</summary>
        public const long DefaultTtlMs = 3_600_000;

        private static readonly HttpClient SharedHttp = new HttpClient();

        private readonly string _url;
        private readonly HttpClient _http;
        private readonly Func<long> _now;
        private readonly long _ttlMs;
        private readonly long _refetchIntervalMs;

        private Jwks? _cached;
        private long _fetchedAt;
        private long _lastAttemptAt;

        /// <summary>Exposed so a caller can assert the rate limit actually bit.</summary>
        public FetchOutcome LastOutcome { get; private set; } = FetchOutcome.Fresh;

        public JwksCache(
            string url,
            HttpClient? http = null,
            Func<long>? now = null,
            long ttlMs = DefaultTtlMs,
            long refetchIntervalMs = RefetchIntervalMs)
        {
            _url = url;
            _http = http ?? SharedHttp;
            _now = now ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _ttlMs = ttlMs;
            _refetchIntervalMs = refetchIntervalMs;
        }

        private bool Has(string? kid)
        {
            if (_cached == null) return false;
            if (string.IsNullOrEmpty(kid)) return _cached.Count > 0;
            foreach (var k in _cached.Kids)
            {
                if (k == kid) return true;
            }
            return false;
        }

        /// <summary>
        /// The JWKS to verify against, refetching if this <paramref name="kid"/> is
        /// unknown. Returns whatever is cached when a refetch is throttled or fails
        /// — a stale JWKS still verifies every token signed by a key it contains,
        /// so a JWKS outage degrades to "new keys do not work yet", not "nobody can
        /// authenticate".
        /// </summary>
        public async Task<Jwks?> GetAsync(string? kid = null, CancellationToken ct = default)
        {
            var now = _now();

            var expired = _cached == null || now - _fetchedAt >= _ttlMs;
            var kidMiss = _cached != null && !Has(kid);

            if (!expired && !kidMiss)
            {
                LastOutcome = FetchOutcome.Cached;
                return _cached;
            }

            // The throttle. A miss inside the window is answered from cache, and
            // the token simply fails to verify.
            if (kidMiss && !expired && now - _lastAttemptAt < _refetchIntervalMs)
            {
                LastOutcome = FetchOutcome.Throttled;
                return _cached;
            }

            _lastAttemptAt = now;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, _url);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"JWKS fetch failed ({(int)response.StatusCode})");
                }

                var text =
#if NET8_0_OR_GREATER
                    await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

                var parsed = Parse(text);
                _cached = parsed;
                _fetchedAt = now;
                LastOutcome = kidMiss ? FetchOutcome.Refetched : FetchOutcome.Fresh;
                return _cached;
            }
            catch
            {
                // Keep serving what we have.
                LastOutcome = FetchOutcome.Failed;
                return _cached;
            }
        }

        private static Jwks Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array)
            {
                throw new Exception("JWKS is malformed");
            }

            var kids = new List<string?>();
            foreach (var key in keys.EnumerateArray())
            {
                if (key.TryGetProperty("kid", out var kid) && kid.ValueKind == JsonValueKind.String)
                {
                    kids.Add(kid.GetString());
                }
                else
                {
                    kids.Add(null);
                }
            }
            return new Jwks(json, kids);
        }

        /// <summary>Test and diagnostic surface — never used for a security decision.</summary>
        public (int Keys, long FetchedAt) Snapshot() => (_cached?.Count ?? 0, _fetchedAt);

        /// <summary>Read the <c>kid</c> from a JWT header without verifying anything.</summary>
        public static string? ReadKid(string jwt)
        {
            if (string.IsNullOrEmpty(jwt)) return null;
            var dot = jwt.IndexOf('.');
            if (dot <= 0) return null;
            var header = jwt.Substring(0, dot);
            try
            {
                var bytes = Base64UrlDecode(header);
                using var doc = JsonDocument.Parse(bytes);
                if (doc.RootElement.TryGetProperty("kid", out var kid) && kid.ValueKind == JsonValueKind.String)
                {
                    return kid.GetString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Base64url-decode a JWT segment (no padding, URL alphabet).</summary>
        internal static byte[] Base64UrlDecode(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
