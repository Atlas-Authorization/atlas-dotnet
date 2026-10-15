using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class MessagingProvider
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "messaging_provider";
        public string Channel { get; init; } = "";
        public string Transport { get; init; } = "";
        public string? FromAddress { get; init; }
        public string Status { get; init; } = "";
        /// <summary>Non-secret config only — secret values are stripped before serialising.</summary>
        public Dictionary<string, object?> Config { get; init; } = new Dictionary<string, object?>();
        /// <summary>Which secret keys are set for this transport, never their values.</summary>
        public Dictionary<string, bool> SecretsSet { get; init; } = new Dictionary<string, bool>();
        public long UpdatedAt { get; init; }
    }

    public sealed class MessagingProviders
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "messaging_providers";
        public MessagingProvider? Email { get; init; }
        public MessagingProvider? Sms { get; init; }
    }

    public sealed class MessagingProviderInput
    {
        /// <summary>The transport, e.g. <c>resend</c>, <c>ses</c>, <c>smtp</c> (email) or <c>twilio</c> (sms).</summary>
        public string Transport { get; set; } = "";
        public string? FromAddress { get; set; }
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

        /// <summary>Send a fixed, clearly-marked test message through the channel's provider.</summary>
        public Task<MessagingTestResult> TestAsync(string channel, string to, CancellationToken ct = default)
            => Req<MessagingTestResult>(HttpVerb.Post, $"/v1/messaging_providers/{Enc(channel)}/test",
                body: Body(("to", to)), cancellationToken: ct);
    }
}
