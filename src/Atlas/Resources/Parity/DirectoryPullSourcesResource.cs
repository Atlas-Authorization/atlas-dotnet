using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/directory_pull_sources</c>. Only the fields set are sent.</summary>
    public sealed class DirectoryPullSourcesCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("organization_id")] public string? OrganizationId { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("provider")] public string? Provider { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("config")] public Dictionary<string, object?>? Config { get; set; }
        /// <summary>Write-only.</summary>
        [JsonPropertyName("credentials")] public Dictionary<string, object?>? Credentials { get; set; }
        [JsonPropertyName("default_role_id")] public string? DefaultRoleId { get; set; }
        /// <summary>Schedule, role mappings and disable-on-removal.</summary>
        [JsonPropertyName("settings")] public Dictionary<string, object?>? Settings { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/directory_pull_sources/{id}</c>. Only the fields set are sent.</summary>
    public sealed class DirectoryPullSourcesUpdateBody
    {
        [JsonPropertyName("organization_id")] public string? OrganizationId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("provider")] public string? Provider { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("config")] public Dictionary<string, object?>? Config { get; set; }
        /// <summary>Write-only.</summary>
        [JsonPropertyName("credentials")] public Dictionary<string, object?>? Credentials { get; set; }
        [JsonPropertyName("default_role_id")] public string? DefaultRoleId { get; set; }
        /// <summary>Schedule, role mappings and disable-on-removal.</summary>
        [JsonPropertyName("settings")] public Dictionary<string, object?>? Settings { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/directory_pull_sources/{id}/csv</c>. Only the fields set are sent.</summary>
    public sealed class DirectoryPullSourcesSetCsvBody
    {
        [JsonPropertyName("csv")] public string? Csv { get; set; }
    }

    /// <summary>Operations of <c>directoryPullSources</c>.</summary>
    public sealed partial class DirectoryPullSourcesResource : ResourceBase
    {
        public DirectoryPullSourcesResource(AtlasTransport t) : base(t) { }

        private DirectoryPullSourcesSyncRunsResource? _syncRuns;
        /// <summary>The <c>directoryPullSources.syncRuns</c> operations.</summary>
        public DirectoryPullSourcesSyncRunsResource SyncRuns => _syncRuns ??= new DirectoryPullSourcesSyncRunsResource(Transport);

        /// <summary><c>GET /v1/directory_pull_sources</c> - List HR / directory pull sources, optionally for one organization (organization_id). Credentials are never returned.</summary>
        /// <remarks>Needs the <c>directory_pull:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? organizationId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/directory_pull_sources", query: Q(("organization_id", organizationId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/directory_pull_sources</c> - Create a pull source for an organization: provider (entra, google_workspace, bamboohr, hibob, personio, csv, rest_json), config, write-only credentials and settings (schedule, role mappings, disable on removal).</summary>
        /// <remarks>Needs the <c>directory_pull:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(DirectoryPullSourcesCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/directory_pull_sources", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/directory_pull_sources/{id}</c> - Read one pull source.</summary>
        /// <remarks>Needs the <c>directory_pull:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/directory_pull_sources/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/directory_pull_sources/{id}</c> - Change the source name, status (active or paused), config, credentials, default role or settings. Settings are merged.</summary>
        /// <remarks>Needs the <c>directory_pull:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, DirectoryPullSourcesUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/directory_pull_sources/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/directory_pull_sources/{id}</c> - Delete a pull source and its run log. Users it created are kept.</summary>
        /// <remarks>Needs the <c>directory_pull:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/directory_pull_sources/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>PUT /v1/directory_pull_sources/{id}/csv</c> - Upload the CSV a csv source without a URL reads on its next run (up to 5 MiB).</summary>
        /// <remarks>Needs the <c>directory_pull:write</c> scope.</remarks>
        public Task SetCsvAsync(string id, DirectoryPullSourcesSetCsvBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Put, $"/v1/directory_pull_sources/{Enc(id)}/csv", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
