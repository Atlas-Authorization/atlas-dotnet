using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Operations of <c>directoryPullSources.syncRuns</c>.</summary>
    public sealed partial class DirectoryPullSourcesSyncRunsResource : ResourceBase
    {
        public DirectoryPullSourcesSyncRunsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/directory_pull_sources/{id}/sync_runs</c> - Queue a pull now. One run at a time per source (409 otherwise); the worker executes it.</summary>
        /// <remarks>Needs the <c>directory_pull:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/directory_pull_sources/{Enc(id)}/sync_runs", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/directory_pull_sources/{id}/sync_runs</c> - List the runs of a source with counts of people seen, created, re-roled, disabled and re-enabled.</summary>
        /// <remarks>Needs the <c>directory_pull:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/directory_pull_sources/{Enc(id)}/sync_runs", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/directory_pull_sources/{id}/sync_runs/{runId}</c> - Read one directory pull run with its counts and error.</summary>
        /// <remarks>Needs the <c>directory_pull:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, string runId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/directory_pull_sources/{Enc(id)}/sync_runs/{Enc(runId)}", cancellationToken: ct);
    }
}
