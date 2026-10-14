using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class LegacyAuthConnectionBody
    {
        /// <summary>Whether sign-in consults the legacy store. Default false.</summary>
        public bool? Enabled { get; set; }
        /// <summary>The tenant's HTTPS verify endpoint. Atlas POSTs an HMAC-signed {identifier, password}; a 2xx with {user:{email,...}} authenticates. Public https required on a production instance.</summary>
        public string? VerifyUrl { get; set; }
        /// <summary>HMAC signing secret for the verify webhook. Write-only; required on create, omit on edit to keep the stored one. Never returned.</summary>
        public string? SigningSecret { get; set; }
        /// <summary>Create the user locally (hashing the verified password) on a successful verify — the drain. Default true. False mirrors the profile but keeps the password in the legacy store.</summary>
        public bool? ImportOnLogin { get; set; }
        /// <summary>Fail-closed call budget, 1–30000. Default 5000.</summary>
        public long? TimeoutMs { get; set; }
        /// <summary>Only "password" today.</summary>
        public string? Strategy { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public LegacyAuthConnectionBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class LegacyAuthConnectionConfig
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public bool? Enabled { get; init; }
        public string? VerifyUrl { get; init; }
        /// <summary>Whether a secret is set — never the secret.</summary>
        public bool? HasSigningSecret { get; init; }
        public bool? ImportOnLogin { get; init; }
        public long? TimeoutMs { get; init; }
        public string? Strategy { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long? CreatedAt { get; init; }
        /// <summary>Updated (epoch ms).</summary>
        public long? UpdatedAt { get; init; }
        /// <summary>Present and false when no connection exists yet.</summary>
        public bool? Configured { get; init; }
    }

    public sealed class DeletedLegacyAuthConnection
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public bool? Deleted { get; init; }
    }

    /// <summary>The lazy-migration (legacy-auth / custom database) connection (<c>/v1/legacy_auth_connection</c>).</summary>
    public sealed class LegacyAuthConnectionResource : ResourceBase
    {
        public LegacyAuthConnectionResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/legacy_auth_connection</c> — Read the instance's lazy-migration (legacy-auth / custom-DB) connection.</summary>
        public Task<LegacyAuthConnectionConfig> GetAsync(CancellationToken ct = default)
            => Req<LegacyAuthConnectionConfig>(HttpVerb.Get, "/v1/legacy_auth_connection", cancellationToken: ct);

        /// <summary><c>PUT /v1/legacy_auth_connection</c> — Create or replace the instance's lazy-migration connection (verify_url + signing_secret, enabled, import_on_login, timeout_ms).</summary>
        public Task<LegacyAuthConnectionConfig> ReplaceAsync(LegacyAuthConnectionBody body, CancellationToken ct = default)
            => Req<LegacyAuthConnectionConfig>(HttpVerb.Put, "/v1/legacy_auth_connection", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/legacy_auth_connection</c> — Delete the instance's lazy-migration (legacy-auth / custom-DB) connection.</summary>
        public Task<DeletedLegacyAuthConnection> DeleteAsync(CancellationToken ct = default)
            => Req<DeletedLegacyAuthConnection>(HttpVerb.Delete, "/v1/legacy_auth_connection", cancellationToken: ct);
    }
}
