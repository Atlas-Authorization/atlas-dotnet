using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/vc/credential_types</c>. Only the fields set are sent.</summary>
    public sealed class VcCredentialTypesCreateBody
    {
        /// <summary>Required. The credential type identifier (a URN or URL); for mso_mdoc the docType, for example org.iso.18013.5.1.mDL.</summary>
        [JsonPropertyName("vct")] public string? Vct { get; set; }
        /// <summary>Default dc+sd-jwt. mso_mdoc issues an ISO 18013-5 mdoc (ES256, holder key P-256).</summary>
        [JsonPropertyName("format")] public string? Format { get; set; }
        /// <summary>mso_mdoc only: the namespace the claims live in (default the docType).</summary>
        [JsonPropertyName("namespace")] public string? Namespace { get; set; }
        /// <summary>mso_mdoc only: PEM IACA root certificates (ISO 18013-5 Annex B). With roots set, a presentation must carry an x5chain leading to one of them and the issuer signature must verify under its Document Signer key; bare trusted keys are then not accepted.</summary>
        [JsonPropertyName("iaca_roots")] public List<string>? IacaRoots { get; set; }
        /// <summary>mso_mdoc only: PEM Document Signer certificate first, then intermediates, published as x5chain. The first certificate must be for this instance’s mdoc signing key (the ES256 key in its key set), otherwise 400.</summary>
        [JsonPropertyName("signer_chain")] public List<string>? SignerChain { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("claims")] public List<object?>? Claims { get; set; }
        /// <summary>Default one year.</summary>
        [JsonPropertyName("lifetime_seconds")] public long? LifetimeSeconds { get; set; }
        [JsonPropertyName("require_wallet_attestation")] public bool? RequireWalletAttestation { get; set; }
        /// <summary>JWK Set of accepted wallet providers; required with require_wallet_attestation.</summary>
        [JsonPropertyName("wallet_attestation_jwks")] public Dictionary<string, object?>? WalletAttestationJwks { get; set; }
    }

    /// <summary>Operations of <c>vc.credentialTypes</c>.</summary>
    public sealed partial class VcCredentialTypesResource : ResourceBase
    {
        public VcCredentialTypesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/vc/credential_types</c> - List the credential types you issue.</summary>
        /// <remarks>Needs the <c>vc:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/vc/credential_types", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/vc/credential_types</c> - Define a credential type: its vct, and how each claim is filled from a user’s attributes. Issued as SD-JWT VC.</summary>
        /// <remarks>Needs the <c>vc:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(VcCredentialTypesCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/vc/credential_types", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/vc/credential_types/{id}</c> - Read one credential type.</summary>
        /// <remarks>Needs the <c>vc:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/vc/credential_types/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/vc/credential_types/{id}</c> - Delete a credential type and the records of what was issued under it.</summary>
        /// <remarks>Needs the <c>vc:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/vc/credential_types/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
