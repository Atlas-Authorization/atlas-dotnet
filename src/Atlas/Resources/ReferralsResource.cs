using System.Text.Json;
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
        /// <summary>Re-keys a customer whose external id changed after sign-up.</summary>
        public bool Alias { get; init; }
        /// <summary>Takes user lifecycle updates (email, name, suspension).</summary>
        public bool User { get; init; }
        /// <summary>Answers a ping for the connection test (org, program, clock).</summary>
        public bool Ping { get; init; }
        /// <summary>Sends referral changes made after sign-up to Atlas (two-way sync).</summary>
        public bool Inbound { get; init; }
    }

    /// <summary>A referral provider Atlas supports.</summary>
    public sealed class ReferralProvider
    {
        /// <summary><c>webhook</c> (the Atlas referral protocol), <c>referalli</c> or another vendor adapter id.</summary>
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public ReferralProviderCapabilities Capabilities { get; init; } = new ReferralProviderCapabilities();
        /// <summary>What must be set before <c>enabled</c> can be true: endpoint_url, signing_secret, api_key, connection_id, tracking_token.</summary>
        public List<string> Needs { get; init; } = new List<string>();
        public string? DefaultContextCookie { get; init; }
        public string? DocsUrl { get; init; }
        /// <summary>Every cookie the vendor has written, in the order Atlas tries them.</summary>
        public List<string>? DefaultContextCookies { get; init; }
        /// <summary>Retired spellings of <see cref="Id"/> that are still accepted on write (for example <c>referally</c> for <c>referalli</c>).</summary>
        public List<string>? Aliases { get; init; }
    }

    /// <summary>Referral provider ids. The built-in adapter was renamed from <c>referally</c> to <c>referalli</c>; the API accepts both on write.</summary>
    public static class ReferralProviders
    {
        public const string Webhook = "webhook";
        public const string Referalli = "referalli";
        public const string ReferalliLegacy = "referally";
    }

    /// <summary>A one-click (OAuth) install a deployment offers for a referral platform.</summary>
    public sealed class ReferralOneClick
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public bool Available { get; init; }
        /// <summary>What the connection is saved as: <c>referalli</c> or <c>webhook</c> (the generic protocol).</summary>
        public string? Provider { get; init; }
        public string? Scope { get; init; }
        public string? RedirectUri { get; init; }
    }

    /// <summary>The answer to <c>POST /v1/instance/referrals/{preset}/install</c>.</summary>
    public sealed class ReferralInstall
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Preset { get; init; } = "";
        public string? Provider { get; init; }
        /// <summary>Where to send the merchant to pick an organization and program.</summary>
        public string AuthorizeUrl { get; init; } = "";
        public string? RedirectUri { get; init; }
        /// <summary>Epoch ms; the link lasts 15 minutes.</summary>
        public long ExpiresAt { get; init; }
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

    /// <summary>What a referral platform answered to a ping (the connection test, where supported).</summary>
    public sealed class ReferralPing
    {
        public bool Ok { get; init; }
        /// <summary>Epoch ms.</summary>
        public long? At { get; init; }
        public int? HttpStatus { get; init; }
        /// <summary>e.g. signature_rejected, connection_paused, timeout, network_error, ping_not_ok.</summary>
        public string? ErrorCode { get; init; }
        public string? Error { get; init; }
        public int? LatencyMs { get; init; }
        public string? ConnectionId { get; init; }
        public string? ProgramId { get; init; }
        public string? ProgramName { get; init; }
        public string? OrgName { get; init; }
        /// <summary>The platform connection's own auto-enrol setting.</summary>
        public bool? AutoEnrollUsers { get; init; }
        /// <summary>The platform's clock, as it reported it.</summary>
        public string? ServerTime { get; init; }
        /// <summary>The platform's clock minus Atlas's, in ms. Signed calls fail past 5 minutes.</summary>
        public long? ClockSkewMs { get; init; }
        /// <summary>True when the skew is over 60 seconds.</summary>
        public bool ClockSkewWarning { get; init; }
        /// <summary>What the platform said it can do.</summary>
        public ReferralProviderCapabilities? AdvertisedCapabilities { get; init; }
    }

    /// <summary>Delivery is blocked on something only a person can fix. Show it as a banner.</summary>
    public sealed class ReferralAlert
    {
        /// <summary><c>signature_rejected</c> (the platform answered 401: a rotated secret or clock skew) or <c>connection_paused</c>.</summary>
        public string Code { get; init; } = "";
        public string Message { get; init; } = "";
        /// <summary>Epoch ms.</summary>
        public long At { get; init; }
        public string? CallId { get; init; }
    }

    /// <summary>The connection's health.</summary>
    public sealed class ReferralConnectionStatus
    {
        public ReferralLastTest? LastTest { get; init; }
        /// <summary>The last ping, when the provider answers one.</summary>
        public ReferralPing? LastPing { get; init; }
        /// <summary>Set while delivery is blocked on something only a person can fix.</summary>
        public ReferralAlert? Alert { get; init; }
        /// <summary>Calls that failed permanently in the last 7 days.</summary>
        [JsonPropertyName("failed_calls_7d")] public int FailedCalls7d { get; init; }
        /// <summary>Calls waiting for a retry.</summary>
        public int RetryingCalls { get; init; }
        public long? LastSuccessAt { get; init; }
    }

    /// <summary>The latest event a platform sent to the inbound URL.</summary>
    public sealed class ReferralInboundEvent
    {
        /// <summary>When Atlas received it (epoch ms).</summary>
        public long At { get; init; }
        public string Type { get; init; } = "";
        /// <summary><c>applied</c>, <c>unchanged</c>, <c>stale</c>, <c>unknown_customer</c>, <c>other_program</c> or <c>ignored</c>.</summary>
        public string Outcome { get; init; } = "";
    }

    /// <summary>Two-way sync: where the platform sends referral changes made after sign-up, and the latest one received.</summary>
    public sealed class ReferralInboundStatus
    {
        /// <summary>Give this URL to the platform; null when the provider sends no events.</summary>
        public string? Url { get; init; }
        /// <summary>Whether an inbound secret is stored (the value is never returned).</summary>
        public bool SecretSet { get; init; }
        /// <summary>The platform's id for Atlas's event subscription, kept for reference.</summary>
        public string? EndpointId { get; init; }
        /// <summary>The latest event received, or null.</summary>
        public ReferralInboundEvent? LastEvent { get; init; }
    }

    /// <summary>One entry of <see cref="ReferralSettings.Warnings"/>.</summary>
    public sealed class ReferralSettingsWarning
    {
        /// <summary><c>renamed</c> or <c>defaulted</c>.</summary>
        public string Code { get; init; } = "";
        public string? Field { get; init; }
        public JsonElement? From { get; init; }
        public JsonElement? To { get; init; }
        public string Message { get; init; } = "";
    }

    /// <summary><c>GET /v1/instance/referrals</c>. Secrets are never returned, only <c>SigningSecretSet</c> / <c>ApiKeySet</c> / <c>Inbound.SecretSet</c>.</summary>
    public sealed class ReferralSettings
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "referral_settings";
        public bool Enabled { get; init; }
        /// <summary>Update only: what was stored differently from what was sent (<c>renamed</c>: a retired provider name; <c>defaulted</c>: tracking cookies filled in).</summary>
        public List<ReferralSettingsWarning>? Warnings { get; init; }
        /// <summary><c>webhook</c> (the Atlas referral protocol), <c>referalli</c> or another built-in vendor adapter id.</summary>
        public string Provider { get; init; } = "";
        public string? EndpointUrl { get; init; }
        public string? ProgramId { get; init; }
        public string? ConnectionId { get; init; }
        public bool SigningSecretSet { get; init; }
        public bool ApiKeySet { get; init; }
        /// <summary>Query parameters that carry a referral code, in priority order.</summary>
        public List<string> UrlParams { get; init; } = new List<string>();
        public string? ContextCookie { get; init; }
        /// <summary>Every tracking cookie, in the order they are tried.</summary>
        public List<string>? ContextCookies { get; init; }
        /// <summary>Retired spellings of <see cref="Provider"/> still accepted on write.</summary>
        public List<string>? ProviderAliases { get; init; }
        /// <summary>What the chosen provider can do now: its built-in capabilities plus what its last ping advertised.</summary>
        public ReferralProviderCapabilities? Capabilities { get; init; }
        /// <summary>The one-click connect to Referalli (the <c>referalli</c> preset).</summary>
        public ReferralOneClick? OneClick { get; init; }
        /// <summary>Every platform this deployment offers a one-click install for.</summary>
        public List<ReferralOneClick>? OneClickPresets { get; init; }
        public string? ConsentCategory { get; init; }
        public string? ConsentFallbackCategory { get; init; }
        public bool AutoEnrollUsers { get; init; }
        /// <summary>e.g. <c>https://app.example.com/join?ref={code}</c>; builds <c>referral_link</c>.</summary>
        public string? LinkTemplate { get; init; }
        public int TimeoutMs { get; init; }
        public ReferralSecuritySettings Security { get; init; } = new ReferralSecuritySettings();
        /// <summary>Public tracking token (the <c>referalli</c> provider derives the code lookup from it). Not a secret.</summary>
        public string? TrackingToken { get; init; }
        /// <summary>Public, CORS-enabled URL template with <c>{code}</c> for "X invited you" on hosted sign-up.</summary>
        public string? CodeLookupUrl { get; init; }
        /// <summary>Show an optional "Referral code" field on hosted sign-up.</summary>
        public bool SignupCodeField { get; init; }
        /// <summary>
        /// The referral programs whose script or cookie the browser reads, in order (<c>referalli</c>, <c>rewardful</c>,
        /// <c>tolt</c>, <c>promotekit</c>, <c>firstpromoter</c>, <c>partnerstack</c>, <c>dub</c>); the provider's default unless set.
        /// </summary>
        public List<string>? BrowserSources { get; init; }
        /// <summary>Whether <c>BrowserSources</c> is the provider's default.</summary>
        public bool BrowserSourcesDefault { get; init; }
        /// <summary>
        /// Which consent manager decides on the tracking cookie's ids: <c>auto</c> (any common one), one manager
        /// (<c>cookiemunch</c>, <c>onetrust</c>, <c>cookiebot</c>, <c>didomi</c>, <c>usercentrics</c>, <c>osano</c>, <c>cookieyes</c>,
        /// <c>complianz</c>, <c>klaro</c>, <c>tcf</c>, <c>consent_mode</c>, <c>fdconsent</c>, <c>gpc</c>), or <c>app</c> (only the app's own decision).
        /// </summary>
        public string? ConsentProvider { get; init; }
        /// <summary>Forward email, name and suspension changes to the platform (default true).</summary>
        public bool ForwardLifecycle { get; init; }
        /// <summary>Mirror the result into <c>public_metadata.referral</c> (default false).</summary>
        public bool MirrorToPublicMetadata { get; init; }
        /// <summary>Add the user's own code as a <c>referral_code</c> claim to session and ID tokens (default false).</summary>
        public bool IncludeReferralClaims { get; init; }
        /// <summary>What the chosen provider still needs before referrals can be enabled.</summary>
        public List<string>? Missing { get; init; }
        public ReferralConnectionStatus? Connection { get; init; }
        /// <summary>Two-way sync: the URL the platform sends referral changes to, and the latest one received.</summary>
        public ReferralInboundStatus? Inbound { get; init; }
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
        /// <summary>
        /// Write-only, 16-512 characters: verifies the platform's events to <c>inbound.url</c> (two-way sync).
        /// Optional for the Atlas referral protocol (the signing secret is used when unset).
        /// </summary>
        public string? InboundSecret { get; set; }
        /// <summary>The platform's id for Atlas's event subscription, kept for reference.</summary>
        public string? InboundEndpointId { get; set; }
        public List<string>? UrlParams { get; set; }
        public string? ContextCookie { get; set; }
        /// <summary>A list of up to 5 cookie names tried in order; it wins over <see cref="ContextCookie"/>.</summary>
        public List<string>? ContextCookies { get; set; }
        public string? ConsentCategory { get; set; }
        public string? ConsentFallbackCategory { get; set; }
        public bool? AutoEnrollUsers { get; set; }
        /// <summary>An https URL containing <c>{code}</c>.</summary>
        public string? LinkTemplate { get; set; }
        /// <summary>100-5000.</summary>
        public int? TimeoutMs { get; set; }
        public UpdateReferralSecurityBody? Security { get; set; }
        /// <summary>1-128 of <c>A-Z a-z 0-9 _ -</c>.</summary>
        public string? TrackingToken { get; set; }
        /// <summary>An https URL template containing <c>{code}</c>.</summary>
        public string? CodeLookupUrl { get; set; }
        public bool? SignupCodeField { get; set; }
        /// <summary>The programs to read, in order; <c>Clear("browser_sources")</c> goes back to the provider's default.</summary>
        public List<string>? BrowserSources { get; set; }
        /// <summary><c>auto</c>, one consent manager, or <c>app</c>.</summary>
        public string? ConsentProvider { get; set; }
        public bool? ForwardLifecycle { get; set; }
        public bool? MirrorToPublicMetadata { get; set; }
        public bool? IncludeReferralClaims { get; set; }

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
        /// <c>signing_secret</c>, <c>api_key</c>, <c>inbound_secret</c>, <c>inbound_endpoint_id</c>,
        /// <c>context_cookie</c>, <c>consent_fallback_category</c>,
        /// <c>link_template</c>, <c>tracking_token</c>, <c>code_lookup_url</c>, <c>browser_sources</c>): it is sent as <c>null</c>.
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

    /// <summary>
    /// <c>POST /v1/instance/referrals/test</c>: a ping where the provider answers one (<see cref="Method"/> ping), else a
    /// signed probe (ok on 2xx or 404 from the platform).
    /// </summary>
    public sealed class ReferralConnectionTest
    {
        public bool Ok { get; init; }
        public int? HttpStatus { get; init; }
        public int? LatencyMs { get; init; }
        public ReferralTestError? Error { get; init; }
        /// <summary><c>ping</c> or <c>probe</c>.</summary>
        public string? Method { get; init; }
        /// <summary>The ping answer, when the provider supports ping.</summary>
        public ReferralPing? Ping { get; init; }
        /// <summary>e.g. a clock more than 60 seconds off.</summary>
        public List<string>? Warnings { get; init; }
    }

    /// <summary>The matched referral in a <see cref="ReferralSimulateResult"/>.</summary>
    public sealed class ReferralSimulationMatch
    {
        public string? PartnerId { get; init; }
        public string? PartnerExternalRef { get; init; }
        public string? MatchedVia { get; init; }
        public string? Status { get; init; }
    }

    /// <summary>The made-up user's own partner record, when the platform enrolled them.</summary>
    public sealed class ReferralSimulationPartner
    {
        public string? Id { get; init; }
        public string? Code { get; init; }
        public string? Link { get; init; }
    }

    /// <summary>The platform's answer to a simulation, normalised.</summary>
    public sealed class ReferralSimulationResult
    {
        public ReferralSimulationMatch? Referral { get; init; }
        public ReferralSimulationPartner? Partner { get; init; }
        public List<string> Notes { get; init; } = new List<string>();
    }

    /// <summary><c>POST /v1/instance/referrals/simulate</c>: a sign-up call for a made-up user. No user is created.</summary>
    public sealed class ReferralSimulateResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "referral_simulation";
        public bool Ok { get; init; }
        /// <summary>The logged call, in the delivery log.</summary>
        public string? CallId { get; init; }
        /// <summary><c>atlas_test_&lt;random&gt;</c>.</summary>
        public string CustomerRef { get; init; } = "";
        public int? HttpStatus { get; init; }
        public int? LatencyMs { get; init; }
        /// <summary>The platform's answer, normalised; null when the call failed.</summary>
        public ReferralSimulationResult? Result { get; init; }
        /// <summary>The platform's raw answer, personal fields removed.</summary>
        public Dictionary<string, object?>? Response { get; init; }
        public ReferralTestError? Error { get; init; }
    }

    /// <summary><c>POST /v1/instance/referrals/calls/retry_failed</c>.</summary>
    public sealed class ReferralRetryFailedResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "referral_retry";
        /// <summary>How many failed calls were queued to send now.</summary>
        public int Requeued { get; init; }
    }

    /// <summary>Body of <c>POST /v1/instance/referrals/backfill</c>.</summary>
    public sealed class ReferralBackfillBody
    {
        /// <summary>ISO 8601 date or date-time: users created at or after it. Required.</summary>
        public string Since { get; set; } = "";
        /// <summary>customerRef (external id or user id) to referral code, up to 10,000. Wins over <see cref="CodeMetadataPath"/>.</summary>
        public Dictionary<string, string>? Codes { get; set; }
        /// <summary>Where each user keeps the code they arrived with, e.g. <c>public_metadata.referral_code</c>.</summary>
        public string? CodeMetadataPath { get; set; }
    }

    /// <summary>Where a backfill's queued calls stand.</summary>
    public sealed class ReferralBackfillCalls
    {
        public int Pending { get; init; }
        public int Succeeded { get; init; }
        public int Failed { get; init; }
        public int Skipped { get; init; }
        public int Held { get; init; }
    }

    /// <summary>A referral backfill job. Users already reported are skipped.</summary>
    public sealed class ReferralBackfillJob
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "referral_backfill";
        public string Id { get; init; } = "";
        /// <summary><c>running</c> while any queued call is still to be sent, then <c>completed</c>.</summary>
        public string Status { get; init; } = "";
        /// <summary>Epoch ms.</summary>
        public long Since { get; init; }
        public string? CodeMetadataPath { get; init; }
        /// <summary>How many codes were given in <c>codes</c>.</summary>
        public int? CodesGiven { get; init; }
        public int UsersScanned { get; init; }
        /// <summary>Already reported, or no code while auto-enrol is off.</summary>
        public int UsersSkipped { get; init; }
        public int CallsQueued { get; init; }
        /// <summary>More users matched than one backfill takes: run again from <see cref="NextSince"/>.</summary>
        public bool Truncated { get; init; }
        /// <summary>Epoch ms, when truncated.</summary>
        public long? NextSince { get; init; }
        public ReferralBackfillCalls Calls { get; init; } = new ReferralBackfillCalls();
        public long CreatedAt { get; init; }
        public long? FinishedAt { get; init; }
    }

    /// <summary>Filters for <see cref="ReferralsResource.ListCallsAsync(ListReferralCallsQuery, CancellationToken)"/>. Null filters are omitted.</summary>
    public sealed class ListReferralCallsQuery
    {
        /// <summary>1-1000 (default 50; servers before referral attribution v2 take at most 200), newest first.</summary>
        public int? Limit { get; set; }
        public string? Status { get; set; }
        public string? Kind { get; set; }
        public string? UserId { get; set; }
        /// <summary>Only calls whose last failure had this code, e.g. <c>signature_rejected</c>.</summary>
        public string? ErrorCode { get; set; }
        /// <summary>Only the calls of one operation (W3C trace id).</summary>
        public string? Trace { get; set; }
        /// <summary>The id of the last call of the previous page.</summary>
        public string? StartingAfter { get; set; }
    }

    /// <summary>How the platform matched a sign-up's referral, and its notes.</summary>
    public sealed class ReferralCallHighlights
    {
        public string? MatchedVia { get; init; }
        public string? PartnerExternalRef { get; init; }
        public List<string> Notes { get; init; } = new List<string>();
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
        /// <summary>signup, erase, export, partner, signal, alias, user, ping or test.</summary>
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
        /// <summary>Round trip of the last attempt, in ms.</summary>
        public int? LatencyMs { get; init; }
        /// <summary>
        /// Why the last attempt failed, e.g. signature_rejected (401, not retried), connection_paused (not retried),
        /// rate_limited, timeout, network_error, alias_conflict, http_&lt;status&gt;.
        /// </summary>
        public string? ErrorCode { get; init; }
        /// <summary>The platform's last answer, personal fields removed.</summary>
        public Dictionary<string, object?>? Response { get; init; }
        /// <summary>Sign-up calls: how the platform matched the referral and why.</summary>
        public ReferralCallHighlights? Highlights { get; init; }
        /// <summary>The platform's request id for its last answer.</summary>
        public string? RequestId { get; init; }
        /// <summary>The backfill job that queued the call.</summary>
        public string? BatchId { get; init; }
        /// <summary>The W3C trace id of the operation that caused the call.</summary>
        public string? TraceId { get; init; }
    }

    /// <summary>
    /// The referral attribution namespace (<c>/v1/instance/referrals</c>): the instance's connection to a referral
    /// platform, the connection test and the call log. With referrals on, every interactive sign-up is reported to the
    /// platform before <c>user.created</c> is sent; the answer lands on the user (<c>Referral</c>, <c>ReferralCode</c>,
    /// <c>ReferralLink</c>). A late answer arrives as the <c>user.referral_resolved</c> webhook, whose <c>data</c> is the
    /// full <see cref="User"/> (and <c>user.referral_attributed</c> when it attributes the user).
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

        /// <summary>
        /// <c>POST /v1/instance/referrals/test</c> — a ping where the provider answers one (<c>Method</c> ping, with
        /// <c>Ping</c> and <c>Warnings</c> such as clock skew), else a signed probe (ok on 2xx or 404);
        /// <c>signature_rejected</c> on 401/403.
        /// </summary>
        public Task<ReferralConnectionTest> TestAsync(CancellationToken ct = default)
            => Req<ReferralConnectionTest>(HttpVerb.Post, "/v1/instance/referrals/test", cancellationToken: ct);

        /// <summary>
        /// <c>GET /v1/instance/referrals/calls</c> — the call log, newest first. Filters may be null: <paramref name="limit"/>
        /// 1-1000 (default 50), <paramref name="status"/>, <paramref name="kind"/>, <paramref name="userId"/>. For
        /// <c>error_code</c> and <c>starting_after</c> paging, pass a <see cref="ListReferralCallsQuery"/>.
        /// </summary>
        public Task<ListPage<ReferralCall>> ListCallsAsync(int? limit = null, string? status = null, string? kind = null, string? userId = null, CancellationToken ct = default)
            => Req<ListPage<ReferralCall>>(HttpVerb.Get, "/v1/instance/referrals/calls",
                query: Q(("limit", limit), ("status", status), ("kind", kind), ("user_id", userId)), cancellationToken: ct);

        /// <summary><c>POST /v1/instance/referrals/calls/:id/retry</c> — retry a failed, retrying or skipped call now.</summary>
        public Task<ReferralCall> RetryCallAsync(string id, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ReferralCall>(HttpVerb.Post, $"/v1/instance/referrals/calls/{Enc(id)}/retry", idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>
        /// <c>GET /v1/instance/referrals/calls</c> — the call log with every filter, including <c>ErrorCode</c> and
        /// <c>StartingAfter</c> (the id of the last call of the previous page, while <c>HasMore</c>).
        /// </summary>
        public Task<ListPage<ReferralCall>> ListCallsAsync(ListReferralCallsQuery query, CancellationToken ct = default)
            => Req<ListPage<ReferralCall>>(HttpVerb.Get, "/v1/instance/referrals/calls",
                query: Q(("limit", query.Limit), ("status", query.Status), ("kind", query.Kind), ("user_id", query.UserId),
                    ("error_code", query.ErrorCode), ("trace", query.Trace), ("starting_after", query.StartingAfter)), cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/instance/referrals/{preset}/install</c> — start a one-click connection to a referral platform (<c>referalli</c>, or any id in
        /// <c>OneClickPresets</c>). 422 <c>REFERRALS_NOT_CONFIGURED</c> when the deployment does not offer it; connect with a key or signing secret then.
        /// </summary>
        public Task<ReferralInstall> InstallAsync(string preset, string? returnTo = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ReferralInstall>(HttpVerb.Post, $"/v1/instance/referrals/{Enc(preset)}/install", Body(("return_to", returnTo)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/instance/referrals/calls/retry_failed</c> — re-queue every failed call (not tests) to send now.
        /// Filters may be null: <paramref name="kind"/>, <paramref name="errorCode"/> (e.g. signature_rejected after
        /// replacing the secret).
        /// </summary>
        public Task<ReferralRetryFailedResult> RetryFailedAsync(string? kind = null, string? errorCode = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ReferralRetryFailedResult>(HttpVerb.Post, "/v1/instance/referrals/calls/retry_failed", Body(("kind", kind), ("error_code", errorCode)),
                idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/instance/referrals/simulate</c> — send a sign-up call for a made-up user (<c>atlas_test_&lt;random&gt;</c>;
        /// no user is created) with an optional code, and return what the platform answered.
        /// </summary>
        public Task<ReferralSimulateResult> SimulateAsync(string? code = null, CancellationToken ct = default)
            => Req<ReferralSimulateResult>(HttpVerb.Post, "/v1/instance/referrals/simulate", Body(("code", code)), cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/instance/referrals/backfill</c> — queue sign-up calls for existing users created at or after
        /// <c>Since</c>, with their real sign-up time. Users already reported are skipped. Referrals must be on (422
        /// <c>REFERRALS_NOT_CONFIGURED</c> otherwise). Poll <see cref="GetBackfillAsync"/>.
        /// </summary>
        public Task<ReferralBackfillJob> BackfillAsync(ReferralBackfillBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ReferralBackfillJob>(HttpVerb.Post, "/v1/instance/referrals/backfill", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/instance/referrals/backfill/:id</c> — a backfill job and where its calls stand.</summary>
        public Task<ReferralBackfillJob> GetBackfillAsync(string id, CancellationToken ct = default)
            => Req<ReferralBackfillJob>(HttpVerb.Get, $"/v1/instance/referrals/backfill/{Enc(id)}", cancellationToken: ct);

        /// <summary>
        /// <c>GET /v1/users/:id/referral</c> — the stored result for one user (who referred them, how, their own code),
        /// with no call to the platform. 404 when the user was never reported.
        /// </summary>
        public Task<UserReferralResult> GetForUserAsync(string userId, CancellationToken ct = default)
            => Req<UserReferralResult>(HttpVerb.Get, $"/v1/users/{Enc(userId)}/referral", cancellationToken: ct);

        /// <summary>One call of the delivery log, by its id or the platform's request id.</summary>
        public Task<JsonElement> GetCallAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/instance/referrals/calls/{Enc(id)}", cancellationToken: ct);
    }
}
