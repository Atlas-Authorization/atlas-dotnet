using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Operations of <c>oauthClients.workloadIdentity</c>.</summary>
    public sealed partial class OAuthClientsWorkloadIdentityResource : ResourceBase
    {
        public OAuthClientsWorkloadIdentityResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/workload_identity/presets</c> - List the workload platforms Atlas knows defaults for (SPIFFE, Kubernetes, GitHub Actions, GCP, Azure): issuer, JWKS URL and the shape of the token subject.</summary>
        /// <remarks>Needs the <c>oauth_clients:read</c> scope.</remarks>
        public Task<JsonElement> PresetsAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/workload_identity/presets", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/workload_identity/cloud_trust</c> - Trust-policy snippets that make AWS (AssumeRoleWithWebIdentity), GCP (workload identity federation) or Azure (federated credentials) accept an access token issued by this instance. Nothing is changed in the cloud.</summary>
        /// <remarks>Needs the <c>oauth_clients:read</c> scope.</remarks>
        public Task<JsonElement> CloudTrustAsync(string provider, string audience, string subject, string? tenantId = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/workload_identity/cloud_trust", query: Q(("provider", provider), ("audience", audience), ("subject", subject), ("tenant_id", tenantId)), cancellationToken: ct);
    }
}
