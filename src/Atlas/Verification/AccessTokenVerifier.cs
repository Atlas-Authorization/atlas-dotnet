using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.Verification
{
    /// <summary>
    /// The JWS algorithms Atlas signs tokens with: an instance key is RS256 (the default) or ES256, and an
    /// API may use either. Every verifier in this package is pinned to exactly this list — <c>none</c> and
    /// HMAC never verify.
    /// </summary>
    public static class SigningAlgs
    {
        public static readonly string[] All = { "RS256", "ES256" };
    }

    /// <summary>Configures an <see cref="AccessTokenVerifier"/>.</summary>
    public sealed class AccessTokenVerifierOptions
    {
        /// <summary>Your instance issuer. Required.</summary>
        public string Issuer { get; set; } = "";
        /// <summary>Your API identifier(s); the token's <c>aud</c> must contain one.</summary>
        public IReadOnlyList<string> Audiences { get; set; } = Array.Empty<string>();
        /// <summary>Skip the audience check (userinfo-style tokens). Off by default.</summary>
        public bool AnyAudience { get; set; }
        /// <summary>Scopes every request must carry.</summary>
        public IReadOnlyList<string> RequiredScopes { get; set; } = Array.Empty<string>();
        /// <summary>Defaults to <c>{Issuer}/.well-known/jwks.json</c>.</summary>
        public string? JwksUrl { get; set; }
        /// <summary>Advertised in every challenge as <c>resource_metadata</c> (RFC 9728).</summary>
        public string? ResourceMetadataUrl { get; set; }
        public string? Realm { get; set; }
        public HttpClient? HttpClient { get; set; }
        /// <summary>Clock override (epoch ms), for tests.</summary>
        public Func<long>? Now { get; set; }
        /// <summary>Refuse every token that is not DPoP-bound and challenge with the <c>DPoP</c> scheme.</summary>
        public bool RequireDpop { get; set; }
        /// <summary>This API's public origin, for the DPoP <c>htu</c> when the request URL is a path (set it behind a proxy).</summary>
        public string? PublicOrigin { get; set; }
        /// <summary>Where DPoP proof <c>jti</c>s are recorded; default a <see cref="MemoryDpopReplayStore"/> (one process).</summary>
        public IDpopReplayStore? DpopReplayStore { get; set; }
        /// <summary>Require every DPoP proof to carry a nonce this source issued (<c>use_dpop_nonce</c> otherwise).</summary>
        public IDpopNonceSource? DpopNonces { get; set; }
        /// <summary>
        /// Validate opaque access tokens (and, with <see cref="IntrospectionOptions.IntrospectJwts"/>, JWTs too) at
        /// Atlas's RFC 7662 introspection endpoint with your API's client credentials, cached briefly.
        /// </summary>
        public IntrospectionOptions? Introspection { get; set; }
    }

    /// <summary>
    /// What verification reads from an HTTP request: the method, the URL (absolute, or a path resolved against
    /// <see cref="AccessTokenVerifierOptions.PublicOrigin"/>) for the DPoP <c>htu</c>, and the <c>Authorization</c>
    /// and <c>DPoP</c> header values.
    /// </summary>
    public sealed class AccessTokenRequest
    {
        public string Method { get; init; } = "GET";
        public string Url { get; init; } = "";
        public string? Authorization { get; init; }
        public string? Dpop { get; init; }
    }

    /// <summary>
    /// The verdict. <see cref="Ok"/> with <see cref="Claims"/> on success; otherwise <see cref="Status"/>
    /// (400, 401 or 403), <see cref="Error"/> (null when no token was sent) and
    /// <see cref="WwwAuthenticate"/> (set it as the response header verbatim). <see cref="DpopNonce"/>, when set,
    /// goes in the <c>DPoP-Nonce</c> response header (on success and refusal alike).
    /// </summary>
    public sealed class AccessTokenResult
    {
        public bool Ok { get; init; }
        public int Status { get; init; } = 200;
        public string? Error { get; init; }
        public string Description { get; init; } = "";
        public string WwwAuthenticate { get; init; } = "";
        public JsonElement? Claims { get; init; }
        public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();
        public string Subject { get; init; } = "";
        public string? ClientId { get; init; }
        /// <summary>Whether the token was DPoP-bound (and its proof checked).</summary>
        public bool DpopBound { get; init; }
        public string? DpopNonce { get; init; }
    }

    /// <summary>
    /// Verifies Atlas-issued OAuth access tokens for your own API (an OAuth 2.0 resource server), locally
    /// against the instance JWKS — RS256 or ES256 only: the issuer, expiry, that it IS an access token, that
    /// its <c>aud</c> names your API (RFC 8707), and the scopes a route needs. A refusal carries the RFC 6750 /
    /// RFC 9728 <c>WWW-Authenticate</c> challenge. Sender-constrained tokens (RFC 9449 DPoP) are enforced: a token
    /// bound to a key (<c>cnf.jkt</c>) is accepted only with the <c>DPoP</c> scheme and a valid DPoP proof from that
    /// key, never as a plain Bearer token.
    /// </summary>
    public sealed class AccessTokenVerifier
    {
        private static readonly Regex Authz = new Regex(@"^(Bearer|DPoP)\s+([A-Za-z0-9\-._~+/]+=*)$", RegexOptions.IgnoreCase);
        private readonly AccessTokenVerifierOptions _options;
        private readonly string _issuer;
        private readonly JwksCache _jwks;
        private readonly IDpopReplayStore _replay;
        private readonly Introspector? _introspector;

        public AccessTokenVerifier(AccessTokenVerifierOptions options)
        {
            if (string.IsNullOrEmpty(options.Issuer)) throw new ArgumentException("Issuer is required.", nameof(options));
            if (!options.AnyAudience && options.Audiences.Count == 0)
                throw new ArgumentException("An audience is required (or AnyAudience).", nameof(options));
            _options = options;
            _issuer = options.Issuer.TrimEnd('/');
            _jwks = new JwksCache(options.JwksUrl ?? _issuer + "/.well-known/jwks.json", options.HttpClient ?? new HttpClient(), options.Now);
            _replay = options.DpopReplayStore ?? new MemoryDpopReplayStore(10_000, options.Now);
            if (options.Introspection != null)
                _introspector = new Introspector(_issuer, options.Introspection, options.HttpClient, options.Now);
        }

        /// <summary>Whether a <see cref="AccessTokenVerifierOptions.PublicOrigin"/> is set (adapters then pass the path, not the full URL).</summary>
        public bool HasPublicOrigin => _options.PublicOrigin != null;

        /// <summary>
        /// Verify an <c>Authorization</c> header value with no DPoP proof: a DPoP-bound token is refused. Use
        /// <see cref="VerifyRequestAsync"/> for DPoP. Never throws for a bad token.
        /// </summary>
        public Task<AccessTokenResult> VerifyAsync(string? authorization, IEnumerable<string>? extraScopes = null, CancellationToken ct = default)
            => VerifyRequestAsync(new AccessTokenRequest { Authorization = authorization }, extraScopes, ct);

        /// <summary>
        /// Verify a request: its <c>Authorization</c> header (<c>Bearer</c> or <c>DPoP</c> scheme) and, for a
        /// DPoP-bound token, its <c>DPoP</c> proof against the method and URL. Never throws for a bad token.
        /// </summary>
        public async Task<AccessTokenResult> VerifyRequestAsync(AccessTokenRequest request, IEnumerable<string>? extraScopes = null, CancellationToken ct = default)
        {
            var required = _options.RequiredScopes.Concat(extraScopes ?? Array.Empty<string>()).ToList();
            var nonce = _options.DpopNonces?.Current();
            AccessTokenResult Refuse(int status, string? error, string description, IReadOnlyList<string> req, string? scheme = null)
                => RefuseWith(status, error, description, req, scheme, nonce);
            var value = (request.Authorization ?? "").Trim();
            if (value.Length == 0) return Refuse(401, null, "Authentication required.", required);
            var m = Authz.Match(value);
            if (!m.Success) return Refuse(400, "invalid_request", "Malformed Authorization header.", required);
            var presentedDpop = string.Equals(m.Groups[1].Value, "dpop", StringComparison.OrdinalIgnoreCase);
            var token = m.Groups[2].Value;
            const string invalid = "The access token is invalid or expired.";
            JsonElement claims;
            var isJwt = token.Split('.').Length == 3;
            if (!isJwt || (_introspector != null && _introspector.IntrospectJwts))
            {
                // An opaque token (or a JWT the caller wants checked live) is validated by Atlas itself.
                if (_introspector == null)
                    return Refuse(401, "invalid_token", "The access token is not a JWT; configure Introspection to accept opaque tokens.", required);
                JsonElement? active;
                try
                {
                    active = await _introspector.IntrospectAsync(token, ct).ConfigureAwait(false);
                }
                catch (IntrospectionException)
                {
                    return Refuse(401, "invalid_token", "The access token could not be validated right now.", required);
                }
                if (active == null) return Refuse(401, "invalid_token", invalid, required);
                if (active.Value.TryGetProperty("iss", out var iss) && (iss.ValueKind != JsonValueKind.String || iss.GetString()!.TrimEnd('/') != _issuer))
                    return Refuse(401, "invalid_token", "The access token was issued by another authorization server.", required);
                var nowSec = (_options.Now != null ? _options.Now() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 1000.0;
                if (active.Value.TryGetProperty("exp", out var exp) && exp.ValueKind == JsonValueKind.Number
                    && exp.GetDouble() + AtlasBackend.ClockSkewSeconds <= nowSec)
                    return Refuse(401, "invalid_token", invalid, required);
                claims = active.Value;
            }
            else
            {
                var keys = await _jwks.GetAsync(JwksCache.ReadKid(token), ct).ConfigureAwait(false);
                if (keys == null || keys.Count == 0)
                    return Refuse(401, "invalid_token", "No signing keys are available to verify the token.", required);
                try
                {
                    var now = _options.Now != null ? DateTimeOffset.FromUnixTimeMilliseconds(_options.Now()).UtcDateTime : DateTime.UtcNow;
                    var skew = TimeSpan.FromSeconds(AtlasBackend.ClockSkewSeconds);
                    var parameters = new TokenValidationParameters
                    {
                        ValidAlgorithms = SigningAlgs.All,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeys = new JsonWebKeySet(keys.Raw).GetSigningKeys(),
                        ValidateIssuer = true,
                        ValidIssuer = _issuer,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        LifetimeValidator = (nbf, exp, _, __) =>
                            exp.HasValue && now - skew <= exp.Value && (!nbf.HasValue || now + skew >= nbf.Value),
                    };
                    var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters).ConfigureAwait(false);
                    if (!result.IsValid) return Refuse(401, "invalid_token", invalid, required);
                    using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(JwksCache.Base64UrlDecode(token.Split('.')[1])));
                    claims = doc.RootElement.Clone();
                }
                catch
                {
                    return Refuse(401, "invalid_token", invalid, required);
                }
            }

            if (claims.TryGetProperty("token_use", out var tu) && !(tu.ValueKind == JsonValueKind.String && tu.GetString() == "access_token"))
                return Refuse(401, "invalid_token", "The token is not an access token.", required);
            if (!_options.AnyAudience)
            {
                var aud = new List<string>();
                if (claims.TryGetProperty("aud", out var a))
                {
                    if (a.ValueKind == JsonValueKind.String) aud.Add(a.GetString()!);
                    else if (a.ValueKind == JsonValueKind.Array)
                        aud.AddRange(a.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!));
                }
                if (!aud.Any(_options.Audiences.Contains))
                    return Refuse(401, "invalid_token", "The access token was not issued for this resource.", required);
            }
            // Sender constraint (RFC 9449). A bound token is only ever a DPoP token.
            string? jkt = null;
            if (claims.TryGetProperty("cnf", out var cnf) && cnf.ValueKind == JsonValueKind.Object && cnf.TryGetProperty("jkt", out var jktEl))
            {
                jkt = jktEl.ValueKind == JsonValueKind.String ? jktEl.GetString() : null;
                if (string.IsNullOrEmpty(jkt)) return Refuse(401, "invalid_token", invalid, required);
            }
            if (jkt != null || _options.RequireDpop || presentedDpop)
            {
                if (jkt == null) return Refuse(401, "invalid_token", "This resource requires a DPoP-bound access token.", required, "DPoP");
                if (!presentedDpop) return Refuse(401, "invalid_token", "A DPoP-bound access token must be sent with the DPoP scheme.", required, "DPoP");
                var proof = (request.Dpop ?? "").Trim();
                if (proof.Length == 0) return Refuse(401, "invalid_dpop_proof", "A DPoP proof is required.", required, "DPoP");
                var nowMs = _options.Now != null ? _options.Now() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var method = string.IsNullOrEmpty(request.Method) ? "GET" : request.Method.ToUpperInvariant();
                var refusal = Dpop.Check(proof, token, jkt, method, request.Url ?? "", _options.PublicOrigin, nowMs, _replay, _options.DpopNonces);
                if (refusal != null) return Refuse(401, refusal.Error, refusal.Description, required, "DPoP");
            }

            var scopes = claims.TryGetProperty("scope", out var sc) && sc.ValueKind == JsonValueKind.String
                ? sc.GetString()!.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();
            var granted = new HashSet<string>(scopes);
            if (claims.TryGetProperty("permissions", out var perms) && perms.ValueKind == JsonValueKind.Array)
                foreach (var p in perms.EnumerateArray())
                    if (p.ValueKind == JsonValueKind.String) granted.Add(p.GetString()!);
            var missing = required.Where(s => !granted.Contains(s)).ToList();
            if (missing.Count > 0)
                return Refuse(403, "insufficient_scope", "The access token is missing the required scope(s): " + string.Join(" ", missing) + ".", required);

            string? Str(string name) => claims.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            return new AccessTokenResult
            {
                Ok = true,
                Claims = claims,
                Scopes = scopes,
                Subject = Str("sub") ?? "",
                ClientId = Str("client_id") ?? Str("azp"),
                DpopBound = jkt != null,
                DpopNonce = nonce,
            };
        }

        private AccessTokenResult RefuseWith(int status, string? error, string description, IReadOnlyList<string> required, string? scheme, string? nonce)
            => new AccessTokenResult
            {
                Ok = false,
                Status = status,
                Error = error,
                Description = description,
                DpopNonce = nonce,
                WwwAuthenticate = BuildWwwAuthenticate(_options.Realm, error, description,
                    error == null || error == "insufficient_scope" ? required : null, _options.ResourceMetadataUrl,
                    scheme ?? (_options.RequireDpop ? "DPoP" : "Bearer")),
            };

        /// <summary>
        /// An RFC 6750 / RFC 9449 / RFC 9728 challenge (<paramref name="scheme"/> <c>Bearer</c> or <c>DPoP</c>; a DPoP
        /// challenge also lists the accepted proof algorithms). Without <paramref name="error"/> it is the bare
        /// "authentication required" challenge.
        /// </summary>
        public static string BuildWwwAuthenticate(string? realm = null, string? error = null, string? description = null,
            IReadOnlyList<string>? scope = null, string? resourceMetadataUrl = null, string scheme = "Bearer")
        {
            static string Q(string v) => "\"" + v.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            var parts = new List<string>();
            if (realm != null) parts.Add("realm=" + Q(realm));
            if (error != null)
            {
                parts.Add("error=" + Q(error));
                if (description != null) parts.Add("error_description=" + Q(description));
            }
            if (scope != null && scope.Count > 0) parts.Add("scope=" + Q(string.Join(" ", scope)));
            if (resourceMetadataUrl != null) parts.Add("resource_metadata=" + Q(resourceMetadataUrl));
            var sch = scheme == "DPoP" ? "DPoP" : "Bearer";
            if (sch == "DPoP") parts.Add("algs=" + Q(string.Join(" ", Dpop.Algs)));
            return parts.Count == 0 ? sch : sch + " " + string.Join(", ", parts);
        }

        /// <summary>
        /// The RFC 9728 protected-resource metadata document for your API. <paramref name="dpopBoundAccessTokensRequired"/>
        /// advertises that tokens must be DPoP-bound (pair it with <see cref="AccessTokenVerifierOptions.RequireDpop"/>).
        /// </summary>
        public static Dictionary<string, object> ProtectedResourceMetadata(string resource, string issuer, IReadOnlyList<string>? scopesSupported = null,
            bool dpopBoundAccessTokensRequired = false)
        {
            var doc = new Dictionary<string, object>
            {
                ["resource"] = resource,
                ["authorization_servers"] = new[] { issuer.TrimEnd('/') },
                ["bearer_methods_supported"] = new[] { "header" },
            };
            if (scopesSupported != null && scopesSupported.Count > 0) doc["scopes_supported"] = scopesSupported;
            doc["dpop_signing_alg_values_supported"] = Dpop.Algs;
            if (dpopBoundAccessTokensRequired) doc["dpop_bound_access_tokens_required"] = true;
            return doc;
        }

        /// <summary>The RFC 9728 well-known path for a resource identifier.</summary>
        public static string ProtectedResourceMetadataPath(string resource)
            => "/.well-known/oauth-protected-resource" + new Uri(resource).AbsolutePath.TrimEnd('/');
    }
}
