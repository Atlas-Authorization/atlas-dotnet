using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/organizations/{id}/webhook_endpoints</c>. Only the fields set are sent.</summary>
    public sealed class OrganizationsWebhookEndpointsCreateBody
    {
        /// <summary>The HTTPS receiver (any public URL that answers 2xx).</summary>
        [JsonPropertyName("url")] public string? Url { get; set; }
        /// <summary>Event types to receive, or ["*"]. Only events that belong to this organization are ever delivered.</summary>
        [JsonPropertyName("enabled_events")] public List<string>? EnabledEvents { get; set; }
        /// <summary>A label, up to 500 characters.</summary>
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>Set false to pause deliveries.</summary>
        [JsonPropertyName("active")] public bool? Active { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/organizations/{id}/webhook_endpoints/{endpointId}</c>. Only the fields set are sent.</summary>
    public sealed class OrganizationsWebhookEndpointsUpdateBody
    {
        /// <summary>The HTTPS receiver (any public URL that answers 2xx).</summary>
        [JsonPropertyName("url")] public string? Url { get; set; }
        /// <summary>Event types to receive, or ["*"]. Only events that belong to this organization are ever delivered.</summary>
        [JsonPropertyName("enabled_events")] public List<string>? EnabledEvents { get; set; }
        /// <summary>A label, up to 500 characters.</summary>
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>Set false to pause deliveries.</summary>
        [JsonPropertyName("active")] public bool? Active { get; set; }
    }

    /// <summary>Operations of <c>organizations.webhookEndpoints</c>.</summary>
    public sealed partial class OrganizationsWebhookEndpointsResource : ResourceBase
    {
        public OrganizationsWebhookEndpointsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/organizations/{id}/webhook_endpoints</c> - List the organization's own webhook endpoints. Backend API. Never returns a secret.</summary>
        /// <remarks>Needs the <c>webhooks:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/webhook_endpoints", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/organizations/{id}/webhook_endpoints</c> - Register a webhook endpoint for one organization. It receives only that organization's events (membership, invitation, role, domain and organization events). The signing secret is returned in this response only. Backend API. At most 20 per organization.</summary>
        /// <remarks>Needs the <c>webhooks:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(string id, OrganizationsWebhookEndpointsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/organizations/{Enc(id)}/webhook_endpoints", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/{id}/webhook_endpoints/{endpointId}</c> - Retrieve one of the organization's webhook endpoints, with its delivery health.</summary>
        /// <remarks>Needs the <c>webhooks:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, string endpointId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/webhook_endpoints/{Enc(endpointId)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/organizations/{id}/webhook_endpoints/{endpointId}</c> - Update the url, subscribed events, description or active flag of one of the organization's endpoints. Never re-reveals the secret.</summary>
        /// <remarks>Needs the <c>webhooks:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, string endpointId, OrganizationsWebhookEndpointsUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/organizations/{Enc(id)}/webhook_endpoints/{Enc(endpointId)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/organizations/{id}/webhook_endpoints/{endpointId}</c> - Delete one of the organization's webhook endpoints and its delivery log. An id from another organization is a 404.</summary>
        /// <remarks>Needs the <c>webhooks:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, string endpointId, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/organizations/{Enc(id)}/webhook_endpoints/{Enc(endpointId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/organizations/{id}/webhook_endpoints/{endpointId}/rotate_secret</c> - Rotate the signing secret. The previous secret keeps signing alongside the new one for 24 hours so the receiver can switch over without dropping events.</summary>
        /// <remarks>Needs the <c>webhooks:write</c> scope.</remarks>
        public Task<JsonElement> RotateSecretAsync(string id, string endpointId, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/organizations/{Enc(id)}/webhook_endpoints/{Enc(endpointId)}/rotate_secret", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/{id}/webhook_endpoints/{endpointId}/deliveries</c> - The endpoint's delivery log: payload, response and retry state for each attempt, filterable by status, event type and time.</summary>
        /// <remarks>Needs the <c>webhooks:read</c> scope.</remarks>
        public Task<JsonElement> DeliveriesAsync(string id, string endpointId, long? limit = null, string? startingAfter = null, string? status = null, string? eventType = null, string? since = null, string? until = null, string? trace = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/webhook_endpoints/{Enc(endpointId)}/deliveries", query: Q(("limit", limit), ("starting_after", startingAfter), ("status", status), ("event_type", eventType), ("since", since), ("until", until), ("trace", trace)), cancellationToken: ct);
    }
}
