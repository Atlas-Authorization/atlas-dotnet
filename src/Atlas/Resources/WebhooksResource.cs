using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class WebhookEndpoint
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "webhook_endpoint";
        public string Id { get; init; } = "";
        public string Url { get; init; } = "";
        public List<string> EnabledEvents { get; init; } = new List<string>();
        public bool Active { get; init; }
        public long? DisabledAt { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>Create reveals the signing secret (<c>whsec_...</c>) exactly once.</summary>
    public sealed class WebhookEndpointWithSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "webhook_endpoint";
        public string Id { get; init; } = "";
        public string Url { get; init; } = "";
        public List<string> EnabledEvents { get; init; } = new List<string>();
        public bool Active { get; init; }
        public long? DisabledAt { get; init; }
        public long CreatedAt { get; init; }
        public string Secret { get; init; } = "";
    }

    public sealed class WebhookDelivery
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "webhook_delivery";
        public string Id { get; init; } = "";
        public string EventId { get; init; } = "";
        public int AttemptNumber { get; init; }
        public string Status { get; init; } = "";
        public int? HttpStatus { get; init; }
        public string? ResponseSnippet { get; init; }
        public long? NextRetryAt { get; init; }
        public long? DeliveredAt { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class CreateWebhookEndpointBody
    {
        public string Url { get; set; } = "";
        /// <summary>Each entry is <c>"*"</c> or a known event name.</summary>
        public List<string>? EnabledEvents { get; set; }
    }

    /// <summary>Nested webhook-endpoints namespace.</summary>
    public sealed class WebhookEndpointsResource : ResourceBase
    {
        public WebhookEndpointsResource(AtlasTransport t) : base(t) { }

        public Task<ListPage<WebhookEndpoint>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<WebhookEndpoint>>(HttpVerb.Get, "/v1/webhook_endpoints", cancellationToken: ct);

        public Task<WebhookEndpointWithSecret> CreateAsync(CreateWebhookEndpointBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<WebhookEndpointWithSecret>(HttpVerb.Post, "/v1/webhook_endpoints", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/webhook_endpoints/{Enc(id)}", cancellationToken: ct);
    }

    /// <summary>The webhooks namespace (<c>/v1/webhook_endpoints</c>).</summary>
    public sealed class WebhooksResource : ResourceBase
    {
        public WebhooksResource(AtlasTransport transport) : base(transport)
        {
            Endpoints = new WebhookEndpointsResource(transport);
        }

        public WebhookEndpointsResource Endpoints { get; }

        /// <summary>Delivery log for an endpoint.</summary>
        public Task<ListPage<WebhookDelivery>> DeliveriesAsync(string id, CancellationToken ct = default)
            => Req<ListPage<WebhookDelivery>>(HttpVerb.Get, $"/v1/webhook_endpoints/{Enc(id)}/deliveries", cancellationToken: ct);
    }
}
