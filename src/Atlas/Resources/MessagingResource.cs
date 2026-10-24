using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class MessagingProvider
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "messaging_provider";
        /// <summary>The provider id: pass it as <c>provider_id</c> to managed email or the development sender.</summary>
        public string? Id { get; init; }
        public string Channel { get; init; } = "";
        /// <summary><c>standard</c>, <c>development</c> or <c>managed_domain</c>.</summary>
        public string? Mode { get; init; }
        public string Transport { get; init; } = "";
        public string? FromAddress { get; init; }
        public string Status { get; init; } = "";
        /// <summary>Non-secret config only — secret values are stripped before serialising.</summary>
        public Dictionary<string, object?> Config { get; init; } = new Dictionary<string, object?>();
        /// <summary>Which secret keys are set for this transport, never their values.</summary>
        public Dictionary<string, bool> SecretsSet { get; init; } = new Dictionary<string, bool>();
        public long UpdatedAt { get; init; }
        /// <summary>False when the singleton has never been set (the GET answers 200, no longer 404).</summary>
        public bool? Configured { get; init; }
    }

    public sealed class MessagingProviders
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "messaging_providers";
        public MessagingProvider? Email { get; init; }
        public MessagingProvider? Sms { get; init; }
        /// <summary>The Atlas-managed sending domain, or null.</summary>
        public ManagedEmailSummary? ManagedEmail { get; init; }
        /// <summary>The email provider in development mode, or null.</summary>
        public DevelopmentEmailSummary? DevelopmentEmail { get; init; }
    }

    public sealed class DevelopmentEmailSummary
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "development_email";
        public bool Enabled { get; init; }
        public string? FromAddress { get; init; }
    }

    public sealed class ManagedEmailSummary
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "managed_email";
        public string FromAddress { get; init; } = "";
        public string Domain { get; init; } = "";
        public string Status { get; init; } = "";
        public bool Verified { get; init; }
    }

    /// <summary>A DNS record to publish for the managed sending domain.</summary>
    public sealed class ManagedEmailRecord
    {
        /// <summary>dkim, spf, return_path or other.</summary>
        public string Purpose { get; init; } = "";
        /// <summary>TXT, MX or CNAME.</summary>
        public string Type { get; init; } = "";
        public string Name { get; init; } = "";
        public string Value { get; init; } = "";
        public int? Priority { get; init; }
        public string? Status { get; init; }
    }

    public sealed class ManagedEmailLimits
    {
        public int Hourly { get; init; }
        public int Daily { get; init; }
    }

    public sealed class ManagedEmailUsage
    {
        public int Sent { get; init; }
        public int Bounces { get; init; }
        public int Complaints { get; init; }
        public long? LastBounceAt { get; init; }
        public long? LastComplaintAt { get; init; }
    }

    /// <summary>Atlas-managed email: your own sending domain on Atlas's email account. Email sends only once Verified.</summary>
    public sealed class ManagedEmail
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "managed_email";
        public string Id { get; init; } = "";
        public string FromAddress { get; init; } = "";
        public string Domain { get; init; } = "";
        public string Provider { get; init; } = "";
        /// <summary>The email service the domain lives on (your own account there): <c>resend</c> or <c>brevo</c>.</summary>
        public string? Backend { get; init; }
        /// <summary>Your email provider (mode <c>managed_domain</c>) that holds the API key.</summary>
        public string? ProviderId { get; init; }
        /// <summary>Brevo, on create: <c>registered</c> or <c>not_registered</c>.</summary>
        public string? FeedbackWebhook { get; init; }
        public string? Region { get; init; }
        /// <summary>not_started, pending, verified, failed or temporary_failure.</summary>
        public string Status { get; init; } = "";
        public bool Verified { get; init; }
        public List<ManagedEmailRecord> Records { get; init; } = new List<ManagedEmailRecord>();
        /// <summary>Null: no Atlas sending limits apply (servers before 2026-10 sent hourly and daily).</summary>
        public ManagedEmailLimits? Limits { get; init; }
        public ManagedEmailUsage? Usage { get; init; }
        public string? Reputation { get; init; }
        /// <summary>DNS automation (the token is never returned), or null.</summary>
        public CustomDomainDns? Dns { get; init; }
        public string? FailureReason { get; init; }
        public long? LastCheckedAt { get; init; }
        public long? VerifiedAt { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>Create only, with Dns: the zone and the records Atlas wrote.</summary>
        public System.Text.Json.JsonElement? DnsResult { get; init; }
        public string? NextStep { get; init; }
        /// <summary>False when the singleton has never been set (the GET answers 200, no longer 404).</summary>
        public bool? Configured { get; init; }
    }

    /// <summary>The body of <c>POST /v1/messaging_providers/email/managed</c>.</summary>
    public sealed class ManagedEmailBody
    {
        /// <summary>Bare or "Name &lt;address&gt;"; its domain becomes the sending domain.</summary>
        public string FromAddress { get; set; } = "";
        /// <summary>Your own provider account Atlas sets the domain up on: <c>resend</c> or <c>brevo</c>. With <see cref="Config"/>, unless <see cref="ProviderId"/> is set.</summary>
        public string? Transport { get; set; }
        /// <summary>Your provider API key (write-only).</summary>
        public ManagedEmailProviderKey? Config { get; set; }
        /// <summary>Instead of Transport + Config: your configured Resend or Brevo email provider.</summary>
        public string? ProviderId { get; set; }
        /// <summary>us-east-1, eu-west-1, sa-east-1 or ap-northeast-1 (Resend only).</summary>
        public string? Region { get; set; }
        /// <summary>Let Atlas write the DNS records (the token is write-only).</summary>
        public CustomDomainDnsInput? Dns { get; set; }
    }

    public sealed class ManagedEmailProviderKey
    {
        /// <summary>Your Resend (re_…) or Brevo (xkeysib-…) API key, with domain permissions. Write-only.</summary>
        public string ApiKey { get; set; } = "";
    }

    public sealed class ManagedEmailDeleted
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "managed_email";
        public string Domain { get; init; } = "";
        public bool Deleted { get; init; }
        /// <summary>Whether the email provider that held the key (mode <c>managed_domain</c>) was removed too.</summary>
        public bool? ProviderRemoved { get; init; }
    }

    public sealed class DevelopmentEmailLimits
    {
        public int Daily { get; init; }
        public int PerRecipientHourly { get; init; }
    }

    /// <summary>Your own email provider in development mode: a development instance sends Atlas's sign-in emails without a verified domain.</summary>
    public sealed class DevelopmentEmail
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "development_email";
        public string ProviderId { get; init; } = "";
        public bool Enabled { get; init; }
        /// <summary>Whether mail goes out through it now; always false on a production instance.</summary>
        public bool Active { get; init; }
        public string Transport { get; init; } = "";
        public string? FromAddress { get; init; }
        /// <summary>Added to every subject: <c>"[Atlas test] "</c>.</summary>
        public string SubjectPrefix { get; init; } = "";
        /// <summary>The line added to the end of every message.</summary>
        public string Footer { get; init; } = "";
        /// <summary>The only emails it sends: Atlas's own sign-in templates.</summary>
        public List<string> Templates { get; init; } = new List<string>();
        public DevelopmentEmailLimits? Limits { get; init; }
        /// <summary>Present for onboarding@resend.dev: Resend delivers it only to your Resend account address.</summary>
        public string? Note { get; init; }
        public string? NextStep { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>False when the singleton has never been set (the GET answers 200, no longer 404).</summary>
        public bool? Configured { get; init; }
    }

    /// <summary>The body of <c>POST /v1/messaging_providers/email/development</c>: <c>ProviderId</c>, or <c>Transport</c> + <c>Config</c> exactly as <c>SetEmailAsync</c> takes them.</summary>
    public sealed class DevelopmentEmailBody
    {
        public string? ProviderId { get; set; }
        /// <summary><c>resend</c>, <c>ses</c>, <c>smtp</c>, <c>brevo</c> or <c>mailgun</c>.</summary>
        public string? Transport { get; set; }
        /// <summary>A sender your provider accepts without a verified domain (default for Resend: onboarding@resend.dev).</summary>
        public string? FromAddress { get; set; }
        public Dictionary<string, object?>? Config { get; set; }
    }

    public sealed class DevelopmentEmailDisabled
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "development_email";
        public bool Deleted { get; init; }
        public string ProviderId { get; init; } = "";
        public string Mode { get; init; } = "";
    }

    public sealed class MessagingProviderInput
    {
        /// <summary>The transport, e.g. <c>resend</c>, <c>ses</c>, <c>smtp</c> (email) or <c>twilio</c> (sms).</summary>
        public string Transport { get; set; } = "";
        public string? FromAddress { get; set; }
        /// <summary>Email only: <c>development</c> lets a development instance send without a verified domain (409 on a production instance); null keeps the stored mode.</summary>
        public string? Mode { get; set; }
        /// <summary>Transport config; carries the write-only secret keys on write.</summary>
        public Dictionary<string, object?>? Config { get; set; }
    }

    public sealed class MessagingTestResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "messaging_test";
        public string Channel { get; init; } = "";
        public string SentTo { get; init; } = "";
        public bool Ok { get; init; }
    }

    public sealed class DeletedMessagingProvider
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "messaging_provider";
        public string Channel { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary>
    /// The messaging providers namespace (<c>/v1/messaging_providers</c>) — use your own
    /// email or SMS provider. Prefer the typed <see cref="EmailProviderConfig"/> /
    /// <see cref="SmsProviderConfig"/> overloads: each record carries exactly the
    /// <c>config</c> keys the API accepts for its transport.
    /// </summary>
    public sealed class MessagingResource : ResourceBase
    {
        public MessagingResource(AtlasTransport transport) : base(transport) { }

        /// <summary>The instance's configured email and SMS providers, secrets omitted.</summary>
        public Task<MessagingProviders> GetAsync(CancellationToken ct = default)
            => Req<MessagingProviders>(HttpVerb.Get, "/v1/messaging_providers", cancellationToken: ct);

        /// <summary>
        /// <c>GET /v1/messaging_providers/{channel}</c> — one channel's provider (<c>email</c> or <c>sms</c>). When none is set it answers 200 with
        /// <c>Configured = false</c> (never a 404).
        /// </summary>
        public Task<MessagingProvider> ChannelAsync(string channel, CancellationToken ct = default)
            => Req<MessagingProvider>(HttpVerb.Get, $"/v1/messaging_providers/{Enc(channel)}", cancellationToken: ct);

        /// <summary>
        /// Configure (or update) the email provider with typed, per-transport settings.
        /// On an update of the same transport, null properties keep their stored values
        /// (including stored secrets).
        /// </summary>
        public Task<MessagingProvider> SetEmailAsync(EmailProviderConfig config, string? fromAddress = null, CancellationToken ct = default)
            => SetEmailAsync(new MessagingProviderInput
            {
                Transport = config.Transport,
                FromAddress = fromAddress,
                Config = config.ToConfig(),
            }, ct);

        /// <summary>
        /// Configure (or update) the SMS provider with typed, per-transport settings.
        /// On an update of the same transport, null properties keep their stored values
        /// (including stored secrets).
        /// </summary>
        public Task<MessagingProvider> SetSmsAsync(SmsProviderConfig config, string? fromAddress = null, CancellationToken ct = default)
            => SetSmsAsync(new MessagingProviderInput
            {
                Transport = config.Transport,
                FromAddress = fromAddress,
                Config = config.ToConfig(),
            }, ct);

        /// <summary>Set the email provider from an untyped body. Prefer the <see cref="EmailProviderConfig"/> overload.</summary>
        public Task<MessagingProvider> SetEmailAsync(MessagingProviderInput body, CancellationToken ct = default)
            => Req<MessagingProvider>(HttpVerb.Put, "/v1/messaging_providers/email", body, cancellationToken: ct);

        /// <summary>Set the SMS provider from an untyped body. Prefer the <see cref="SmsProviderConfig"/> overload.</summary>
        public Task<MessagingProvider> SetSmsAsync(MessagingProviderInput body, CancellationToken ct = default)
            => Req<MessagingProvider>(HttpVerb.Put, "/v1/messaging_providers/sms", body, cancellationToken: ct);

        /// <summary>
        /// Remove the channel's provider. Removing the email provider is refused with 409
        /// <c>EMAIL_PROVIDER_IN_USE</c> (<see cref="ErrorCodes.EmailProviderInUse"/>) while nothing else would deliver
        /// email and email-dependent features are on (the error's <c>meta.features</c> lists them, e.g.
        /// <c>emailCode</c>, <c>emailLink</c>, <c>emailVerification</c>, <c>passwordReset</c>); use the
        /// <c>force</c> overload to remove it anyway.
        /// </summary>
        public Task<DeletedMessagingProvider> DeleteChannelAsync(string channel, CancellationToken ct = default)
            => DeleteChannelAsync(channel, false, ct);

        /// <summary>
        /// Remove the channel's provider. With <paramref name="force"/> true, sends <c>?force=true</c> so the email
        /// provider is removed even while email-dependent features are on (otherwise 409 <c>EMAIL_PROVIDER_IN_USE</c>).
        /// </summary>
        public Task<DeletedMessagingProvider> DeleteChannelAsync(string channel, bool force, CancellationToken ct = default)
            => Req<DeletedMessagingProvider>(HttpVerb.Delete, $"/v1/messaging_providers/{Enc(channel)}",
                query: force ? Q(("force", true)) : null, cancellationToken: ct);

        /// <summary>
        /// Configure (or update) the email provider with typed settings and a mode: <c>development</c> lets a development
        /// instance send without a verified domain (409 on a production instance); null keeps the stored mode.
        /// </summary>
        public Task<MessagingProvider> SetEmailAsync(EmailProviderConfig config, string? fromAddress, string? mode, CancellationToken ct = default)
            => SetEmailAsync(new MessagingProviderInput
            {
                Transport = config.Transport,
                FromAddress = fromAddress,
                Mode = mode,
                Config = config.ToConfig(),
            }, ct);

        /// <summary><c>GET /v1/messaging_providers/email/development</c> — The email provider in development mode (404 when it is not).</summary>
        public Task<DevelopmentEmail> GetDevelopmentEmailAsync(CancellationToken ct = default)
            => Req<DevelopmentEmail>(HttpVerb.Get, "/v1/messaging_providers/email/development", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/messaging_providers/email/development</c> — Put your own email provider in development mode, so a
        /// development instance sends Atlas's sign-in emails without a verified domain (<c>[Atlas test] </c> subjects,
        /// capped). 409 <c>DEVELOPMENT_EMAIL_NOT_ALLOWED</c> on a production instance.
        /// </summary>
        public Task<DevelopmentEmail> EnableDevelopmentEmailAsync(DevelopmentEmailBody body, CancellationToken ct = default)
            => Req<DevelopmentEmail>(HttpVerb.Post, "/v1/messaging_providers/email/development", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/messaging_providers/email/development</c> — Back to standard mode (the provider and its key stay).</summary>
        public Task<DevelopmentEmailDisabled> DisableDevelopmentEmailAsync(CancellationToken ct = default)
            => Req<DevelopmentEmailDisabled>(HttpVerb.Delete, "/v1/messaging_providers/email/development", cancellationToken: ct);

        /// <summary><c>GET /v1/messaging_providers/email/managed</c> — The managed sending domain (404 when none).</summary>
        public Task<ManagedEmail> GetManagedEmailAsync(CancellationToken ct = default)
            => Req<ManagedEmail>(HttpVerb.Get, "/v1/messaging_providers/email/managed", cancellationToken: ct);

        /// <summary><c>POST /v1/messaging_providers/email/managed</c> — Set your sending domain up on your own Resend or Brevo account (Transport + Config, or ProviderId); returns the DNS records to publish.</summary>
        public Task<ManagedEmail> CreateManagedEmailAsync(ManagedEmailBody body, CancellationToken ct = default)
            => Req<ManagedEmail>(HttpVerb.Post, "/v1/messaging_providers/email/managed", body, cancellationToken: ct);

        /// <summary><c>POST /v1/messaging_providers/email/managed/verify</c> — Re-check the DNS records now.</summary>
        public Task<ManagedEmail> VerifyManagedEmailAsync(CancellationToken ct = default)
            => Req<ManagedEmail>(HttpVerb.Post, "/v1/messaging_providers/email/managed/verify", cancellationToken: ct);

        /// <summary>
        /// <c>DELETE /v1/messaging_providers/email/managed</c> — Stop using managed email (DNS records stay). With
        /// <paramref name="force"/> true, sends <c>?force=true</c> (otherwise 409 <c>EMAIL_PROVIDER_IN_USE</c> while it is the only way to send email).
        /// </summary>
        public Task<ManagedEmailDeleted> DeleteManagedEmailAsync(bool force = false, CancellationToken ct = default)
            => Req<ManagedEmailDeleted>(HttpVerb.Delete, "/v1/messaging_providers/email/managed",
                query: force ? Q(("force", true)) : null, cancellationToken: ct);

        /// <summary>Send a fixed, clearly-marked test message through the channel's provider.</summary>
        public Task<MessagingTestResult> TestAsync(string channel, string to, CancellationToken ct = default)
            => Req<MessagingTestResult>(HttpVerb.Post, $"/v1/messaging_providers/{Enc(channel)}/test",
                body: Body(("to", to)), cancellationToken: ct);
    }
}
