using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

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

        /// <summary><c>POST /v1/api_keys/token</c> — Mint a short-lived, JWKS-signed access JWT (token_use:api_key, jti) whose exp is clamped to the smallest of the request, the key expiry and the org ceiling; scopes + constraints are embedded so a resource verifies it OFFLINE against the instance JWKS.</summary>
        public Task<ApiKeyToken> MintTokenAsync(MintApiKeyTokenBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ApiKeyToken>(HttpVerb.Post, "/v1/api_keys/token", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/api_keys/revoked</c> — Revocation feed: the keys revoked since a cursor (since=&lt;iso|cursor&gt;), so a resource can deny-list revoked key ids while their short-lived delegated tokens are still live.</summary>
        public Task<ListPage<RevokedApiKey>> ListRevokedAsync(string? since = null, int? limit = null, CancellationToken ct = default)
            => Req<ListPage<RevokedApiKey>>(HttpVerb.Get, "/v1/api_keys/revoked", query: Q(("since", since), ("limit", limit)), cancellationToken: ct);
    }

    public sealed class MintApiKeyTokenBody
    {
        public string? Secret { get; set; }
        public double? TtlSeconds { get; set; }
        public object? Audience { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public MintApiKeyTokenBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class ApiKeyToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>The short-lived JWKS-signed access JWT (token_use:api_key).</summary>
        public string Token { get; init; } = "";
        public string TokenType { get; init; } = "";
        /// <summary>Unique token id — deny-list this to revoke one token.</summary>
        public string Jti { get; init; } = "";
        /// <summary>The long-lived key this token was minted from (the JWT sub).</summary>
        public string KeyId { get; init; } = "";
        public string? SubjectType { get; init; }
        public string? SubjectId { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        public Dictionary<string, object?> Constraints { get; init; } = new Dictionary<string, object?>();
        /// <summary>Proof-of-possession confirmation — present only for a PoP key.</summary>
        public ApiKeyTokenCnf? Cnf { get; init; }
        /// <summary>Lifetime in seconds (clamped to key expiry + org ceiling + global cap).</summary>
        public long ExpiresIn { get; init; }
        /// <summary>Expiry (epoch ms).</summary>
        public long ExpiresAt { get; init; }
        /// <summary>The issuer/JWKS a verifier checks this token against.</summary>
        public string Issuer { get; init; } = "";
    }

    /// <summary>Proof-of-possession confirmation — present only for a PoP key.</summary>
    public sealed class ApiKeyTokenCnf
    {
        public string? Jkt { get; init; }
    }

    public sealed class RevokedApiKey
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public long? RevokedAt { get; init; }
    }
}
