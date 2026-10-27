using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Operations of <c>scimProvisioningPresets</c>.</summary>
    public sealed partial class ScimProvisioningPresetsResource : ResourceBase
    {
        public ScimProvisioningPresetsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/scim_provisioning_presets</c> - List the pre-built outbound provisioning targets (Slack, GitHub, Zoom, Salesforce, Atlassian) with their base-URL template and required parameters.</summary>
        /// <remarks>Needs the <c>scim_provisioning:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/scim_provisioning_presets", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
