using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/migrations/{source}/imports</c>. Only the fields set are sent.</summary>
    public sealed class MigrationsImportsCreateBody
    {
        /// <summary>What the import does. Every field is optional; an unknown field is refused. A `better_auth` import takes the Better Auth options (see /v1/better_auth_imports); every other source takes these.</summary>
        [JsonPropertyName("options")] public Dictionary<string, object?>? Options { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/migrations/{source}/imports/{id}/parts/{part}</c>. Only the fields set are sent.</summary>
    public sealed class MigrationsImportsPutPartBody
    {
        /// <summary>Required. Lines of the sealed bundle file.</summary>
        [JsonPropertyName("data")] public string? Data { get; set; }
    }

    /// <summary>The body of <c>POST /v1/migrations/{source}/imports/{id}/dry_run</c>. Only the fields set are sent.</summary>
    public sealed class MigrationsImportsDryRunBody
    {
        /// <summary>What the import does. Every field is optional; an unknown field is refused. A `better_auth` import takes the Better Auth options (see /v1/better_auth_imports); every other source takes these.</summary>
        [JsonPropertyName("options")] public Dictionary<string, object?>? Options { get; set; }
    }

    /// <summary>The body of <c>POST /v1/migrations/{source}/imports/{id}/apply</c>. Only the fields set are sent.</summary>
    public sealed class MigrationsImportsApplyBody
    {
        [JsonPropertyName("confirm_conflicts")] public bool? ConfirmConflicts { get; set; }
    }

    /// <summary>Operations of <c>migrations.imports</c>.</summary>
    public sealed partial class MigrationsImportsResource : ResourceBase
    {
        public MigrationsImportsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/migrations/{source}/imports/public_key</c> - The instance's import public key (created on first read). Seal the export to it; only this instance can open the bundle. One key serves every source. A key with jobs:write may read it too.</summary>
        /// <remarks>Needs the <c>jobs:read</c> scope.</remarks>
        public Task<JsonElement> GetPublicKeyAsync(string source, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/migrations/{Enc(source)}/imports/public_key", cancellationToken: ct);

        /// <summary><c>POST /v1/migrations/{source}/imports/public_key/rotate</c> - Replace the import key. A bundle sealed to the old key no longer finalizes; export again with the new key.</summary>
        /// <remarks>Needs the <c>jobs:write</c> scope.</remarks>
        public Task<JsonElement> RotatePublicKeyAsync(string source, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/migrations/{Enc(source)}/imports/public_key/rotate", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/migrations/{source}/imports</c> - Start an import from `source` (status `uploading`). Then upload the sealed bundle with PUT …/parts/{part} and call …/finalize. Nothing is written to users until apply.</summary>
        /// <remarks>Needs the <c>jobs:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(string source, MigrationsImportsCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/migrations/{Enc(source)}/imports", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/migrations/{source}/imports</c> - List the imports from `source`, newest first (cursor pagination).</summary>
        /// <remarks>Needs the <c>jobs:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string source, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/migrations/{Enc(source)}/imports", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/migrations/{source}/imports/{id}</c> - Fetch one import: status, the dry-run report, live progress and the apply result.</summary>
        /// <remarks>Needs the <c>jobs:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string source, string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/migrations/{Enc(source)}/imports/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/migrations/{source}/imports/{id}</c> - Delete an import and its stored bundle. Data it already imported stays. Refused (409) while analyzing or applying.</summary>
        /// <remarks>Needs the <c>jobs:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string source, string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/migrations/{Enc(source)}/imports/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>PUT /v1/migrations/{source}/imports/{id}/parts/{part}</c> - Upload part `part` (1, 2, …) of the sealed bundle: whole lines, at most 10 MiB, as `{ "data": "…" }` or a text/plain body. Re-sending a part replaces it. Only while `uploading`.</summary>
        /// <remarks>Needs the <c>jobs:write</c> scope.</remarks>
        public Task<JsonElement> PutPartAsync(string source, string id, string part, MigrationsImportsPutPartBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/migrations/{Enc(source)}/imports/{Enc(id)}/parts/{Enc(part)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/migrations/{source}/imports/{id}/finalize</c> - Close the upload: every part 1..n present, the bundle sealed to the active import key and in a format `source` reads (else 400 or status `invalid`). The dry run follows: inline up to 4 MiB (200, `ready`), else in the background (202, `analyzing`; poll GET).</summary>
        /// <remarks>Needs the <c>jobs:write</c> scope.</remarks>
        public Task<JsonElement> FinalizeAsync(string source, string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/migrations/{Enc(source)}/imports/{Enc(id)}/finalize", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/migrations/{source}/imports/{id}/dry_run</c> - Run the dry run again, optionally with new options (they replace the stored ones). Reads only.</summary>
        /// <remarks>Needs the <c>jobs:write</c> scope.</remarks>
        public Task<JsonElement> DryRunAsync(string source, string id, MigrationsImportsDryRunBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/migrations/{Enc(source)}/imports/{Enc(id)}/dry_run", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/migrations/{source}/imports/{id}/apply</c> - Import the bundle. Every record is an idempotent upsert keyed by its source id, so applying a failed import again resumes where it stopped. Up to 1,000 remaining records run inline (200), more are queued (202). With conflicts in the dry run, send confirm_conflicts: true.</summary>
        /// <remarks>Needs the <c>jobs:write</c> scope.</remarks>
        public Task<JsonElement> ApplyAsync(string source, string id, MigrationsImportsApplyBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/migrations/{Enc(source)}/imports/{Enc(id)}/apply", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
