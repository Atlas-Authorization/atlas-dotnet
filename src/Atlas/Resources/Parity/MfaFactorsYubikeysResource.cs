using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/yubikeys</c>. Only the fields set are sent.</summary>
    public sealed class MfaFactorsYubikeysRegisterBody
    {
        /// <summary>Required. Modhex prefix printed on the key (2 to 16 characters).</summary>
        [JsonPropertyName("public_id")] public string? PublicId { get; set; }
        /// <summary>Required. 32 hex characters.</summary>
        [JsonPropertyName("aes_key")] public string? AesKey { get; set; }
        /// <summary>Optional 12 hex characters; when given the decrypted token must carry it.</summary>
        [JsonPropertyName("private_id")] public string? PrivateId { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
    }

    /// <summary>Operations of <c>mfaFactors.yubikeys</c>.</summary>
    public sealed partial class MfaFactorsYubikeysResource : ResourceBase
    {
        public MfaFactorsYubikeysResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/yubikeys</c> - List the registered self-held YubiKeys (public id, owner, replay counter). The AES keys are never returned.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/yubikeys", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/yubikeys</c> - Register a YubiKey's AES key for self-held Yubico OTP (mfa.yubikey.mode "self_held"). The key is stored encrypted and never returned. Give user_id to assign it; otherwise the first user to enrol it with a valid OTP claims it. 409 when the public id is already registered.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> RegisterAsync(MfaFactorsYubikeysRegisterBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/yubikeys", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/yubikeys/{id}</c> - Remove a registered YubiKey. A factor already enrolled with it stops verifying.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> RemoveAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/yubikeys/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
