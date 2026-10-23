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
        /// <summary>Per-key spend caps; null when the key has none.</summary>
        public List<ApiKeyLimit>? Limits { get; init; }
        /// <summary>Each cap with its current-window usage (empty when the key has no caps).</summary>
        public List<ApiKeyLimitState> Usage { get; init; } = new List<ApiKeyLimitState>();
        /// <summary>False for a disabled key (it verifies as invalid until re-enabled).</summary>
        public bool? Enabled { get; init; }
        /// <summary>The per-key verify throttle, or null.</summary>
        public ApiKeyRateLimit? RateLimit { get; init; }
        /// <summary>Uses left (each successful verify spends one); null = unlimited.</summary>
        public long? Remaining { get; init; }
        public List<string>? Scopes { get; init; }
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
        /// <summary>Per-key spend caps; null when the key has none.</summary>
        public List<ApiKeyLimit>? Limits { get; init; }
        /// <summary>Each cap with its current-window usage.</summary>
        public List<ApiKeyLimitState> Usage { get; init; } = new List<ApiKeyLimitState>();
    }

    public sealed class CreateApiKeyBody
    {
        public string SubjectType { get; set; } = "";
        public string SubjectId { get; set; } = "";
        public string? Name { get; set; }
        public Dictionary<string, object?>? Claims { get; set; }
        /// <summary>Epoch ms; null (or omitted) never expires.</summary>
        public long? ExpiresAt { get; set; }
        /// <summary>Per-key spend caps, metered with <see cref="ApiKeysResource.RecordUsageAsync(string, RecordApiKeyUsageBody, CancellationToken)"/>.</summary>
        public List<ApiKeyLimit>? Limits { get; set; }
        /// <summary>Alternative to <c>ExpiresAt</c>; without either the instance default lifetime applies.</summary>
        public long? ExpiresInSeconds { get; set; }
        /// <summary>A custom secret prefix, e.g. <c>acme</c> gives <c>acme_&lt;id&gt;_&lt;secret&gt;</c>.</summary>
        public string? Prefix { get; set; }
        /// <summary>False mints the key disabled.</summary>
        public bool? Enabled { get; set; }
        public ApiKeyRateLimit? RateLimit { get; set; }
        /// <summary>A use budget each successful verify spends.</summary>
        public long? Remaining { get; set; }
        public List<string>? Scopes { get; set; }
    }

    /// <summary>A per-key verify throttle: at most <see cref="Max"/> verifies per <see cref="WindowSeconds"/>.</summary>
    public sealed class ApiKeyRateLimit
    {
        public long Max { get; set; }
        public long WindowSeconds { get; set; }
    }

    public sealed class UpdateApiKeyBody
    {
        public string? Name { get; set; }
        public Dictionary<string, object?>? Claims { get; set; }
        public long? ExpiresAt { get; set; }
        /// <summary>Replace the key's spend caps. Use <see cref="ClearLimits"/> to remove them all.</summary>
        public List<ApiKeyLimit>? Limits { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public UpdateApiKeyBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }

        /// <summary>Remove every spend cap (sends <c>"limits": null</c>).</summary>
        public UpdateApiKeyBody ClearLimits()
        {
            Limits = null;
            return Set("limits", null);
        }
        /// <summary>False disables the key without revoking it; true re-enables it.</summary>
        public bool? Enabled { get; set; }
        /// <summary>Set the verify throttle; remove it with <c>Set("rate_limit", null)</c>.</summary>
        public ApiKeyRateLimit? RateLimit { get; set; }
        /// <summary>Set the use budget; make it unlimited with <c>Set("remaining", null)</c>.</summary>
        public long? Remaining { get; set; }
        public List<string>? Scopes { get; set; }
    }

    /// <summary>Spend-cap periods for <see cref="ApiKeyLimit.Period"/>.</summary>
    public static class ApiKeyLimitPeriods
    {
        /// <summary>Caps a single debit.</summary>
        public const string Request = "request";
        /// <summary>A UTC calendar day.</summary>
        public const string Day = "day";
        /// <summary>A UTC calendar month.</summary>
        public const string Month = "month";
        /// <summary>Never resets.</summary>
        public const string Total = "total";
    }

    /// <summary>A per-key spend cap, metered with <c>POST /v1/api_keys/{id}/usage</c>.</summary>
    public sealed class ApiKeyLimit
    {
        public ApiKeyLimit() { }

        public ApiKeyLimit(string unit, long amount, string period)
        {
            Unit = unit;
            Amount = amount;
            Period = period;
        }

        /// <summary>App-defined unit, e.g. <c>requests</c> or <c>usd_cents</c> (1-64 letters, digits, <c>_ . : -</c>).</summary>
        public string Unit { get; set; } = "";
        /// <summary>The cap, a non-negative integer in <see cref="Unit"/>.</summary>
        public long Amount { get; set; }
        /// <summary>One of <see cref="ApiKeyLimitPeriods"/>: request, day, month or total.</summary>
        public string Period { get; set; } = "";
    }

    /// <summary>A spend cap with its current-window usage.</summary>
    public sealed class ApiKeyLimitState
    {
        public string Unit { get; init; } = "";
        public string Period { get; init; } = "";
        public long Amount { get; init; }
        /// <summary>Used in the current window (always 0 for a per-request cap).</summary>
        public long Used { get; init; }
        /// <summary><c>Amount - Used</c>, never below 0 (the cap itself for a per-request cap).</summary>
        public long Remaining { get; init; }
        /// <summary>Epoch ms the current window started (UTC); null for a per-request cap.</summary>
        public long? WindowStart { get; init; }
        /// <summary>Epoch ms the current window ends (UTC); null for per-request and total caps.</summary>
        public long? ResetsAt { get; init; }
    }

    /// <summary>A raw usage counter for one unit in its current window.</summary>
    public sealed class ApiKeyUsageCounter
    {
        public string Unit { get; init; } = "";
        /// <summary>day, month or total.</summary>
        public string Period { get; init; } = "";
        public long Used { get; init; }
        /// <summary>Epoch ms the window started.</summary>
        public long WindowStart { get; init; }
        /// <summary>Epoch ms the window ends; null for total.</summary>
        public long? ResetsAt { get; init; }
    }

    /// <summary>Body for <c>POST /v1/api_keys/{id}/usage</c>.</summary>
    public sealed class RecordApiKeyUsageBody
    {
        /// <summary>A positive integer, in <see cref="Unit"/>.</summary>
        public long Amount { get; set; }
        /// <summary>App-defined unit, e.g. <c>requests</c> or <c>usd_cents</c>.</summary>
        public string Unit { get; set; } = "";
        /// <summary>
        /// Optional. The same key (kept 24 hours) returns the first outcome without counting again;
        /// reusing it with a different unit or amount is <c>409 IDEMPOTENCY_CONFLICT</c>.
        /// </summary>
        public string? IdempotencyKey { get; set; }
    }

    /// <summary>
    /// The outcome of <c>POST /v1/api_keys/{id}/usage</c>. A debit that would exceed a cap is NOT
    /// an exception: <see cref="Allowed"/> is false, <see cref="Code"/> is <c>LIMIT_EXCEEDED</c>
    /// and nothing was recorded.
    /// </summary>
    public sealed class ApiKeyUsageDebit
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "api_key_usage_debit";
        public string KeyId { get; init; } = "";
        public string Unit { get; init; } = "";
        public long Amount { get; init; }
        /// <summary>True when the debit was recorded.</summary>
        public bool Allowed { get; init; }
        /// <summary><c>LIMIT_EXCEEDED</c> when refused; null when allowed.</summary>
        public string? Code { get; init; }
        /// <summary>The caps that refused the debit (empty when allowed).</summary>
        public List<ApiKeyLimitState> Exceeded { get; init; } = new List<ApiKeyLimitState>();
        /// <summary>Every cap on the key after this call.</summary>
        public List<ApiKeyLimitState> Usage { get; init; } = new List<ApiKeyLimitState>();
        /// <summary>The debited unit's day, month and total counters after this call.</summary>
        public List<ApiKeyUsageCounter> Counters { get; init; } = new List<ApiKeyUsageCounter>();
        /// <summary>True when this is the stored outcome of an earlier call with the same idempotency key.</summary>
        public bool IdempotentReplay { get; init; }
    }

    /// <summary><c>GET /v1/api_keys/{id}/usage</c>: each cap with its current-window usage, plus raw counters.</summary>
    public sealed class ApiKeyUsage
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "api_key_usage";
        public string KeyId { get; init; } = "";
        /// <summary>The key's caps; null when it has none.</summary>
        public List<ApiKeyLimit>? Limits { get; init; }
        public List<ApiKeyLimitState> Usage { get; init; } = new List<ApiKeyLimitState>();
        /// <summary>Current-window counters for every unit the key has been debited in, capped or not.</summary>
        public List<ApiKeyUsageCounter> Counters { get; init; } = new List<ApiKeyUsageCounter>();
    }

    /// <summary>
    /// The <c>data</c> of the <c>api_key.created</c>, <c>api_key.updated</c>, <c>api_key.revoked</c> and
    /// <c>api_key.limit_reached</c> webhook events: the key's full non-secret configuration (the secret
    /// and its hash are never included). Deserialize it from <see cref="WebhookPayload.Data"/> with
    /// <see cref="AtlasJson.Options"/>.
    /// </summary>
    public sealed class ApiKeyWebhookData
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "api_key";
        public string Id { get; init; } = "";
        public string Prefix { get; init; } = "";
        public string SubjectType { get; init; } = "";
        public string SubjectId { get; init; } = "";
        public string? Name { get; init; }
        public Dictionary<string, object?>? Claims { get; init; }
        public List<string>? Scopes { get; init; }
        public Dictionary<string, object?>? Constraints { get; init; }
        public List<ApiKeyLimit>? Limits { get; init; }
        public long? MaxTokenLifetimeSeconds { get; init; }
        public long? ExpiresAt { get; init; }
        public long? RevokedAt { get; init; }
        public long? CreatedAt { get; init; }
        public long? UpdatedAt { get; init; }
        /// <summary>The cap (with its window usage) that filled or refused a debit — only on <c>api_key.limit_reached</c>.</summary>
        public ApiKeyLimitState? Limit { get; init; }
    }

    /// <summary>
    /// The verify verdict. Every negative — unknown, malformed, revoked, expired,
    /// or a key whose subject was since deleted — resolves to the SAME
    /// <c>Valid == false</c>, so a caller learns nothing about which keys exist.
    /// The remaining fields (incl. name, scopes, constraints, spend caps and their usage)
    /// are populated only when <see cref="Valid"/> is true.
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
        public string? Name { get; init; }
        public List<string>? Scopes { get; init; }
        public Dictionary<string, object?>? Constraints { get; init; }
        /// <summary>The key's spend caps; null when it has none.</summary>
        public List<ApiKeyLimit>? Limits { get; init; }
        /// <summary>Each cap with its current-window usage.</summary>
        public List<ApiKeyLimitState>? Usage { get; init; }
        /// <summary>On a refusal of a LIVE key by its own policy: <c>insufficient_scope</c>, <c>rate_limited</c> or <c>usage_exhausted</c>; null otherwise.</summary>
        public string? Code { get; init; }
        /// <summary>With <c>insufficient_scope</c>: the required scopes the key lacks.</summary>
        public List<string>? MissingScopes { get; init; }
        /// <summary>With <c>rate_limited</c>: when to retry (ms).</summary>
        public long? RetryAfterMs { get; init; }
        /// <summary>Uses left after this verify; null = unlimited.</summary>
        public long? Remaining { get; init; }
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

        /// <summary>Mint a key. The secret is returned once, here. <see cref="CreateApiKeyBody.Limits"/> sets per-key spend caps.</summary>
        public Task<ApiKeyWithSecret> CreateAsync(CreateApiKeyBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ApiKeyWithSecret>(HttpVerb.Post, "/v1/api_keys", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Check a presented secret. Rate-limited — it is an online credential check.</summary>
        public Task<ApiKeyVerification> VerifyAsync(string secret, CancellationToken ct = default)
            => Req<ApiKeyVerification>(HttpVerb.Post, "/v1/api_keys/verify", body: Body(("secret", secret)), cancellationToken: ct);

        /// <summary>
        /// Verify requiring <paramref name="requiredScopes"/>. A LIVE key refused by its own policy answers
        /// <c>Valid == false</c> with a <see cref="ApiKeyVerification.Code"/>.
        /// </summary>
        public Task<ApiKeyVerification> VerifyAsync(string secret, IEnumerable<string> requiredScopes, CancellationToken ct = default)
            => Req<ApiKeyVerification>(HttpVerb.Post, "/v1/api_keys/verify",
                body: Body(("secret", secret), ("required_scopes", new List<string>(requiredScopes))), cancellationToken: ct);

        public Task<ApiKey> GetAsync(string id, CancellationToken ct = default)
            => Req<ApiKey>(HttpVerb.Get, $"/v1/api_keys/{Enc(id)}", cancellationToken: ct);

        public Task<ApiKey> UpdateAsync(string id, UpdateApiKeyBody body, CancellationToken ct = default)
            => Req<ApiKey>(HttpVerb.Patch, $"/v1/api_keys/{Enc(id)}", body, cancellationToken: ct);

        /// <summary>Revoke a key. It stays queryable but never authenticates again.</summary>
        public Task<RevokedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<RevokedObject>(HttpVerb.Delete, $"/v1/api_keys/{Enc(id)}", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/api_keys/{id}/usage</c> — meter usage against the key's spend caps, atomically.
        /// When the debit fits every cap it is recorded and <see cref="ApiKeyUsageDebit.Allowed"/> is true;
        /// when it would exceed a cap nothing is recorded and the result is <c>Allowed == false</c> with
        /// <c>Code == "LIMIT_EXCEEDED"</c> (a 200, not an exception). Reusing the body's idempotency key with
        /// a different unit or amount throws <see cref="AtlasConflictException"/> (<c>IDEMPOTENCY_CONFLICT</c>).
        /// </summary>
        public Task<ApiKeyUsageDebit> RecordUsageAsync(string id, RecordApiKeyUsageBody body, CancellationToken ct = default)
            => Req<ApiKeyUsageDebit>(HttpVerb.Post, $"/v1/api_keys/{Enc(id)}/usage", body, cancellationToken: ct);

        /// <summary>Meter <paramref name="amount"/> of <paramref name="unit"/> against the key's spend caps.</summary>
        public Task<ApiKeyUsageDebit> RecordUsageAsync(string id, long amount, string unit, string? idempotencyKey = null, CancellationToken ct = default)
            => RecordUsageAsync(id, new RecordApiKeyUsageBody { Amount = amount, Unit = unit, IdempotencyKey = idempotencyKey }, ct);

        /// <summary><c>GET /v1/api_keys/{id}/usage</c> — each cap with used/remaining/resets_at, plus the day, month and total counters of every unit the key has been debited in.</summary>
        public Task<ApiKeyUsage> GetUsageAsync(string id, CancellationToken ct = default)
            => Req<ApiKeyUsage>(HttpVerb.Get, $"/v1/api_keys/{Enc(id)}/usage", cancellationToken: ct);

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
