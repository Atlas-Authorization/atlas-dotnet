using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/org_ldap_connections/{id}/sync</c>. Only the fields set are sent.</summary>
    public sealed class OrgLdapConnectionsSetSyncBody
    {
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("intervalMinutes")] public double? IntervalMinutes { get; set; }
        [JsonPropertyName("pageSize")] public double? PageSize { get; set; }
        [JsonPropertyName("idAttribute")] public string? IdAttribute { get; set; }
        [JsonPropertyName("userFilter")] public string? UserFilter { get; set; }
        [JsonPropertyName("groupBaseDN")] public string? GroupBaseDN { get; set; }
        [JsonPropertyName("groupFilter")] public string? GroupFilter { get; set; }
        [JsonPropertyName("groupMemberAttribute")] public string? GroupMemberAttribute { get; set; }
        [JsonPropertyName("groupNameAttribute")] public string? GroupNameAttribute { get; set; }
        [JsonPropertyName("syncGroups")] public bool? SyncGroups { get; set; }
        [JsonPropertyName("roleMappings")] public List<object?>? RoleMappings { get; set; }
        [JsonPropertyName("disableOnRemoval")] public bool? DisableOnRemoval { get; set; }
        [JsonPropertyName("deltaAttribute")] public string? DeltaAttribute { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/org_ldap_connections/{id}/relay</c>. Only the fields set are sent.</summary>
    public sealed class OrgLdapConnectionsSetRelayBody
    {
        [JsonPropertyName("agent_id")] public string? AgentId { get; set; }
    }

    /// <summary>More operations of <c>orgLdapConnections</c>.</summary>
    public sealed partial class OrgLdapConnectionsResource
    {
        private OrgLdapConnectionsSyncRunsResource? _syncRuns;
        /// <summary>The <c>orgLdapConnections.syncRuns</c> operations.</summary>
        public OrgLdapConnectionsSyncRunsResource SyncRuns => _syncRuns ??= new OrgLdapConnectionsSyncRunsResource(Transport);

        /// <summary><c>GET /v1/org_ldap_connections/{id}/sync</c> - Read the directory-sync settings of an organization LDAP / Active Directory connection (schedule, filters, group-to-role mapping, disable-on-removal).</summary>
        /// <remarks>Needs the <c>sso_connections:read</c> scope.</remarks>
        public Task<JsonElement> GetSyncAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/org_ldap_connections/{Enc(id)}/sync", cancellationToken: ct);

        /// <summary><c>PUT /v1/org_ldap_connections/{id}/sync</c> - Set the directory-sync settings. Fields you omit keep their value. Enabling requires an active connection and schedules the first run immediately; disabling clears the schedule. Role mappings are matched in order, by group DN.</summary>
        /// <remarks>Needs the <c>sso_connections:write</c> scope.</remarks>
        public Task SetSyncAsync(string id, OrgLdapConnectionsSetSyncBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Put, $"/v1/org_ldap_connections/{Enc(id)}/sync", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/org_ldap_connections/{id}/relay</c> - Which relay agent an organization LDAP connection reaches its directory through, if any.</summary>
        /// <remarks>Needs the <c>sso_connections:read</c> scope.</remarks>
        public Task<JsonElement> GetRelayAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/org_ldap_connections/{Enc(id)}/relay", cancellationToken: ct);

        /// <summary><c>PUT /v1/org_ldap_connections/{id}/relay</c> - Route an LDAP connection through a relay agent (agent_id), or back to a direct connection (null). The agent must be active and allow the LDAP server.</summary>
        /// <remarks>Needs the <c>sso_connections:write</c> scope.</remarks>
        public Task SetRelayAsync(string id, OrgLdapConnectionsSetRelayBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Put, $"/v1/org_ldap_connections/{Enc(id)}/relay", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
