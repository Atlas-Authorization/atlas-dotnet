using System.Collections.Generic;

namespace Atlas
{
    /// <summary>Names the Atlas API accepts in configuration and sends in webhooks.</summary>
    public static class AtlasConstants
    {
        /// <summary>
        /// Captcha providers (<c>auth_config.captcha.provider</c>). Captcha is bring-your-own-keys: store the secret
        /// first. <c>vercel_botid</c> is secret-only: set <c>attestationUrl</c> instead of a site key.
        /// </summary>
        public static readonly IReadOnlyList<string> CaptchaProviders = new[]
        {
            "none", "atlas", "hcaptcha", "turnstile", "recaptcha", "recaptcha_v3", "recaptcha_enterprise",
            "funcaptcha", "friendly", "yandex", "geetest", "mcaptcha", "capy", "seon", "awswaf", "perimeterx",
            "akamai", "kasada", "incapsula", "captchafox", "vercel_botid",
        };

        /// <summary><c>auth_config.accountLinkingTrust</c> values.</summary>
        public static readonly IReadOnlyList<string> LinkingTrustLevels = new[] { "always", "verified", "never" };

        /// <summary>Billing providers an instance can sell end-user plans through (bring your own key).</summary>
        public static readonly IReadOnlyList<string> BillingProviders = new[] { "stripe", "polar", "dodo", "creem", "chargebee", "autumn", "commet" };

        /// <summary>Where the token-signing key can live.</summary>
        public static readonly IReadOnlyList<string> SigningKeyProviders = new[] { "local", "aws_kms", "gcp_kms", "azure_key_vault" };

        /// <summary>A session's custom data changed.</summary>
        public const string WebhookEventSessionUpdated = "session.updated";

        /// <summary>A dormant account signed in again.</summary>
        public const string WebhookEventUserReactivated = "user.reactivated";

        /// <summary>The security notice sent when a dormant account signs in again.</summary>
        public const string EmailTemplateAccountReactivated = "account_reactivated";
    }
}
