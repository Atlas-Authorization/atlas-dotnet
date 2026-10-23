using System;
using System.Security.Cryptography;
using System.Text;

namespace Atlas
{
    /// <summary>
    /// Vercel BotID for Atlas: the signing half that runs next to your BotID check. BotID's verdict
    /// (<c>checkBotId()</c>) only exists inside a Vercel-hosted request, so that route turns it into a short signed
    /// attestation; the browser sends it to Atlas as <c>captcha_token</c>, and Atlas checks the signature, freshness
    /// and <c>is_bot == false</c>, and spends it (each attestation passes once). The same secret is stored in Atlas
    /// for the <c>vercel_botid</c> captcha provider.
    /// <para>Format (version 1): <c>atlas_botid.v1.&lt;payload&gt;.&lt;sig&gt;</c> — <c>payload</c> is base64url JSON
    /// <c>{v: 1, is_bot, verified_bot, iat, exp, jti}</c>; <c>sig</c> is base64url HMAC-SHA256 over
    /// <c>atlas_botid.v1.&lt;payload&gt;</c> keyed by the shared secret.</para>
    /// </summary>
    public static class BotId
    {
        public const string Prefix = "atlas_botid.v1.";
        public const int MaxTtlSeconds = 300;
        public const int SecretMinLength = 32;

        /// <summary>
        /// Sign a BotID verdict for Atlas. Throws when the secret is shorter than 32 characters. A bot verdict is still
        /// signed — Atlas refuses it. <paramref name="ttlSeconds"/> is clamped to 1–300.
        /// </summary>
        public static string CreateAttestation(string secret, bool isBot, bool isVerifiedBot = false, int ttlSeconds = 60, long? nowSeconds = null)
        {
            if (secret == null || secret.Length < SecretMinLength)
                throw new ArgumentException($"The BotID attestation secret must be at least {SecretMinLength} characters.", nameof(secret));
            var ttl = Math.Max(1, Math.Min(ttlSeconds, MaxTtlSeconds));
            var iat = nowSeconds ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var jti = new byte[18];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(jti);
            var json = "{\"v\":1,\"is_bot\":" + (isBot ? "true" : "false") + ",\"verified_bot\":" + (isVerifiedBot ? "true" : "false")
                + ",\"iat\":" + iat + ",\"exp\":" + (iat + ttl) + ",\"jti\":\"" + B64u(jti) + "\"}";
            var payload = B64u(Encoding.UTF8.GetBytes(json));
            using var mac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var sig = mac.ComputeHash(Encoding.UTF8.GetBytes(Prefix + payload));
            return Prefix + payload + "." + B64u(sig);
        }

        private static string B64u(byte[] raw) => Convert.ToBase64String(raw).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
