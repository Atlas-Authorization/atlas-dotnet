using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Verification
{
    // Accepting your OLD auth provider's JWTs during a cutover — today, Better Auth's `jwt` plugin tokens.
    //
    // When you move to Atlas, API clients may still hold short-lived JWTs your Better Auth app minted. With
    // AtlasBackendOptions.LegacyIssuers, a token whose `iss` is one of these issuers is verified against THAT issuer's
    // key set (the JWKS you imported into Atlas, or the old app's /api/auth/jwks) for a bounded window, and its `sub`
    // (the Better Auth user id) is mapped to the Atlas user id — Atlas keeps the Better Auth id as the user's
    // external_id. After `Until`, those tokens are refused like any other foreign token.

    /// <summary>One old issuer accepted during the cutover.</summary>
    public sealed class LegacyIssuer
    {
        /// <summary>The <c>iss</c> the old tokens carry (Better Auth: its <c>baseURL</c>, unless <c>jwt.issuer</c> was set). Required.</summary>
        public string Issuer { get; set; } = "";

        /// <summary>End of the acceptance window. Required: a legacy issuer is never accepted forever.</summary>
        public DateTimeOffset Until { get; set; }

        /// <summary>The imported key set JSON (<c>{"keys":[...]}</c>). Exactly one of <see cref="Jwks"/> / <see cref="JwksUrl"/>.</summary>
        public string? Jwks { get; set; }

        /// <summary>Where to fetch the old key set (e.g. <c>https://app.example.com/api/auth/jwks</c>).</summary>
        public string? JwksUrl { get; set; }

        /// <summary>Expected <c>aud</c> (unchecked when unset).</summary>
        public IReadOnlyList<string>? Audiences { get; set; }

        /// <summary>Accepted algorithms; default Better Auth's (EdDSA, ES256, ES512, RS256, PS256).</summary>
        public IReadOnlyList<string>? Algorithms { get; set; }

        /// <summary>
        /// Map the old <c>sub</c> to the Atlas user id (return null to refuse the token). Default: look the user up by
        /// <c>external_id</c> through the Backend API (needs <see cref="AtlasBackendOptions.SecretKey"/>), cached for 5 minutes.
        /// </summary>
        public Func<string, IReadOnlyDictionary<string, JsonElement>, CancellationToken, Task<string?>>? ResolveSubject { get; set; }

        /// <summary>The <c>legacy.kind</c> the verified claims report.</summary>
        public string Kind { get; set; } = "better_auth";
    }

    internal sealed class LegacyIssuerVerifier
    {
        /// <summary>Better Auth's <c>jwt</c> plugin key algorithms.</summary>
        internal static readonly string[] BetterAuthAlgs = { "EdDSA", "ES256", "ES512", "RS256", "PS256" };

        /// <summary>How long a resolved sub → Atlas id mapping is reused.</summary>
        internal const long SubjectTtlMs = 5 * 60_000;

        /// <summary>Cap on cached subjects, so a stream of distinct tokens cannot grow memory unbounded.</summary>
        internal const int SubjectCacheMax = 10_000;

        private readonly List<LegacyIssuer> _issuers;
        private readonly AtlasBackendOptions _options;
        private readonly HttpClient _http;
        private readonly Func<long> _now;
        private readonly object _lock = new object();
        private readonly Dictionary<string, (string? Id, long At)> _subjects = new Dictionary<string, (string?, long)>();
        private readonly Dictionary<string, JwksCache> _caches = new Dictionary<string, JwksCache>();
        private readonly Dictionary<string, JsonElement> _static = new Dictionary<string, JsonElement>();

        internal LegacyIssuerVerifier(IEnumerable<LegacyIssuer> issuers, AtlasBackendOptions options, HttpClient http)
        {
            _issuers = issuers.ToList();
            _options = options;
            _http = http;
            _now = options.Now ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            foreach (var entry in _issuers)
            {
                if (string.IsNullOrEmpty(entry.Issuer))
                    throw new ArgumentException("LegacyIssuers: every entry needs Issuer.", nameof(options));
                if (entry.Issuer == options.Issuer)
                    throw new ArgumentException("LegacyIssuers: a legacy issuer cannot be the Atlas issuer itself.", nameof(options));
                if (string.IsNullOrEmpty(entry.JwksUrl) == string.IsNullOrEmpty(entry.Jwks))
                    throw new ArgumentException($"LegacyIssuers: \"{entry.Issuer}\" needs exactly one of Jwks or JwksUrl.", nameof(options));
                if (entry.Until == default)
                    throw new ArgumentException($"LegacyIssuers: \"{entry.Issuer}\" needs Until (the end of the window).", nameof(options));
                if (entry.ResolveSubject == null && string.IsNullOrEmpty(options.SecretKey))
                    throw new ArgumentException(
                        $"LegacyIssuers: \"{entry.Issuer}\" maps its sub to an Atlas user through the Backend API, which needs SecretKey on AtlasBackendOptions — or give the issuer a ResolveSubject.",
                        nameof(options));
                if (!string.IsNullOrEmpty(entry.JwksUrl))
                {
                    _caches[entry.Issuer] = new JwksCache(entry.JwksUrl!, http, options.Now);
                }
                else
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(entry.Jwks!);
                        if (!doc.RootElement.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array)
                            throw new ArgumentException("no keys array");
                        _static[entry.Issuer] = doc.RootElement.Clone();
                    }
                    catch (Exception e) when (e is JsonException || e is ArgumentException)
                    {
                        throw new ArgumentException($"LegacyIssuers: \"{entry.Issuer}\" Jwks is not a JWKS ({{\"keys\":[...]}}).", nameof(options));
                    }
                }
            }
        }

        /// <summary>The UNVERIFIED <c>iss</c> of a compact JWT, for routing only.</summary>
        internal static string? PeekIssuer(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2) return null;
                using var doc = JsonDocument.Parse(JwksCache.Base64UrlDecode(parts[1]));
                return doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("iss", out var iss) && iss.ValueKind == JsonValueKind.String
                    ? iss.GetString() : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>The configured entry for this <c>iss</c>, when its window is still open.</summary>
        internal LegacyIssuer? Match(string? iss)
        {
            if (string.IsNullOrEmpty(iss)) return null;
            foreach (var entry in _issuers)
            {
                if (entry.Issuer == iss)
                    return entry.Until.ToUnixTimeMilliseconds() > _now() ? entry : null;
            }
            return null;
        }

        private async Task<List<JsonElement>> KeysAsync(LegacyIssuer entry, string? kid, CancellationToken ct)
        {
            JsonElement set;
            if (_static.TryGetValue(entry.Issuer, out var fixedSet))
            {
                set = fixedSet;
            }
            else
            {
                var jwks = await _caches[entry.Issuer].GetAsync(kid, ct).ConfigureAwait(false);
                if (jwks == null) return new List<JsonElement>();
                try
                {
                    using var doc = JsonDocument.Parse(jwks.Raw);
                    set = doc.RootElement.Clone();
                }
                catch (JsonException)
                {
                    return new List<JsonElement>();
                }
            }
            return set.GetProperty("keys").EnumerateArray().Where(k => k.ValueKind == JsonValueKind.Object).ToList();
        }

        /// <summary>The verified claims, <c>Invalid</c>, or <c>NoKeys</c> (the old issuer's key set is unavailable).</summary>
        internal async Task<(VerifyFailureReason? Failure, SessionClaims? Claims)> VerifyAsync(string token, LegacyIssuer entry, CancellationToken ct)
        {
            var invalid = ((VerifyFailureReason?)VerifyFailureReason.Invalid, (SessionClaims?)null);
            var parts = token.Split('.');
            if (parts.Length != 3) return invalid;
            JsonElement header, payload;
            byte[] signature;
            try
            {
                using (var h = JsonDocument.Parse(JwksCache.Base64UrlDecode(parts[0]))) header = h.RootElement.Clone();
                using (var p = JsonDocument.Parse(JwksCache.Base64UrlDecode(parts[1]))) payload = p.RootElement.Clone();
                signature = JwksCache.Base64UrlDecode(parts[2]);
            }
            catch
            {
                return invalid;
            }
            if (header.ValueKind != JsonValueKind.Object || payload.ValueKind != JsonValueKind.Object) return invalid;

            var alg = Str(header, "alg");
            var kid = Str(header, "kid");
            var allowed = entry.Algorithms != null && entry.Algorithms.Count > 0 ? entry.Algorithms : BetterAuthAlgs;
            if (alg == null || !allowed.Contains(alg) || Array.IndexOf(BetterAuthAlgs, alg) < 0) return invalid;

            var keys = await KeysAsync(entry, kid, ct).ConfigureAwait(false);
            if (keys.Count == 0) return ((VerifyFailureReason?)VerifyFailureReason.NoKeys, null);

            var data = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
            var verified = keys
                .Where(k => kid == null || Str(k, "kid") == kid)
                .Any(k => Dpop.VerifySignature(k, alg, data, signature));
            if (!verified) return invalid;

            // Claims. `exp` is REQUIRED: a legacy token without an expiry would outlive the window it is accepted in.
            var nowSec = _now() / 1000.0;
            if (Str(payload, "iss") != entry.Issuer) return invalid;
            if (!Num(payload, "exp", out var exp) || nowSec - ClockSkew > exp) return invalid;
            if (Num(payload, "nbf", out var nbf) && nowSec + ClockSkew < nbf) return invalid;
            if (entry.Audiences != null && entry.Audiences.Count > 0 && !AudienceMatches(payload, entry.Audiences)) return invalid;

            var legacySub = Str(payload, "sub");
            if (string.IsNullOrEmpty(legacySub)) legacySub = Str(payload, "id");
            if (string.IsNullOrEmpty(legacySub)) return invalid;

            var bag = payload.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
            var atlasId = await ResolveAsync(legacySub!, bag, entry, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(atlasId)) return invalid;

            var claims = new Dictionary<string, object?>();
            foreach (var p in bag) claims[p.Key] = p.Value;
            claims["sub"] = atlasId;
            claims["sid"] = Str(payload, "sid") ?? "";
            claims["external_id"] = legacySub;
            claims["legacy"] = new Dictionary<string, object?> { ["kind"] = entry.Kind, ["issuer"] = entry.Issuer, ["sub"] = legacySub };
            var session = AtlasJson.Deserialize<SessionClaims>(AtlasJson.Serialize(claims));
            return session == null ? invalid : ((VerifyFailureReason?)null, session);
        }

        private const double ClockSkew = AtlasBackend.ClockSkewSeconds;

        private static bool AudienceMatches(JsonElement payload, IReadOnlyList<string> expected)
        {
            if (!payload.TryGetProperty("aud", out var aud)) return false;
            if (aud.ValueKind == JsonValueKind.String) return expected.Contains(aud.GetString()!);
            return aud.ValueKind == JsonValueKind.Array
                && aud.EnumerateArray().Any(a => a.ValueKind == JsonValueKind.String && expected.Contains(a.GetString()!));
        }

        private async Task<string?> ResolveAsync(string legacySub, IReadOnlyDictionary<string, JsonElement> claims, LegacyIssuer entry, CancellationToken ct)
        {
            if (entry.ResolveSubject != null)
            {
                try
                {
                    var mapped = await entry.ResolveSubject(legacySub, claims, ct).ConfigureAwait(false);
                    return string.IsNullOrEmpty(mapped) ? null : mapped;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    return null;
                }
            }
            var key = entry.Issuer + "\0" + legacySub;
            var now = _now();
            lock (_lock)
            {
                if (_subjects.TryGetValue(key, out var hit) && now - hit.At < SubjectTtlMs) return hit.Id;
            }
            string? atlasId;
            try
            {
                var baseUrl = (_options.BapiBaseUrl ?? AtlasClientOptions.DefaultApiUrl).TrimEnd('/');
                using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/v1/users?external_id=" + Uri.EscapeDataString(legacySub) + "&limit=1");
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _options.SecretKey);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null; // not cached: a transient failure should not stick
                var text =
#if NET8_0_OR_GREATER
                    await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                using var doc = JsonDocument.Parse(text);
                atlasId = null;
                if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0
                    && data[0].ValueKind == JsonValueKind.Object && data[0].TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String
                    && !string.IsNullOrEmpty(id.GetString()))
                {
                    atlasId = id.GetString();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return null;
            }
            lock (_lock)
            {
                if (_subjects.Count >= SubjectCacheMax) _subjects.Clear();
                _subjects[key] = (atlasId, now);
            }
            return atlasId;
        }

        private static string? Str(JsonElement o, string name)
            => o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static bool Num(JsonElement o, string name, out double value)
        {
            value = 0;
            return o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out value);
        }
    }
}
