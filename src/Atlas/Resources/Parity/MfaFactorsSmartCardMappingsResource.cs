using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/users/{id}/smartcard_mappings</c>. Only the fields set are sent.</summary>
    public sealed class MfaFactorsSmartCardMappingsAddBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("kind")] public string? Kind { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("value")] public string? Value { get; set; }
        /// <summary>Issuer DN. Required for kind "serial".</summary>
        [JsonPropertyName("issuer")] public string? Issuer { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
    }

    /// <summary>Operations of <c>mfaFactors.smartCardMappings</c>.</summary>
    public sealed partial class MfaFactorsSmartCardMappingsResource : ResourceBase
    {
        public MfaFactorsSmartCardMappingsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/users/{id}/smartcard_mappings</c> - List the certificate attributes registered for a user.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/users/{Enc(id)}/smartcard_mappings", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/smartcard_mappings</c> - Pre-register the certificate attribute that signs this user in with a smart card: kind fingerprint (SHA-256, 64 hex), serial (hex, plus the issuer DN), san_email, san_upn or subject_cn. A registered mapping only applies while its kind is listed in mfa.smartCard.mappingRules. 409 when the value is already mapped.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> AddAsync(string id, MfaFactorsSmartCardMappingsAddBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(id)}/smartcard_mappings", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/smartcard_mappings/{mappingId}</c> - Remove a registered certificate mapping.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> RemoveAsync(string id, string mappingId, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/smartcard_mappings/{Enc(mappingId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
