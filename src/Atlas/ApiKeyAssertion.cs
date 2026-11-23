using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sodium;

namespace Atlas
{
    /// <summary>
    /// Signs the short-lived assertion a key-pair API key presents instead of a secret: ES256 for P-256
    /// keys (raw <c>r || s</c>), EdDSA for Ed25519 keys.
    /// </summary>
    public static class ApiKeyAssertion
    {
        private static readonly byte[] Ed25519Pkcs8Prefix = { 0x30, 0x2e, 0x02, 0x01, 0x00, 0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70, 0x04, 0x22, 0x04, 0x20 };

        /// <param name="privateKeyPem">The PKCS#8 PEM Atlas returned once at create.</param>
        /// <param name="scope">Optional space-separated scopes narrowing the key's own.</param>
        /// <param name="jti">Null for a random id.</param>
        public static string Sign(string privateKeyPem, string keyId, string audience, int ttlSeconds = 300, string? scope = null, string? jti = null)
        {
            var der = Convert.FromBase64String(string.Concat(privateKeyPem.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("-----"))));
            var ed = der.Length == 48 && der.Take(16).SequenceEqual(Ed25519Pkcs8Prefix);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var claims = new Dictionary<string, object?>
            {
                ["iss"] = keyId, ["aud"] = audience, ["iat"] = now, ["exp"] = now + ttlSeconds,
                ["jti"] = jti ?? B64U(RandomBytes(16)),
            };
            if (scope != null) claims["scope"] = scope;
            var header = new Dictionary<string, object?> { ["alg"] = ed ? "EdDSA" : "ES256", ["kid"] = keyId, ["typ"] = "JWT" };
            var input = B64U(JsonSerializer.SerializeToUtf8Bytes(header)) + "." + B64U(JsonSerializer.SerializeToUtf8Bytes(claims));
            var data = Encoding.ASCII.GetBytes(input);
            byte[] sig;
            if (ed)
            {
                sig = PublicKeyAuth.SignDetached(data, PublicKeyAuth.GenerateKeyPair(der.Skip(16).ToArray()).PrivateKey);
            }
            else
            {
                using var key = ECDsa.Create();
                key!.ImportPkcs8PrivateKey(der, out _);
                if (key.KeySize != 256) throw new ArgumentException("The key must be a P-256 or Ed25519 PKCS#8 private key.", nameof(privateKeyPem));
                // .NET signs ECDSA as raw r || s (IEEE P1363) by default, which is what a JWS carries.
                sig = key.SignData(data, HashAlgorithmName.SHA256);
            }
            return input + "." + B64U(sig);
        }

        /// <summary>A compact JWS split into its parts.</summary>
        public sealed class Parsed
        {
            public JsonElement Header { get; init; }
            public JsonElement Claims { get; init; }
            public string SigningInput { get; init; } = "";
            public byte[] Signature { get; init; } = Array.Empty<byte>();
        }

        /// <summary>Split a compact JWS; null when it is not three base64url segments of JSON objects.</summary>
        public static Parsed? Parse(string? token)
        {
            var parts = (token ?? "").Split('.');
            if (parts.Length != 3) return null;
            try
            {
                var raw = parts.Select(p => Convert.FromBase64String(p.Replace('-', '+').Replace('_', '/').PadRight((p.Length + 3) / 4 * 4, '='))).ToArray();
                var header = JsonDocument.Parse(raw[0]).RootElement.Clone();
                var claims = JsonDocument.Parse(raw[1]).RootElement.Clone();
                if (header.ValueKind != JsonValueKind.Object || claims.ValueKind != JsonValueKind.Object) return null;
                return new Parsed { Header = header, Claims = claims, SigningInput = parts[0] + "." + parts[1], Signature = raw[2] };
            }
            catch (Exception e) when (e is FormatException || e is JsonException) { return null; }
        }

        private static readonly byte[] Ed25519SpkiPrefix = { 0x30, 0x2a, 0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70, 0x03, 0x21, 0x00 };

        /// <summary>Check the signature with an SPKI PEM public key; <paramref name="alg"/> must fit the key's type.</summary>
        public static bool VerifySignature(string alg, string signingInput, byte[] signature, string publicKeyPem)
        {
            try
            {
                var der = Convert.FromBase64String(string.Concat(publicKeyPem.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("-----"))));
                var data = Encoding.ASCII.GetBytes(signingInput);
                if (alg == "EdDSA")
                    return der.Length == 44 && der.Take(12).SequenceEqual(Ed25519SpkiPrefix) && signature.Length == 64
                        && PublicKeyAuth.VerifyDetached(signature, data, der.Skip(12).ToArray());
                if (alg != "ES256" || signature.Length != 64) return false;
                using var key = ECDsa.Create();
                key!.ImportSubjectPublicKeyInfo(der, out _);
                return key.KeySize == 256 && key.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
            catch (Exception e) when (e is FormatException || e is CryptographicException || e is ArgumentException) { return false; }
        }

        private static byte[] RandomBytes(int n)
        {
            var b = new byte[n];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(b);
            return b;
        }

        private static string B64U(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
