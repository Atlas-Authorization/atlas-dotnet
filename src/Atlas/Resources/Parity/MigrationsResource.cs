using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/migrations/{source}/exchange</c>. Only the fields set are sent.</summary>
    public sealed class MigrationsExchangeBody
    {
        /// <summary>Required. The old session token (JWT or opaque), as the middleware extracted it.</summary>
        [JsonPropertyName("token")] public string? Token { get; set; }
        /// <summary>false records a browser-session (non-persistent) session. Default true.</summary>
        [JsonPropertyName("remember")] public bool? Remember { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/migrations/{source}/bridge</c>. Only the fields set are sent.</summary>
    public sealed class MigrationsSetBridgeBody
    {
        /// <summary>Whether the exchange accepts old sessions. Default false.</summary>
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>End of the migration window (ISO-8601 or Unix ms); null = open until disabled.</summary>
        [JsonPropertyName("ends_at")] public object? EndsAt { get; set; }
        /// <summary>jwt: the old session is a vendor JWT. session_table: an opaque token matched against imported sessions. remote: the vendor is asked about the token with the preset's call (vendors whose sessions only they can check; needs a preset with a remote check). Default: the preset's.</summary>
        [JsonPropertyName("mode")] public string? Mode { get; set; }
        /// <summary>A bridge preset (GET /v1/migrations/sources lists them): fills issuer, keys URL, audience, algorithms, claims and cookie names from preset_vars.</summary>
        [JsonPropertyName("preset")] public string? Preset { get; set; }
        /// <summary>The preset's variables, e.g. {"domain": "acme.us.auth0.com"} or {"region": "us-east-1", "user_pool_id": "…", "client_id": "…"}.</summary>
        [JsonPropertyName("preset_vars")] public Dictionary<string, object?>? PresetVars { get; set; }
        /// <summary>Override: the `iss` the old tokens carry.</summary>
        [JsonPropertyName("issuer")] public string? Issuer { get; set; }
        /// <summary>Override: where the old signing keys are (public https on production).</summary>
        [JsonPropertyName("jwks_url")] public string? JwksUrl { get; set; }
        /// <summary>Override: accepted `aud` values; null = unchecked.</summary>
        [JsonPropertyName("audience")] public List<string>? Audience { get; set; }
        [JsonPropertyName("algorithms")] public List<string>? Algorithms { get; set; }
        [JsonPropertyName("claim_map")] public Dictionary<string, object?>? ClaimMap { get; set; }
        /// <summary>Accept a validly signed token this long past exp (still single use). For vendors whose cookie token is refreshed by their client SDK only.</summary>
        [JsonPropertyName("expired_grace_seconds")] public long? ExpiredGraceSeconds { get; set; }
        /// <summary>Where the SDK middleware finds the token (* wildcards). Default: the preset's.</summary>
        [JsonPropertyName("cookie_names")] public List<string>? CookieNames { get; set; }
        /// <summary>Create an Atlas user for a valid token whose subject was never imported (needs a verified email claim). Default false.</summary>
        [JsonPropertyName("create_users")] public bool? CreateUsers { get; set; }
        /// <summary>Write-only HS256 secret, for vendors that sign with one (Supabase legacy JWT secret). Omit to keep, null to clear.</summary>
        [JsonPropertyName("shared_secret")] public string? SharedSecret { get; set; }
    }

    /// <summary>Operations of <c>migrations</c>.</summary>
    public sealed partial class MigrationsResource : ResourceBase
    {
        public MigrationsResource(AtlasTransport t) : base(t) { }

        private MigrationsImportsResource? _imports;
        /// <summary>The <c>migrations.imports</c> operations.</summary>
        public MigrationsImportsResource Imports => _imports ??= new MigrationsImportsResource(Transport);

        /// <summary><c>POST /v1/migrations/{source}/exchange</c> - Server-to-server exchange of an old `source` session for an Atlas session, once (what migrationBridge({ source }) calls with the secret key). The token is verified (vendor JWT against the bridge's issuer and keys, or sha256 matched against imported sessions), mapped to the imported user, spent in the single-use ledger and then takes the normal sign-in path (guards, MFA policy, post_login Actions, audit). Returns tokens in the body, or a sign_in_attempt when a second factor is owed. Limited per instance (6000/min), per end-user IP (60/min) and per user (10/min). Send the end user's IP / user agent in X-Atlas-Client-IP / X-Atlas-Client-User-Agent.</summary>
        /// <remarks>Needs the <c>legacy_auth:write</c> scope.</remarks>
        public Task<JsonElement> ExchangeAsync(string source, MigrationsExchangeBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/migrations/{Enc(source)}/exchange", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/migrations/sources</c> - Every migration source: what it exports, whether passwords move (or need lazy migration), its bridge preset and variables, and its lazy-migration preset.</summary>
        /// <remarks>Needs the <c>jobs:read</c> scope.</remarks>
        public Task<JsonElement> SourcesAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/migrations/sources", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/migrations/{source}/bridge</c> - Read the zero-logout bridge settings for `source` (window, JWT preset / issuer / keys, claims, cookie names) and exchange counts. Never returns a shared secret.</summary>
        /// <remarks>Needs the <c>legacy_auth:read</c> scope.</remarks>
        public Task<JsonElement> GetBridgeAsync(string source, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/migrations/{Enc(source)}/bridge", cancellationToken: ct);

        /// <summary><c>PUT /v1/migrations/{source}/bridge</c> - Create or update the bridge settings for `source`. Every field is optional and merged onto the stored ones; a preset fills the JWT settings from its variables, explicit fields override it. The keys URL must be public https on a production instance. `better_auth` takes the Better Auth bridge body (see /v1/migrations/better_auth/bridge).</summary>
        /// <remarks>Needs the <c>legacy_auth:write</c> scope.</remarks>
        public Task<JsonElement> SetBridgeAsync(string source, MigrationsSetBridgeBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/migrations/{Enc(source)}/bridge", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/migrations/{source}/bridge</c> - Remove the bridge settings for `source` (the bridge is off and any stored secret deleted).</summary>
        /// <remarks>Needs the <c>legacy_auth:write</c> scope.</remarks>
        public Task<JsonElement> DeleteBridgeAsync(string source, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/migrations/{Enc(source)}/bridge", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
