using System.Collections.Generic;

namespace Atlas.Resources
{
    /// <summary>
    /// Builds the exact <c>config</c> object for a provider, leaving out unset (null) keys.
    /// </summary>
    internal static class ProviderConfigBuilder
    {
        internal static Dictionary<string, object?> Of(params (string Key, object? Value)[] pairs)
        {
            var d = new Dictionary<string, object?>();
            foreach (var (key, value) in pairs)
            {
                if (value != null) d[key] = value;
            }
            return d;
        }
    }

    /// <summary>
    /// Typed settings for an email provider — one derived record per transport, each
    /// producing exactly the <c>config</c> keys the API accepts for that transport.
    /// </summary>
    /// <remarks>
    /// Leave a property null to leave it out of the request. Properties marked
    /// "Required on first setup" must be set the first time you configure that
    /// transport (or switch to it). On an update of the SAME transport, an omitted
    /// key keeps its stored value — including a stored secret — so a from-address
    /// change need not re-enter the API key. Secrets are write-only: they are never
    /// returned; a response reports only which are set (<see cref="MessagingProvider.SecretsSet"/>).
    /// </remarks>
    public abstract record EmailProviderConfig
    {
        private protected EmailProviderConfig() { }

        /// <summary>The transport name sent as <c>transport</c>.</summary>
        public abstract string Transport { get; }

        /// <summary>The exact <c>config</c> object for this transport (null properties omitted).</summary>
        public abstract Dictionary<string, object?> ToConfig();

        /// <summary>Resend.</summary>
        public sealed record Resend : EmailProviderConfig
        {
            /// <summary>Resend API key (<c>re_…</c>). Required on first setup. Write-only.</summary>
            public string? ApiKey { get; init; }
            public override string Transport => "resend";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(("api_key", ApiKey));
        }

        /// <summary>Amazon SES.</summary>
        public sealed record Ses : EmailProviderConfig
        {
            /// <summary>AWS region, e.g. <c>us-east-1</c>. Required on first setup.</summary>
            public string? Region { get; init; }
            /// <summary>IAM access key ID. Required on first setup. Write-only.</summary>
            public string? AccessKeyId { get; init; }
            /// <summary>IAM secret access key. Required on first setup. Write-only.</summary>
            public string? SecretAccessKey { get; init; }
            /// <summary>STS session token, only for temporary credentials. Write-only.</summary>
            public string? SessionToken { get; init; }
            public override string Transport => "ses";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("region", Region), ("access_key_id", AccessKeyId),
                ("secret_access_key", SecretAccessKey), ("session_token", SessionToken));
        }

        /// <summary>Any SMTP server.</summary>
        public sealed record Smtp : EmailProviderConfig
        {
            /// <summary>SMTP server hostname, e.g. <c>smtp.example.com</c>. Required on first setup.</summary>
            public string? Host { get; init; }
            /// <summary>SMTP port (1-65535). Default 587.</summary>
            public int? Port { get; init; }
            /// <summary>Use implicit TLS. Default: true on port 465, otherwise STARTTLS.</summary>
            public bool? Secure { get; init; }
            /// <summary>SMTP username, if the server requires authentication.</summary>
            public string? Username { get; init; }
            /// <summary>SMTP password or app password. Write-only.</summary>
            public string? Password { get; init; }
            public override string Transport => "smtp";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("host", Host), ("port", Port), ("secure", Secure),
                ("username", Username), ("password", Password));
        }

        /// <summary>Brevo.</summary>
        public sealed record Brevo : EmailProviderConfig
        {
            /// <summary>Brevo API key (<c>xkeysib-…</c>). Required on first setup. Write-only.</summary>
            public string? ApiKey { get; init; }
            public override string Transport => "brevo";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(("api_key", ApiKey));
        }

        /// <summary>Mailgun.</summary>
        public sealed record Mailgun : EmailProviderConfig
        {
            /// <summary>Mailgun private API key. Required on first setup. Write-only.</summary>
            public string? ApiKey { get; init; }
            /// <summary>Your Mailgun sending domain, e.g. <c>mg.example.com</c>. Required on first setup.</summary>
            public string? Domain { get; init; }
            /// <summary>Mailgun region. Default <see cref="MailgunRegion.Us"/>.</summary>
            public MailgunRegion? Region { get; init; }
            public override string Transport => "mailgun";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("api_key", ApiKey), ("domain", Domain),
                ("region", Region switch { MailgunRegion.Us => "us", MailgunRegion.Eu => "eu", _ => null }));
        }
    }

    /// <summary>Mailgun API region.</summary>
    public enum MailgunRegion
    {
        Us,
        Eu,
    }

    /// <summary>
    /// Typed settings for an SMS provider — one derived record per transport, each
    /// producing exactly the <c>config</c> keys the API accepts for that transport.
    /// </summary>
    /// <remarks>
    /// Leave a property null to leave it out of the request. Properties marked
    /// "Required on first setup" must be set the first time you configure that
    /// transport (or switch to it). On an update of the SAME transport, an omitted
    /// key keeps its stored value — including a stored secret. Secrets are
    /// write-only: they are never returned. <c>From</c> is the sender number or ID;
    /// the request's <c>fromAddress</c> takes precedence.
    /// </remarks>
    public abstract record SmsProviderConfig
    {
        private protected SmsProviderConfig() { }

        /// <summary>The transport name sent as <c>transport</c>.</summary>
        public abstract string Transport { get; }

        /// <summary>The exact <c>config</c> object for this transport (null properties omitted).</summary>
        public abstract Dictionary<string, object?> ToConfig();

        /// <summary>Sender number or ID. Optional; the request's <c>fromAddress</c> takes precedence.</summary>
        public string? From { get; init; }

        /// <summary>Twilio.</summary>
        public sealed record Twilio : SmsProviderConfig
        {
            /// <summary>Twilio Account SID (<c>AC…</c>). Required on first setup.</summary>
            public string? AccountSid { get; init; }
            /// <summary>Twilio auth token. Required on first setup. Write-only.</summary>
            public string? AuthToken { get; init; }
            /// <summary>WhatsApp-enabled sender number, for the WhatsApp channel.</summary>
            public string? WhatsappFrom { get; init; }
            /// <summary>Voice-capable number, for the voice channel.</summary>
            public string? VoiceFrom { get; init; }
            public override string Transport => "twilio";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("account_sid", AccountSid), ("auth_token", AuthToken), ("from", From),
                ("whatsapp_from", WhatsappFrom), ("voice_from", VoiceFrom));
        }

        /// <summary>Vonage.</summary>
        public sealed record Vonage : SmsProviderConfig
        {
            /// <summary>Vonage API key. Required on first setup.</summary>
            public string? ApiKey { get; init; }
            /// <summary>Vonage API secret. Required on first setup. Write-only.</summary>
            public string? ApiSecret { get; init; }
            public override string Transport => "vonage";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("api_key", ApiKey), ("api_secret", ApiSecret), ("from", From));
        }

        /// <summary>MessageBird (Bird).</summary>
        public sealed record Messagebird : SmsProviderConfig
        {
            /// <summary>MessageBird access key. Required on first setup. Write-only.</summary>
            public string? AccessKey { get; init; }
            public override string Transport => "messagebird";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("access_key", AccessKey), ("from", From));
        }

        /// <summary>Plivo.</summary>
        public sealed record Plivo : SmsProviderConfig
        {
            /// <summary>Plivo Auth ID. Required on first setup.</summary>
            public string? AuthId { get; init; }
            /// <summary>Plivo auth token. Required on first setup. Write-only.</summary>
            public string? AuthToken { get; init; }
            public override string Transport => "plivo";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("auth_id", AuthId), ("auth_token", AuthToken), ("from", From));
        }

        /// <summary>Telnyx.</summary>
        public sealed record Telnyx : SmsProviderConfig
        {
            /// <summary>Telnyx API key. Required on first setup. Write-only.</summary>
            public string? ApiKey { get; init; }
            public override string Transport => "telnyx";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("api_key", ApiKey), ("from", From));
        }

        /// <summary>Sinch.</summary>
        public sealed record Sinch : SmsProviderConfig
        {
            /// <summary>Sinch service plan ID. Required on first setup.</summary>
            public string? ServicePlanId { get; init; }
            /// <summary>Sinch API token. Required on first setup. Write-only.</summary>
            public string? ApiToken { get; init; }
            /// <summary>Sinch API region, a short lowercase code such as <c>us</c> or <c>eu</c>. Default <c>us</c>.</summary>
            public string? Region { get; init; }
            public override string Transport => "sinch";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("service_plan_id", ServicePlanId), ("api_token", ApiToken), ("region", Region), ("from", From));
        }

        /// <summary>Infobip.</summary>
        public sealed record Infobip : SmsProviderConfig
        {
            /// <summary>Your Infobip base URL (https), e.g. <c>https://xxxxx.api.infobip.com</c>. Required on first setup.</summary>
            public string? BaseUrl { get; init; }
            /// <summary>Infobip API key. Required on first setup. Write-only.</summary>
            public string? ApiKey { get; init; }
            public override string Transport => "infobip";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("base_url", BaseUrl), ("api_key", ApiKey), ("from", From));
        }

        /// <summary>ClickSend.</summary>
        public sealed record Clicksend : SmsProviderConfig
        {
            /// <summary>ClickSend username (your login email). Required on first setup.</summary>
            public string? Username { get; init; }
            /// <summary>ClickSend API key. Required on first setup. Write-only.</summary>
            public string? ApiKey { get; init; }
            public override string Transport => "clicksend";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("username", Username), ("api_key", ApiKey), ("from", From));
        }

        /// <summary>Bandwidth.</summary>
        public sealed record Bandwidth : SmsProviderConfig
        {
            /// <summary>Bandwidth account ID. Required on first setup.</summary>
            public string? AccountId { get; init; }
            /// <summary>Bandwidth messaging application ID. Required on first setup.</summary>
            public string? ApplicationId { get; init; }
            /// <summary>Bandwidth API username. Required on first setup.</summary>
            public string? Username { get; init; }
            /// <summary>Bandwidth API password. Required on first setup. Write-only.</summary>
            public string? Password { get; init; }
            public override string Transport => "bandwidth";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("account_id", AccountId), ("application_id", ApplicationId),
                ("username", Username), ("password", Password), ("from", From));
        }

        /// <summary>Amazon SNS.</summary>
        public sealed record AwsSns : SmsProviderConfig
        {
            /// <summary>AWS region, e.g. <c>us-east-1</c>. Required on first setup.</summary>
            public string? Region { get; init; }
            /// <summary>IAM access key ID. Required on first setup.</summary>
            public string? AccessKeyId { get; init; }
            /// <summary>IAM secret access key. Required on first setup. Write-only.</summary>
            public string? SecretAccessKey { get; init; }
            /// <summary>STS session token, only for temporary credentials. Write-only.</summary>
            public string? SessionToken { get; init; }
            public override string Transport => "aws_sns";
            public override Dictionary<string, object?> ToConfig() => ProviderConfigBuilder.Of(
                ("region", Region), ("access_key_id", AccessKeyId),
                ("secret_access_key", SecretAccessKey), ("session_token", SessionToken), ("from", From));
        }
    }
}
