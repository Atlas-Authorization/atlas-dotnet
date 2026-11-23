using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas;
using Sodium;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Key-pair API key assertions: header, claims, and a signature that verifies with the public key.</summary>
    public class ApiKeyAssertionTests
    {
        private static byte[] Dec(string s) => Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/').PadRight((s.Length + 3) / 4 * 4, '='));

        private static string Pem(byte[] der) => "-----BEGIN PRIVATE KEY-----\n" + Convert.ToBase64String(der) + "\n-----END PRIVATE KEY-----\n";

        [Fact]
        public void Es256IsRawRsAndVerifies()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256)!;
            var p = ApiKeyAssertion.Sign(Pem(key.ExportPkcs8PrivateKey()), "key_1", "ins_1", scope: "read", jti: "j1").Split('.');
            var h = JsonDocument.Parse(Dec(p[0])).RootElement;
            var c = JsonDocument.Parse(Dec(p[1])).RootElement;
            Assert.Equal("ES256", h.GetProperty("alg").GetString());
            Assert.Equal("key_1", h.GetProperty("kid").GetString());
            Assert.Equal("JWT", h.GetProperty("typ").GetString());
            Assert.Equal("key_1", c.GetProperty("iss").GetString());
            Assert.Equal("ins_1", c.GetProperty("aud").GetString());
            Assert.Equal("read", c.GetProperty("scope").GetString());
            Assert.Equal("j1", c.GetProperty("jti").GetString());
            Assert.Equal(300, c.GetProperty("exp").GetInt64() - c.GetProperty("iat").GetInt64());
            var sig = Dec(p[2]);
            Assert.Equal(64, sig.Length);
            Assert.True(key.VerifyData(Encoding.ASCII.GetBytes(p[0] + "." + p[1]), sig, HashAlgorithmName.SHA256));
        }

        [Fact]
        public void EdDsaVerifies()
        {
            var seed = new byte[32];
            RandomNumberGenerator.Fill(seed);
            var pair = PublicKeyAuth.GenerateKeyPair(seed);
            var prefix = new byte[] { 0x30, 0x2e, 0x02, 0x01, 0x00, 0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70, 0x04, 0x22, 0x04, 0x20 };
            var p = ApiKeyAssertion.Sign(Pem(prefix.Concat(seed).ToArray()), "key_2", "ins_1", ttlSeconds: 60).Split('.');
            Assert.Equal("EdDSA", JsonDocument.Parse(Dec(p[0])).RootElement.GetProperty("alg").GetString());
            var c = JsonDocument.Parse(Dec(p[1])).RootElement;
            Assert.Equal(60, c.GetProperty("exp").GetInt64() - c.GetProperty("iat").GetInt64());
            Assert.True(PublicKeyAuth.VerifyDetached(Dec(p[2]), Encoding.ASCII.GetBytes(p[0] + "." + p[1]), pair.PublicKey));
        }
    }
}
