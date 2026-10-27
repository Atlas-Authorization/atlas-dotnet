using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/org_ldap_connections/{id}/sync_runs</c>. Only the fields set are sent.</summary>
    public sealed class OrgLdapConnectionsSyncRunsCreateBody
    {
        [JsonPropertyName("kind")] public string? Kind { get; set; }
    }

    /// <summary>Operations of <c>orgLdapConnections.syncRuns</c>.</summary>
    public sealed partial class OrgLdapConnectionsSyncRunsResource : ResourceBase
    {
        public OrgLdapConnectionsSyncRunsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/org_ldap_connections/{id}/sync_runs</c> - Queue a sync run now (202). `full` (default) lists the whole directory and, when disable-on-removal is on, bans users that left it; `delta` only reads entries changed since the last successful run. Refused with 409 while another run is queued or running.</summary>
        /// <remarks>Needs the <c>sso_connections:write</c> scope.</remarks>
        public Task CreateAsync(string id, OrgLdapConnectionsSyncRunsCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, $"/v1/org_ldap_connections/{Enc(id)}/sync_runs", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/org_ldap_connections/{id}/sync_runs</c> - The sync run log, newest first, with per-run counts (seen, created, updated, skipped, disabled, role changes, groups, errors) and any failure message.</summary>
        /// <remarks>Needs the <c>sso_connections:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/org_ldap_connections/{Enc(id)}/sync_runs", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/org_ldap_connections/{id}/sync_runs/{runId}</c> - Read one LDAP directory sync run with its counts and error.</summary>
        /// <remarks>Needs the <c>sso_connections:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, string runId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/org_ldap_connections/{Enc(id)}/sync_runs/{Enc(runId)}", cancellationToken: ct);
    }
}
