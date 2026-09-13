using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.Verification
{
    /// <summary>Configuration for an <see cref="AtlasBackend"/> verifier.</summary>
    public sealed class AtlasBackendOptions
    {
        /// <summary>The instance's JWKS URL.</summary>
        public string JwksUrl { get; set; } = "";

        /// <summary>Expected <c>iss</c>. Required — an unchecked issuer accepts any Atlas instance.</summary>
        public string Issuer { get; set; } = "";

        /// <summary>
        /// §7.3 optional azp allowlist. When set, a token minted for a different
        /// origin is refused — what stops a token issued to one of the customer's
        /// apps being replayed against another.
        /// </summary>
        public IReadOnlyList<string>? AuthorizedParties { get; set; }

        /// <summary>The secret key, required only for <see cref="AtlasBackend.VerifyOnlineAsync"/>.</summary>
        public string? SecretKey { get; set; }

        /// <summary>The Backend-API base URL, required only for <see cref="AtlasBackend.VerifyOnlineAsync"/>.</summary>
        public string? BapiBaseUrl { get; set; }

        /// <summary>An optional shared <see cref="HttpClient"/> for JWKS fetches and online verify.</summary>
        public HttpClient? HttpClient { get; set; }

        /// <summary>An optional clock override (epoch milliseconds) for tests.</summary>
        public Func<long>? Now { get; set; }
    }

    /// <summary>
    /// §7.3 verification by customer backends. Verifies signature, exp/nbf with a
    /// 5-second clock-skew tolerance, iss, and optionally azp against an allowlist
    /// — locally, against cached JWKS, never calling Atlas on the hot path.
    /// </summary>
    public sealed class AtlasBackend
    {
        /// <summary>§7.3: five seconds either side, matching the server's minting tolerance.</summary>
        public const int ClockSkewSeconds = 5;

        private readonly AtlasBackendOptions _options;
        private readonly JwksCache _jwks;
        private readonly HttpClient _http;

        public AtlasBackend(AtlasBackendOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.JwksUrl)) throw new ArgumentException("JwksUrl is required.", nameof(options));
            if (string.IsNullOrEmpty(options.Issuer)) throw new ArgumentException("Issuer is required — an unchecked issuer accepts any Atlas instance.", nameof(options));

            _options = options;
            _http = options.HttpClient ?? new HttpClient();
            _jwks = new JwksCache(options.JwksUrl, _http, options.Now);
        }

        private DateTime NowUtc()
            => _options.Now != null
                ? DateTimeOffset.FromUnixTimeMilliseconds(_options.Now()).UtcDateTime
                : DateTime.UtcNow;

        /// <summary>
        /// Verify locally. No network call unless the <c>kid</c> is unknown, and at
        /// most one of those a minute.
        /// </summary>
        public async Task<VerifyResult> VerifyAsync(string token, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(token) || CountSegments(token) != 3)
                return VerifyResult.Failure(VerifyFailureReason.Malformed);

            var keys = await _jwks.GetAsync(JwksCache.ReadKid(token), ct).ConfigureAwait(false);
            if (keys == null || keys.Count == 0)
                return VerifyResult.Failure(VerifyFailureReason.NoKeys);

            SessionClaims claims;
            try
            {
                var signingKeys = new JsonWebKeySet(keys.Raw).GetSigningKeys();
                var parameters = new TokenValidationParameters
                {
                    // Pinned. Without this, a token whose header says `alg: none` — or
                    // any algorithm the key material can be coerced into — verifies.
                    ValidAlgorithms = new[] { "RS256" },
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = signingKeys,
                    ValidateIssuer = true,
                    ValidIssuer = _options.Issuer,
                    // The aud confusion guard below handles audience; an id_token that
                    // carries `aud` must be REJECTED, not merely audience-matched.
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(ClockSkewSeconds),
                    LifetimeValidator = (nbf, exp, _, __) =>
                    {
                        var now = NowUtc();
                        var skew = TimeSpan.FromSeconds(ClockSkewSeconds);
                        if (nbf.HasValue && now + skew < nbf.Value) return false;
                        if (exp.HasValue && now - skew > exp.Value) return false;
                        return true;
                    },
                };

                var handler = new JsonWebTokenHandler();
                var result = await handler.ValidateTokenAsync(token, parameters).ConfigureAwait(false);
                if (!result.IsValid)
                    return VerifyResult.Failure(VerifyFailureReason.Invalid);

                claims = DecodeClaims(token);
            }
            catch
            {
                // One reason for every failure. Telling a caller whether the
                // signature, the issuer or the expiry was wrong helps someone
                // refining a forged token more than it helps a developer.
                return VerifyResult.Failure(VerifyFailureReason.Invalid);
            }

            // §13.1 token-confusion guard. An OP access_token (`token_use:'access_token'`)
            // and an id_token (carries `aud`) are signed with the SAME per-instance key,
            // issuer and header as a first-party session JWT. Reject any token carrying
            // an OP marker so a "Sign in with Atlas" RP cannot replay one as a customer
            // session. An ABSENT `token_use`/`aud` is a valid (backward-compatible)
            // session, so this never mass-invalidates a live fleet.
            if (claims.Extra.TryGetValue("token_use", out var tokenUse))
            {
                var value = tokenUse.ValueKind == JsonValueKind.String ? tokenUse.GetString() : null;
                if (value != "session")
                    return VerifyResult.Failure(VerifyFailureReason.Invalid);
            }
            if (claims.Extra.ContainsKey("aud"))
                return VerifyResult.Failure(VerifyFailureReason.Invalid);

            if (_options.AuthorizedParties != null && _options.AuthorizedParties.Count > 0)
            {
                if (string.IsNullOrEmpty(claims.Azp) || !Contains(_options.AuthorizedParties, claims.Azp!))
                    return VerifyResult.Failure(VerifyFailureReason.UnauthorizedParty);
            }

            return VerifyResult.Success(new AuthorizedSession(claims));
        }

        /// <summary>
        /// §7.3 the documented slow path: ask Atlas whether the session is still
        /// live. Costs a round trip on every call and FAILS CLOSED on an outage —
        /// use it only where a 60-second revocation window is genuinely
        /// unacceptable (deleting an account, moving money).
        /// </summary>
        public async Task<VerifyResult> VerifyOnlineAsync(string token, CancellationToken ct = default)
        {
            var local = await VerifyAsync(token, ct).ConfigureAwait(false);
            if (!local.Ok) return local;

            if (string.IsNullOrEmpty(_options.SecretKey) || string.IsNullOrEmpty(_options.BapiBaseUrl))
            {
                throw new InvalidOperationException(
                    "VerifyOnline needs SecretKey and BapiBaseUrl. Without them it would silently fall back to local verification, the opposite of what the caller asked for.");
            }

            try
            {
                var url = _options.BapiBaseUrl!.TrimEnd('/') + "/v1/tokens/verify";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _options.SecretKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new { token }), Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return VerifyResult.Failure(VerifyFailureReason.Invalid);

                var body =
#if NET8_0_OR_GREATER
                    await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                using var doc = JsonDocument.Parse(body);
                var verified = doc.RootElement.TryGetProperty("verified", out var v)
                    && v.ValueKind == JsonValueKind.True;
                return verified ? local : VerifyResult.Failure(VerifyFailureReason.Invalid);
            }
            catch
            {
                // Fails CLOSED. The caller reached for VerifyOnline precisely because
                // a stale answer was unacceptable.
                return VerifyResult.Failure(VerifyFailureReason.Invalid);
            }
        }

        /// <summary>
        /// Verify whatever a request carries. §7.5: the <c>Authorization</c> header
        /// wins over the <c>__session</c> cookie — a caller that set it deliberately
        /// should not be overridden by a stale cookie.
        /// </summary>
        public Task<VerifyResult> AuthenticateRequestAsync(
            IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
        {
            var header = ReadHeader(headers, "authorization");
            var cookie = ReadHeader(headers, "cookie");

            string? bearer = header != null && header.StartsWith("Bearer ", StringComparison.Ordinal)
                ? header.Substring(7)
                : null;
            string? fromCookie = cookie != null ? ReadCookie(cookie, "__session") : null;

            var token = bearer ?? fromCookie;
            if (string.IsNullOrEmpty(token))
                return Task.FromResult(VerifyResult.Failure(VerifyFailureReason.Malformed));

            return VerifyAsync(token!, ct);
        }

        // ---- helpers ----

        private SessionClaims DecodeClaims(string token)
        {
            var parts = token.Split('.');
            var json = Encoding.UTF8.GetString(JwksCache.Base64UrlDecode(parts[1]));
            return AtlasJson.Deserialize<SessionClaims>(json) ?? new SessionClaims();
        }

        private static int CountSegments(string token)
        {
            var count = 1;
            foreach (var c in token)
            {
                if (c == '.') count++;
            }
            return count;
        }

        private static bool Contains(IReadOnlyList<string> list, string value)
        {
            foreach (var item in list)
            {
                if (item == value) return true;
            }
            return false;
        }

        private static string? ReadHeader(IReadOnlyDictionary<string, string> headers, string name)
        {
            if (headers.TryGetValue(name, out var exact)) return exact;
            // Header names are case-insensitive.
            foreach (var pair in headers)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }
            return null;
        }

        private static string? ReadCookie(string header, string name)
        {
            foreach (var part in header.Split(';'))
            {
                var trimmed = part.Trim();
                var eq = trimmed.IndexOf('=');
                if (eq <= 0) continue;
                if (trimmed.Substring(0, eq) == name)
                    return trimmed.Substring(eq + 1);
            }
            return null;
        }
    }
}
