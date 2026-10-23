using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The X25519 key the Better Auth exporter seals a bundle to.</summary>
    public sealed class BetterAuthImportKey
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Kid { get; init; }
        /// <summary>base64url raw X25519 public key.</summary>
        public string? PublicKey { get; init; }
        public string? Alg { get; init; }
        /// <summary><c>active</c> or <c>retired</c>.</summary>
        public string? Status { get; init; }
        /// <summary>SHA-256 of the raw key, first 16 bytes as hex pairs.</summary>
        public string? Fingerprint { get; init; }
        /// <summary><c>kid:public_key</c> — the form the exporter's <c>--public-key</c> takes.</summary>
        public string? Compact { get; init; }
        public long? CreatedAt { get; init; }
        public long? RetiredAt { get; init; }
    }

    /// <summary>Import options (all optional); also sent verbatim on <c>dry_run</c>.</summary>
    public sealed class BetterAuthImportOptions
    {
        public List<string>? Models { get; set; }
        /// <summary><c>skip</c> or <c>link</c>.</summary>
        public string? OnEmailConflict { get; set; }
        public string? DefaultRole { get; set; }
        public bool? CreateAppRoles { get; set; }
        public bool? ImportOauthTokens { get; set; }
        /// <summary><c>skip</c> or <c>import</c>.</summary>
        public string? PasskeysOnRpMismatch { get; set; }
        public bool? ImportSubscriptions { get; set; }
        public Dictionary<string, string>? PlanMap { get; set; }
        public bool? ImportJwks { get; set; }
        /// <summary>1–365, default 30.</summary>
        public long? JwksCutoverDays { get; set; }
        public bool? ImportSessions { get; set; }
        public bool? RetainBundle { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is.</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any option by its wire name.</summary>
        public BetterAuthImportOptions Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class BetterAuthImportActionItem
    {
        public string? Code { get; init; }
        /// <summary><c>required</c> (blocks those rows), <c>recommended</c> or <c>info</c>.</summary>
        public string? Severity { get; init; }
        public string? Message { get; init; }
        public long? Count { get; init; }
        public List<string>? Items { get; init; }
    }

    public sealed class BetterAuthImportProgress
    {
        public long? Total { get; init; }
        public long? Processed { get; init; }
        public long? Succeeded { get; init; }
        public long? Failed { get; init; }
        public long? Percent { get; init; }
    }

    public sealed class BetterAuthImportError
    {
        /// <summary>bundle_malformed | bundle_unsupported | bundle_wrong_key | bundle_tampered | bundle_truncated | bundle_count_mismatch | import_key_retired | analysis_failed | apply_failed</summary>
        public string? Code { get; init; }
        public string? Message { get; init; }
    }

    /// <summary>One Better Auth bundle import: <c>uploading → analyzing → ready | invalid → queued → applying → completed | failed</c>.</summary>
    public sealed class BetterAuthImport
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        /// <summary>uploading, analyzing, ready, invalid, queued, applying, completed or failed.</summary>
        public string? Status { get; init; }
        public string? KeyId { get; init; }
        public long? Parts { get; init; }
        public long? Bytes { get; init; }
        public string? BundleSha256 { get; init; }
        public bool? BundlePurged { get; init; }
        public Dictionary<string, JsonElement>? Options { get; init; }
        /// <summary>The exporter's manifest (Better Auth version, plugins, models, rp_id, ...).</summary>
        public Dictionary<string, JsonElement>? Source { get; init; }
        public long? Total { get; init; }
        public BetterAuthImportProgress? Progress { get; init; }
        /// <summary>The dry run: <c>models</c>, <c>skipped</c>, <c>conflicts</c>, <c>action_items</c>, ...</summary>
        public Dictionary<string, JsonElement>? Report { get; init; }
        /// <summary>The apply outcome: per-model created/updated/unchanged/skipped/failed.</summary>
        public Dictionary<string, JsonElement>? Result { get; init; }
        public BetterAuthImportError? Error { get; init; }
        public long? CreatedAt { get; init; }
        public long? UpdatedAt { get; init; }
        public long? AnalyzedAt { get; init; }
        public long? ApplyStartedAt { get; init; }
        public long? CompletedAt { get; init; }
    }

    public sealed class BetterAuthImportPart
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? ImportId { get; init; }
        public long? Part { get; init; }
        public long? Bytes { get; init; }
        public string? Sha256 { get; init; }
    }

    public sealed class BetterAuthImportDeleted
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public bool? Deleted { get; init; }
    }

    /// <summary>A Better Auth JWT key published in the instance JWKS for the cutover.</summary>
    public sealed class ImportedJwk
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Kid { get; init; }
        public string? Source { get; init; }
        public Dictionary<string, JsonElement>? Jwk { get; init; }
        public string? ImportId { get; init; }
        public long? ExpiresAt { get; init; }
        public long? CreatedAt { get; init; }
    }

    public sealed class ImportedJwkDeleted
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Kid { get; init; }
        public bool? Deleted { get; init; }
    }

    /// <summary>
    /// Migrate from Better Auth: import a sealed bundle made by the Better Auth exporter. (1)
    /// <see cref="GetPublicKeyAsync"/> — the key the exporter seals to. (2) <see cref="UploadAsync"/> (or
    /// <see cref="CreateAsync"/>, <see cref="UploadPartAsync(string, int, string, CancellationToken)"/> per part and
    /// <see cref="FinalizeAsync"/>) — Atlas opens the bundle and runs the dry run. (3) Read the import's
    /// <c>Report</c>, then <see cref="ApplyAsync"/>; applying again after a failure resumes where it stopped.
    /// </summary>
    public sealed class BetterAuthImportsResource : ResourceBase
    {
        /// <summary>Bytes per uploaded bundle part (the API takes at most 10 MiB per part).</summary>
        public const int PartBytes = 8 * 1024 * 1024;

        /// <summary>Statuses an import stays in while Atlas is working on it (<see cref="WaitForAsync"/> polls through them).</summary>
        public static readonly string[] InFlightStatuses = { "analyzing", "queued", "applying" };

        public BetterAuthImportsResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/better_auth_imports/public_key</c> — The instance's import public key (created on first read).</summary>
        public Task<BetterAuthImportKey> GetPublicKeyAsync(CancellationToken ct = default)
            => Req<BetterAuthImportKey>(HttpVerb.Get, "/v1/better_auth_imports/public_key", cancellationToken: ct);

        /// <summary><c>POST /v1/better_auth_imports/public_key/rotate</c> — Replace the import key; bundles sealed to the old one no longer finalize.</summary>
        public Task<BetterAuthImportKey> RotatePublicKeyAsync(string? idempotencyKey = null, CancellationToken ct = default)
            => Req<BetterAuthImportKey>(HttpVerb.Post, "/v1/better_auth_imports/public_key/rotate", idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/better_auth_imports</c> — Start an import (status <c>uploading</c>), optionally with options.</summary>
        public Task<BetterAuthImport> CreateAsync(BetterAuthImportOptions? options = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<BetterAuthImport>(HttpVerb.Post, "/v1/better_auth_imports", OptionsBody(options), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/better_auth_imports</c> — List imports, newest first.</summary>
        public Task<CursorPage<BetterAuthImport>> ListAsync(int? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<CursorPage<BetterAuthImport>>(HttpVerb.Get, "/v1/better_auth_imports", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/better_auth_imports/{id}</c> — Fetch an import: status, progress, the dry-run report and the result.</summary>
        public Task<BetterAuthImport> GetAsync(string importId, CancellationToken ct = default)
            => Req<BetterAuthImport>(HttpVerb.Get, $"/v1/better_auth_imports/{Enc(importId)}", cancellationToken: ct);

        /// <summary><c>PUT /v1/better_auth_imports/{id}/parts/{part}</c> — Upload part <paramref name="part"/> (1-based): whole lines, at most 10 MiB. Re-sending a part replaces it.</summary>
        public Task<BetterAuthImportPart> UploadPartAsync(string importId, int part, string data, CancellationToken ct = default)
            => Req<BetterAuthImportPart>(HttpVerb.Put, $"/v1/better_auth_imports/{Enc(importId)}/parts/{part}", Body(("data", data)), cancellationToken: ct);

        /// <summary><c>PUT /v1/better_auth_imports/{id}/parts/{part}</c> — The same, with the part's raw UTF-8 bytes sent as <c>text/plain</c>.</summary>
        public Task<BetterAuthImportPart> UploadPartAsync(string importId, int part, byte[] data, CancellationToken ct = default)
            => ReqBytes<BetterAuthImportPart>(HttpVerb.Put, $"/v1/better_auth_imports/{Enc(importId)}/parts/{part}", data, "text/plain; charset=utf-8", cancellationToken: ct);

        /// <summary><c>POST /v1/better_auth_imports/{id}/finalize</c> — Close the upload; Atlas checks the parts and key, then runs the dry run (inline up to 4 MiB, <c>analyzing</c> above that).</summary>
        public Task<BetterAuthImport> FinalizeAsync(string importId, CancellationToken ct = default)
            => Req<BetterAuthImport>(HttpVerb.Post, $"/v1/better_auth_imports/{Enc(importId)}/finalize", cancellationToken: ct);

        /// <summary><c>POST /v1/better_auth_imports/{id}/dry_run</c> — Run the dry run again; new options replace the stored ones.</summary>
        public Task<BetterAuthImport> DryRunAsync(string importId, BetterAuthImportOptions? options = null, CancellationToken ct = default)
            => Req<BetterAuthImport>(HttpVerb.Post, $"/v1/better_auth_imports/{Enc(importId)}/dry_run", OptionsBody(options), cancellationToken: ct);

        /// <summary><c>POST /v1/better_auth_imports/{id}/apply</c> — Import (or resume a failed import from its cursor). Pass <paramref name="confirmConflicts"/> when the dry run reported conflicts.</summary>
        public Task<BetterAuthImport> ApplyAsync(string importId, bool? confirmConflicts = null, CancellationToken ct = default)
            => Req<BetterAuthImport>(HttpVerb.Post, $"/v1/better_auth_imports/{Enc(importId)}/apply", Body(("confirm_conflicts", confirmConflicts)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/better_auth_imports/{id}</c> — Remove the import and its stored bundle; data already imported stays.</summary>
        public Task<BetterAuthImportDeleted> DeleteAsync(string importId, CancellationToken ct = default)
            => Req<BetterAuthImportDeleted>(HttpVerb.Delete, $"/v1/better_auth_imports/{Enc(importId)}", cancellationToken: ct);

        /// <summary><c>GET /v1/better_auth_imports/jwks</c> — The Better Auth JWT keys Atlas publishes in the instance JWKS for the cutover.</summary>
        public Task<CursorPage<ImportedJwk>> ListJwksAsync(CancellationToken ct = default)
            => Req<CursorPage<ImportedJwk>>(HttpVerb.Get, "/v1/better_auth_imports/jwks", cancellationToken: ct);

        /// <summary><c>DELETE /v1/better_auth_imports/jwks/{kid}</c> — Stop publishing one imported Better Auth JWT key.</summary>
        public Task<ImportedJwkDeleted> DeleteJwkAsync(string kid, CancellationToken ct = default)
            => Req<ImportedJwkDeleted>(HttpVerb.Delete, $"/v1/better_auth_imports/jwks/{Enc(kid)}", cancellationToken: ct);

        /// <summary>
        /// Create an import, upload the sealed bundle in line-aligned parts of at most <paramref name="partBytes"/>
        /// and finalize it. Returns the finalized import (<c>ready</c> / <c>invalid</c>, or <c>analyzing</c> for a
        /// large bundle — see <see cref="WaitForAsync"/>). <paramref name="onPart"/> receives (part, total).
        /// </summary>
        public async Task<BetterAuthImport> UploadAsync(
            string bundle,
            BetterAuthImportOptions? options = null,
            int partBytes = PartBytes,
            Action<int, int>? onPart = null,
            CancellationToken ct = default)
        {
            var parts = SplitBundleParts(bundle, partBytes);
            if (parts.Count == 0) throw new ArgumentException("The bundle is empty.", nameof(bundle));
            var created = await CreateAsync(options, null, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(created.Id)) throw new InvalidOperationException("Atlas did not return an import id.");
            for (var i = 0; i < parts.Count; i++)
            {
                await UploadPartAsync(created.Id!, i + 1, parts[i], ct).ConfigureAwait(false);
                onPart?.Invoke(i + 1, parts.Count);
            }
            return await FinalizeAsync(created.Id!, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Poll until the import leaves <c>analyzing</c> / <c>queued</c> / <c>applying</c> and return it
        /// (<c>ready</c>, <c>invalid</c>, <c>completed</c> or <c>failed</c>). Throws <see cref="TimeoutException"/> after
        /// <paramref name="timeout"/> (default 6 hours); <paramref name="interval"/> defaults to 2 seconds.
        /// </summary>
        public async Task<BetterAuthImport> WaitForAsync(
            string importId,
            TimeSpan? interval = null,
            TimeSpan? timeout = null,
            Action<BetterAuthImport>? onProgress = null,
            CancellationToken ct = default)
        {
            var wait = interval ?? TimeSpan.FromSeconds(2);
            var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromHours(6));
            while (true)
            {
                var imp = await GetAsync(importId, ct).ConfigureAwait(false);
                onProgress?.Invoke(imp);
                if (Array.IndexOf(InFlightStatuses, imp.Status) < 0) return imp;
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException($"Timed out waiting for Better Auth import {importId} (status {imp.Status}).");
                await Task.Delay(wait, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Split a sealed Better Auth bundle into upload parts of at most <paramref name="maxBytes"/> (UTF-8), each
        /// ending on a line boundary. Empty lines are dropped; a single line longer than <paramref name="maxBytes"/>
        /// becomes a part of its own (the API refuses parts over 10 MiB, so keep it at or under the default).
        /// </summary>
        public static List<string> SplitBundleParts(string bundle, int maxBytes = PartBytes)
        {
            if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes), "maxBytes must be positive.");
            var parts = new List<string>();
            var current = new StringBuilder();
            long size = 0;
            foreach (var raw in (bundle ?? "").Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0) continue;
                var n = Encoding.UTF8.GetByteCount(line) + 1;
                if (size > 0 && size + n > maxBytes)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    size = 0;
                }
                current.Append(line).Append('\n');
                size += n;
            }
            if (current.Length > 0) parts.Add(current.ToString());
            return parts;
        }

        private static Dictionary<string, object?> OptionsBody(BetterAuthImportOptions? options)
            => Body(("options", options));
    }
}
