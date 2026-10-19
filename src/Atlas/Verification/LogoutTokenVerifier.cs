using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.Verification
{
    /// <summary>Configuration for a <see cref="LogoutTokenVerifier"/>.</summary>
    public sealed class LogoutTokenVerifierOptions
    {
        /// <summary>The instance's JWKS URL (the same one <see cref="AtlasBackend"/> uses).</summary>
        public string JwksUrl { get; set; } = "";

        /// <summary>The issuer your app's ID tokens carry (<c>iss</c>).</summary>
        public string Issuer { get; set; } = "";

        /// <summary>Your app's client id — the logout token's audience.</summary>
        public string ClientId { get; set; } = "";

        /// <summary>Tolerance for clock differences on <c>iat</c> and <c>exp</c>. Default 60 seconds.</summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>An optional shared <see cref="HttpClient"/> for JWKS fetches.</summary>
        public HttpClient? HttpClient { get; set; }

        /// <summary>An optional clock override (epoch milliseconds) for tests.</summary>
        public Func<long>? Now { get; set; }
    }

    /// <summary>Why a <c>logout_token</c> was refused.</summary>
    public enum LogoutTokenFailure
    {
        Malformed,
        BadAlgorithm,
        WrongType,
        NoKeys,
        BadSignature,
        WrongIssuer,
        WrongAudience,
        MissingIat,
        IssuedInFuture,
        Expired,
        MissingJti,
        MissingEvent,
        HasNonce,
        MissingSubject,
    }

    /// <summary>
    /// Thrown by <see cref="LogoutTokenVerifier"/> when a token is not valid. Answer the request with 400,
    /// or with 503 and <c>Retry-After</c> when <see cref="Retryable"/> is true (Atlas then retries the
    /// delivery; it never retries a 400).
    /// </summary>
    public sealed class LogoutTokenException : Exception
    {
        public LogoutTokenException(LogoutTokenFailure reason, string message) : base(message)
        {
            Reason = reason;
        }

        /// <summary>Why the token was refused.</summary>
        public LogoutTokenFailure Reason { get; }

        /// <summary>
        /// True when the failure is temporary and on your side: <see cref="LogoutTokenFailure.NoKeys"/>,
        /// the issuer's keys could not be fetched (a network error or a JWKS outage).
        /// </summary>
        public bool Retryable => Reason == LogoutTokenFailure.NoKeys;
    }

    /// <summary>
    /// The verified claims of a back-channel <c>logout_token</c>. End every local
    /// session matching <see cref="Sid"/> (when present), or every session of <see cref="Sub"/>.
    /// </summary>
    public sealed class LogoutTokenClaims
    {
        /// <summary>The user whose session ended (may be null when <see cref="Sid"/> is set).</summary>
        public string? Sub { get; init; }

        /// <summary>The Atlas session id your ID tokens carried (may be null when <see cref="Sub"/> is set).</summary>
        public string? Sid { get; init; }

        /// <summary>The token's unique id — record it to reject a replay.</summary>
        public string Jti { get; init; } = "";

        /// <summary>Issued-at, in seconds since the epoch.</summary>
        public long Iat { get; init; }

        /// <summary>Every claim, as decoded.</summary>
        public IReadOnlyDictionary<string, JsonElement> Claims { get; init; } = new Dictionary<string, JsonElement>();
    }

    /// <summary>
    /// Verifies an OIDC Back-Channel Logout <c>logout_token</c> (OpenID Connect
    /// Back-Channel Logout 1.0 §2.6) that Atlas POSTs to your app's
    /// <c>backchannel_logout_uri</c> when a user signs out.
    ///
    /// Checks: RS256 signature against the instance JWKS (cached); header <c>typ</c>
    /// is <c>logout+jwt</c> when present; <c>iss</c> equals the issuer; <c>aud</c>
    /// contains your client id; <c>iat</c> is present and not in the future;
    /// <c>exp</c> (when present) has not passed; <c>jti</c> is present; <c>events</c>
    /// contains the back-channel logout event with an object value; <c>sid</c> and/or
    /// <c>sub</c> is present; and there is no <c>nonce</c>.
    ///
    /// The verifier does not remember tokens: to reject a replay, record each
    /// <see cref="LogoutTokenClaims.Jti"/> until the token would have expired and
    /// refuse one you have already seen.
    /// </summary>
    public sealed class LogoutTokenVerifier
    {
        /// <summary>The event key that marks a JWT as a back-channel logout token.</summary>
        public const string BackchannelLogoutEvent = "http://schemas.openid.net/event/backchannel-logout";

        private readonly LogoutTokenVerifierOptions _options;
        private readonly JwksCache _jwks;

        public LogoutTokenVerifier(LogoutTokenVerifierOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.JwksUrl)) throw new ArgumentException("JwksUrl is required.", nameof(options));
            if (string.IsNullOrEmpty(options.Issuer)) throw new ArgumentException("Issuer is required.", nameof(options));
            if (string.IsNullOrEmpty(options.ClientId)) throw new ArgumentException("ClientId (the token audience) is required.", nameof(options));
            _options = options;
            _jwks = new JwksCache(options.JwksUrl, options.HttpClient ?? new HttpClient(), options.Now);
        }

        /// <summary>
        /// Read <c>logout_token</c> out of an <c>application/x-www-form-urlencoded</c>
        /// request body and verify it.
        /// </summary>
        /// <exception cref="LogoutTokenException">The field is missing or the token is invalid.</exception>
        public Task<LogoutTokenClaims> VerifyFormBodyAsync(string formBody, CancellationToken ct = default)
            => VerifyAsync(ExtractLogoutToken(formBody) ?? "", ct);

        /// <summary>The <c>logout_token</c> field of a form-encoded body, or null when absent.</summary>
        public static string? ExtractLogoutToken(string? formBody)
        {
            if (formBody == null) return null;
            foreach (var pair in formBody.Split('&'))
            {
                var eq = pair.IndexOf('=');
                var key = Uri.UnescapeDataString((eq < 0 ? pair : pair.Substring(0, eq)).Replace('+', ' '));
                if (key == "logout_token")
                    return eq < 0 ? "" : Uri.UnescapeDataString(pair.Substring(eq + 1).Replace('+', ' '));
            }
            return null;
        }

        /// <summary>Verify a logout token and return the claims you need to end the matching session(s).</summary>
        /// <exception cref="LogoutTokenException">The token is not valid; <see cref="LogoutTokenException.Reason"/> says why.</exception>
        public async Task<LogoutTokenClaims> VerifyAsync(string token, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(token) || token.Split('.').Length != 3)
                throw new LogoutTokenException(LogoutTokenFailure.Malformed, "logout_token is missing or not a JWT");

            Dictionary<string, JsonElement> header, payload;
            try
            {
                var parts = token.Split('.');
                header = DecodeSegment(parts[0]);
                payload = DecodeSegment(parts[1]);
            }
            catch
            {
                throw new LogoutTokenException(LogoutTokenFailure.Malformed, "logout_token could not be parsed");
            }

            if (!header.TryGetValue("alg", out var alg) || alg.ValueKind != JsonValueKind.String || alg.GetString() != "RS256")
                throw new LogoutTokenException(LogoutTokenFailure.BadAlgorithm, "logout_token must be signed with RS256");
            if (header.TryGetValue("typ", out var typ))
            {
                var t = typ.ValueKind == JsonValueKind.String ? typ.GetString()!.ToLowerInvariant() : "";
                if (t != "logout+jwt" && t != "application/logout+jwt")
                    throw new LogoutTokenException(LogoutTokenFailure.WrongType, "logout_token typ must be logout+jwt");
            }

            var kid = JwksCache.ReadKid(token);
            var keys = await _jwks.GetAsync(kid, ct).ConfigureAwait(false);
            if (keys == null || keys.Count == 0)
                throw new LogoutTokenException(LogoutTokenFailure.NoKeys, "no signing keys available");
            // A key Atlas just rotated to, whose refetch failed: temporary.
            if (kid != null && _jwks.LastOutcome == FetchOutcome.Failed && !keys.Kids.Contains(kid))
                throw new LogoutTokenException(LogoutTokenFailure.NoKeys, "signing key " + kid + " could not be fetched");
            if (!await SignatureValidAsync(token, keys).ConfigureAwait(false))
                throw new LogoutTokenException(LogoutTokenFailure.BadSignature, "logout_token signature is invalid");

            if (Str(payload, "iss") != _options.Issuer)
                throw new LogoutTokenException(LogoutTokenFailure.WrongIssuer, "logout_token iss does not match");
            if (!AudienceContains(payload, _options.ClientId))
                throw new LogoutTokenException(LogoutTokenFailure.WrongAudience, "logout_token aud does not include this client");

            var nowSec = (_options.Now?.Invoke() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 1000;
            var skew = (long)_options.ClockSkew.TotalSeconds;
            var iat = Num(payload, "iat");
            if (iat == null)
                throw new LogoutTokenException(LogoutTokenFailure.MissingIat, "logout_token has no iat");
            if (iat.Value > nowSec + skew)
                throw new LogoutTokenException(LogoutTokenFailure.IssuedInFuture, "logout_token iat is in the future");
            var exp = Num(payload, "exp");
            if (exp != null && nowSec > exp.Value + skew)
                throw new LogoutTokenException(LogoutTokenFailure.Expired, "logout_token has expired");

            var jti = Str(payload, "jti");
            if (string.IsNullOrEmpty(jti))
                throw new LogoutTokenException(LogoutTokenFailure.MissingJti, "logout_token has no jti");

            if (!payload.TryGetValue("events", out var events) || events.ValueKind != JsonValueKind.Object
                || !events.TryGetProperty(BackchannelLogoutEvent, out var ev) || ev.ValueKind != JsonValueKind.Object)
                throw new LogoutTokenException(LogoutTokenFailure.MissingEvent, "logout_token events must contain the back-channel logout event");
            if (payload.ContainsKey("nonce"))
                throw new LogoutTokenException(LogoutTokenFailure.HasNonce, "logout_token must not contain a nonce");

            var sub = Str(payload, "sub");
            var sid = Str(payload, "sid");
            if (string.IsNullOrEmpty(sub) && string.IsNullOrEmpty(sid))
                throw new LogoutTokenException(LogoutTokenFailure.MissingSubject, "logout_token needs sid or sub");

            return new LogoutTokenClaims
            {
                Sub = string.IsNullOrEmpty(sub) ? null : sub,
                Sid = string.IsNullOrEmpty(sid) ? null : sid,
                Jti = jti!,
                Iat = iat.Value,
                Claims = payload,
            };
        }

        private static async Task<bool> SignatureValidAsync(string token, Jwks keys)
        {
            try
            {
                var parameters = new TokenValidationParameters
                {
                    ValidAlgorithms = new[] { "RS256" },
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = new JsonWebKeySet(keys.Raw).GetSigningKeys(),
                    // Claims are checked one by one above/below so each failure has a reason.
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    RequireExpirationTime = false,
                    // The handler would otherwise refuse a non-"JWT" typ.
                    ValidateTokenReplay = false,
                    TypeValidator = (type, _, __) => type ?? "",
                };
                var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters).ConfigureAwait(false);
                return result.IsValid;
            }
            catch
            {
                return false;
            }
        }

        private static Dictionary<string, JsonElement> DecodeSegment(string segment)
        {
            var s = segment.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(s));
            using var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, JsonElement>();
            foreach (var p in doc.RootElement.EnumerateObject()) dict[p.Name] = p.Value.Clone();
            return dict;
        }

        private static string? Str(Dictionary<string, JsonElement> d, string key)
            => d.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static long? Num(Dictionary<string, JsonElement> d, string key)
            => d.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n) ? n : (long?)null;

        private static bool AudienceContains(Dictionary<string, JsonElement> d, string clientId)
        {
            if (!d.TryGetValue("aud", out var aud)) return false;
            if (aud.ValueKind == JsonValueKind.String) return aud.GetString() == clientId;
            if (aud.ValueKind != JsonValueKind.Array) return false;
            foreach (var a in aud.EnumerateArray())
            {
                if (a.ValueKind == JsonValueKind.String && a.GetString() == clientId) return true;
            }
            return false;
        }
    }
}
