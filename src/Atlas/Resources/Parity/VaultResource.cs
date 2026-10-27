using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/vault/objects</c>. Only the fields set are sent.</summary>
    public sealed class VaultCreateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("key_context")] public Dictionary<string, object?>? KeyContext { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, object?>? Metadata { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/vault/objects/{id}</c>. Only the fields set are sent.</summary>
    public sealed class VaultUpdateBody
    {
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("version_check")] public double? VersionCheck { get; set; }
    }

    /// <summary>Operations of <c>vault</c>.</summary>
    public sealed partial class VaultResource : ResourceBase
    {
        public VaultResource(AtlasTransport t) : base(t) { }

        private VaultKeyProviderResource? _keyProvider;
        /// <summary>The <c>vault.keyProvider</c> operations.</summary>
        public VaultKeyProviderResource KeyProvider => _keyProvider ??= new VaultKeyProviderResource(Transport);

        /// <summary><c>POST /v1/vault/objects</c> - Store a secret. `key_context` (1–10 string labels) is mandatory and is authenticated into the encryption: the value can only be opened with the context it was written under. The value (up to 64 KiB) is sealed under a fresh data key wrapped by the instance key, or by your own cloud KMS key when one is configured. 409 when the name is taken.</summary>
        /// <remarks>Needs the <c>vault:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(VaultCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/vault/objects", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/vault/objects</c> - List vault objects (metadata only, never values), newest first. Filter by `name_prefix` or by `key_context` (a JSON object; objects whose context contains every given label).</summary>
        /// <remarks>Needs the <c>vault:list</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? namePrefix = null, string? keyContext = null, string? startingAfter = null, long? limit = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/vault/objects", query: Q(("name_prefix", namePrefix), ("key_context", keyContext), ("starting_after", startingAfter), ("limit", limit)), cancellationToken: ct);

        /// <summary><c>GET /v1/vault/objects/{id}/metadata</c> - Read a vault object's metadata without its value.</summary>
        /// <remarks>Needs the <c>vault:list</c> scope.</remarks>
        public Task<JsonElement> MetadataAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/vault/objects/{Enc(id)}/metadata", cancellationToken: ct);

        /// <summary><c>GET /v1/vault/objects/{id}/versions</c> - List the versions of a vault object (number, which key protects it, when written) — never values.</summary>
        /// <remarks>Needs the <c>vault:list</c> scope.</remarks>
        public Task<JsonElement> VersionsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/vault/objects/{Enc(id)}/versions", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/vault/objects/{id}</c> - Read a vault object with its decrypted value (the current version, or `version`). Every read is audited. 409 if the version's wrapping key provider is no longer configured; 502 VAULT_KMS_ERROR when your KMS refuses.</summary>
        /// <remarks>Needs the <c>vault:read</c> scope.</remarks>
        public Task<JsonElement> ReadAsync(string id, long? version = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/vault/objects/{Enc(id)}", query: Q(("version", version)), cancellationToken: ct);

        /// <summary><c>PUT /v1/vault/objects/{id}</c> - Append a new version with a new value. Earlier versions stay readable. Pass `version_check` (the version you last read) for optimistic concurrency: 409 if another update landed since.</summary>
        /// <remarks>Needs the <c>vault:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, VaultUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/vault/objects/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/vault/objects/{id}</c> - Delete a vault object and every version of it. Irreversible.</summary>
        /// <remarks>Needs the <c>vault:delete</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/vault/objects/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
