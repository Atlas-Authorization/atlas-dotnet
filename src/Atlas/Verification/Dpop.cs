using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Atlas.Verification
{
    /// <summary>
    /// Guards against a DPoP proof being used twice. Back it with something every instance of your API
    /// shares (Redis <c>SET key 1 NX PX ttl</c>, a unique database key) when it runs on more than one;
    /// <see cref="MemoryDpopReplayStore"/> protects a single process.
    /// </summary>
    public interface IDpopReplayStore
    {
        /// <summary>Record <paramref name="key"/> until <paramref name="expiresAtMs"/> (epoch ms). True the first time, false on a replay.</summary>
        bool Claim(string key, long expiresAtMs);
    }

    /// <summary>The in-process <see cref="IDpopReplayStore"/>: entries live until they expire, at most <c>maxEntries</c>.</summary>
    public sealed class MemoryDpopReplayStore : IDpopReplayStore
    {
        private readonly int _max;
        private readonly Func<long> _now;
        private readonly Dictionary<string, long> _seen = new Dictionary<string, long>();

        public MemoryDpopReplayStore(int maxEntries = 10_000, Func<long>? now = null)
        {
            _max = maxEntries > 0 ? maxEntries : 10_000;
            _now = now ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        /// <summary>How many entries are held.</summary>
        public int Count { get { lock (_seen) return _seen.Count; } }

        public bool Claim(string key, long expiresAtMs)
        {
            lock (_seen)
            {
                var now = _now();
                if (_seen.TryGetValue(key, out var prior) && prior > now) return false;
                _seen[key] = expiresAtMs;
                if (_seen.Count > _max)
                {
                    foreach (var k in _seen.Where(e => e.Value <= now).Select(e => e.Key).ToList()) _seen.Remove(k);
                    while (_seen.Count > _max)
                    {
                        var oldest = _seen.Where(e => e.Key != key).OrderBy(e => e.Value).Select(e => e.Key).FirstOrDefault();
                        if (oldest == null) break;
                        _seen.Remove(oldest);
                    }
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Hands out the server nonce a DPoP proof must carry (RFC 9449 §8) and checks one. <see cref="Current"/>
    /// goes to the client in the <c>DPoP-Nonce</c> header; <see cref="Valid"/> accepts the nonces still in force.
    /// </summary>
    public interface IDpopNonceSource
    {
        string Current();
        bool Valid(string nonce);
    }

    /// <summary>
    /// DPoP nonces derived from a secret and the clock: a nonce is good for the window it was issued in and
    /// the next one, so every instance sharing the secret agrees without shared state.
    /// </summary>
    public sealed class HmacDpopNonceSource : IDpopNonceSource
    {
        private readonly byte[] _secret;
        private readonly long _windowMs;
        private readonly Func<long> _now;

        /// <param name="secret">At least 32 bytes.</param>
        /// <param name="windowSeconds">How long one nonce is issued for.</param>
        /// <param name="now">Clock override (epoch ms).</param>
        public HmacDpopNonceSource(byte[] secret, int windowSeconds = 300, Func<long>? now = null)
        {
            if (secret == null || secret.Length < 32) throw new ArgumentException("The DPoP nonce secret must be at least 32 bytes.", nameof(secret));
            if (windowSeconds <= 0) throw new ArgumentException("The DPoP nonce window must be positive.", nameof(windowSeconds));
            _secret = (byte[])secret.Clone();
            _windowMs = windowSeconds * 1000L;
            _now = now ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        public string Current() => At(Epoch());

        public bool Valid(string nonce)
        {
            if (nonce == null) return false;
            var e = Epoch();
            var got = Encoding.ASCII.GetBytes(nonce);
            return Dpop.FixedTimeEquals(Encoding.ASCII.GetBytes(At(e)), got) || Dpop.FixedTimeEquals(Encoding.ASCII.GetBytes(At(e - 1)), got);
        }

        private long Epoch()
        {
            var t = _now();
            return t >= 0 ? t / _windowMs : -((-t + _windowMs - 1) / _windowMs);
        }

        private string At(long epoch)
        {
            using var mac = new HMACSHA256(_secret);
            var be = BitConverter.GetBytes(epoch);
            if (BitConverter.IsLittleEndian) Array.Reverse(be);
            var input = Encoding.ASCII.GetBytes("atlas-dpop-nonce:").Concat(be).ToArray();
            return Dpop.B64U(mac.ComputeHash(input));
        }
    }

    /// <summary>
    /// DPoP proof verification (RFC 9449) for <see cref="AccessTokenVerifier"/>. A DPoP-bound access token
    /// carries <c>cnf.jkt</c>, the RFC 7638 thumbprint of the client's key; each request must then arrive with
    /// the <c>DPoP</c> scheme and a <c>DPoP</c> header: a short-lived JWT (<c>typ</c> <c>dpop+jwt</c>) signed by
    /// that key, carrying the public key itself, the request method (<c>htm</c>) and URL (<c>htu</c>), the hash
    /// of the access token (<c>ath</c>), a unique <c>jti</c>, and the server's nonce when the API asks for one.
    /// </summary>
    public static class Dpop
    {
        /// <summary>How old a DPoP proof may be.</summary>
        public const int ProofMaxAgeSeconds = 300;
        /// <summary>Clock skew tolerated on a proof's <c>iat</c>.</summary>
        public const int ClockSkewSeconds = 5;
        /// <summary>The JWS algorithms accepted on a DPoP proof (and advertised in challenges / metadata).</summary>
        public static readonly IReadOnlyList<string> Algs = new[] { "ES256", "ES384", "ES512", "RS256", "RS384", "RS512", "PS256", "PS384", "PS512", "EdDSA" };

        private static readonly string[] PrivateMembers = { "d", "k", "p", "q", "dp", "dq", "qi" };
        private static readonly Regex B64UChars = new Regex("^[A-Za-z0-9_-]*$");

        /// <summary>A refusal: <see cref="Error"/> is <c>invalid_dpop_proof</c> or <c>use_dpop_nonce</c>.</summary>
        public sealed class Refusal
        {
            public Refusal(string error, string description) { Error = error; Description = description; }
            public string Error { get; }
            public string Description { get; }
        }

        internal static string B64U(byte[] raw) => Convert.ToBase64String(raw).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        internal static byte[]? B64UDecode(string? s)
        {
            if (s == null || !B64UChars.IsMatch(s) || s.Length % 4 == 1) return null;
            var t = s.Replace('-', '+').Replace('_', '/');
            t += new string('=', (4 - t.Length % 4) % 4);
            try { return Convert.FromBase64String(t); } catch (FormatException) { return null; }
        }

        internal static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static byte[] Sha256(byte[] input)
        {
            using var h = SHA256.Create();
            return h.ComputeHash(input);
        }

        /// <summary>The <c>ath</c> a DPoP proof carries for <paramref name="token"/>: base64url(SHA-256(token)).</summary>
        public static string AccessTokenHash(string token) => B64U(Sha256(Encoding.ASCII.GetBytes(token)));

        /// <summary>The RFC 7638 SHA-256 thumbprint (base64url) of a public EC, RSA or OKP JWK: the <c>cnf.jkt</c> a DPoP-bound token carries.</summary>
        /// <exception cref="ArgumentException">An unsupported or incomplete key.</exception>
        public static string JwkThumbprint(JsonElement jwk)
        {
            var kty = Str(jwk, "kty");
            string[] members = kty switch
            {
                "EC" => new[] { "crv", "kty", "x", "y" },
                "RSA" => new[] { "e", "kty", "n" },
                "OKP" => new[] { "crv", "kty", "x" },
                _ => throw new ArgumentException("Unsupported JWK kty."),
            };
            var sb = new StringBuilder("{");
            foreach (var m in members)
            {
                var v = Str(jwk, m);
                if (string.IsNullOrEmpty(v)) throw new ArgumentException("JWK member " + m + " is missing.");
                if (sb.Length > 1) sb.Append(',');
                sb.Append(JsonSerializer.Serialize(m)).Append(':').Append(JsonSerializer.Serialize(v));
            }
            return B64U(Sha256(Encoding.UTF8.GetBytes(sb.Append('}').ToString())));
        }

        /// <summary>
        /// Reduce a URL to the form RFC 9449 §4.3 compares <c>htu</c> in: scheme and host lower-cased, the default
        /// port dropped, an empty path as "/", no query or fragment. Null for anything but an absolute http(s) URL.
        /// </summary>
        public static string? NormalizeHtu(string? url)
        {
            if (url == null || !Regex.IsMatch(url, "^https?://", RegexOptions.IgnoreCase)) return null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u) || string.IsNullOrEmpty(u.Host)) return null;
            var scheme = u.Scheme.ToLowerInvariant();
            if (scheme != "http" && scheme != "https") return null;
            var host = u.Host.ToLowerInvariant();
            var path = u.AbsolutePath.Length == 0 ? "/" : u.AbsolutePath;
            return scheme + "://" + host + (u.IsDefaultPort ? "" : ":" + u.Port) + path;
        }

        private static string? Str(JsonElement o, string name)
            => o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static Refusal Bad(string d) => new Refusal("invalid_dpop_proof", d);

        private static JsonElement? Json(string segment)
        {
            var raw = B64UDecode(segment);
            if (raw == null) return null;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return doc.RootElement.ValueKind == JsonValueKind.Object ? doc.RootElement.Clone() : (JsonElement?)null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        internal static Refusal? Check(string proof, string token, string jkt, string method, string url, string? publicOrigin,
            long nowMs, IDpopReplayStore replay, IDpopNonceSource? nonces)
        {
            var parts = proof.Split('.');
            if (parts.Length != 3) return Bad("The DPoP proof is malformed.");
            var header = Json(parts[0]);
            if (header == null) return Bad("The DPoP proof is malformed.");
            var h = header.Value;
            if (Str(h, "typ") != "dpop+jwt") return Bad("The DPoP proof must have typ dpop+jwt.");
            var alg = Str(h, "alg");
            if (alg == null || !Algs.Contains(alg)) return Bad("The DPoP proof uses an unsupported algorithm.");
            if (!h.TryGetProperty("jwk", out var jwk) || jwk.ValueKind != JsonValueKind.Object) return Bad("The DPoP proof must carry its public key.");
            if (PrivateMembers.Any(m => jwk.TryGetProperty(m, out _))) return Bad("The DPoP proof key must be a public key.");
            var sig = B64UDecode(parts[2]);
            if (sig == null || !VerifySignature(jwk, alg, Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), sig))
                return Bad("The DPoP proof signature is invalid.");
            var payload = Json(parts[1]);
            if (payload == null) return Bad("The DPoP proof is malformed.");
            var p = payload.Value;
            string thumb;
            try { thumb = JwkThumbprint(jwk); } catch (ArgumentException) { thumb = ""; }
            if (thumb.Length == 0 || !FixedTimeEquals(Encoding.ASCII.GetBytes(thumb), Encoding.ASCII.GetBytes(jkt)))
                return Bad("The DPoP proof key does not match the token binding.");
            if (Str(p, "htm") != method) return Bad("The DPoP proof htm does not match the request method.");
            string? target = url;
            if (!Regex.IsMatch(url, "^https?://", RegexOptions.IgnoreCase))
            {
                target = null;
                if (publicOrigin != null && Uri.TryCreate(publicOrigin, UriKind.Absolute, out var origin) && Uri.TryCreate(origin, url, out var joined))
                    target = joined.ToString();
            }
            var expected = NormalizeHtu(target);
            var htu = Str(p, "htu");
            if (expected == null || htu == null || NormalizeHtu(htu) != expected) return Bad("The DPoP proof htu does not match the request URL.");
            var nowSec = nowMs / 1000.0;
            if (!p.TryGetProperty("iat", out var iatEl) || iatEl.ValueKind != JsonValueKind.Number || !iatEl.TryGetDouble(out var iat)
                || iat > nowSec + ClockSkewSeconds || nowSec - iat > ProofMaxAgeSeconds)
                return Bad("The DPoP proof is too old or from the future.");
            if (p.TryGetProperty("exp", out var expEl) && expEl.ValueKind == JsonValueKind.Number && expEl.TryGetDouble(out var exp)
                && nowSec >= exp + ClockSkewSeconds)
                return Bad("The DPoP proof is too old or from the future.");
            if (Str(p, "ath") != AccessTokenHash(token)) return Bad("The DPoP proof ath does not match the access token.");
            var jti = Str(p, "jti");
            if (string.IsNullOrEmpty(jti)) return Bad("The DPoP proof needs a jti.");
            if (nonces != null)
            {
                var nonce = Str(p, "nonce");
                if (string.IsNullOrEmpty(nonce) || !nonces.Valid(nonce!))
                    return new Refusal("use_dpop_nonce", "The DPoP proof must carry the nonce from the DPoP-Nonce header.");
            }
            if (!replay.Claim(jkt + ":" + jti, (long)(iat * 1000) + ProofMaxAgeSeconds * 1000L)) return Bad("The DPoP proof has already been used.");
            return null;
        }

        private static bool VerifySignature(JsonElement jwk, string alg, byte[] data, byte[] sig)
        {
            var keyAlg = Str(jwk, "alg");
            if (!string.IsNullOrEmpty(keyAlg) && keyAlg != alg) return false;
            var kty = Str(jwk, "kty");
            try
            {
                if (alg.StartsWith("ES", StringComparison.Ordinal))
                {
                    var (crv, curve, size, hash) = alg switch
                    {
                        "ES256" => ("P-256", ECCurve.NamedCurves.nistP256, 32, HashAlgorithmName.SHA256),
                        "ES384" => ("P-384", ECCurve.NamedCurves.nistP384, 48, HashAlgorithmName.SHA384),
                        _ => ("P-521", ECCurve.NamedCurves.nistP521, 66, HashAlgorithmName.SHA512),
                    };
                    var x = B64UDecode(Str(jwk, "x"));
                    var y = B64UDecode(Str(jwk, "y"));
                    if (kty != "EC" || Str(jwk, "crv") != crv || x == null || y == null || x.Length != size || y.Length != size || sig.Length != 2 * size)
                        return false;
                    using var ec = ECDsa.Create(new ECParameters { Curve = curve, Q = new ECPoint { X = x, Y = y } });
                    return ec.VerifyData(data, sig, hash);
                }
                if (alg.StartsWith("RS", StringComparison.Ordinal) || alg.StartsWith("PS", StringComparison.Ordinal))
                {
                    var n = B64UDecode(Str(jwk, "n"));
                    var e = B64UDecode(Str(jwk, "e"));
                    if (kty != "RSA" || n == null || e == null || e.Length == 0 || e.Length > 4) return false;
                    n = n.SkipWhile(b => b == 0).ToArray();
                    if (n.Length < 256 || sig.Length != n.Length) return false;
                    var hash = alg.Substring(2) switch
                    {
                        "256" => HashAlgorithmName.SHA256,
                        "384" => HashAlgorithmName.SHA384,
                        _ => HashAlgorithmName.SHA512,
                    };
                    using var rsa = RSA.Create();
                    rsa.ImportParameters(new RSAParameters { Modulus = n, Exponent = e });
                    return rsa.VerifyData(data, sig, hash, alg[0] == 'P' ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1);
                }
                if (alg == "EdDSA")
                {
                    var x = B64UDecode(Str(jwk, "x"));
                    if (kty != "OKP" || Str(jwk, "crv") != "Ed25519" || x == null || x.Length != 32) return false;
                    return Ed25519.Verify(x, data, sig);
                }
            }
            catch (CryptographicException)
            {
                return false;
            }
            return false;
        }
    }
}
