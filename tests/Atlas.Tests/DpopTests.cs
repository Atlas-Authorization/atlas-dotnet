using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas.AspNetCore;
using Atlas.Verification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>
    /// DPoP (RFC 9449) on the access-token verifier: the same vectors as the TypeScript @atlasauth/backend suite,
    /// plus every other refusal path. Keys are generated per run; the proofs are genuine ES256 / RS256 / PS256 /
    /// EdDSA JWSs.
    /// </summary>
    public class DpopTests
    {
        private const string Issuer = "https://auth.example.com";
        private const string Api = "https://api.example.com/mcp";
        private const string Meta = "https://api.example.com/.well-known/oauth-protected-resource/mcp";

        private readonly ECDsa _as = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        private sealed class Key
        {
            public string Alg = "";
            public Dictionary<string, object> Jwk = new Dictionary<string, object>();
            public string Jkt = "";
            public Func<byte[], byte[]> Sign = _ => Array.Empty<byte>();
        }

        private static string B64(byte[] raw) => Base64UrlEncoder.Encode(raw);

        private static JsonElement Element(object o) => JsonDocument.Parse(JsonSerializer.Serialize(o)).RootElement.Clone();

        private static Key NewKey(string alg = "ES256")
        {
            var k = new Key { Alg = alg };
            switch (alg)
            {
                case "ES256":
                {
                    var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                    var q = ec.ExportParameters(false).Q;
                    k.Jwk = new Dictionary<string, object> { ["kty"] = "EC", ["crv"] = "P-256", ["x"] = B64(q.X!), ["y"] = B64(q.Y!) };
                    k.Sign = d => ec.SignData(d, HashAlgorithmName.SHA256);
                    break;
                }
                case "RS256":
                case "PS256":
                {
                    var rsa = RSA.Create(2048);
                    var p = rsa.ExportParameters(false);
                    k.Jwk = new Dictionary<string, object> { ["kty"] = "RSA", ["n"] = B64(p.Modulus!), ["e"] = B64(p.Exponent!) };
                    var pad = alg == "PS256" ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1;
                    k.Sign = d => rsa.SignData(d, HashAlgorithmName.SHA256, pad);
                    break;
                }
                default:
                {
                    var seed = RandomNumberGenerator.GetBytes(32);
                    k.Jwk = new Dictionary<string, object> { ["kty"] = "OKP", ["crv"] = "Ed25519", ["x"] = B64(Ed25519Sign.PublicKey(seed)) };
                    k.Sign = d => Ed25519Sign.Sign(seed, d);
                    break;
                }
            }
            k.Jkt = Dpop.JwkThumbprint(Element(k.Jwk));
            return k;
        }

        /// <summary>Ed25519 signing (RFC 8032 §5.1.6) over the verifier's curve arithmetic, for test proofs.</summary>
        private static class Ed25519Sign
        {
            private static (BigInteger a, byte[] prefix) Expand(byte[] seed)
            {
                var h = Ed25519.Sha512(seed);
                var a = h.Take(32).ToArray();
                a[0] &= 248;
                a[31] &= 127;
                a[31] |= 64;
                return (Ed25519.FromLe(a), h.Skip(32).ToArray());
            }

            public static byte[] PublicKey(byte[] seed) => Ed25519.Encode(Ed25519.Mul(Expand(seed).a, Ed25519.B));

            public static byte[] Sign(byte[] seed, byte[] msg)
            {
                var (a, prefix) = Expand(seed);
                var pub = Ed25519.Encode(Ed25519.Mul(a, Ed25519.B));
                var r = Ed25519.Mod(Ed25519.FromLe(Ed25519.Sha512(prefix, msg)), Ed25519.L);
                var rEnc = Ed25519.Encode(Ed25519.Mul(r, Ed25519.B));
                var k = Ed25519.Mod(Ed25519.FromLe(Ed25519.Sha512(rEnc, pub, msg)), Ed25519.L);
                var s = Ed25519.Mod(r + k * a, Ed25519.L).ToByteArray();
                var sig = new byte[64];
                Array.Copy(rEnc, sig, 32);
                Array.Copy(s, 0, sig, 32, Math.Min(32, s.Length));
                return sig;
            }
        }

        private static string Proof(Key k, string token, Dictionary<string, object?>? claims = null, Dictionary<string, object>? header = null, bool tamper = false)
        {
            var body = new Dictionary<string, object?>
            {
                ["htm"] = "POST",
                ["htu"] = Api,
                ["jti"] = Guid.NewGuid().ToString(),
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["ath"] = Dpop.AccessTokenHash(token),
            };
            foreach (var kv in claims ?? new Dictionary<string, object?>())
            {
                if (kv.Value == null) body.Remove(kv.Key);
                else body[kv.Key] = kv.Value;
            }
            var h = header ?? new Dictionary<string, object> { ["typ"] = "dpop+jwt", ["alg"] = k.Alg, ["jwk"] = k.Jwk };
            string Enc(object o) => B64(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(o)));
            var input = Enc(h) + "." + Enc(body);
            var sig = k.Sign(Encoding.ASCII.GetBytes(input));
            if (tamper) input = Enc(h) + "." + Enc(new Dictionary<string, object> { ["htm"] = "POST", ["jti"] = "x" });
            return input + "." + B64(sig);
        }

        private static Dictionary<string, object?> C(params object?[] kv)
        {
            var d = new Dictionary<string, object?>();
            for (var i = 0; i < kv.Length; i += 2) d[(string)kv[i]!] = kv[i + 1];
            return d;
        }

        private string Jwks()
        {
            var q = _as.ExportParameters(false).Q;
            return "{\"keys\":[{\"kty\":\"EC\",\"crv\":\"P-256\",\"kid\":\"k1\",\"alg\":\"ES256\",\"x\":\"" + B64(q.X!) + "\",\"y\":\"" + B64(q.Y!) + "\"}]}";
        }

        private string Token(string? jkt = null)
        {
            var claims = new Dictionary<string, object>
            {
                ["sub"] = "user_1",
                ["token_use"] = "access_token",
                ["scope"] = "tools:read",
                ["client_id"] = "cli_1",
                ["jti"] = Guid.NewGuid().ToString(),
            };
            if (jkt != null) claims["cnf"] = new Dictionary<string, object> { ["jkt"] = jkt };
            return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = Api,
                Claims = claims,
                Expires = DateTime.UtcNow.AddMinutes(5),
                NotBefore = DateTime.UtcNow.AddMinutes(-1),
                IssuedAt = DateTime.UtcNow.AddMinutes(-1),
                SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(_as) { KeyId = "k1" }, SecurityAlgorithms.EcdsaSha256),
            });
        }

        private AccessTokenVerifier Verifier(Action<AccessTokenVerifierOptions>? mod = null)
        {
            var o = new AccessTokenVerifierOptions
            {
                Issuer = Issuer,
                Audiences = new[] { Api },
                JwksUrl = "https://jwks.test/keys",
                ResourceMetadataUrl = Meta,
                HttpClient = new HttpClient(MockHttpMessageHandler.Json(Jwks())),
            };
            mod?.Invoke(o);
            return new AccessTokenVerifier(o);
        }

        private static Task<AccessTokenResult> Check(AccessTokenVerifier v, string scheme, string tok, string? proof, string method = "POST", string url = Api + "?x=1")
            => v.VerifyRequestAsync(new AccessTokenRequest { Method = method, Url = url, Authorization = scheme + " " + tok, Dpop = proof });

        private static void Refused(AccessTokenResult r, string error, string name = "")
        {
            Assert.False(r.Ok, name);
            Assert.Equal(401, r.Status);
            Assert.True(error == r.Error, $"{name}: want {error}, got {r.Error} ({r.Description})");
            Assert.StartsWith("DPoP ", r.WwwAuthenticate);
            Assert.Contains("error=\"" + error + "\"", r.WwwAuthenticate);
            Assert.Contains("algs=\"ES256", r.WwwAuthenticate);
        }

        [Fact]
        public async Task AcceptsBoundTokenWithMatchingProofForEveryAlg()
        {
            var v = Verifier();
            foreach (var alg in new[] { "ES256", "RS256", "PS256", "EdDSA" })
            {
                var k = NewKey(alg);
                var tok = Token(k.Jkt);
                var r = await Check(v, "DPoP", tok, Proof(k, tok));
                Assert.True(r.Ok, alg + ": " + r.Description);
                Assert.True(r.DpopBound);
                Assert.Equal("user_1", r.Subject);
            }
            var plain = await Check(v, "Bearer", Token(), null);
            Assert.True(plain.Ok);
            Assert.False(plain.DpopBound);
        }

        [Fact]
        public async Task NeverAcceptsBoundTokenAsBearer()
        {
            var k = NewKey();
            var tok = Token(k.Jkt);
            Refused(await Check(Verifier(), "Bearer", tok, Proof(k, tok)), "invalid_token", "bearer");
            Refused(await Verifier().VerifyAsync("DPoP " + tok), "invalid_dpop_proof", "no proof");
            var unbound = Token();
            Refused(await Check(Verifier(), "DPoP", unbound, Proof(k, unbound)), "invalid_token", "unbound as DPoP");
        }

        [Fact]
        public async Task RejectsEveryBadProof()
        {
            var v = Verifier(o => o.DpopReplayStore = new MemoryDpopReplayStore());
            var k = NewKey();
            var other = NewKey();
            var tok = Token(k.Jkt);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var priv = new Dictionary<string, object>(k.Jwk) { ["d"] = "AAAA" };
            var cases = new Dictionary<string, string?>
            {
                ["missing proof"] = null,
                ["malformed"] = "not-a-jwt",
                ["garbage header"] = "e30.e30.e30",
                ["another key"] = Proof(other, tok),
                ["wrong htm"] = Proof(k, tok, C("htm", "GET")),
                ["wrong htu"] = Proof(k, tok, C("htu", "https://api.example.com/other")),
                ["wrong host"] = Proof(k, tok, C("htu", "https://evil.example.com/mcp")),
                ["relative htu"] = Proof(k, tok, C("htu", "/mcp")),
                ["wrong ath"] = Proof(k, tok, C("ath", "nope")),
                ["no ath"] = Proof(k, tok, C("ath", null)),
                ["proof for another token"] = Proof(k, Token(k.Jkt)),
                ["stale"] = Proof(k, tok, C("iat", now - 3600)),
                ["future"] = Proof(k, tok, C("iat", now + 60)),
                ["no iat"] = Proof(k, tok, C("iat", null)),
                ["expired exp"] = Proof(k, tok, C("exp", now - 60)),
                ["no jti"] = Proof(k, tok, C("jti", null)),
                ["typ JWT"] = Proof(k, tok, null, new Dictionary<string, object> { ["typ"] = "JWT", ["alg"] = "ES256", ["jwk"] = k.Jwk }),
                ["alg HS256"] = Proof(k, tok, null, new Dictionary<string, object> { ["typ"] = "dpop+jwt", ["alg"] = "HS256", ["jwk"] = k.Jwk }),
                ["alg none"] = Proof(k, tok, null, new Dictionary<string, object> { ["typ"] = "dpop+jwt", ["alg"] = "none", ["jwk"] = k.Jwk }),
                ["no jwk"] = Proof(k, tok, null, new Dictionary<string, object> { ["typ"] = "dpop+jwt", ["alg"] = "ES256" }),
                ["private jwk"] = Proof(k, tok, null, new Dictionary<string, object> { ["typ"] = "dpop+jwt", ["alg"] = "ES256", ["jwk"] = priv }),
                ["alg/key mismatch"] = Proof(NewKey("RS256"), tok, null, new Dictionary<string, object> { ["typ"] = "dpop+jwt", ["alg"] = "RS256", ["jwk"] = k.Jwk }),
                ["bad signature"] = Proof(k, tok, null, null, true),
            };
            foreach (var c in cases) Refused(await Check(v, "DPoP", tok, c.Value), "invalid_dpop_proof", c.Key);

            var once = Proof(k, tok, C("jti", "fixed"));
            Assert.True((await Check(v, "DPoP", tok, once)).Ok);
            Refused(await Check(v, "DPoP", tok, once), "invalid_dpop_proof", "replay");
            var tokOther = Token(other.Jkt);
            Assert.True((await Check(v, "DPoP", tokOther, Proof(other, tokOther, C("jti", "fixed")))).Ok, "same jti, other key");
        }

        [Fact]
        public async Task RequireDpopRefusesUnboundToken()
        {
            var v = Verifier(o => o.RequireDpop = true);
            Refused(await Check(v, "Bearer", Token(), null), "invalid_token", "requireDpop");
            var none = await v.VerifyAsync(null);
            Assert.Equal(401, none.Status);
            Assert.Null(none.Error);
            Assert.Matches("^DPoP .*algs=\"", none.WwwAuthenticate);
        }

        [Fact]
        public async Task HtuNormalisationAndPublicOrigin()
        {
            var k = NewKey();
            var tok = Token(k.Jkt);
            var v = Verifier();
            foreach (var htu in new[] { "HTTPS://API.Example.COM:443/mcp", "https://api.example.com/mcp?other=2#frag" })
                Assert.True((await Check(v, "DPoP", tok, Proof(k, tok, C("htu", htu)))).Ok, htu);
            Refused(await Check(v, "DPoP", tok, Proof(k, tok), "POST", "/mcp?x=1"), "invalid_dpop_proof", "path, no origin");
            var vo = Verifier(o => o.PublicOrigin = "https://api.example.com");
            Assert.True((await Check(vo, "DPoP", tok, Proof(k, tok), "post", "/mcp?x=1")).Ok);
            var norm = new Dictionary<string, string>
            {
                ["https://A.example.com:443"] = "https://a.example.com/",
                ["http://a.example.com:80/x?y#z"] = "http://a.example.com/x",
                ["https://a.example.com:8443/p/"] = "https://a.example.com:8443/p/",
                ["https://[::1]:443/p"] = "https://[::1]/p",
            };
            foreach (var n in norm) Assert.Equal(n.Value, Dpop.NormalizeHtu(n.Key));
            Assert.Null(Dpop.NormalizeHtu("ftp://x/y"));
        }

        [Fact]
        public async Task ServerNonce()
        {
            var clock = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var nonces = new HmacDpopNonceSource(Encoding.ASCII.GetBytes(new string('s', 32)), 60, () => clock);
            var v = Verifier(o => o.DpopNonces = nonces);
            var k = NewKey();
            var tok = Token(k.Jkt);
            var r = await Check(v, "DPoP", tok, Proof(k, tok));
            Refused(r, "use_dpop_nonce", "no nonce");
            Assert.Equal(nonces.Current(), r.DpopNonce);
            Refused(await Check(v, "DPoP", tok, Proof(k, tok, C("nonce", "made-up"))), "use_dpop_nonce", "wrong nonce");
            var ok = await Check(v, "DPoP", tok, Proof(k, tok, C("nonce", r.DpopNonce)));
            Assert.True(ok.Ok, ok.Description);
            Assert.NotNull(ok.DpopNonce);
            var issued = nonces.Current();
            clock += 60_000;
            Assert.True(nonces.Valid(issued));
            clock += 60_000;
            Assert.False(nonces.Valid(issued));
            Refused(await Check(v, "DPoP", tok, Proof(k, tok, C("nonce", issued))), "use_dpop_nonce", "stale nonce");
            Assert.Throws<ArgumentException>(() => new HmacDpopNonceSource(Encoding.ASCII.GetBytes("short")));
        }

        [Fact]
        public void Helpers()
        {
            const string n = "0vx7agoebGcQSuuPiLJXZptN9nndrQmbXEps2aiAFbWhM78LhWx4cbbfAAtVT86zwu1RK7aPFFxuhDR1L6tSoc_BJECPebWKRXjBZCiFV4n3oknjhMstn64tZ_2W-5JsGY4Hc5n9yBXArwl93lqt7_RN5w6Cf0h4QyQ5v-65YGjQR0_FDW2QvzqY368QQMicAtaSqzs8KJZgnYb9c7d0zgdAZHzu6qMQvRL5hajrn1n91CbOpbISD08qNLyrdkt-bFTWhAI4vMQFh6WeZu0fM4lFd2NcRwr3XPksINHaQ-G_xBniIqbw0Ls1jF44-csFCur-kEgU8awapJzKnqDKgw";
            Assert.Equal("NzbLsXh8uDCcd-6MNwXF4W_7noWXFZAfHkxZsRGC9Xs",
                Dpop.JwkThumbprint(Element(new Dictionary<string, string> { ["kty"] = "RSA", ["e"] = "AQAB", ["alg"] = "RS256", ["kid"] = "2011-04-29", ["n"] = n })));
            Assert.Throws<ArgumentException>(() => Dpop.JwkThumbprint(Element(new Dictionary<string, string> { ["kty"] = "oct", ["k"] = "x" })));
            long clock = 1_000_000;
            var store = new MemoryDpopReplayStore(2, () => clock);
            Assert.True(store.Claim("a", 1_060_000));
            Assert.False(store.Claim("a", 1_060_000));
            clock = 1_120_000;
            Assert.True(store.Claim("a", 1_180_000));
            store.Claim("b", 1_180_000);
            store.Claim("c", 1_180_000);
            Assert.True(store.Count <= 2);
            var doc = AccessTokenVerifier.ProtectedResourceMetadata(Api, Issuer, null, true);
            Assert.Equal(true, doc["dpop_bound_access_tokens_required"]);
            Assert.Equal(Dpop.Algs, doc["dpop_signing_alg_values_supported"]);
            Assert.Equal("DPoP error=\"use_dpop_nonce\", error_description=\"n\", algs=\"" + string.Join(" ", Dpop.Algs) + "\"",
                AccessTokenVerifier.BuildWwwAuthenticate(error: "use_dpop_nonce", description: "n", scheme: "DPoP"));
        }

        private static byte[] Hex(string h) => Enumerable.Range(0, h.Length / 2).Select(i => Convert.ToByte(h.Substring(i * 2, 2), 16)).ToArray();

        [Fact]
        public void Ed25519MatchesRfc8032Vectors()
        {
            // RFC 8032 §7.1 TEST 1 and TEST 2.
            var pub1 = Hex("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a");
            var sig1 = Hex("e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b");
            Assert.True(Ed25519.Verify(pub1, Array.Empty<byte>(), sig1));
            Assert.Equal(pub1, Ed25519Sign.PublicKey(Hex("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60")));
            Assert.Equal(sig1, Ed25519Sign.Sign(Hex("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60"), Array.Empty<byte>()));
            var pub2 = Hex("3d4017c3e843895a92b70aa74d1b7ebc9c982ccf2ec4968cc0cd55f12af4660c");
            var sig2 = Hex("92a009a9f0d4cab8720e820b5f642540a2b27b5416503f8fb3762223ebdb69da085ac1e43e15996e458f3613d0f11d8c387b2eaeb4302aeeb00d291612bb0c00");
            Assert.True(Ed25519.Verify(pub2, new byte[] { 0x72 }, sig2));
            Assert.False(Ed25519.Verify(pub2, new byte[] { 0x73 }, sig2));
            var bad = (byte[])sig2.Clone();
            bad[40] ^= 1;
            Assert.False(Ed25519.Verify(pub2, new byte[] { 0x72 }, bad));
            Assert.False(Ed25519.Verify(pub1, new byte[] { 0x72 }, sig2));
            // S >= L is refused (malleability).
            var high = (byte[])sig2.Clone();
            for (var i = 32; i < 64; i++) high[i] = 0xff;
            Assert.False(Ed25519.Verify(pub2, new byte[] { 0x72 }, high));
        }

        [Fact]
        public async Task AspNetCoreMiddlewareDpopWithNonce()
        {
            var nonces = new HmacDpopNonceSource(Encoding.ASCII.GetBytes(new string('k', 32)));
            var v = Verifier(o => o.DpopNonces = nonces);
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAtlasAccessToken(v);
                    app.Run(ctx => ctx.Response.WriteAsync(ctx.GetAtlasAccessToken()!.DpopBound ? "bound" : "plain"));
                });
            using var server = new TestServer(builder) { BaseAddress = new Uri("https://api.example.com/") };
            var client = server.CreateClient();
            var k = NewKey("EdDSA");
            var tok = Token(k.Jkt);
            async Task<HttpResponseMessage> Send(params string[] proofs)
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "https://api.example.com/docs/1?draft=1");
                req.Headers.TryAddWithoutValidation("Authorization", "DPoP " + tok);
                foreach (var p in proofs) req.Headers.TryAddWithoutValidation("DPoP", p);
                return await client.SendAsync(req);
            }
            string P(string? nonce, string jti) => Proof(k, tok, C("htm", "PUT", "htu", "https://api.example.com/docs/1", "nonce", nonce, "jti", jti));

            var first = await Send(P(null, "a"));
            Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
            var nonce = first.Headers.GetValues("DPoP-Nonce").Single();
            Assert.StartsWith("DPoP error=\"use_dpop_nonce\"", first.Headers.NonValidated["WWW-Authenticate"].ToString());
            var ok = await Send(P(nonce, "b"));
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Equal("bound", await ok.Content.ReadAsStringAsync());
            Assert.True(ok.Headers.Contains("DPoP-Nonce"));
            var replay = await Send(P(nonce, "b"));
            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
            Assert.Contains("error=\"invalid_dpop_proof\"", replay.Headers.NonValidated["WWW-Authenticate"].ToString());
            Assert.Equal(HttpStatusCode.Unauthorized, (await Send(P(nonce, "c"), P(nonce, "d"))).StatusCode);
        }
    }
}
