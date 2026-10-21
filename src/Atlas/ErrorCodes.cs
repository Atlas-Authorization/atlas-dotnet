namespace Atlas
{
    /// <summary>
    /// Stable error codes the API returns in <see cref="AtlasErrorItem.Code"/>. Compare
    /// against <see cref="AtlasException.Code"/> or use <see cref="AtlasException.HasCode"/>.
    /// Not exhaustive: the server may add codes; branch on the ones you handle.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>The request failed validation; <c>Param</c> names the field.</summary>
        public const string ValidationFailed = "VALIDATION_FAILED";
        /// <summary>A strict-mode write sent a body field the endpoint does not accept.</summary>
        public const string UnknownField = "UNKNOWN_FIELD";
        /// <summary>The body set a field that is not writable on this endpoint (<c>param</c> names it).</summary>
        public const string ReadOnlyField = "READ_ONLY_FIELD";
        public const string NotFound = "NOT_FOUND";
        public const string RateLimited = "RATE_LIMITED";
        /// <summary>An Idempotency-Key was reused with different parameters.</summary>
        public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
        public const string NotConfigured = "NOT_CONFIGURED";
        public const string Unauthenticated = "UNAUTHENTICATED";
        /// <summary>The key is unknown, revoked or malformed.</summary>
        public const string InvalidKey = "INVALID_KEY";
        /// <summary>The key is valid but for a different surface (e.g. a platform key on an instance route).</summary>
        public const string WrongKeySurface = "WRONG_KEY_SURFACE";
        public const string Forbidden = "FORBIDDEN";
        /// <summary>The key lacks the scope this route needs.</summary>
        public const string ScopeMissing = "SCOPE_MISSING";
        /// <summary>Email sending is not set up for this instance.</summary>
        public const string EmailDeliveryNotConfigured = "EMAIL_DELIVERY_NOT_CONFIGURED";
        /// <summary>The operation needs an email provider to be configured first.</summary>
        public const string EmailProviderRequired = "EMAIL_PROVIDER_REQUIRED";
        /// <summary>
        /// The email provider cannot be removed while email-dependent features are on and nothing else would deliver
        /// email; the error's <c>meta.features</c> lists them. Delete with <c>force</c> to remove it anyway.
        /// </summary>
        public const string EmailProviderInUse = "EMAIL_PROVIDER_IN_USE";
        /// <summary>
        /// Captcha is bring-your-own-keys: turning it on (or switching provider) needs that provider's site key and
        /// secret stored first.
        /// </summary>
        public const string CaptchaKeysRequired = "CAPTCHA_KEYS_REQUIRED";
        /// <summary>
        /// A captcha key a live flow uses cannot be removed (<c>meta.flows</c>); delete with <c>force</c> to remove it
        /// anyway.
        /// </summary>
        public const string CaptchaInUse = "CAPTCHA_IN_USE";
        /// <summary>The development email sender is not available on a production instance (409).</summary>
        public const string DevelopmentEmailNotAllowed = "DEVELOPMENT_EMAIL_NOT_ALLOWED";
        /// <summary>The development email sender is not available on this deployment (503).</summary>
        public const string DevelopmentEmailUnavailable = "DEVELOPMENT_EMAIL_UNAVAILABLE";
        /// <summary>The development sender's daily or per-recipient hourly cap is spent (429; <c>meta.window</c>, <c>meta.retry_after_seconds</c>).</summary>
        public const string DevelopmentEmailLimitReached = "DEVELOPMENT_EMAIL_LIMIT_REACHED";
        /// <summary>The development sender sends only Atlas's own sign-in emails (422).</summary>
        public const string DevelopmentEmailTemplateNotAllowed = "DEVELOPMENT_EMAIL_TEMPLATE_NOT_ALLOWED";
        /// <summary>A billing plan cannot be changed or removed while subscriptions use it.</summary>
        public const string PlanInUse = "PLAN_IN_USE";
        public const string LastAdmin = "LAST_ADMIN";
        public const string IdentifierExists = "IDENTIFIER_EXISTS";
        public const string IdentifierNotFound = "IDENTIFIER_NOT_FOUND";
        /// <summary>A usage debit would exceed one of the API key's spend caps (the <c>Code</c> of a refused debit).</summary>
        public const string LimitExceeded = "LIMIT_EXCEEDED";
        /// <summary>The session was revoked.</summary>
        public const string SessionRevoked = "SESSION_REVOKED";
        /// <summary>The session timed out from inactivity.</summary>
        public const string SessionIdleExpired = "SESSION_IDLE_EXPIRED";
        /// <summary>The session passed its absolute expiry.</summary>
        public const string SessionExpired = "SESSION_EXPIRED";
    }
}
