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

    /// <summary>The BYOK messaging namespace (<c>/v1/messaging_providers</c>).</summary>
    public sealed class MessagingResource : ResourceBase
    {
        public MessagingResource(AtlasTransport transport) : base(transport) { }

        /// <summary>The instance's configured email and SMS providers, secrets omitted.</summary>
        public Task<MessagingProviders> GetAsync(CancellationToken ct = default)
            => Req<MessagingProviders>(HttpVerb.Get, "/v1/messaging_providers", cancellationToken: ct);

        public Task<MessagingProvider> SetEmailAsync(MessagingProviderInput body, CancellationToken ct = default)
            => Req<MessagingProvider>(HttpVerb.Put, "/v1/messaging_providers/email", body, cancellationToken: ct);

        public Task<MessagingProvider> SetSmsAsync(MessagingProviderInput body, CancellationToken ct = default)
            => Req<MessagingProvider>(HttpVerb.Put, "/v1/messaging_providers/sms", body, cancellationToken: ct);

        public Task<DeletedMessagingProvider> DeleteChannelAsync(string channel, CancellationToken ct = default)
            => Req<DeletedMessagingProvider>(HttpVerb.Delete, $"/v1/messaging_providers/{Enc(channel)}", cancellationToken: ct);

        /// <summary>Send a fixed, clearly-marked test message through the channel's provider.</summary>
        public Task<MessagingTestResult> TestAsync(string channel, string to, CancellationToken ct = default)
            => Req<MessagingTestResult>(HttpVerb.Post, $"/v1/messaging_providers/{Enc(channel)}/test",
                body: Body(("to", to)), cancellationToken: ct);
    }
}
