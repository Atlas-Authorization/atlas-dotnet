using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The old app's live-session fallback (an unknown token is checked against the old Better Auth app).</summary>
    public sealed class BetterAuthLiveFallbackBody
    {
        public bool? Enabled { get; set; }
        /// <summary>The old app's Better Auth base URL including its base path (<c>https://app.example.com/api/auth</c>).</summary>
        public string? BaseUrl { get; set; }
        /// <summary>The old app's <c>BETTER_AUTH_SECRET</c>. Write-only: omit to keep, <c>Set("secret", null)</c> to clear.</summary>
        public string? Secret { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c> (clears <c>base_url</c> / <c>secret</c>).</summary>
        public BetterAuthLiveFallbackBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>Bridge settings, merged onto the stored ones; every field is optional.</summary>
    public sealed class UpdateBetterAuthBridgeBody
    {
        public bool? Enabled { get; set; }
        /// <summary>End of the migration window (ISO-8601 string or epoch ms number); <c>Set("ends_at", null)</c> = open until disabled.</summary>
        public object? EndsAt { get; set; }
        /// <summary>Better Auth's <c>advanced.cookiePrefix</c> (default <c>better-auth</c>).</summary>
        public string? CookiePrefix { get; set; }
        public BetterAuthLiveFallbackBody? LiveFallback { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>.</summary>
        public UpdateBetterAuthBridgeBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class BetterAuthLiveFallback
    {
        public bool? Enabled { get; init; }
        public string? BaseUrl { get; init; }
        /// <summary>Whether a secret is stored — never the secret.</summary>
        public bool? HasSecret { get; init; }
    }

    public sealed class BetterAuthBridgeStats
    {
        public long? Exchanged { get; init; }
        public long? FromImport { get; init; }
        public long? FromLive { get; init; }
    }

    public sealed class BetterAuthBridgeSettings
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public bool? Enabled { get; init; }
        public long? EndsAt { get; init; }
        public string? CookiePrefix { get; init; }
        public BetterAuthLiveFallback? LiveFallback { get; init; }
        public string? ExchangePath { get; init; }
        public BetterAuthBridgeStats? Stats { get; init; }
        public long? CreatedAt { get; init; }
        public long? UpdatedAt { get; init; }
    }

    public sealed class BetterAuthBridgeDeleted
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public bool? Deleted { get; init; }
    }

    /// <summary>The raw exchange answer: <c>session_tokens</c>, or a <c>sign_in_attempt</c> when a second factor is owed.</summary>
    public sealed class BetterAuthExchangeResponse
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        /// <summary>sign_in_attempt only: the attempt to continue.</summary>
        public string? Id { get; init; }
        public string? SessionId { get; init; }
        public string? UserId { get; init; }
        public string? Jwt { get; init; }
        public string? RefreshToken { get; init; }
        public long? ExpiresIn { get; init; }
        /// <summary><c>needs_second_factor</c> or <c>needs_mfa_enrollment</c> (sign_in_attempt only).</summary>
        public string? Status { get; init; }
    }

    /// <summary>
    /// The Better Auth session bridge: during a migration window a returning user's Better Auth session is
    /// exchanged, once, for an Atlas session. See <see cref="BetterAuthSessionBridge"/> and the ASP.NET Core
    /// <c>UseAtlasBetterAuthBridge</c> middleware.
    /// </summary>
    public sealed class BetterAuthBridgeResource : ResourceBase
    {
        public BetterAuthBridgeResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/migrations/better_auth/bridge</c> — The bridge settings and exchange counts (never the secret).</summary>
        public Task<BetterAuthBridgeSettings> GetAsync(CancellationToken ct = default)
            => Req<BetterAuthBridgeSettings>(HttpVerb.Get, "/v1/migrations/better_auth/bridge", cancellationToken: ct);

        /// <summary><c>PUT /v1/migrations/better_auth/bridge</c> — Create or update the settings (merged onto the stored ones; the old app's secret is write-only).</summary>
        public Task<BetterAuthBridgeSettings> UpdateAsync(UpdateBetterAuthBridgeBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<BetterAuthBridgeSettings>(HttpVerb.Put, "/v1/migrations/better_auth/bridge", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/migrations/better_auth/bridge</c> — Remove the settings: the bridge is off and the stored secret is deleted.</summary>
        public Task<BetterAuthBridgeDeleted> DeleteAsync(string? idempotencyKey = null, CancellationToken ct = default)
            => Req<BetterAuthBridgeDeleted>(HttpVerb.Delete, "/v1/migrations/better_auth/bridge", idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/migrations/better_auth/exchange</c> — Exchange a Better Auth session (the raw cookie value or the bare
        /// token) for an Atlas session, once. <paramref name="clientIp"/> / <paramref name="userAgent"/> are the END USER's,
        /// forwarded (<c>X-Atlas-Client-IP</c>, <c>X-Atlas-Client-User-Agent</c>) so Atlas's guards, risk and geo checks and
        /// the device record see the user rather than your server. Throws <see cref="AtlasException"/> on refusal — use
        /// <see cref="BetterAuthExchange.ExchangeAsync"/> for a classified result.
        /// </summary>
        public Task<BetterAuthExchangeResponse> ExchangeAsync(string token, string? clientIp = null, string? userAgent = null, bool? remember = null, CancellationToken ct = default)
        {
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(clientIp)) headers["X-Atlas-Client-IP"] = clientIp!;
            if (!string.IsNullOrEmpty(userAgent)) headers["X-Atlas-Client-User-Agent"] = userAgent!;
            var body = Body(("token", token));
            if (remember == false) body["remember"] = false;
            return Req<BetterAuthExchangeResponse>(HttpVerb.Post, "/v1/migrations/better_auth/exchange", body, cancellationToken: ct, headers: headers);
        }
    }
}
