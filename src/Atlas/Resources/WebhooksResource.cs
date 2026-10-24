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
        /// <summary>The HTTPS receiver, or a descriptor such as <c>aws-eventbridge://us-east-1/default</c>.</summary>
        public string? Description { get; init; }
        /// <summary>Which names <c>*</c> delivers under: <c>canonical</c> or <c>legacy</c>.</summary>
        public string? EventNaming { get; init; }
        /// <summary>Marks a third-party receiver: preflight does not flag its host.</summary>
        public bool? External { get; init; }
        /// <summary>The signature versions sent: <c>v1</c>, and <c>v2</c> (which also covers the URL).</summary>
        public List<string>? SignatureVersions { get; init; }
        /// <summary>Where deliveries go; credentials are never returned.</summary>
        public WebhookDestination? Destination { get; init; }
        /// <summary><c>healthy</c>, <c>failing</c> or <c>disabled</c>.</summary>
        public string? Health { get; init; }
        public long? LastSuccessAt { get; init; }
        public WebhookLastFailure? LastFailure { get; init; }
        public int? ConsecutiveFailures { get; init; }
        /// <summary>The share of attempts in the last 24 hours that succeeded (0-1), null with none.</summary>
        [JsonPropertyName("success_rate_24h")] public double? SuccessRate24h { get; init; }
        [JsonPropertyName("attempts_24h")] public WebhookAttempts24h? Attempts24h { get; init; }
        /// <summary>Who is emailed when it fails; null = the account's owners.</summary>
        public List<string>? AlertContacts { get; init; }
        public WebhookAlertsSent? AlertsSent { get; init; }
        public WebhookAutoDisableRule? AutoDisable { get; init; }
        /// <summary>Set on an update that re-enabled the endpoint: the replay that re-sends what it missed.</summary>
        public WebhookReplayHint? Replay { get; init; }
        /// <summary>On create / upsert: whether this call created it.</summary>
        public bool? Created { get; init; }
        public long? UpdatedAt { get; init; }
        /// <summary>Start of the failure streak that preceded the last re-enable (epoch ms); what a replay resolves <c>since: "failing_since"</c> to once the live streak is cleared.</summary>
        public long? LastFailingSince { get; init; }
        /// <summary>Provisioning tags.</summary>
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        public string? ExternalRef { get; init; }
        public bool? IdempotentReplay { get; init; }
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
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        /// <summary>Epoch ms since deliveries started failing, or null while healthy.</summary>
        public long? FailingSince { get; init; }
        public string? ExternalRef { get; init; }
        public bool? IdempotentReplay { get; init; }
        public Dictionary<string, JsonElement>? Metadata { get; init; }
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
        /// <summary>How long the receiver took to answer, in milliseconds.</summary>
        public long? LatencyMs { get; init; }
        /// <summary>The absolute <c>Location</c> of a 3xx answer. Not followed.</summary>
        public string? Location { get; init; }
        public string? EventType { get; init; }
        public int? MaxAttempts { get; init; }
        /// <summary>The W3C trace id of the operation that caused the event.</summary>
        public string? TraceId { get; init; }
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
        /// <summary>The rule that disables a failing endpoint.</summary>
        public WebhookAutoDisableRule? AutoDisable { get; init; }
        public WebhookRetention? Retention { get; init; }
        /// <summary>The versions Atlas can sign with.</summary>
        public List<string>? SignatureVersions { get; init; }
        /// <summary>The destinations an endpoint can deliver to.</summary>
        public List<WebhookDestinationTypeInfo>? DestinationTypes { get; init; }
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
        /// <summary><c>rename</c> (sent whenever the canonical event is) or <c>companion</c> (only for some occurrences).</summary>
        public string? AliasKind { get; init; }
        /// <summary>After <c>SunsetAt</c> (ISO 8601) the deprecated spelling is no longer sent.</summary>
        public string? DeprecatedAt { get; init; }
        public string? SunsetAt { get; init; }
        /// <summary>A JSON pointer into the OpenAPI document's webhooks section.</summary>
        public string? PayloadSchema { get; init; }
        public int? Version { get; init; }
        /// <summary>When the event became available (YYYY-MM-DD).</summary>
        public string? Since { get; init; }
    }

    public sealed class CreateWebhookEndpointBody
    {
        /// <summary>Required unless <see cref="Destination"/> is an event destination.</summary>
        public string? Url { get; set; }
        /// <summary>Each entry is <c>"*"</c> or a known event name.</summary>
        public List<string>? EnabledEvents { get; set; }
        /// <summary><c>canonical</c> or <c>legacy</c>.</summary>
        public string? EventNaming { get; set; }
        public bool? External { get; set; }
        /// <summary><c>v1</c>, <c>v2</c> (the HMAC also covers the URL).</summary>
        public List<string>? SignatureVersions { get; set; }
        /// <summary>Up to 20 addresses; empty leaves the default (the account's owners).</summary>
        public List<string>? AlertContacts { get; set; }
        public string? Description { get; set; }
        /// <summary>An event destination instead of an HTTPS url.</summary>
        public WebhookDestination? Destination { get; set; }
        /// <summary>Your own credentials for the destination (stored encrypted, write-only).</summary>
        public Dictionary<string, object?>? DestinationCredentials { get; set; }
        /// <summary>A stable key of your own: a repeat create with it returns the existing object.</summary>
        public string? ExternalRef { get; set; }
        /// <summary>Provisioning tags (strings, numbers or booleans).</summary>
        public Dictionary<string, object?>? Metadata { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/webhook_endpoints/by_url</c>: find-or-create by url.</summary>
    public sealed class UpsertWebhookEndpointBody
    {
        public string Url { get; set; } = "";
        public List<string>? EnabledEvents { get; set; }
        public bool? Active { get; set; }
        public string? EventNaming { get; set; }
        public bool? External { get; set; }
        public List<string>? SignatureVersions { get; set; }
        public List<string>? AlertContacts { get; set; }
        public string? Description { get; set; }
        public WebhookDestination? Destination { get; set; }
        public Dictionary<string, object?>? DestinationCredentials { get; set; }
        public string? ExternalRef { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }
    }

    /// <summary>The result of an upsert: the endpoint (<c>Created</c> says whether this call created it) and, then, its <c>Secret</c> once.</summary>
    public sealed class WebhookEndpointUpserted
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "webhook_endpoint";
        public string Id { get; init; } = "";
        public string Url { get; init; } = "";
        public List<string> EnabledEvents { get; init; } = new List<string>();
        public bool Active { get; init; }
        public long CreatedAt { get; init; }
        public bool? Created { get; init; }
        public string? Secret { get; init; }
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        public string? EventNaming { get; init; }
        public List<string>? SignatureVersions { get; init; }
        public WebhookDestination? Destination { get; init; }
        public string? Health { get; init; }
        public string? ExternalRef { get; init; }
        public Dictionary<string, JsonElement>? Metadata { get; init; }
    }

    /// <summary>Webhook destination types.</summary>
    public static class WebhookDestinationTypes
    {
        public const string Https = "https";
        public const string AwsEventBridge = "aws_eventbridge";
        public const string GcpPubSub = "gcp_pubsub";
        public const string CloudflareQueue = "cloudflare_queue";
    }

    /// <summary>Describes a destination type the API offers.</summary>
    public sealed class WebhookDestinationTypeInfo
    {
        public string Type { get; init; } = "";
        public string Name { get; init; } = "";
        public List<string>? Config { get; init; }
        public List<string>? Credentials { get; init; }
    }

    /// <summary>
    /// Where an endpoint's deliveries go. <see cref="Type"/> is one of <see cref="WebhookDestinationTypes"/>; set the fields that type
    /// needs. Credentials are never returned (<see cref="CredentialsSet"/> says whether they are stored).
    /// </summary>
    public sealed class WebhookDestination
    {
        public string Type { get; set; } = "";
        // aws_eventbridge
        public string? Region { get; set; }
        public string? EventBusName { get; set; }
        public string? Source { get; set; }
        // gcp_pubsub
        public string? ProjectId { get; set; }
        public string? TopicId { get; set; }
        // cloudflare_queue
        public string? AccountId { get; set; }
        public string? QueueId { get; set; }
        /// <summary>Returned by the API: whether credentials are stored.</summary>
        public bool? CredentialsSet { get; set; }
    }

    /// <summary>The last failed attempt.</summary>
    public sealed class WebhookLastFailure
    {
        public long At { get; init; }
        [JsonPropertyName("status")] public int? HttpStatus { get; init; }
        public string? Error { get; init; }
    }

    /// <summary>The last 24 hours of attempts.</summary>
    public sealed class WebhookAttempts24h
    {
        public int Succeeded { get; init; }
        public int Failed { get; init; }
    }

    /// <summary>Which failure emails went out.</summary>
    public sealed class WebhookAlertsSent
    {
        public long? FirstSentAt { get; init; }
        public long? SecondSentAt { get; init; }
        public long? DisabledSentAt { get; init; }
    }

    /// <summary>When a failing endpoint is switched off.</summary>
    public sealed class WebhookAutoDisableRule
    {
        public long AfterFailingMs { get; init; }
        public int AfterConsecutiveFailures { get; init; }
        public long ConsecutiveFailuresMinFailingMs { get; init; }
        public string? Description { get; init; }
    }

    /// <summary>How long events, delivery logs and replays are kept.</summary>
    public sealed class WebhookRetention
    {
        public int EventsDays { get; init; }
        public int DeliveryLogDays { get; init; }
        public int ReplayWindowDays { get; init; }
    }

    /// <summary>The replay call that re-sends what a re-enabled endpoint missed.</summary>
    public sealed class WebhookReplayHint
    {
        public long Since { get; init; }
        public string Method { get; init; } = "";
        public string Path { get; init; } = "";
        public string? Note { get; init; }
    }

    /// <summary>The inline result of <see cref="WebhookEndpointsResource.TestNowAsync"/>.</summary>
    public sealed class WebhookTestResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string DeliveryId { get; init; } = "";
        public string EventId { get; init; } = "";
        public string Status { get; init; } = "";
        public int? HttpStatus { get; init; }
        public long? LatencyMs { get; init; }
        public string? ResponseSnippet { get; init; }
        public string? Error { get; init; }
        public string? SignatureHeaderSent { get; init; }
        public bool WillRetry { get; init; }
    }

    /// <summary>The body of <c>POST /v1/webhook_endpoints/{id}/redeliver</c>: redeliver every event whose latest attempt in the window has <c>Status</c>.</summary>
    public sealed class RedeliverWebhookWindowBody
    {
        /// <summary>Epoch ms (at most 90 days ago) — or the string <c>"failing_since"</c> to start where the endpoint last started failing.</summary>
        public object? Since { get; set; }
        /// <summary>Epoch ms.</summary>
        public long? Until { get; set; }
        /// <summary><c>failed</c> (default) or <c>success</c>.</summary>
        public string? Status { get; set; }
    }

    /// <summary>The body of <c>POST /v1/webhook_endpoints/{id}/replay</c>: re-send everything since <c>Since</c> that the endpoint subscribes to and has not received.</summary>
    public sealed class ReplayWebhookBody
    {
        /// <summary>Epoch ms (at most 90 days ago) — or the string <c>"failing_since"</c> to start where the endpoint last started failing.</summary>
        public object? Since { get; set; }
        public long? Until { get; set; }
        /// <summary>Limits the replay (comma-separated event names).</summary>
        public string? EventType { get; set; }
    }

    /// <summary>The answer to a bulk redelivery or replay.</summary>
    public sealed class WebhookBulkResult
    {
        /// <summary><c>webhook_redelivery</c> or <c>webhook_replay</c>.</summary>
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string EndpointId { get; init; } = "";
        public int Queued { get; init; }
        public int? EventsConsidered { get; init; }
        public bool HasMore { get; init; }
        /// <summary>When <see cref="HasMore"/>, the <c>since</c> that continues the call.</summary>
        public long? NextSince { get; init; }
        public WebhookRetention? Retention { get; init; }
    }

    /// <summary>Filters an endpoint's delivery log.</summary>
    public sealed class ListWebhookDeliveriesParams
    {
        /// <summary>1-1000 (null = the server default of 50).</summary>
        public int? Limit { get; set; }
        public string? StartingAfter { get; set; }
        /// <summary><c>pending</c>, <c>success</c> or <c>failed</c>.</summary>
        public string? Status { get; set; }
        /// <summary>Comma-separated event names.</summary>
        public string? EventType { get; set; }
        /// <summary>Epoch ms.</summary>
        public long? Since { get; set; }
        public long? Until { get; set; }
        /// <summary>Finds one operation by its W3C trace id.</summary>
        public string? Trace { get; set; }

        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("limit", Limit);
            yield return new KeyValuePair<string, object?>("starting_after", StartingAfter);
            yield return new KeyValuePair<string, object?>("status", Status);
            yield return new KeyValuePair<string, object?>("event_type", EventType);
            yield return new KeyValuePair<string, object?>("since", Since);
            yield return new KeyValuePair<string, object?>("until", Until);
            yield return new KeyValuePair<string, object?>("trace", Trace);
        }
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

        /// <summary>
        /// <c>GET /v1/webhook_endpoints</c> with <c>include_deprecated</c> (adds the deprecated event spellings to the catalogue) and the
        /// <c>metadata[key]=value</c> filters.
        /// </summary>
        public Task<WebhookEndpointList> ListWithEventsAsync(bool? includeDeprecated, IDictionary<string, object?>? metadata = null, CancellationToken ct = default)
        {
            var q = AtlasProvisioning.MetadataQuery(metadata);
            if (includeDeprecated == true) q.Add(new KeyValuePair<string, object?>("include_deprecated", true));
            return Req<WebhookEndpointList>(HttpVerb.Get, "/v1/webhook_endpoints", query: q, cancellationToken: ct);
        }

        /// <summary><c>GET /v1/webhook_endpoints/{id}</c> — one endpoint with its health.</summary>
        public Task<WebhookEndpoint> GetAsync(string id, CancellationToken ct = default)
            => Req<WebhookEndpoint>(HttpVerb.Get, $"/v1/webhook_endpoints/{Enc(id)}", cancellationToken: ct);

        /// <summary>
        /// <c>PUT /v1/webhook_endpoints/by_url</c> — find the endpoint with this url and update it, or create it (then <c>Secret</c> is set,
        /// once). Safe to run on every deploy.
        /// </summary>
        public Task<WebhookEndpointUpserted> UpsertByUrlAsync(UpsertWebhookEndpointBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<WebhookEndpointUpserted>(HttpVerb.Put, "/v1/webhook_endpoints/by_url", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/webhook_endpoints/{id}/test?wait=</c> — send <c>webhook.test</c> now and return the result: status, HTTP status, latency,
        /// response snippet, error and the signature header sent. <paramref name="wait"/> is at most 10 s (<c>"10s"</c>, <c>"5000ms"</c>).
        /// </summary>
        public Task<WebhookTestResult> TestNowAsync(string id, string wait = "10s", CancellationToken ct = default)
            => Req<WebhookTestResult>(HttpVerb.Post, $"/v1/webhook_endpoints/{Enc(id)}/test", query: Q(("wait", wait)), cancellationToken: ct);

        /// <summary><c>POST /v1/webhook_endpoints/{id}/redeliver</c> — redeliver every event whose latest attempt in the window has the given status (default failed).</summary>
        public Task<WebhookBulkResult> RedeliverWindowAsync(string id, RedeliverWebhookWindowBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<WebhookBulkResult>(HttpVerb.Post, $"/v1/webhook_endpoints/{Enc(id)}/redeliver", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/webhook_endpoints/{id}/replay</c> — re-send everything since <c>Since</c> that the endpoint subscribes to and has not received.</summary>
        public Task<WebhookBulkResult> ReplayAsync(string id, ReplayWebhookBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<WebhookBulkResult>(HttpVerb.Post, $"/v1/webhook_endpoints/{Enc(id)}/replay", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

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

        /// <summary>
        /// An endpoint's delivery log with the status, event_type, since/until, trace and starting_after filters (limit up to 1000),
        /// newest first. <c>GET /v1/webhook_endpoints/{id}/deliveries</c>.
        /// </summary>
        public Task<CursorPage<WebhookDelivery>> DeliveriesAsync(string id, ListWebhookDeliveriesParams @params, CancellationToken ct = default)
            => Req<CursorPage<WebhookDelivery>>(HttpVerb.Get, $"/v1/webhook_endpoints/{Enc(id)}/deliveries", query: (@params ?? new ListWebhookDeliveriesParams()).ToQuery(), cancellationToken: ct);

        /// <summary><c>POST /v1/webhook_endpoints/{id}/deliveries/{deliveryId}/redeliver</c> — Manually redeliver a past event as a fresh attempt.</summary>
        public Task<WebhookDeliveryQueued> RedeliverAsync(string id, string deliveryId, CancellationToken ct = default)
            => Req<WebhookDeliveryQueued>(HttpVerb.Post, $"/v1/webhook_endpoints/{Enc(id)}/deliveries/{Enc(deliveryId)}/redeliver", cancellationToken: ct);
    }

    public sealed class UpdateWebhookEndpointBody
    {
        public string? Url { get; set; }
        public List<string>? EnabledEvents { get; set; }
        public bool? Active { get; set; }
        public string? EventNaming { get; set; }
        public bool? External { get; set; }
        public List<string>? SignatureVersions { get; set; }
        public List<string>? AlertContacts { get; set; }
        public string? Description { get; set; }
        public WebhookDestination? Destination { get; set; }
        public Dictionary<string, object?>? DestinationCredentials { get; set; }
        /// <summary>Replaces the provisioning tags.</summary>
        public Dictionary<string, object?>? Metadata { get; set; }

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
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
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
