using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas.Primitives;
using Atlas.Verification;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Every verifier accepts the five signing algorithms (a fresh runtime key each) and refuses algorithm confusion.</summary>
    public class SigningAlgorithmsTests
    {
        private const string Issuer = "https://issuer.test";
        private const string Kid = "k1";
        public static IEnumerable<object[]> Algs() => new[] { "RS256", "ES256", "ES384", "PS256", "EdDSA" }.Select(a => new object[] { a });

        private sealed class Key
        {
            public string Alg = "";
            public string Jwks = "";
            public Func<byte[], byte[]> Sig = null!;

            public string Token(string claimsJson, string? header = null)
            {
                var h = header ?? "{\"alg\":\"" + Alg + "\",\"typ\":\"JWT\",\"kid\":\"" + Kid + "\"}";
                var input = B64(h) + "." + B64(claimsJson);
                return input + "." + Base64UrlEncoder.Encode(Sig(Encoding.ASCII.GetBytes(input)));
            }
        }

        private static string B64(string s) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(s));

        private static Key Make(string alg)
        {
            var k = new Key { Alg = alg };
            string Set(string body) => "{\"keys\":[{" + body + ",\"kid\":\"" + Kid + "\",\"use\":\"sig\",\"alg\":\"" + alg + "\"}]}";
            switch (alg)
            {
                case "RS256":
                case "PS256":
                {
                    var rsa = RSA.Create(2048);
                    var p = rsa.ExportParameters(false);
                    k.Jwks = Set("\"kty\":\"RSA\",\"n\":\"" + Base64UrlEncoder.Encode(p.Modulus) + "\",\"e\":\"" + Base64UrlEncoder.Encode(p.Exponent) + "\"");
                    k.Sig = d => rsa.SignData(d, HashAlgorithmName.SHA256, alg == "RS256" ? RSASignaturePadding.Pkcs1 : RSASignaturePadding.Pss);
                    break;
                }
                case "ES256":
                case "ES384":
                {
                    var ec = ECDsa.Create(alg == "ES256" ? ECCurve.NamedCurves.nistP256 : ECCurve.NamedCurves.nistP384);
                    var p = ec.ExportParameters(false);
                    k.Jwks = Set("\"kty\":\"EC\",\"crv\":\"" + (alg == "ES256" ? "P-256" : "P-384") + "\",\"x\":\"" + Base64UrlEncoder.Encode(p.Q.X) + "\",\"y\":\"" + Base64UrlEncoder.Encode(p.Q.Y) + "\"");
                    k.Sig = d => ec.SignData(d, alg == "ES256" ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA384);
                    break;
                }
                default:
                {
                    var seed = new byte[32];
                    RandomNumberGenerator.Fill(seed);
                    var pair = Sodium.PublicKeyAuth.GenerateKeyPair(seed);
                    k.Jwks = Set("\"kty\":\"OKP\",\"crv\":\"Ed25519\",\"x\":\"" + Base64UrlEncoder.Encode(pair.PublicKey) + "\"");
                    k.Sig = d => Sodium.PublicKeyAuth.SignDetached(d, pair.PrivateKey);
                    break;
                }
            }
            return k;
        }

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private static string Session(string iss = Issuer) =>
            "{\"iss\":\"" + iss + "\",\"sub\":\"user_1\",\"sid\":\"s1\",\"token_use\":\"session\",\"iat\":" + Now + ",\"exp\":" + (Now + 60) + "}";

        private static HttpClient Served(string jwks) => new HttpClient(MockHttpMessageHandler.Json(jwks));

        private static AtlasBackend Backend(string jwks) =>
            new AtlasBackend(new AtlasBackendOptions { JwksUrl = "https://jwks.test/k", Issuer = Issuer, HttpClient = Served(jwks) });

        [Theory, MemberData(nameof(Algs))]
        public async Task SessionToken_Verifies_ForEveryAlgorithm(string alg)
        {
            var k = Make(alg);
            var r = await Backend(k.Jwks).VerifyAsync(k.Token(Session()));
            Assert.True(r.Ok, alg);
            Assert.Equal("user_1", r.Claims!.Sub);
            Assert.False((await Backend(k.Jwks).VerifyAsync(k.Token(Session("https://evil.test")))).Ok);
            Assert.False((await Backend(k.Jwks).VerifyAsync(k.Token(Session().Replace("\"exp\":" + (Now + 60), "\"exp\":" + (Now - 600))))).Ok);
            var parts = k.Token(Session()).Split('.');
            var tampered = parts[0] + "." + B64(Session().Replace("user_1", "user_2")) + "." + parts[2];
            Assert.False((await Backend(k.Jwks).VerifyAsync(tampered)).Ok);
        }

        [Theory, MemberData(nameof(Algs))]
        public async Task AccessToken_Verifies_ForEveryAlgorithm(string alg)
        {
            var k = Make(alg);
            var claims = "{\"iss\":\"" + Issuer + "\",\"sub\":\"u\",\"aud\":\"api\",\"scope\":\"a\",\"iat\":" + Now + ",\"exp\":" + (Now + 60) + "}";
            var v = new AccessTokenVerifier(new AccessTokenVerifierOptions { Issuer = Issuer, Audiences = new[] { "api" }, JwksUrl = "https://jwks.test/k", HttpClient = Served(k.Jwks) });
            Assert.True((await v.VerifyAsync("Bearer " + k.Token(claims))).Ok, alg);
        }

        [Theory, MemberData(nameof(Algs))]
        public async Task LogoutToken_Verifies_ForEveryAlgorithm(string alg)
        {
            var k = Make(alg);
            var claims = "{\"iss\":\"" + Issuer + "\",\"aud\":\"c1\",\"iat\":" + Now + ",\"jti\":\"j1\",\"sid\":\"s1\",\"events\":{\"http://schemas.openid.net/event/backchannel-logout\":{}}}";
            var v = new LogoutTokenVerifier(new LogoutTokenVerifierOptions { JwksUrl = "https://jwks.test/k", Issuer = Issuer, ClientId = "c1", HttpClient = Served(k.Jwks) });
            var c = await v.VerifyAsync(k.Token(claims, "{\"alg\":\"" + alg + "\",\"typ\":\"logout+jwt\",\"kid\":\"" + Kid + "\"}"));
            Assert.Equal("s1", c.Sid);
        }

        [Theory, MemberData(nameof(Algs))]
        public async Task TransactionToken_Verifies_ForEveryAlgorithm(string alg)
        {
            var k = Make(alg);
            var claims = "{\"iss\":\"" + Issuer + "\",\"sub\":\"u\",\"aud\":\"td\",\"txn\":\"t\",\"scope\":\"a b\",\"req_wl\":\"w\",\"iat\":" + Now + ",\"exp\":" + (Now + 60) + "}";
            var v = new TransactionTokenVerifier(Issuer, "td", jwksJson: k.Jwks);
            var r = await v.VerifyAsync(k.Token(claims, "{\"alg\":\"" + alg + "\",\"typ\":\"txntoken+jwt\",\"kid\":\"" + Kid + "\"}"));
            Assert.True(r.Ok, alg);
            Assert.Equal(new[] { "a", "b" }, r.Claims!.Scopes);
        }

        [Theory, MemberData(nameof(Algs))]
        public async Task StatusList_Verifies_ForEveryAlgorithm(string alg)
        {
            var k = Make(alg);
            var sha = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("sid-x")).Take(8).ToArray();
            var claims = "{\"iss\":\"" + Issuer + "\",\"aud\":\"atlas:session-status-list\",\"token_use\":\"session_status_list\",\"iat\":" + Now + ",\"exp\":" + (Now + 60) +
                ",\"hash_bytes\":8,\"count\":1,\"since\":1,\"h\":\"" + Base64UrlEncoder.Encode(sha) + "\"}";
            var list = await VerifiedStatusList.VerifyAsync(k.Token(claims, "{\"alg\":\"" + alg + "\",\"typ\":\"statuslist+jwt\",\"kid\":\"" + Kid + "\"}"), k.Jwks, Issuer, Now * 1000);
            Assert.True(list.Has("sid-x"));
            Assert.False(list.Has("other"));
        }

        [Theory, MemberData(nameof(Algs))]
        public async Task IdTokenRules_ApplyToEveryAlgorithm(string alg)
        {
            var k = Make(alg);
            var p = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = Issuer, ValidateAudience = true, ValidAudience = "c1", ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.FromSeconds(5) };
            string Id(string aud, long exp) => k.Token("{\"iss\":\"" + Issuer + "\",\"aud\":\"" + aud + "\",\"sub\":\"u\",\"iat\":" + Now + ",\"exp\":" + exp + "}");
            Assert.True((await Jws.ValidateAsync(Id("c1", Now + 60), k.Jwks, p)).IsValid, alg);
            Assert.False((await Jws.ValidateAsync(Id("other", Now + 60), k.Jwks, p)).IsValid);
            Assert.False((await Jws.ValidateAsync(Id("c1", Now - 600), k.Jwks, p)).IsValid);
        }

        // ---- algorithm confusion ----

        [Theory, MemberData(nameof(Algs))]
        public async Task KeyOfTheWrongType_NeverVerifies(string alg)
        {
            var signer = Make(alg);
            var other = new[] { "RS256", "ES256", "ES384", "EdDSA" }.First(a => !SameFamily(a, alg));
            var served = Make(other).Jwks.Replace("\"alg\":\"" + other + "\"", "\"alg\":\"" + alg + "\""); // even a key labelled with the token's alg
            Assert.False((await Backend(served).VerifyAsync(signer.Token(Session()))).Ok);
        }

        private static bool SameFamily(string a, string b) => a == b || (a.StartsWith("RS") || a.StartsWith("PS")) && (b.StartsWith("RS") || b.StartsWith("PS"));

        [Fact]
        public async Task JwkNamingAnotherAlgorithm_IsNotUsed()
        {
            var k = Make("RS256");
            var jwks = k.Jwks.Replace("\"alg\":\"RS256\"", "\"alg\":\"PS256\"");
            Assert.False((await Backend(jwks).VerifyAsync(k.Token(Session()))).Ok);
        }

        [Theory]
        [InlineData("none")]
        [InlineData("HS256")]
        [InlineData("HS384")]
        [InlineData("HS512")]
        public async Task NoneAndHmac_NeverVerify(string alg)
        {
            var k = Make("RS256");
            // The classic confusion: an HMAC token keyed with the public key's bytes.
            var hs = new Key { Alg = alg, Jwks = k.Jwks, Sig = d => alg == "none" ? Array.Empty<byte>() : new HMACSHA256(Encoding.UTF8.GetBytes(k.Jwks)).ComputeHash(d) };
            Assert.False((await Backend(k.Jwks).VerifyAsync(hs.Token(Session()))).Ok);
            Assert.False((await Backend(k.Jwks).VerifyAsync(B64("{\"alg\":\"" + alg + "\"}") + "." + B64(Session()) + ".")).Ok);
        }
    }
}
