using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class PcaRevocationBody
    {
        public string? GrantRef { get; set; }
        public string? CapId { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public PcaRevocationBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class PcaRevocation
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? GrantRef { get; init; }
        public string? CapId { get; init; }
        public bool? Revoked { get; init; }
    }

    public sealed class PcaFreezeState
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public bool? Frozen { get; init; }
    }

    public sealed class PcaStepUp
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? StepupId { get; init; }
        public string? GrantRef { get; init; }
        public string? ActionDigest { get; init; }
        public long? RequiredT { get; init; }
        public string? Status { get; init; }
        /// <summary>Roles that have contributed a share.</summary>
        public List<string>? Shares { get; init; }
        public string? CreatedAt { get; init; }
        public string? ExpiresAt { get; init; }
        /// <summary>Ledger anchoring receipt (present once approved).</summary>
        public Dictionary<string, object?>? Receipt { get; init; }
        /// <summary>Present on a cosign that has not yet met the threshold.</summary>
        public bool? StepUpRequired { get; init; }
    }

    public sealed class PcaAttestors
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        /// <summary>b64u Ed25519 public keys.</summary>
        public List<string>? TrustedAttestorKeys { get; init; }
    }

    /// <summary>Proof-carrying authority controls (<c>/v1/pca</c>).</summary>
    public sealed class PcaResource : ResourceBase
    {
        public PcaResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>POST /v1/pca/revocations</c> — Revoke a capability id under a grant; any PCActn whose chain contains it is denied.</summary>
        public Task<PcaRevocation> RevokeAsync(PcaRevocationBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<PcaRevocation>(HttpVerb.Post, "/v1/pca/revocations", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/pca/freeze</c> — Engage the instance-wide PCA kill switch: every PCActn is denied until it is cleared.</summary>
        public Task<PcaFreezeState> FreezeAsync(string? reason = null, CancellationToken ct = default)
            => Req<PcaFreezeState>(HttpVerb.Post, "/v1/pca/freeze", Body(("reason", reason)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/pca/freeze</c> — Clear the instance-wide PCA kill switch.</summary>
        public Task<PcaFreezeState> UnfreezeAsync(CancellationToken ct = default)
            => Req<PcaFreezeState>(HttpVerb.Delete, "/v1/pca/freeze", cancellationToken: ct);

        /// <summary><c>POST /v1/pca/stepups/{id}/deny</c> — Deny a pending step-up: the held action is never anchored.</summary>
        public Task<PcaStepUp> DenyStepUpAsync(string id, string? reason = null, CancellationToken ct = default)
            => Req<PcaStepUp>(HttpVerb.Post, $"/v1/pca/stepups/{Enc(id)}/deny", Body(("reason", reason)), cancellationToken: ct);

        /// <summary><c>GET /v1/pca/attestors</c> — The instance's trusted software-attestor public keys.</summary>
        public Task<PcaAttestors> GetAttestorsAsync(CancellationToken ct = default)
            => Req<PcaAttestors>(HttpVerb.Get, "/v1/pca/attestors", cancellationToken: ct);

        /// <summary><c>PUT /v1/pca/attestors</c> — Replace the trusted software-attestor key list.</summary>
        public Task<PcaAttestors> ReplaceAttestorsAsync(List<string>? trustedAttestorKeys, CancellationToken ct = default)
            => Req<PcaAttestors>(HttpVerb.Put, "/v1/pca/attestors", Body(("trusted_attestor_keys", trustedAttestorKeys)), cancellationToken: ct);
    }
}
