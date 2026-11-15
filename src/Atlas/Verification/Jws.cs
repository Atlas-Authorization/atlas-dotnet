using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.Verification
{
    internal sealed class JwsResult
    {
        public bool IsValid { get; init; }
        public Exception? Exception { get; init; }
    }

    /// <summary>
    /// Signature and claim validation for every token this SDK verifies, over the five algorithms Atlas instances sign with: RS256, ES256, ES384,
    /// PS256 and EdDSA. The algorithm is pinned to that list (<c>none</c> and HMAC never verify) and only keys whose type matches it are tried (RSA
    /// for RS256/PS256, EC P-256 for ES256, EC P-384 for ES384, OKP Ed25519 for EdDSA; a JWK that names an <c>alg</c> must name this one), so a key
    /// can never be coerced into another algorithm. EdDSA, which Microsoft.IdentityModel does not implement, is verified with the managed
    /// <see cref="Ed25519"/> verifier and the claims are then checked here against the same <see cref="TokenValidationParameters"/>.
    /// </summary>
    internal static class Jws
    {
        public static async Task<JwsResult> ValidateAsync(string token, string jwksJson, TokenValidationParameters parameters)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3) return Bad("not a JWS");
                string? alg, kid;
                using (var h = JsonDocument.Parse(Base64Url(parts[0])))
                {
                    alg = h.RootElement.TryGetProperty("alg", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString() : null;
                    kid = h.RootElement.TryGetProperty("kid", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
                }
                if (alg == null || Array.IndexOf(SigningAlgs.All, alg) < 0) return Bad("algorithm not accepted");
                var all = new JsonWebKeySet(jwksJson).Keys;
                var compatible = all.Where(k => Compatible(k, alg)).ToList();
                var byKid = kid == null ? compatible : compatible.Where(k => k.Kid == kid).ToList();
                // a kid the set does not hold: any key of the right type (it may have just rotated). A kid the set DOES hold, as a key of another type, never falls back.
                var keys = byKid.Count > 0 ? byKid : (kid != null && all.Any(k => k.Kid == kid) ? byKid : compatible);
                if (keys.Count == 0) return Bad("no key of the right type");

                if (alg != "EdDSA")
                {
                    var p = parameters.Clone();
                    p.ValidAlgorithms = new[] { alg };
                    p.IssuerSigningKeys = keys;
                    p.IssuerSigningKey = null;
                    var r = await new JsonWebTokenHandler().ValidateTokenAsync(token, p).ConfigureAwait(false);
                    return new JwsResult { IsValid = r.IsValid, Exception = r.Exception };
                }

                var input = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
                var sig = Base64Url(parts[2]);
                var ok = sig.Length == 64 && keys.Any(k => { var x = Base64UrlEncoder.DecodeBytes(k.X); return x.Length == 32 && Ed25519.Verify(x, input, sig); });
                if (!ok) return Bad("signature");
                using var doc = JsonDocument.Parse(Base64Url(parts[1]));
                return CheckClaims(doc.RootElement, parameters) is { } why ? Bad(why) : new JwsResult { IsValid = true };
            }
            catch (Exception e) { return new JwsResult { Exception = e }; }
        }

        private static JwsResult Bad(string why) => new JwsResult { Exception = new SecurityTokenException(why) };

        private static byte[] Base64Url(string s) => Base64UrlEncoder.DecodeBytes(s);

        private static bool Compatible(JsonWebKey k, string alg)
        {
            if (k.Use != null && k.Use != "sig") return false;
            if (!string.IsNullOrEmpty(k.Alg) && k.Alg != alg) return false;
            switch (alg)
            {
                case "RS256": case "PS256": return k.Kty == "RSA";
                case "ES256": return k.Kty == "EC" && k.Crv == "P-256";
                case "ES384": return k.Kty == "EC" && k.Crv == "P-384";
                case "EdDSA": return k.Kty == "OKP" && k.Crv == "Ed25519" && !string.IsNullOrEmpty(k.X);
                default: return false;
            }
        }

        /// <summary>The issuer, audience and lifetime rules of <paramref name="p"/> applied to the claims; the reason it fails, or null.</summary>
        private static string? CheckClaims(JsonElement c, TokenValidationParameters p)
        {
            DateTime? Time(string n) => c.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? DateTimeOffset.FromUnixTimeSeconds(v.GetInt64()).UtcDateTime : (DateTime?)null;
            var exp = Time("exp"); var nbf = Time("nbf");
            if (p.RequireExpirationTime && exp == null) return "no exp";
            if (p.ValidateLifetime)
            {
                if (p.LifetimeValidator != null) { if (!p.LifetimeValidator(nbf, exp, null!, p)) return "lifetime"; }
                else
                {
                    var now = DateTime.UtcNow;
                    if ((exp.HasValue && now - p.ClockSkew > exp.Value) || (nbf.HasValue && now + p.ClockSkew < nbf.Value)) return "lifetime";
                }
            }
            if (p.ValidateIssuer)
            {
                var allowed = new List<string>(p.ValidIssuers ?? Enumerable.Empty<string>());
                if (p.ValidIssuer != null) allowed.Add(p.ValidIssuer);
                if (!(c.TryGetProperty("iss", out var iss) && iss.ValueKind == JsonValueKind.String && allowed.Contains(iss.GetString()!))) return "issuer";
            }
            if (p.ValidateAudience)
            {
                var allowed = new List<string>(p.ValidAudiences ?? Enumerable.Empty<string>());
                if (p.ValidAudience != null) allowed.Add(p.ValidAudience);
                var present = new List<string>();
                if (c.TryGetProperty("aud", out var aud))
                {
                    if (aud.ValueKind == JsonValueKind.String) present.Add(aud.GetString()!);
                    else if (aud.ValueKind == JsonValueKind.Array) present.AddRange(aud.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!));
                }
                if (!present.Any(allowed.Contains)) return "audience";
            }
            return null;
        }
    }
}
