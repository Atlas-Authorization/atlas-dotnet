using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

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
        /// <summary>Epoch ms since deliveries started failing, or null while healthy.</summary>
        public long? FailingSince { get; init; }
        /// <summary>While a secret rotation overlap is open: when the previous secret stops signing deliveries (epoch ms); else null.</summary>
        public long? PreviousSecretExpiresAt { get; init; }
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
        /// <summary>Epoch ms since deliveries started failing, or null while healthy.</summary>
        public long? FailingSince { get; init; }
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
        /// <summary>Why the attempt failed: <c>webhook_redirect_not_followed</c> (see <see cref="Location"/>), <c>webhook_http_error</c>, <c>webhook_timeout</c>, <c>webhook_network_error</c> or <c>webhook_url_blocked</c>. Null on success or while pending.</summary>
        public string? Error { get; init; }
        /// <summary>The absolute <c>Location</c> of a 3xx answer. Not followed.</summary>
        public string? Location { get; init; }
        public string? EventType { get; init; }
        public int? MaxAttempts { get; init; }
        /// <summary>The JSON payload that was sent.</summary>
        public JsonElement? Payload { get; init; }
    }

    /// <summary><c>GET /v1/webhook_endpoints</c> in full: the endpoints plus the subscribable event types and the retry schedule.</summary>
    public sealed class WebhookEndpointList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<WebhookEndpoint> Data { get; init; } = new List<WebhookEndpoint>();
        public bool? HasMore { get; init; }
        /// <summary>Every event type an endpoint can list in <c>enabled_events</c>.</summary>
        public List<string> AvailableEvents { get; init; } = new List<string>();
        /// <summary>Delay before each retry, in milliseconds.</summary>
        public List<long> RetryScheduleMs { get; init; } = new List<long>();
        public int MaxAttempts { get; init; }
        /// <summary>The same events as <see cref="AvailableEvents"/>, each described.</summary>
        public List<WebhookEventCatalogEntry>? EventCatalog { get; init; }
        /// <summary>Every value <see cref="WebhookEventCatalogEntry.RelevantFor"/> can hold.</summary>
        public List<string>? EventAudiences { get; init; }
    }

    /// <summary>One subscribable webhook event, described.</summary>
    public sealed class WebhookEventCatalogEntry
    {
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        /// <summary>Who should act on it (rp_sessions, rp_accounts, dsar, …).</summary>
        public List<string> RelevantFor { get; init; } = new List<string>();
        /// <summary>Set on a superseded spelling: subscribe to this event instead.</summary>
        public string? DeprecatedAliasOf { get; init; }
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

        /// <summary>The endpoints plus the subscribable event types and the retry schedule.</summary>
        public Task<WebhookEndpointList> ListWithEventsAsync(CancellationToken ct = default)
            => Req<WebhookEndpointList>(HttpVerb.Get, "/v1/webhook_endpoints", cancellationToken: ct);

        public Task<WebhookEndpointWithSecret> CreateAsync(CreateWebhookEndpointBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<WebhookEndpointWithSecret>(HttpVerb.Post, "/v1/webhook_endpoints", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/webhook_endpoints/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/webhook_endpoints/{id}</c> — Update an endpoint’s url, enabled_events, or active flag in place.</summary>
        public Task<WebhookEndpoint> UpdateAsync(string id, UpdateWebhookEndpointBody body, CancellationToken ct = default)
            => Req<WebhookEndpoint>(HttpVerb.Patch, $"/v1/webhook_endpoints/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>POST /v1/webhook_endpoints/{id}/reveal_secret</c> — Show the endpoint's current signing secret again (for a verifier that lost it).</summary>
        public Task<WebhookSecret> RevealSecretAsync(string id, CancellationToken ct = default)
            => Req<WebhookSecret>(HttpVerb.Post, $"/v1/webhook_endpoints/{Enc(id)}/reveal_secret", cancellationToken: ct);

        /// <summary><c>POST /v1/webhook_endpoints/{id}/rotate_secret</c> — Rotate the endpoint signing secret.</summary>
        public Task<WebhookSecret> RotateSecretAsync(string id, CancellationToken ct = default)
            => Req<WebhookSecret>(HttpVerb.Post, $"/v1/webhook_endpoints/{Enc(id)}/rotate_secret", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/webhook_endpoints/{id}/rotate_secret</c> — Rotate the signing secret with a chosen overlap: for
        /// <c>OverlapSeconds</c> every delivery is signed with both secrets, so a verifier holding either passes.
        /// </summary>
        public Task<WebhookSecret> RotateSecretAsync(string id, RotateWebhookSecretBody body, CancellationToken ct = default)
            => Req<WebhookSecret>(HttpVerb.Post, $"/v1/webhook_endpoints/{Enc(id)}/rotate_secret", body, cancellationToken: ct);

        /// <summary><c>POST /v1/webhook_endpoints/{id}/test</c> — Send a synthetic webhook.test event to this endpoint only.</summary>
        public Task<WebhookDeliveryQueued> TestAsync(string id, CancellationToken ct = default)
            => Req<WebhookDeliveryQueued>(HttpVerb.Post, $"/v1/webhook_endpoints/{Enc(id)}/test", cancellationToken: ct);
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

        /// <summary>Delivery log for an endpoint, newest first; <paramref name="limit"/> defaults to 50 (max 100).</summary>
        public Task<ListPage<WebhookDelivery>> DeliveriesAsync(string id, int? limit, CancellationToken ct = default)
            => Req<ListPage<WebhookDelivery>>(HttpVerb.Get, $"/v1/webhook_endpoints/{Enc(id)}/deliveries",
                query: Q(("limit", limit)), cancellationToken: ct);

        /// <summary><c>POST /v1/webhook_endpoints/{id}/deliveries/{deliveryId}/redeliver</c> — Manually redeliver a past event as a fresh attempt.</summary>
        public Task<WebhookDeliveryQueued> RedeliverAsync(string id, string deliveryId, CancellationToken ct = default)
            => Req<WebhookDeliveryQueued>(HttpVerb.Post, $"/v1/webhook_endpoints/{Enc(id)}/deliveries/{Enc(deliveryId)}/redeliver", cancellationToken: ct);
    }

    public sealed class UpdateWebhookEndpointBody
    {
        public string? Url { get; set; }
        public List<string>? EnabledEvents { get; set; }
        public bool? Active { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public UpdateWebhookEndpointBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class WebhookSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>The current whsec_ signing secret.</summary>
        public string Secret { get; init; } = "";
        /// <summary>On a rotation: what happens to the old secret.</summary>
        public string? Warning { get; init; }
        /// <summary>On a rotation: the overlap applied, in seconds.</summary>
        public int? OverlapSeconds { get; init; }
        /// <summary>On a rotation: when the previous secret stops signing deliveries (epoch ms), or null when it already has.</summary>
        public long? PreviousSecretExpiresAt { get; init; }
    }

    /// <summary>The body of <c>POST /v1/webhook_endpoints/{id}/rotate_secret</c>.</summary>
    public sealed class RotateWebhookSecretBody
    {
        /// <summary>How long the previous secret keeps signing deliveries (as a second <c>atlas-signature</c> entry), in seconds. Null or 0: it stops at once (the default); at most 604800 (7 days). Set it only once every receiver verifies with a verifier that accepts several signatures (1.6.0+).</summary>
        public int? OverlapSeconds { get; set; }
    }

    public sealed class WebhookDeliveryQueued
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string EventId { get; init; } = "";
        public string Status { get; init; } = "";
        public string Note { get; init; } = "";
    }
}
