using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>More operations of <c>organizations</c>.</summary>
    public sealed partial class OrganizationsResource
    {
        private OrganizationsWebhookEndpointsResource? _webhookEndpoints;
        /// <summary>The <c>organizations.webhookEndpoints</c> operations.</summary>
        public OrganizationsWebhookEndpointsResource WebhookEndpoints => _webhookEndpoints ??= new OrganizationsWebhookEndpointsResource(Transport);

        /// <summary><c>GET /v1/organizations/{id}/connected_apps</c> - List the apps (OAuth clients) that hold an active grant in this organization's context, one entry per grant.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> ListConnectedAppsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/connected_apps", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/organizations/{id}/connected_apps/{grantId}</c> - Revoke one connected app grant of the organization: its tokens and any session minted from it stop working. Emits grant.revoked; audited as organization.connected_app_revoked.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> RevokeConnectedAppAsync(string id, string grantId, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/organizations/{Enc(id)}/connected_apps/{Enc(grantId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/{id}/connections/{connectionId}/secret</c> - The same read, scoped to an organization: 404 unless the connection is owned by that organization. Needs connections:secret_read; audited.</summary>
        /// <remarks>Needs the <c>connections:secret_read</c> scope.</remarks>
        public Task<JsonElement> GetConnectionSecretAsync(string id, string connectionId, string? version = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/connections/{Enc(connectionId)}/secret", query: Q(("version", version)), cancellationToken: ct);
    }
}
