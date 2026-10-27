using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/attack_protection/app_attestation_secret</c>. Only the fields set are sent.</summary>
    public sealed class AttackProtectionSetAppAttestationSecretBody
    {
        [JsonPropertyName("secret")] public string? Secret { get; set; }
        [JsonPropertyName("kind")] public string? Kind { get; set; }
    }

    /// <summary>More operations of <c>attackProtection</c>.</summary>
    public sealed partial class AttackProtectionResource
    {
        /// <summary><c>PUT /v1/attack_protection/app_attestation_secret</c> - Store the Play Integrity response-decryption key (base64 AES-256) for app attestation. Write-only; the public verification key is attackProtection.appAttestation.android.verificationKey.</summary>
        /// <remarks>Needs the <c>attack_protection:write</c> scope.</remarks>
        public Task<JsonElement> SetAppAttestationSecretAsync(AttackProtectionSetAppAttestationSecretBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/attack_protection/app_attestation_secret", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/attack_protection/app_attestation_secret</c> - Remove a stored attestation secret: the Play Integrity decryption key (default) or, with kind=devicecheck, the Apple DeviceCheck key. That platform then refuses every request.</summary>
        /// <remarks>Needs the <c>attack_protection:write</c> scope.</remarks>
        public Task<JsonElement> RemoveAppAttestationSecretAsync(bool? dryRun = null, string? kind = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, "/v1/attack_protection/app_attestation_secret", query: Q(("dry_run", dryRun), ("kind", kind)), cancellationToken: ct);
    }
}
