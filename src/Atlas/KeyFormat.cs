using System;

namespace Atlas
{
    /// <summary>What a credential is.</summary>
    public enum KeyKind
    {
        /// <summary>An instance secret key (<c>sk</c>).</summary>
        Secret,
        /// <summary>A platform key (<c>psk</c>).</summary>
        Platform,
        /// <summary>A publishable key (<c>pk</c>; not a secret, no suite form).</summary>
        Publishable,
        /// <summary>A deploy key (<c>dsk</c>).</summary>
        Deploy,
        /// <summary>A webhook signing secret (<c>whsec</c>).</summary>
        WebhookSecret,
        /// <summary>A partner-link secret (<c>pl</c>).</summary>
        PartnerLink,
        /// <summary>A one-hour access token from an operator token exchange (<c>at</c>).</summary>
        AccessToken,
    }

    /// <summary>What <see cref="AtlasKeys.TryParse"/> learned from a credential's prefix.</summary>
    public sealed class ParsedKey
    {
        public ParsedKey(KeyKind kind, string? environment, bool suite)
        {
            Kind = kind;
            Environment = environment;
            Suite = suite;
        }

        /// <summary>What the credential is.</summary>
        public KeyKind Kind { get; }

        /// <summary><c>live</c>, <c>test</c> or null when the key carries none.</summary>
        public string? Environment { get; }

        /// <summary>True for the <c>atlas_&lt;env&gt;_&lt;kind&gt;_…</c> format, false for the legacy one.</summary>
        public bool Suite { get; }
    }

    /// <summary>
    /// Atlas keys come in two formats. The suite format is <c>atlas_&lt;live|test&gt;_&lt;kind&gt;_&lt;random&gt;&lt;checksum&gt;</c>
    /// (sk, psk, dsk, whsec, pl, at); the legacy format is <c>sk_live_…</c>, <c>sk_test_…</c>, <c>pk_…</c>, <c>psk_…</c>,
    /// <c>whsec_…</c> and <c>dsk_…</c>. Any prefix check in your code must accept both, which is what these helpers do.
    /// Prefix checks only: they do not validate a checksum or contact Atlas.
    /// </summary>
    public static class AtlasKeys
    {
        private const string SuitePrefix = "atlas_";

        private static bool IsEnv(string s) => s == "live" || s == "test";

        private static bool TryKind(string s, bool allowPublishable, out KeyKind kind)
        {
            switch (s)
            {
                case "sk": kind = KeyKind.Secret; return true;
                case "psk": kind = KeyKind.Platform; return true;
                case "dsk": kind = KeyKind.Deploy; return true;
                case "whsec": kind = KeyKind.WebhookSecret; return true;
                case "pl": kind = KeyKind.PartnerLink; return true;
                case "at": kind = KeyKind.AccessToken; return true;
                case "pk" when allowPublishable: kind = KeyKind.Publishable; return true;
                default: kind = KeyKind.Secret; return false;
            }
        }

        /// <summary>
        /// Classify a credential by its prefix, accepting the suite format (<c>atlas_live_sk_…</c>) and the legacy one
        /// (<c>sk_live_…</c>, <c>pk_test_…</c>, <c>psk_…</c>, <c>whsec_…</c>). Returns false for anything else.
        /// </summary>
        public static bool TryParse(string? key, out ParsedKey? parsed)
        {
            parsed = null;
            if (string.IsNullOrEmpty(key)) return false;
            var k = key!;
            if (k.StartsWith(SuitePrefix, StringComparison.Ordinal))
            {
                var parts = k.Substring(SuitePrefix.Length).Split(new[] { '_' }, 3);
                if (parts.Length == 3 && IsEnv(parts[0]) && TryKind(parts[1], false, out var suiteKind))
                {
                    parsed = new ParsedKey(suiteKind, parts[0], true);
                    return true;
                }
                return false;
            }
            var legacy = k.Split(new[] { '_' }, 3);
            if (legacy.Length < 2 || !TryKind(legacy[0], true, out var kind)) return false;
            switch (kind)
            {
                case KeyKind.Secret:
                case KeyKind.Publishable:
                    if (legacy.Length == 3 && IsEnv(legacy[1]))
                    {
                        parsed = new ParsedKey(kind, legacy[1], false);
                        return true;
                    }
                    return false;
                case KeyKind.Platform:
                case KeyKind.Deploy:
                case KeyKind.WebhookSecret:
                    parsed = new ParsedKey(kind, legacy.Length == 3 && IsEnv(legacy[1]) ? legacy[1] : null, false);
                    return true;
                default:
                    return false;
            }
        }

        private static bool Is(string? key, KeyKind kind) => TryParse(key, out var p) && p!.Kind == kind;

        /// <summary>True for an instance secret key in either format (<c>atlas_live_sk_…</c> or <c>sk_live_…</c>).</summary>
        public static bool IsSecretKey(string? key) => Is(key, KeyKind.Secret);

        /// <summary>True for a platform key in either format (<c>atlas_live_psk_…</c> or <c>psk_…</c>).</summary>
        public static bool IsPlatformKey(string? key) => Is(key, KeyKind.Platform);

        /// <summary>True for a publishable key (<c>pk_live_…</c> / <c>pk_test_…</c>).</summary>
        public static bool IsPublishableKey(string? key) => Is(key, KeyKind.Publishable);

        /// <summary>True for a webhook signing secret in either format. Verification is unchanged: the whole string is the HMAC key.</summary>
        public static bool IsWebhookSecret(string? key) => Is(key, KeyKind.WebhookSecret);

        /// <summary>True for a partner-link secret (<c>atlas_live_pl_…</c>).</summary>
        public static bool IsPartnerLinkSecret(string? key) => Is(key, KeyKind.PartnerLink);

        /// <summary>True for a one-hour Atlas access token from an operator token exchange (<c>atlas_live_at_…</c>).</summary>
        public static bool IsAccessToken(string? key) => Is(key, KeyKind.AccessToken);
    }
}
