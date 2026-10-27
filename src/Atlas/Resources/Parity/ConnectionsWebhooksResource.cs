using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/connections/{id}/webhooks/verify</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsWebhooksVerifyBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("scheme")] public string? Scheme { get; set; }
        /// <summary>Required. The delivery's headers.</summary>
        [JsonPropertyName("headers")] public Dictionary<string, object?>? Headers { get; set; }
        /// <summary>Required. The body exactly as received (up to 1 MiB).</summary>
        [JsonPropertyName("raw_body")] public string? RawBody { get; set; }
        [JsonPropertyName("raw_body_encoding")] public string? RawBodyEncoding { get; set; }
        /// <summary>hubspot_v3: the HTTP method of the delivery (default POST).</summary>
        [JsonPropertyName("method")] public string? Method { get; set; }
        /// <summary>hubspot_v3: the full URL the provider posted to.</summary>
        [JsonPropertyName("url")] public string? Url { get; set; }
        /// <summary>Use this webhook endpoint's signing secret and options.</summary>
        [JsonPropertyName("endpoint_id")] public string? EndpointId { get; set; }
        [JsonPropertyName("tolerance_seconds")] public long? ToleranceSeconds { get; set; }
        /// <summary>generic_hmac_sha256: signature_header, encoding, prefix, algorithm.</summary>
        [JsonPropertyName("options")] public Dictionary<string, object?>? Options { get; set; }
        /// <summary>Set false to check without remembering the delivery (default true).</summary>
        [JsonPropertyName("record_replay")] public bool? RecordReplay { get; set; }
    }

    /// <summary>The body of <c>POST /v1/connections/{id}/webhook_endpoints</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsWebhooksCreateEndpointBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("scheme")] public string? Scheme { get; set; }
        /// <summary>Required. https URL that receives verified deliveries as Atlas-signed webhooks.</summary>
        [JsonPropertyName("forward_url")] public string? ForwardUrl { get; set; }
        /// <summary>Write-only. The provider's signing secret for this endpoint (Slack signing secret, Zoom secret token, Calendly signing key...). Without it, the connection's hmac_secret credential or its OAuth client secret is used.</summary>
        [JsonPropertyName("signing_secret")] public string? SigningSecret { get; set; }
        /// <summary>generic_hmac_sha256 only: signature_header, encoding (hex or base64), prefix, algorithm.</summary>
        [JsonPropertyName("options")] public Dictionary<string, object?>? Options { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/connections/{id}/webhook_endpoints/{endpointId}</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsWebhooksUpdateEndpointBody
    {
        [JsonPropertyName("scheme")] public string? Scheme { get; set; }
        /// <summary>https URL that receives verified deliveries as Atlas-signed webhooks.</summary>
        [JsonPropertyName("forward_url")] public string? ForwardUrl { get; set; }
        /// <summary>Write-only. The provider's signing secret for this endpoint (Slack signing secret, Zoom secret token, Calendly signing key...). Without it, the connection's hmac_secret credential or its OAuth client secret is used.</summary>
        [JsonPropertyName("signing_secret")] public string? SigningSecret { get; set; }
        /// <summary>generic_hmac_sha256 only: signature_header, encoding (hex or base64), prefix, algorithm.</summary>
        [JsonPropertyName("options")] public Dictionary<string, object?>? Options { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>Operations of <c>connections.webhooks</c>.</summary>
    public sealed partial class ConnectionsWebhooksResource : ResourceBase
    {
        public ConnectionsWebhooksResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/connections/{id}/webhooks/verify</c> - Verify a provider webhook signature with the connection's secret (the endpoint's own signing secret, else the connection's hmac_secret, else its OAuth client secret). Schemes: hubspot_v3 (needs method and url), shopify_hmac, intercom, zoom, slack_v0, calendly, stripe, generic_hmac_sha256. Constant-time comparison; a signed timestamp must be within the tolerance (5 minutes, Calendly 3); a delivery id or signature already seen on the connection answers `replayed`. Send the body exactly as received (`raw_body`, or base64 with raw_body_encoding). Zoom and Slack handshakes come back in `handshake`. Needs connections:webhook_verify.</summary>
        /// <remarks>Needs the <c>connections:webhook_verify</c> scope.</remarks>
        public Task<JsonElement> VerifyAsync(string id, ConnectionsWebhooksVerifyBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/connections/{Enc(id)}/webhooks/verify", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/connections/{id}/webhook_endpoints</c> - Create a webhook ingress endpoint on a connection. Point the provider at `ingress_url`; Atlas verifies each delivery, refuses forged, stale and replayed ones, answers Zoom and Slack handshakes, and forwards only genuine deliveries to `forward_url` as Atlas-signed webhooks (atlas-id, atlas-timestamp, atlas-signature with `forward_secret`, returned once). Bodies over 1 MiB are refused; the ingress is rate limited.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> CreateEndpointAsync(string id, ConnectionsWebhooksCreateEndpointBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/connections/{Enc(id)}/webhook_endpoints", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/connections/{id}/webhook_endpoints</c> - List a connection's webhook ingress endpoints.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> ListEndpointsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/connections/{Enc(id)}/webhook_endpoints", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>PATCH /v1/connections/{id}/webhook_endpoints/{endpointId}</c> - Change an endpoint's forward_url, enabled flag or signing secret.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> UpdateEndpointAsync(string id, string endpointId, ConnectionsWebhooksUpdateEndpointBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/connections/{Enc(id)}/webhook_endpoints/{Enc(endpointId)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/connections/{id}/webhook_endpoints/{endpointId}</c> - Delete a webhook ingress endpoint and its secrets.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task DeleteEndpointAsync(string id, string endpointId, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/connections/{Enc(id)}/webhook_endpoints/{Enc(endpointId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
