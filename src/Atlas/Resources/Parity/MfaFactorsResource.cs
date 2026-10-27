using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/instance/yubico_secret</c>. Only the fields set are sent.</summary>
    public sealed class MfaFactorsSetYubicoSecretBody
    {
        /// <summary>Required. Base64 secret key.</summary>
        [JsonPropertyName("secret")] public string? Secret { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/instance/duo_secret</c>. Only the fields set are sent.</summary>
    public sealed class MfaFactorsSetDuoSecretBody
    {
        /// <summary>Required. Duo client secret.</summary>
        [JsonPropertyName("secret")] public string? Secret { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/instance/smart_card_secret</c>. Only the fields set are sent.</summary>
    public sealed class MfaFactorsSetSmartCardSecretBody
    {
        /// <summary>Required. Random shared secret, 24 characters or more.</summary>
        [JsonPropertyName("secret")] public string? Secret { get; set; }
    }

    /// <summary>Operations of <c>mfaFactors</c>.</summary>
    public sealed partial class MfaFactorsResource : ResourceBase
    {
        public MfaFactorsResource(AtlasTransport t) : base(t) { }

        private MfaFactorsYubikeysResource? _yubikeys;
        /// <summary>The <c>mfaFactors.yubikeys</c> operations.</summary>
        public MfaFactorsYubikeysResource Yubikeys => _yubikeys ??= new MfaFactorsYubikeysResource(Transport);

        private MfaFactorsSmartCardMappingsResource? _smartCardMappings;
        /// <summary>The <c>mfaFactors.smartCardMappings</c> operations.</summary>
        public MfaFactorsSmartCardMappingsResource SmartCardMappings => _smartCardMappings ??= new MfaFactorsSmartCardMappingsResource(Transport);

        /// <summary><c>PUT /v1/instance/yubico_secret</c> - Store (write-only, encrypted) the Yubico cloud API secret key (base64, from upgrade.yubico.com/getapikey). The client id is mfa.yubikey.clientId. Used when mfa.yubikey.mode is "cloud".</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> SetYubicoSecretAsync(MfaFactorsSetYubicoSecretBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/instance/yubico_secret", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/yubico_secret</c> - Remove the stored Yubico cloud secret. Cloud-mode YubiKey sign-ins then fail until a new one is stored.</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> RemoveYubicoSecretAsync(bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, "/v1/instance/yubico_secret", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/duo_secret</c> - Store (write-only, encrypted) the Duo Web SDK / Universal Prompt client secret. The client id (integration key) and API host are mfa.duo.clientId and mfa.duo.apiHost.</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> SetDuoSecretAsync(MfaFactorsSetDuoSecretBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/instance/duo_secret", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/duo_secret</c> - Remove the stored Duo client secret.</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> RemoveDuoSecretAsync(bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, "/v1/instance/duo_secret", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/smart_card_secret</c> - Store (write-only, encrypted) the shared secret your TLS proxy sends in the mfa.smartCard.secretHeader header, so Atlas believes the certificate headers only from your proxy. Use a random value of at least 24 characters.</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> SetSmartCardSecretAsync(MfaFactorsSetSmartCardSecretBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/instance/smart_card_secret", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/smart_card_secret</c> - Remove the stored proxy secret. Smart card sign-in then refuses every request.</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> RemoveSmartCardSecretAsync(bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, "/v1/instance/smart_card_secret", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/instance/duo_health_check</c> - Ask Duo whether the configured integration key, stored secret and API host are accepted (Duo's health check). Reports ok and a message; nothing is changed.</summary>
        /// <remarks>Needs the <c>instance:read</c> scope.</remarks>
        public Task<JsonElement> DuoHealthCheckAsync(bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/instance/duo_health_check", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/security_questions</c> - Clear a user's security-question answers (and any lockout). They set new ones from their account.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> ResetSecurityQuestionsAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/security_questions", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
