using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>What a referral provider can do. An operation it lacks is recorded as skipped.</summary>
    public sealed class ReferralProviderCapabilities
    {
        public bool Signup { get; init; }
        public bool Erase { get; init; }
        public bool Export { get; init; }
        public bool Partner { get; init; }
        public bool AutoEnroll { get; init; }
        public bool Signal { get; init; }
    }

    /// <summary>A referral provider Atlas supports.</summary>
    public sealed class ReferralProvider
    {
        /// <summary><c>webhook</c> (the Atlas referral protocol) or a vendor adapter id.</summary>
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public ReferralProviderCapabilities Capabilities { get; init; } = new ReferralProviderCapabilities();
        /// <summary>What must be set before <c>enabled</c> can be true: endpoint_url, signing_secret, api_key, connection_id.</summary>
        public List<string> Needs { get; init; } = new List<string>();
        public string? DefaultContextCookie { get; init; }
        public string? DocsUrl { get; init; }
    }

    /// <summary>Fraud and bot protection for referred sign-ups (<c>security</c> on the referral settings).</summary>
    public sealed class ReferralSecuritySettings
    {
        /// <summary><c>always</c> (default: a sign-up carrying a referral code must pass the bot challenge) or <c>inherit</c>.</summary>
        public string ChallengeReferredSignups { get; init; } = "always";
        /// <summary><c>flag</c> (default), <c>strip</c> or <c>hold</c>.</summary>
        public string OnHighRisk { get; init; } = "flag";
        /// <summary>Hold every referred sign-up until the email is proven.</summary>
        public bool HoldUntilEmailVerified { get; init; }
        /// <summary>Referred sign-ups per code from one IP network or device per hour before a velocity signal (1-1000, default 5).</summary>
        public int MaxReferredSignupsPerIpHour { get; init; }
    }

    /// <summary>A partial update of the referral security settings: only the fields set change.</summary>
    public sealed class UpdateReferralSecurityBody
    {
        public string? ChallengeReferredSignups { get; set; }
        public string? OnHighRisk { get; set; }
        public bool? HoldUntilEmailVerified { get; set; }
        public int? MaxReferredSignupsPerIpHour { get; set; }
    }

    /// <summary>The last connection test.</summary>
    public sealed class ReferralLastTest
    {
        public bool Ok { get; init; }
        /// <summary>Epoch ms.</summary>
        public long At { get; init; }
        public int? HttpStatus { get; init; }
        public string? Error { get; init; }
    }

    /// <summary>The connection's health.</summary>
    public sealed class ReferralConnectionStatus
    {
        public ReferralLastTest? LastTest { get; init; }
        /// <summary>Calls that failed permanently in the last 7 days.</summary>
        [JsonPropertyName("failed_calls_7d")] public int FailedCalls7d { get; init; }
        /// <summary>Calls waiting for a retry.</summary>
        public int RetryingCalls { get; init; }
        public long? LastSuccessAt { get; init; }
    }

    /// <summary><c>GET /v1/instance/referrals</c>. Secrets are never returned, only <c>SigningSecretSet</c> / <c>ApiKeySet</c>.</summary>
    public sealed class ReferralSettings
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "referral_settings";
        public bool Enabled { get; init; }
        /// <summary><c>webhook</c> (the Atlas referral protocol) or a built-in vendor adapter id.</summary>
        public string Provider { get; init; } = "";
        public string? EndpointUrl { get; init; }
        public string? ProgramId { get; init; }
        public string? ConnectionId { get; init; }
        public bool SigningSecretSet { get; init; }
        public bool ApiKeySet { get; init; }
        /// <summary>Query parameters that carry a referral code, in priority order.</summary>
        public List<string> UrlParams { get; init; } = new List<string>();
        public string? ContextCookie { get; init; }
        public string? ConsentCategory { get; init; }
        public string? ConsentFallbackCategory { get; init; }
        public bool AutoEnrollUsers { get; init; }
        /// <summary>e.g. <c>https://app.example.com/join?ref={code}</c>; builds <c>referral_link</c>.</summary>
        public string? LinkTemplate { get; init; }
        public int TimeoutMs { get; init; }
        public ReferralSecuritySettings Security { get; init; } = new ReferralSecuritySettings();
        /// <summary>What the chosen provider still needs before referrals can be enabled.</summary>
        public List<string>? Missing { get; init; }
        public ReferralConnectionStatus? Connection { get; init; }
        public List<ReferralProvider> Providers { get; init; } = new List<ReferralProvider>();
        public long? UpdatedAt { get; init; }
    }

    /// <summary>
    /// Body of <c>PATCH /v1/instance/referrals</c>: only the fields set change. Clear a nullable field with
    /// <see cref="Clear"/> (sent as <c>null</c>). Enabling without what the provider needs is 422
    /// <c>REFERRALS_NOT_CONFIGURED</c>.
    /// </summary>
    public sealed class UpdateReferralSettingsBody
    {
        public bool? Enabled { get; set; }
        public string? Provider { get; set; }
        /// <summary>https only (http only for localhost on a development instance).</summary>
        public string? EndpointUrl { get; set; }
        public string? ProgramId { get; set; }
        public string? ConnectionId { get; set; }
        /// <summary>Write-only, 16-512 characters.</summary>
        public string? SigningSecret { get; set; }
        /// <summary>Write-only vendor API key.</summary>
        public string? ApiKey { get; set; }
        public List<string>? UrlParams { get; set; }
        public string? ContextCookie { get; set; }
        public string? ConsentCategory { get; set; }
        public string? ConsentFallbackCategory { get; set; }
        public bool? AutoEnrollUsers { get; set; }
        /// <summary>An https URL containing <c>{code}</c>.</summary>
        public string? LinkTemplate { get; set; }
        /// <summary>100-5000.</summary>
        public int? TimeoutMs { get; set; }
        public UpdateReferralSecurityBody? Security { get; set; }

        /// <summary>Fields set with <see cref="Set"/> or <see cref="Clear"/>, written as-is.</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name, including one this SDK does not model yet.</summary>
        public UpdateReferralSettingsBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }

        /// <summary>
        /// Clear a nullable field by its wire name (<c>endpoint_url</c>, <c>program_id</c>, <c>connection_id</c>,
        /// <c>signing_secret</c>, <c>api_key</c>, <c>context_cookie</c>, <c>consent_fallback_category</c>,
        /// <c>link_template</c>): it is sent as <c>null</c>.
        /// </summary>
        public UpdateReferralSettingsBody Clear(string key) => Set(key, null);
    }

    /// <summary>Why a connection test failed.</summary>
    public sealed class ReferralTestError
    {
        /// <summary>e.g. <c>signature_rejected</c> (401/403), <c>timeout</c>, <c>network_error</c>, <c>url_blocked</c>, <c>http_500</c>.</summary>
        public string Code { get; init; } = "";
        public string Message { get; init; } = "";
    }

    /// <summary><c>POST /v1/instance/referrals/test</c>: ok on 2xx or 404 from the platform.</summary>
    public sealed class ReferralConnectionTest
    {
        public bool Ok { get; init; }
        public int? HttpStatus { get; init; }
        public int? LatencyMs { get; init; }
        public ReferralTestError? Error { get; init; }
    }

    /// <summary>The risk verdict sent with a sign-up (camelCase on the wire, as on the referral protocol).</summary>
    public sealed class ReferralRisk
    {
        /// <summary>low, medium or high.</summary>
        public string Level { get; init; } = "";
        /// <summary>0-100.</summary>
        public int Score { get; init; }
        /// <summary>e.g. self_referral, ip_velocity, device_velocity, disposable_email, datacenter_ip, captcha_not_passed.</summary>
        public List<string> Signals { get; init; } = new List<string>();
        /// <summary>passed, not_required or not_configured.</summary>
        public string Captcha { get; init; } = "";
        [JsonPropertyName("emailVerified")] public bool EmailVerified { get; init; }
    }

    /// <summary>One call in the referral call log.</summary>
    public sealed class ReferralCall
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "referral_call";
        /// <summary>Also the signed <c>atlas-id</c> header of every attempt.</summary>
        public string Id { get; init; } = "";
        /// <summary>signup, erase, export, partner, signal or test.</summary>
        public string Kind { get; init; } = "";
        /// <summary>pending, succeeded, retrying, failed, held or skipped.</summary>
        public string Status { get; init; } = "";
        /// <summary>Sign-ups: referred, not_referred or stripped.</summary>
        public string? Outcome { get; init; }
        public string CustomerRef { get; init; } = "";
        public string? UserId { get; init; }
        public int? HttpStatus { get; init; }
        public string? LastError { get; init; }
        public int Attempts { get; init; }
        /// <summary>Epoch ms of the next retry, while pending or retrying.</summary>
        public long? NextAttemptAt { get; init; }
        public ReferralRisk? Risk { get; init; }
        /// <summary>What was sent (never secrets).</summary>
        public Dictionary<string, object?>? Payload { get; init; }
        public long CreatedAt { get; init; }
        public long? UpdatedAt { get; init; }
    }

    /// <summary>
    /// The referral attribution namespace (<c>/v1/instance/referrals</c>): the instance's connection to a referral
    /// platform, the connection test and the call log. With referrals on, every interactive sign-up is reported to the
    /// platform before <c>user.created</c> is sent; the answer lands on the user (<c>Referral</c>, <c>ReferralCode</c>,
    /// <c>ReferralLink</c>). A late answer arrives as the <c>user.referral_attributed</c> webhook.
    /// </summary>
    public sealed class ReferralsResource : ResourceBase
    {
        public ReferralsResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>GET /v1/instance/referrals</c> — settings, connection status and supported providers.</summary>
        public Task<ReferralSettings> GetAsync(CancellationToken ct = default)
            => Req<ReferralSettings>(HttpVerb.Get, "/v1/instance/referrals", cancellationToken: ct);

        /// <summary><c>PATCH /v1/instance/referrals</c> — partial update; secrets are write-only.</summary>
        public Task<ReferralSettings> UpdateAsync(UpdateReferralSettingsBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ReferralSettings>(HttpVerb.Patch, "/v1/instance/referrals", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/instance/referrals/test</c> — a signed probe: ok on 2xx or 404; <c>signature_rejected</c> on 401/403.</summary>
        public Task<ReferralConnectionTest> TestAsync(CancellationToken ct = default)
            => Req<ReferralConnectionTest>(HttpVerb.Post, "/v1/instance/referrals/test", cancellationToken: ct);

        /// <summary>
        /// <c>GET /v1/instance/referrals/calls</c> — the call log, newest first. Filters may be null: <paramref name="limit"/>
        /// 1-200 (default 50), <paramref name="status"/>, <paramref name="kind"/>, <paramref name="userId"/>.
        /// </summary>
        public Task<ListPage<ReferralCall>> ListCallsAsync(int? limit = null, string? status = null, string? kind = null, string? userId = null, CancellationToken ct = default)
            => Req<ListPage<ReferralCall>>(HttpVerb.Get, "/v1/instance/referrals/calls",
                query: Q(("limit", limit), ("status", status), ("kind", kind), ("user_id", userId)), cancellationToken: ct);

        /// <summary><c>POST /v1/instance/referrals/calls/:id/retry</c> — retry a failed, retrying or skipped call now.</summary>
        public Task<ReferralCall> RetryCallAsync(string id, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ReferralCall>(HttpVerb.Post, $"/v1/instance/referrals/calls/{Enc(id)}/retry", idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
