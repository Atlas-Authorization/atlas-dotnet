using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/vc/offers</c>. Only the fields set are sent.</summary>
    public sealed class VcCreateOfferBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("credential_type_id")] public string? CredentialTypeId { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        /// <summary>4 to 8 digits the wallet must send, delivered to the user separately.</summary>
        [JsonPropertyName("tx_code")] public string? TxCode { get; set; }
        /// <summary>Default 600.</summary>
        [JsonPropertyName("expires_in_seconds")] public long? ExpiresInSeconds { get; set; }
    }

    /// <summary>Operations of <c>vc</c>.</summary>
    public sealed partial class VcResource : ResourceBase
    {
        public VcResource(AtlasTransport t) : base(t) { }

        private VcCredentialTypesResource? _credentialTypes;
        /// <summary>The <c>vc.credentialTypes</c> operations.</summary>
        public VcCredentialTypesResource CredentialTypes => _credentialTypes ??= new VcCredentialTypesResource(Transport);

        private VcCredentialsResource? _credentials;
        /// <summary>The <c>vc.credentials</c> operations.</summary>
        public VcCredentialsResource Credentials => _credentials ??= new VcCredentialsResource(Transport);

        private VcPresentationRequestsResource? _presentationRequests;
        /// <summary>The <c>vc.presentationRequests</c> operations.</summary>
        public VcPresentationRequestsResource PresentationRequests => _presentationRequests ??= new VcPresentationRequestsResource(Transport);

        /// <summary><c>POST /v1/vc/offers</c> - Create a wallet offer for a user (pre-authorized code flow). The response carries the code once, as an openid-credential-offer:// link.</summary>
        /// <remarks>Needs the <c>vc:write</c> scope.</remarks>
        public Task<JsonElement> CreateOfferAsync(VcCreateOfferBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/vc/offers", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
