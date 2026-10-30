using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>An entry of your app's event catalog (<c>/v1/app_event_types</c>).</summary>
    public sealed class AppEventType
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "app_event_type";
        public string Id { get; init; } = "";
        /// <summary>Lowercase dotted name, e.g. <c>call.completed</c>.</summary>
        public string Type { get; init; } = "";
        public string? Description { get; init; }
        /// <summary>JSON Schema every published <c>data</c> must match; null = any object.</summary>
        public JsonElement? Schema { get; init; }
        /// <summary>Still publishable; hidden from the endpoint picker.</summary>
        public bool Deprecated { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    /// <summary>The body of <c>POST /v1/app_event_types</c>.</summary>
    public sealed class CreateAppEventTypeBody
    {
        /// <summary>Lowercase dotted name, e.g. <c>call.completed</c>.</summary>
        public string Type { get; set; } = "";
        public string? Description { get; set; }
        public Dictionary<string, object?>? Schema { get; set; }
        public bool? Deprecated { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/app_event_types/{id_or_type}</c>. A type cannot be renamed. Use <see cref="Set"/> to send an explicit <c>null</c>.</summary>
    public sealed class UpdateAppEventTypeBody
    {
        public string? Description { get; set; }
        public Dictionary<string, object?>? Schema { get; set; }
        public bool? Deprecated { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name, including an explicit <c>null</c>.</summary>
        public UpdateAppEventTypeBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>The answer to <c>DELETE /v1/app_event_types/{id_or_type}</c>.</summary>
    public sealed class DeletedAppEventType
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "app_event_type";
        public string Id { get; init; } = "";
        public string Type { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary>The body of <c>POST /v1/app_events</c>.</summary>
    public sealed class PublishAppEventBody
    {
        /// <summary>The organization (your customer) whose endpoints receive the event.</summary>
        public string? OrganizationId { get; set; }
        /// <summary>Shorthand for <see cref="OrganizationId"/>.</summary>
        public string? Org { get; set; }
        public string Type { get; set; } = "";
        public Dictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>();
        /// <summary>A repeat with the same key returns the first event and queues nothing (kept 90 days).</summary>
        public string? IdempotencyKey { get; set; }
    }

    /// <summary>A published application event.</summary>
    public sealed class AppEvent
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "app_event";
        public string Id { get; init; } = "";
        public string Type { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public Dictionary<string, JsonElement>? Data { get; init; }
        public string? IdempotencyKey { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>The answer to a publish: the event, plus whether it was a replay and where it was queued.</summary>
    public sealed class PublishedAppEvent
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "app_event";
        public string Id { get; init; } = "";
        public string Type { get; init; } = "";
        public string OrganizationId { get; init; } = "";
        public Dictionary<string, JsonElement>? Data { get; init; }
        public string? IdempotencyKey { get; init; }
        public long CreatedAt { get; init; }
        /// <summary>True: an earlier publish with the same key; nothing new was queued.</summary>
        public bool IdempotentReplay { get; init; }
        /// <summary>First publish only: how many endpoints it was queued for.</summary>
        public int? DeliveriesQueued { get; init; }
        /// <summary>First publish only: the endpoints it was queued for.</summary>
        public List<string>? EndpointIds { get; init; }
    }

    /// <summary>Filters for <c>GET /v1/app_events</c>.</summary>
    public sealed class ListAppEventsParams
    {
        /// <summary>Required.</summary>
        public string OrganizationId { get; set; } = "";
        public string? Type { get; set; }
        public int? Limit { get; set; }
        public string? StartingAfter { get; set; }

        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("organization_id", OrganizationId);
            yield return new KeyValuePair<string, object?>("type", Type);
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
        }
    }

    /// <summary>The body of an app webhook endpoint create or update. <c>Url</c> is required on create.</summary>
    public sealed class AppWebhookEndpointBody
    {
        public string? Url { get; set; }
        /// <summary>Types from your catalog, or <c>["*"]</c> (the default) for all of them.</summary>
        public List<string>? EnabledEvents { get; set; }
        public string? Description { get; set; }
        public bool? Active { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name, including an explicit <c>null</c>.</summary>
        public AppWebhookEndpointBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>The app event catalog (<c>/v1/app_event_types</c>).</summary>
    public sealed class AppEventTypesResource : ResourceBase
    {
        public AppEventTypesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/app_event_types</c></summary>
        public Task<ListPage<AppEventType>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<AppEventType>>(HttpVerb.Get, "/v1/app_event_types", cancellationToken: ct);

        /// <summary><c>POST /v1/app_event_types</c> — register a type (409 if it exists).</summary>
        public Task<AppEventType> CreateAsync(CreateAppEventTypeBody body, CancellationToken ct = default)
            => Req<AppEventType>(HttpVerb.Post, "/v1/app_event_types", body, cancellationToken: ct);

        /// <summary><c>GET /v1/app_event_types/{id_or_type}</c> — by <c>aet_</c> id or by name.</summary>
        public Task<AppEventType> GetAsync(string idOrType, CancellationToken ct = default)
            => Req<AppEventType>(HttpVerb.Get, $"/v1/app_event_types/{Enc(idOrType)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/app_event_types/{id_or_type}</c></summary>
        public Task<AppEventType> UpdateAsync(string idOrType, UpdateAppEventTypeBody body, CancellationToken ct = default)
            => Req<AppEventType>(HttpVerb.Patch, $"/v1/app_event_types/{Enc(idOrType)}", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/app_event_types/{id_or_type}</c> — deprecate instead while customers still subscribe.</summary>
        public Task<DeletedAppEventType> DeleteAsync(string idOrType, CancellationToken ct = default)
            => Req<DeletedAppEventType>(HttpVerb.Delete, $"/v1/app_event_types/{Enc(idOrType)}", cancellationToken: ct);
    }

    /// <summary>One organization's endpoints for your app events (<c>/v1/organizations/{org_id}/app_webhook_endpoints</c>).</summary>
    public sealed class AppWebhookEndpointsResource : ResourceBase
    {
        public AppWebhookEndpointsResource(AtlasTransport t) : base(t) { }

        private static string Base(string orgId) => $"/v1/organizations/{Enc(orgId)}/app_webhook_endpoints";

        public Task<ListPage<WebhookEndpoint>> ListAsync(string orgId, CancellationToken ct = default)
            => Req<ListPage<WebhookEndpoint>>(HttpVerb.Get, Base(orgId), cancellationToken: ct);

        public Task<WebhookEndpoint> GetAsync(string orgId, string endpointId, CancellationToken ct = default)
            => Req<WebhookEndpoint>(HttpVerb.Get, $"{Base(orgId)}/{Enc(endpointId)}", cancellationToken: ct);

        /// <summary>The <c>whsec_</c> secret is in this response only (or <see cref="RevealSecretAsync"/>).</summary>
        public Task<WebhookEndpointWithSecret> CreateAsync(string orgId, AppWebhookEndpointBody body, CancellationToken ct = default)
            => Req<WebhookEndpointWithSecret>(HttpVerb.Post, Base(orgId), body, cancellationToken: ct);

        public Task<WebhookEndpoint> UpdateAsync(string orgId, string endpointId, AppWebhookEndpointBody body, CancellationToken ct = default)
            => Req<WebhookEndpoint>(HttpVerb.Patch, $"{Base(orgId)}/{Enc(endpointId)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string orgId, string endpointId, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"{Base(orgId)}/{Enc(endpointId)}", cancellationToken: ct);

        /// <summary>The previous secret keeps signing for 24 hours.</summary>
        public Task<WebhookSecret> RotateSecretAsync(string orgId, string endpointId, CancellationToken ct = default)
            => Req<WebhookSecret>(HttpVerb.Post, $"{Base(orgId)}/{Enc(endpointId)}/rotate_secret", cancellationToken: ct);

        public Task<WebhookSecret> RevealSecretAsync(string orgId, string endpointId, CancellationToken ct = default)
            => Req<WebhookSecret>(HttpVerb.Post, $"{Base(orgId)}/{Enc(endpointId)}/reveal_secret", cancellationToken: ct);

        /// <summary>Queue a test event. With <paramref name="wait"/> (e.g. <c>"10s"</c>) it is attempted now and the result comes back as JSON.</summary>
        public Task<JsonElement> TestAsync(string orgId, string endpointId, string? wait = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"{Base(orgId)}/{Enc(endpointId)}/test", query: Q(("wait", wait)), cancellationToken: ct);

        /// <summary>The endpoint's delivery log, newest first.</summary>
        public Task<CursorPage<WebhookDelivery>> DeliveriesAsync(string orgId, string endpointId, ListWebhookDeliveriesParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<WebhookDelivery>>(HttpVerb.Get, $"{Base(orgId)}/{Enc(endpointId)}/deliveries", query: (@params ?? new ListWebhookDeliveriesParams()).ToQuery(), cancellationToken: ct);

        public Task<WebhookDeliveryQueued> RedeliverAsync(string orgId, string endpointId, string deliveryId, CancellationToken ct = default)
            => Req<WebhookDeliveryQueued>(HttpVerb.Post, $"{Base(orgId)}/{Enc(endpointId)}/deliveries/{Enc(deliveryId)}/redeliver", cancellationToken: ct);

        /// <summary>Re-send what the endpoint missed since <c>Since</c> (or <c>"failing_since"</c>).</summary>
        public Task<WebhookBulkResult> ReplayAsync(string orgId, string endpointId, ReplayWebhookBody body, CancellationToken ct = default)
            => Req<WebhookBulkResult>(HttpVerb.Post, $"{Base(orgId)}/{Enc(endpointId)}/replay", body, cancellationToken: ct);

        /// <summary><c>GET /v1/organizations/{org_id}/app_event_types</c> — the event types this organization can subscribe its endpoints to (the picker list; deprecated types left out).</summary>
        public Task<ListPage<AppEventType>> EventTypesAsync(string orgId, CancellationToken ct = default)
            => Req<ListPage<AppEventType>>(HttpVerb.Get, $"/v1/organizations/{Enc(orgId)}/app_event_types", cancellationToken: ct);
    }

    /// <summary>
    /// Application webhooks: send your app's own events (<c>call.completed</c>, <c>invoice.paid</c>) to endpoints your customers
    /// (organizations) register. Atlas signs them (Standard Webhooks; verify with <see cref="Atlas.Verification.StandardWebhookVerifier"/>),
    /// retries, tracks health and keeps the delivery log with redeliver and replay.
    /// </summary>
    public sealed class AppEventsResource : ResourceBase
    {
        public AppEventsResource(AtlasTransport t) : base(t)
        {
            Types = new AppEventTypesResource(t);
            Endpoints = new AppWebhookEndpointsResource(t);
        }

        /// <summary>Your event catalog.</summary>
        public AppEventTypesResource Types { get; }

        /// <summary>One organization's endpoints for your events.</summary>
        public AppWebhookEndpointsResource Endpoints { get; }

        /// <summary>
        /// <c>POST /v1/app_events</c> — publish one event to one organization's subscribed endpoints. A repeat with the same
        /// idempotency key (in the body or as <paramref name="idempotencyKey"/>) returns the first event with <c>IdempotentReplay</c>.
        /// </summary>
        public Task<PublishedAppEvent> PublishAsync(PublishAppEventBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<PublishedAppEvent>(HttpVerb.Post, "/v1/app_events", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/app_events</c> — one organization's published events, newest first.</summary>
        public Task<ListPage<AppEvent>> ListAsync(ListAppEventsParams @params, CancellationToken ct = default)
            => Req<ListPage<AppEvent>>(HttpVerb.Get, "/v1/app_events", query: (@params ?? new ListAppEventsParams()).ToQuery(), cancellationToken: ct);

        /// <summary><c>GET /v1/app_events/{id}</c></summary>
        public Task<AppEvent> GetAsync(string eventId, CancellationToken ct = default)
            => Req<AppEvent>(HttpVerb.Get, $"/v1/app_events/{Enc(eventId)}", cancellationToken: ct);
    }
}
