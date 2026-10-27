using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/integrations/{integration}/card/actions/{action}</c>. Only the fields set are sent.</summary>
    public sealed class IntegrationsRunCardActionBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("sub")] public string? Sub { get; set; }
        [JsonPropertyName("iss")] public string? Iss { get; set; }
        /// <summary>Who pressed the button in the integrated service (recorded in the audit log).</summary>
        [JsonPropertyName("agent")] public Dictionary<string, object?>? Agent { get; set; }
    }

    /// <summary>More operations of <c>integrations</c>.</summary>
    public sealed partial class IntegrationsResource
    {
        /// <summary><c>POST /v1/integrations/{integration}/card/actions/{action}</c> - Run a card action. revoke_sessions signs the user out of every device (their sessions end, back-channel logouts go out) and is audited with the agent the integration names. Partner link with sessions:write, or the secret key.</summary>
        /// <remarks>Needs the <c>sessions:write</c> scope.</remarks>
        public Task<JsonElement> RunCardActionAsync(string integration, string action, IntegrationsRunCardActionBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/integrations/{Enc(integration)}/card/actions/{Enc(action)}", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
