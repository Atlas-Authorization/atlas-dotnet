using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    // Identity integrations: any service that verifies a user with an Atlas-minted JWT, a preset (chatly, forumulate,
    // cookiemunch, referalli) or a custom one. The claims of an integration token follow
    // https://atlasauth.net/schemas/suite-token-v1.json.

    /// <summary>A receiver's health, in the webhook health vocabulary.</summary>
    public sealed class IntegrationReceiverHealth
    {
        public string Url { get; init; } = "";
        public string? EndpointId { get; init; }
        /// <summary><c>healthy</c>, <c>failing</c>, <c>disabled</c> or <c>unknown</c>.</summary>
        public string State { get; init; } = "";
        public long? FailingSince { get; init; }
        public long? DisabledAt { get; init; }
        public long? LastSuccessAt { get; init; }
        public WebhookLastFailure? LastFailure { get; init; }
        public int ConsecutiveFailures { get; init; }
        [JsonPropertyName("success_rate_24h")] public double? SuccessRate24h { get; init; }
        public List<string>? Events { get; init; }
    }

    /// <summary>The consent-manager block of an integration.</summary>
    public sealed class IntegrationCmp
    {
        public string ScriptUrl { get; init; } = "";
        public string? ScriptId { get; init; }
        public string? SiteId { get; init; }
        public string? BlockingMode { get; init; }
        public List<string>? ExtraOrigins { get; init; }
        public string? SettingsPreset { get; init; }
    }

    /// <summary>The Sign in with Atlas issuer an integration trusts.</summary>
    public sealed class IntegrationAtlasIdIssuer
    {
        public string Issuer { get; init; } = "";
        public string JwksUrl { get; init; } = "";
    }

    /// <summary>The latest token an integration refused.</summary>
    public sealed class IntegrationLastVerifyError
    {
        public string Error { get; init; } = "";
        public string? Audience { get; init; }
        public long At { get; init; }
        public long ReportedAt { get; init; }
    }

    /// <summary>An integration's mint, verify and receiver health.</summary>
    public sealed class IntegrationHealthInfo
    {
        public long? LastMintedAt { get; init; }
        [JsonPropertyName("mints_24h")] public int Mints24h { get; init; }
        [JsonPropertyName("verify_errors_24h")] public int VerifyErrors24h { get; init; }
        public IntegrationLastVerifyError? LastVerifyError { get; init; }
        public IntegrationReceiverHealth? Events { get; init; }
        public IntegrationReceiverHealth? BackchannelLogout { get; init; }
    }

    /// <summary>
    /// An identity integration. <c>Enabled = false, Connected = false</c> when it was never connected. Preset-specific fields
    /// (chatly: channel, template; forumulate: org_id, spaces; cookiemunch: site_id; referalli: org_id) are read from <see cref="Extra"/>.
    /// </summary>
    public sealed class Integration
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        /// <summary>The same value as <see cref="Id"/>.</summary>
        public string? Product { get; init; }
        /// <summary><c>chatly</c>, <c>forumulate</c>, <c>cookiemunch</c>, <c>referalli</c> or null (custom).</summary>
        public string? Preset { get; init; }
        public string? Name { get; init; }
        /// <summary><c>support_chat</c>, <c>docs_community</c>, <c>consent</c>, <c>referrals</c> or <c>other</c>.</summary>
        public string? Role { get; init; }
        public bool Enabled { get; init; }
        public bool Connected { get; init; }
        public string? Audience { get; init; }
        public string? AudienceFormat { get; init; }
        /// <summary><c>audience_hint</c> or null.</summary>
        public string? PrincipalClaim { get; init; }
        /// <summary><c>external_id</c> or null.</summary>
        public string? ExtClaim { get; init; }
        public int? TtlSeconds { get; init; }
        public string? Issuer { get; init; }
        public string? JwksUrl { get; init; }
        public string? TokenSchema { get; init; }
        public string? EventsUrl { get; init; }
        public bool? EventsUrlDefault { get; init; }
        public List<string>? Events { get; init; }
        public string? BackchannelLogoutUrl { get; init; }
        public bool? BackchannelLogoutUrlDefault { get; init; }
        public string? VerifyErrorReportUrl { get; init; }
        public IntegrationCmp? Cmp { get; init; }
        public bool? CmpDefault { get; init; }
        public bool? AtlasIdUsers { get; init; }
        public IntegrationAtlasIdIssuer? AtlasIdIssuer { get; init; }
        /// <summary>The OAuth client the integration signs users in with, when it has one.</summary>
        public string? OauthClientId { get; init; }
        public string? BackchannelLogoutSource { get; init; }
        public string? BackchannelLogoutClientId { get; init; }
        public string? ExternalRef { get; init; }
        public long? EnabledAt { get; init; }
        public long? UpdatedAt { get; init; }
        /// <summary>Present on a <c>?dry_run=true</c> answer.</summary>
        public bool? DryRun { get; init; }
        public IntegrationHealthInfo? Health { get; init; }

        /// <summary>Every field the SDK does not model, including the preset-specific ones.</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    /// <summary>A Forumulate space in an integration body.</summary>
    public sealed class IntegrationSpace
    {
        public string Id { get; set; } = "";
        public string? RedeemUrl { get; set; }
    }

    /// <summary>The <c>cmp</c> block of <see cref="SetIntegrationBody"/>.</summary>
    public sealed class IntegrationCmpBody
    {
        public string ScriptUrl { get; set; } = "";
        public string? ScriptId { get; set; }
        public string? SiteId { get; set; }
        public string? BlockingMode { get; set; }
        public List<string>? ExtraOrigins { get; set; }
        /// <summary><c>cookiemunch</c>, <c>cookiebot</c>, <c>onetrust</c> or <c>custom</c>.</summary>
        public string? SettingsPreset { get; set; }
    }

    /// <summary>
    /// The body of <c>PUT /v1/instance/integrations/{id}</c>. Use <see cref="Set"/> to send an explicit <c>null</c> for a nullable
    /// field: events_url, backchannel_logout_url, verify_error_report_url, cmp, audience_format, principal_claim, ext_claim, external_ref.
    /// </summary>
    public sealed class SetIntegrationBody
    {
        public bool? Enabled { get; set; }
        /// <summary><c>&lt;preset&gt;:&lt;id&gt;</c> for a preset; any value for a custom one.</summary>
        public string? Audience { get; set; }
        /// <summary>The display name (required for a custom integration).</summary>
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? AudienceFormat { get; set; }
        public string? PrincipalClaim { get; set; }
        public string? ExtClaim { get; set; }
        /// <summary>10-120 (default 120).</summary>
        public int? TtlSeconds { get; set; }
        public string? EventsUrl { get; set; }
        public string? BackchannelLogoutUrl { get; set; }
        public string? VerifyErrorReportUrl { get; set; }
        public IntegrationCmpBody? Cmp { get; set; }
        public bool? AtlasIdUsers { get; set; }
        public string? ExternalRef { get; set; }
        /// <summary>Chatly channel (preset field).</summary>
        public string? Channel { get; set; }
        /// <summary>Forumulate org (preset field).</summary>
        public string? OrgId { get; set; }
        /// <summary>Forumulate spaces (preset field).</summary>
        public List<IntegrationSpace>? Spaces { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>.</summary>
        public SetIntegrationBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>One event or back-channel logout delivery to an integration's receiver.</summary>
    public sealed class IntegrationDelivery
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary><c>event</c> or <c>backchannel_logout</c>.</summary>
        public string Kind { get; init; } = "";
        public string Integration { get; init; } = "";
        public string Id { get; init; } = "";
        public string Url { get; init; } = "";
        public string? EventId { get; init; }
        public string? EventType { get; init; }
        public string? Sid { get; init; }
        public string? TraceId { get; init; }
        /// <summary><c>pending</c>, <c>success</c> or <c>failed</c>.</summary>
        public string Status { get; init; } = "";
        public int AttemptNumber { get; init; }
        public int? HttpStatus { get; init; }
        public string? Error { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>An event receiver's signing secret.</summary>
    public sealed class IntegrationEventsSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Integration { get; init; } = "";
        public string EndpointId { get; init; } = "";
        public string Url { get; init; } = "";
        public string Secret { get; init; } = "";
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
    }

    /// <summary>An identity-integration token; its claims follow <c>/schemas/suite-token-v1.json</c>.</summary>
    public sealed class IntegrationToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Integration { get; init; } = "";
        public string? Product { get; init; }
        public string Jwt { get; init; } = "";
        /// <summary>Seconds until the token expires (at most 120).</summary>
        public int ExpiresIn { get; init; }
        public string? Audience { get; init; }
        public string? OrgId { get; init; }
        public string? SpaceId { get; init; }
        public string? RedeemUrl { get; init; }
        public string? Issuer { get; init; }
        public string? JwksUri { get; init; }
    }

    /// <summary>Verify error codes an integration can report.</summary>
    public static class IntegrationVerifyErrors
    {
        public const string AudMismatch = "aud_mismatch";
        public const string Expired = "expired";
        public const string BadSignature = "bad_signature";
        public const string UnknownKid = "unknown_kid";
        public const string SessionRevoked = "session_revoked";
        public const string InvalidClaims = "invalid_claims";
        public const string Other = "other";
    }

    /// <summary>The body of <c>POST /v1/integrations/verify_errors</c>.</summary>
    public sealed class ReportVerifyErrorBody
    {
        /// <summary>The id the token was minted for.</summary>
        public string Integration { get; set; } = "";
        /// <summary>One of <see cref="IntegrationVerifyErrors"/>.</summary>
        public string Error { get; set; } = "";
        public string? Audience { get; set; }
        /// <summary>Epoch ms (default now).</summary>
        public long? At { get; set; }
        public string? Kid { get; set; }
        public string? Sid { get; set; }
        public string? Detail { get; set; }
    }

    /// <summary>A recorded verify error.</summary>
    public sealed class IntegrationVerifyError
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Integration { get; init; } = "";
        public string? Audience { get; init; }
        public string Error { get; init; } = "";
        public long At { get; init; }
        public string? TraceId { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class IntegrationCardOrg
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Role { get; init; } = "";
    }

    /// <summary>The user in the agent-sidebar card.</summary>
    public sealed class IntegrationCardUser
    {
        public string Id { get; init; } = "";
        public string? Email { get; init; }
        public bool EmailVerified { get; init; }
        public string? Name { get; init; }
        public string? ExternalId { get; init; }
        public bool Banned { get; init; }
        public bool MfaEnabled { get; init; }
        public long? LastSignInAt { get; init; }
        public long CreatedAt { get; init; }
        public List<IntegrationCardOrg> Organizations { get; init; } = new List<IntegrationCardOrg>();
    }

    /// <summary>An action offered on the card.</summary>
    public sealed class IntegrationCardAction
    {
        public string Id { get; init; } = "";
        public string Label { get; init; } = "";
        public bool Confirm { get; init; }
        public bool Available { get; init; }
        public string? RequiresScope { get; init; }
    }

    /// <summary>The agent-sidebar card for a user.</summary>
    public sealed class IntegrationCard
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Integration { get; init; } = "";
        public IntegrationCardUser User { get; init; } = new IntegrationCardUser();
        public int SessionsActive { get; init; }
        public List<IntegrationCardAction> Actions { get; init; } = new List<IntegrationCardAction>();
    }

    /// <summary>The answer to a card action.</summary>
    public sealed class IntegrationCardActionResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Integration { get; init; } = "";
        public string Action { get; init; } = "";
        public bool Ok { get; init; }
        public int SessionsRevoked { get; init; }
    }

    /// <summary>Says which support agent ran a card action (audited).</summary>
    public sealed class IntegrationAgent
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>An issuer an instance trusts (or trusted).</summary>
    public sealed class InstanceIssuer
    {
        public string Issuer { get; init; } = "";
        public string JwksUri { get; init; } = "";
        /// <summary><c>instance</c> or <c>atlas_id</c> (trusted issuers only).</summary>
        public string? Kind { get; init; }
        /// <summary>When a previous issuer was replaced (epoch ms).</summary>
        public long? ReplacedAt { get; init; }
    }

    /// <summary>One integration in the public instance metadata.</summary>
    public sealed class InstanceIntegrationMeta
    {
        public string Audience { get; init; } = "";
        public string? Preset { get; init; }
        public int TtlSeconds { get; init; }
        public List<string>? AdditionalIssuers { get; init; }
    }

    /// <summary>The public metadata of an instance.</summary>
    public sealed class InstanceMetadata
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Issuer { get; init; } = "";
        public string JwksUri { get; init; } = "";
        public string? TokenSchema { get; init; }
        public Dictionary<string, InstanceIntegrationMeta> Integrations { get; init; } = new Dictionary<string, InstanceIntegrationMeta>();
        public List<InstanceIssuer> TrustedIssuers { get; init; } = new List<InstanceIssuer>();
        public List<InstanceIssuer> PreviousIssuers { get; init; } = new List<InstanceIssuer>();
    }

    /// <summary>
    /// <c>/v1/integrations/*</c>: the routes an integrated service calls with the instance secret key (or the partner link the
    /// instance owner granted it).
    /// </summary>
    public sealed class IntegrationsResource : ResourceBase
    {
        public IntegrationsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/integrations/verify_errors</c> — record a token an integrated service refused.</summary>
        public Task<IntegrationVerifyError> ReportVerifyErrorAsync(ReportVerifyErrorBody body, CancellationToken ct = default)
            => Req<IntegrationVerifyError>(HttpVerb.Post, "/v1/integrations/verify_errors", body, cancellationToken: ct);

        /// <summary><c>GET /v1/integrations/{integration}/card</c> — the agent-sidebar card for a user (by token <c>sub</c>, and <c>iss</c> when the user is not on this instance's issuer).</summary>
        public Task<IntegrationCard> CardAsync(string integration, string sub, string? iss = null, CancellationToken ct = default)
            => Req<IntegrationCard>(HttpVerb.Get, $"/v1/integrations/{Enc(integration)}/card", query: Q(("sub", sub), ("iss", iss)), cancellationToken: ct);

        /// <summary><c>POST /v1/integrations/{integration}/card/actions/revoke_sessions</c> — sign the user out everywhere, audited with the agent.</summary>
        public Task<IntegrationCardActionResult> RevokeSessionsAsync(string integration, string sub, string? iss = null, IntegrationAgent? agent = null, CancellationToken ct = default)
            => Req<IntegrationCardActionResult>(HttpVerb.Post, $"/v1/integrations/{Enc(integration)}/card/actions/revoke_sessions",
                Body(("sub", sub), ("iss", iss), ("agent", agent)), cancellationToken: ct);

        /// <summary><c>GET /v1/public/instances/{id}</c> — the public metadata of an instance (no key needed).</summary>
        public Task<InstanceMetadata> InstanceMetadataAsync(string instanceId, CancellationToken ct = default)
            => Req<InstanceMetadata>(HttpVerb.Get, $"/v1/public/instances/{Enc(instanceId)}", cancellationToken: ct);
    }
}
