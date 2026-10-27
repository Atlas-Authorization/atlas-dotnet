using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/vault/key_provider</c>. Only the fields set are sent.</summary>
    public sealed class VaultKeyProviderSetBody
    {
        [JsonPropertyName("provider")] public string? Provider { get; set; }
        [JsonPropertyName("config")] public Dictionary<string, object?>? Config { get; set; }
        [JsonPropertyName("credentials")] public Dictionary<string, object?>? Credentials { get; set; }
    }

    /// <summary>Operations of <c>vault.keyProvider</c>.</summary>
    public sealed partial class VaultKeyProviderResource : ResourceBase
    {
        public VaultKeyProviderResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/vault/key_provider</c> - The customer-managed key that wraps new vault data keys. 404 when none is configured (the instance key is used).</summary>
        /// <remarks>Needs the <c>vault:manage</c> scope.</remarks>
        public Task<JsonElement> GetAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/vault/key_provider", cancellationToken: ct);

        /// <summary><c>PUT /v1/vault/key_provider</c> - Bring your own key: wrap new vault data keys with your AWS KMS (`config.region`, `config.key_id`; symmetric key), Google Cloud KMS (`config.crypto_key`; symmetric key) or Azure Key Vault (`config.vault_url`, `config.key_name`; RSA key with wrap/unwrap) key. Atlas proves access with a wrap/unwrap round trip before saving (422 or 502 on refusal). Credentials are write-only. Existing versions stay on the key that wrapped them; switching provider type is a 409 while versions remain on the old one.</summary>
        /// <remarks>Needs the <c>vault:manage</c> scope.</remarks>
        public Task<JsonElement> SetAsync(VaultKeyProviderSetBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/vault/key_provider", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/vault/key_provider</c> - Stop using your own key for new versions. 409 while any version is still wrapped by it.</summary>
        /// <remarks>Needs the <c>vault:manage</c> scope.</remarks>
        public Task<JsonElement> RemoveAsync(bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, "/v1/vault/key_provider", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
