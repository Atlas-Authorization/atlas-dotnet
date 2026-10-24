using System;
using System.Text;

namespace Atlas
{
    /// <summary>A key pair for sealed secret delivery.</summary>
    public sealed class SealKeyPair
    {
        public SealKeyPair(byte[] publicKey, byte[] secretKey)
        {
            PublicKey = publicKey;
            SecretKey = secretKey;
        }

        public byte[] PublicKey { get; }

        /// <summary>Keep this private.</summary>
        public byte[] SecretKey { get; }

        /// <summary>The value for <see cref="AtlasClientOptions.SecretRecipient"/>: <c>x25519:&lt;base64 public key&gt;</c>.</summary>
        public string Recipient => "x25519:" + Convert.ToBase64String(PublicKey);
    }

    /// <summary>Thrown when a sealed box does not open: it is too short, was sealed to another key, or was altered.</summary>
    public sealed class SealedBoxException : Exception
    {
        public SealedBoxException() : base("atlas: sealed box does not open") { }
    }

    /// <summary>
    /// Sealed secret delivery. With <see cref="AtlasClientOptions.SecretRecipient"/> set to <c>x25519:&lt;public key&gt;</c>, every
    /// shown-once secret in a response arrives as a base64 libsodium sealed box (<c>sealed_secret</c>,
    /// <c>sealed_client_secret</c>, …) instead of plaintext. The cryptography is libsodium's
    /// <c>crypto_box_seal</c> / <c>crypto_box_seal_open</c> (through the vetted Sodium.Core binding); nothing is hand-rolled.
    /// <code>
    /// var pair = SealedBox.GenerateKeyPair();
    /// using var atlas = new AtlasClient(new AtlasClientOptions { SecretKey = "...", SecretRecipient = pair.Recipient });
    /// var created = await atlas.SecretKeys.CreateAsync(...);
    /// var secret = SealedBox.OpenSealedSecret(created.SealedSecret!, pair);
    /// </code>
    /// </summary>
    public static class SealedBox
    {
        /// <summary>Make a key pair for sealed secret delivery; pass <see cref="SealKeyPair.Recipient"/> to the client and keep the secret key private.</summary>
        public static SealKeyPair GenerateKeyPair()
        {
            var pair = Sodium.PublicKeyBox.GenerateKeyPair();
            return new SealKeyPair(pair.PublicKey, pair.PrivateKey);
        }

        /// <summary>Open a <c>sealed_*</c> field (base64 sealed box) from an Atlas response with the key pair whose <see cref="SealKeyPair.Recipient"/> the client was built with.</summary>
        /// <exception cref="SealedBoxException">The box is malformed, was sealed to another key, or was altered.</exception>
        public static string OpenSealedSecret(string sealedBase64, SealKeyPair pair)
        {
            if (sealedBase64 == null) throw new ArgumentNullException(nameof(sealedBase64));
            if (pair == null) throw new ArgumentNullException(nameof(pair));
            byte[] sealedBytes;
            try { sealedBytes = Convert.FromBase64String(sealedBase64); }
            catch (FormatException) { throw new SealedBoxException(); }
            return Encoding.UTF8.GetString(Open(sealedBytes, pair.PublicKey, pair.SecretKey));
        }

        /// <summary>Seal <paramref name="message"/> to a recipient public key (<c>crypto_box_seal</c>).</summary>
        public static byte[] Seal(byte[] message, byte[] recipientPublicKey)
            => Sodium.SealedPublicKeyBox.Create(message, recipientPublicKey);

        /// <summary>Open a sealed box addressed to <paramref name="publicKey"/> (<c>crypto_box_seal_open</c>).</summary>
        public static byte[] Open(byte[] sealedBox, byte[] publicKey, byte[] secretKey)
        {
            if (sealedBox == null || sealedBox.Length < 48) throw new SealedBoxException();
            try { return Sodium.SealedPublicKeyBox.Open(sealedBox, secretKey, publicKey); }
            catch (Exception e) when (!(e is SealedBoxException)) { throw new SealedBoxException(); }
        }
    }
}
