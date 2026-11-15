using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Verification;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.Primitives
{
    /// <summary>The verified claims of a Transaction Token (draft-ietf-oauth-transaction-tokens).</summary>
    public sealed class TxnTokenClaims
    {
        public string Iss { get; init; } = "";
        public string Sub { get; init; } = "";
        public string Aud { get; init; } = "";
        public long Iat { get; init; }
        public long Exp { get; init; }
        public string Txn { get; init; } = "";
        public string Scope { get; init; } = "";
        public string ReqWl { get; init; } = "";
        /// <summary>The transaction context (<c>tctx</c>), when present.</summary>
        public JsonElement? Tctx { get; init; }
        /// <summary>The requester context (<c>rctx</c>), when present.</summary>
        public JsonElement? Rctx { get; init; }
        /// <summary>Every claim.</summary>
        public JsonElement All { get; init; }
        public IReadOnlyList<string> Scopes => Scope.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public sealed class TxnTokenResult
    {
        public bool Ok { get; init; }
        public TxnTokenClaims? Claims { get; init; }
        public string? Error { get; init; }
    }

    /// <summary>
    /// Verify a Transaction Token a service received: <c>typ txntoken+jwt</c>, signed by the issuer's keys, for THIS trust domain, unexpired,
    /// with the required claims. A recipient must refuse a token whose <c>aud</c> is not its own trust domain.
    /// </summary>
    public sealed class TransactionTokenVerifier
    {
        private readonly string _issuer;
        private readonly string _trustDomain;
        private readonly JwksCache? _jwks;
        private readonly string? _staticJwks;
        private readonly Func<long> _now;

        public TransactionTokenVerifier(string issuer, string trustDomain, string? jwksUrl = null, string? jwksJson = null, HttpClient? http = null, Func<long>? nowMs = null)
        {
            if (string.IsNullOrEmpty(issuer)) throw new ArgumentException("issuer is required.", nameof(issuer));
            if (string.IsNullOrEmpty(trustDomain)) throw new ArgumentException("trustDomain is required.", nameof(trustDomain));
            _issuer = issuer.TrimEnd('/');
            _trustDomain = trustDomain;
            _staticJwks = jwksJson;
            _now = nowMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (jwksJson == null) _jwks = new JwksCache(jwksUrl ?? _issuer + "/.well-known/jwks.json", http ?? new HttpClient(), nowMs);
        }

        /// <summary>Never throws for a bad token.</summary>
        public async Task<TxnTokenResult> VerifyAsync(string token, CancellationToken ct = default)
        {
            const string refusal = "The transaction token is invalid, expired, or for another trust domain.";
            TxnTokenResult Fail(string e) => new TxnTokenResult { Error = e };
            try
            {
                var parts = (token ?? "").Split('.');
                if (parts.Length != 3) return Fail(refusal);
                using (var header = JsonDocument.Parse(JwksCache.Base64UrlDecode(parts[0])))
                {
                    var typ = header.RootElement.TryGetProperty("typ", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
                    var alg = header.RootElement.TryGetProperty("alg", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString() : null;
                    if (!string.Equals(typ, "txntoken+jwt", StringComparison.OrdinalIgnoreCase) || alg == null || !SigningAlgs.All.Contains(alg)) return Fail(refusal);
                }
                var raw = _staticJwks ?? (await _jwks!.GetAsync(JwksCache.ReadKid(token!), ct).ConfigureAwait(false))?.Raw;
                if (raw == null) return Fail(refusal);
                var now = DateTimeOffset.FromUnixTimeMilliseconds(_now()).UtcDateTime;
                var skew = TimeSpan.FromSeconds(5);
                var result = await Jws.ValidateAsync(token, raw, new TokenValidationParameters
                {
                    ValidAlgorithms = SigningAlgs.All, ValidateIssuerSigningKey = true, 
                    ValidateIssuer = true, ValidIssuer = _issuer, ValidateAudience = true, ValidAudience = _trustDomain,
                    ValidateLifetime = true, RequireExpirationTime = true,
                    LifetimeValidator = (nbf, exp, _, __) => exp.HasValue && now - skew <= exp.Value && (!nbf.HasValue || now + skew >= nbf.Value),
                }).ConfigureAwait(false);
                if (!result.IsValid) return Fail(refusal);
                JsonElement c;
                using (var doc = JsonDocument.Parse(JwksCache.Base64UrlDecode(parts[1]))) c = doc.RootElement.Clone();
                foreach (var name in new[] { "txn", "sub", "scope", "req_wl", "iat", "exp" })
                    if (!c.TryGetProperty(name, out _)) return Fail($"The transaction token has no {name}.");
                string S(string n) => c.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
                JsonElement? O(string n) => c.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Object ? v : (JsonElement?)null;
                return new TxnTokenResult
                {
                    Ok = true,
                    Claims = new TxnTokenClaims
                    {
                        Iss = S("iss"), Sub = S("sub"), Aud = c.TryGetProperty("aud", out var aud) && aud.ValueKind == JsonValueKind.Array && aud.GetArrayLength() > 0 ? aud[0].GetString()! : S("aud"),
                        Iat = c.GetProperty("iat").GetInt64(), Exp = c.GetProperty("exp").GetInt64(), Txn = S("txn"), Scope = S("scope"), ReqWl = S("req_wl"),
                        Tctx = O("tctx"), Rctx = O("rctx"), All = c,
                    },
                };
            }
            catch (Exception) when (!ct.IsCancellationRequested) { return Fail(refusal); }
        }
    }
}
