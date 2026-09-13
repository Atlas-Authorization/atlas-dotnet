using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class ApiKey
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "api_key";
        public string Id { get; init; } = "";
        public string SubjectType { get; init; } = "";
        public string SubjectId { get; init; } = "";
        public string? Name { get; init; }
        /// <summary>Enough to tell two keys apart in a listing; not enough to use one.</summary>
        public string Prefix { get; init; } = "";
        public Dictionary<string, object?> Claims { get; init; } = new Dictionary<string, object?>();
        public long? LastUsedAt { get; init; }
        public long? ExpiresAt { get; init; }
        public long? RevokedAt { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>The mint response: the key plus its secret, revealed exactly once.</summary>
    public sealed class ApiKeyWithSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "api_key";
        public string Id { get; init; } = "";
        public string SubjectType { get; init; } = "";
        public string SubjectId { get; init; } = "";
        public string? Name { get; init; }
        public string Prefix { get; init; } = "";
        public Dictionary<string, object?> Claims { get; init; } = new Dictionary<string, object?>();
        public long? LastUsedAt { get; init; }
        public long? ExpiresAt { get; init; }
        public long? RevokedAt { get; init; }
        public long CreatedAt { get; init; }
        public string Secret { get; init; } = "";
        public string Note { get; init; } = "";
    }

    public sealed class CreateApiKeyBody
    {
        public string SubjectType { get; set; } = "";
        public string SubjectId { get; set; } = "";
        public string? Name { get; set; }
        public Dictionary<string, object?>? Claims { get; set; }
        /// <summary>Epoch ms; null (or omitted) never expires.</summary>
        public long? ExpiresAt { get; set; }
    }

    public sealed class UpdateApiKeyBody
    {
        public string? Name { get; set; }
        public Dictionary<string, object?>? Claims { get; set; }
        public long? ExpiresAt { get; set; }
    }

    /// <summary>
    /// The verify verdict. Every negative — unknown, malformed, revoked, expired,
    /// or a key whose subject was since deleted — resolves to the SAME
    /// <c>Valid == false</c>, so a caller learns nothing about which keys exist.
    /// The remaining fields are populated only when <see cref="Valid"/> is true.
    /// </summary>
    public sealed class ApiKeyVerification
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "api_key_verification";
        public bool Valid { get; init; }
        public string? Id { get; init; }
        public string? SubjectType { get; init; }
        public string? SubjectId { get; init; }
        public Dictionary<string, object?>? Claims { get; init; }
        public long? LastUsedAt { get; init; }
    }

    public sealed class ListApiKeysParams
    {
        public string? SubjectType { get; set; }
        public string? SubjectId { get; set; }

        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("subject_type", SubjectType);
            yield return new KeyValuePair<string, object?>("subject_id", SubjectId);
        }
    }

    /// <summary>The end-user API-keys namespace (<c>/v1/api_keys</c>).</summary>
    public sealed class ApiKeysResource : ResourceBase
    {
        public ApiKeysResource(AtlasTransport transport) : base(transport) { }

        /// <summary>Optionally narrowed to one subject.</summary>
        public Task<ListPage<ApiKey>> ListAsync(ListApiKeysParams? @params = null, CancellationToken ct = default)
            => Req<ListPage<ApiKey>>(HttpVerb.Get, "/v1/api_keys", query: (@params ?? new ListApiKeysParams()).ToQuery(), cancellationToken: ct);

        /// <summary>Mint a key. The secret is returned once, here.</summary>
        public Task<ApiKeyWithSecret> CreateAsync(CreateApiKeyBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ApiKeyWithSecret>(HttpVerb.Post, "/v1/api_keys", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Check a presented secret. Rate-limited — it is an online credential check.</summary>
        public Task<ApiKeyVerification> VerifyAsync(string secret, CancellationToken ct = default)
            => Req<ApiKeyVerification>(HttpVerb.Post, "/v1/api_keys/verify", body: Body(("secret", secret)), cancellationToken: ct);

        public Task<ApiKey> GetAsync(string id, CancellationToken ct = default)
            => Req<ApiKey>(HttpVerb.Get, $"/v1/api_keys/{Enc(id)}", cancellationToken: ct);

        public Task<ApiKey> UpdateAsync(string id, UpdateApiKeyBody body, CancellationToken ct = default)
            => Req<ApiKey>(HttpVerb.Patch, $"/v1/api_keys/{Enc(id)}", body, cancellationToken: ct);

        /// <summary>Revoke a key. It stays queryable but never authenticates again.</summary>
        public Task<RevokedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<RevokedObject>(HttpVerb.Delete, $"/v1/api_keys/{Enc(id)}", cancellationToken: ct);
    }
}
