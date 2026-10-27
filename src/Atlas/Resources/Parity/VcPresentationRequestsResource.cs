using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/vc/presentation_requests</c>. Only the fields set are sent.</summary>
    public sealed class VcPresentationRequestsCreateBody
    {
        /// <summary>Required. The vct, or the docType for mso_mdoc.</summary>
        [JsonPropertyName("vct")] public string? Vct { get; set; }
        /// <summary>Default dc+sd-jwt.</summary>
        [JsonPropertyName("format")] public string? Format { get; set; }
        /// <summary>mso_mdoc only: PEM IACA roots for this request; overrides the credential type’s. Certificate chain mode only: no bare key is accepted when roots apply.</summary>
        [JsonPropertyName("iaca_roots")] public List<string>? IacaRoots { get; set; }
        /// <summary>Claims the wallet must disclose.</summary>
        [JsonPropertyName("claims")] public List<string>? Claims { get; set; }
        /// <summary>Other issuers accepted besides this instance.</summary>
        [JsonPropertyName("trusted_issuers")] public List<object?>? TrustedIssuers { get; set; }
        /// <summary>Default 600.</summary>
        [JsonPropertyName("expires_in_seconds")] public long? ExpiresInSeconds { get; set; }
    }

    /// <summary>Operations of <c>vc.presentationRequests</c>.</summary>
    public sealed partial class VcPresentationRequestsResource : ResourceBase
    {
        public VcPresentationRequestsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/vc/presentation_requests</c> - Start a verifier request for a credential type and claims. Show authorization_request_uri to the wallet, then read the request for the outcome.</summary>
        /// <remarks>Needs the <c>vc:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(VcPresentationRequestsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/vc/presentation_requests", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/vc/presentation_requests/{id}</c> - Read a presentation request and its outcome.</summary>
        /// <remarks>Needs the <c>vc:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/vc/presentation_requests/{Enc(id)}", cancellationToken: ct);
    }
}
