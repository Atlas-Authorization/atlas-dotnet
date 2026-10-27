using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/instance/integrations/{product}/events_secret/rotate</c>. Only the fields set are sent.</summary>
    public sealed class InstanceIntegrationsRotateEventsSecretBody
    {
        /// <summary>How long the previous secret keeps signing deliveries (default 0 = stop now).</summary>
        [JsonPropertyName("overlap_seconds")] public long? OverlapSeconds { get; set; }
    }

    /// <summary>Operations of <c>instance.integrations</c>.</summary>
    public sealed partial class InstanceIntegrationsResource : ResourceBase
    {
        public InstanceIntegrationsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/instance/integrations/{product}/events_secret/rotate</c> - Rotate the signing secret of the integration's event receiver. The new secret is revealed exactly once. By default (`overlap_seconds` 0) the old secret stops signing at once. With `overlap_seconds` (at most 604800 = 7 days) every delivery carries two signatures in `Atlas-Signature` (`v1,&lt;new&gt; v1,&lt;old&gt;`, the new one first), so the service verifies with either secret while it deploys the new one; after that only the new secret signs. `previous_secret_expires_at` says when the old secret stops signing (epoch ms), or null when it already has. Audited as integration.&lt;id&gt;.events_secret_rotated. 404 when the integration has no event receiver.</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> RotateEventsSecretAsync(string product, InstanceIntegrationsRotateEventsSecretBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/instance/integrations/{Enc(product)}/events_secret/rotate", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
