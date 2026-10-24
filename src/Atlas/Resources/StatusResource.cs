using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    // Atlas service status for your instance: the incident subscription (owner email, status.incident.* webhooks, a private
    // RSS / Atom feed) filtered by region, component and impact; the incidents that concern you; and the upstream status
    // pages (any Statuspage-compatible URL) your app depends on.
    //
    // The public Statuspage-compatible feed itself needs no key: https://<frontend api>/v1/status/api/v2/summary.json.

    /// <summary>Status webhook events.</summary>
    public static class StatusEvents
    {
        public const string IncidentCreated = "status.incident.created";
        public const string IncidentUpdated = "status.incident.updated";
        public const string IncidentResolved = "status.incident.resolved";
    }

    /// <summary>Preflight codes of the status dependencies.</summary>
    public static class StatusPreflightCodes
    {
        public const string DependencyUnreachable = "status_dependency_unreachable";
        public const string DependencyOutage = "status_dependency_outage";
    }

    /// <summary>Status components.</summary>
    public static class StatusComponents
    {
        public const string FrontendApi = "frontend_api";
        public const string BackendApi = "backend_api";
        public const string Database = "database";
        public const string Cache = "cache";
        public const string BackgroundJobs = "background_jobs";
    }

    public sealed class StatusGroup
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public List<string> ComponentIds { get; init; } = new List<string>();
    }

    public sealed class StatusComponent
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary><c>operational</c>, <c>degraded</c>, <c>major_outage</c> or <c>unknown</c>.</summary>
        public string Status { get; init; } = "";
        public string? Description { get; init; }
        public string? Group { get; init; }
        public List<string>? Regions { get; init; }
    }

    /// <summary>An open incident on <c>GET /v1/status</c>.</summary>
    public sealed class StatusIncidentSummary
    {
        public string Id { get; init; } = "";
        /// <summary><c>incident</c> or <c>maintenance</c>.</summary>
        public string Kind { get; init; } = "";
        public string Title { get; init; } = "";
        /// <summary><c>investigating</c>, <c>identified</c>, <c>monitoring</c>, <c>resolved</c>, <c>scheduled</c>, <c>in_progress</c> or <c>completed</c>.</summary>
        public string Status { get; init; } = "";
        /// <summary><c>none</c>, <c>minor</c>, <c>major</c> or <c>critical</c>.</summary>
        public string Impact { get; init; } = "";
        public List<string>? Regions { get; init; }
        public List<string>? ComponentIds { get; init; }
        public long StartedAt { get; init; }
        public long? ResolvedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    /// <summary><c>GET /v1/status</c> (public).</summary>
    public sealed class ServiceStatus
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary><c>operational</c>, <c>degraded</c> or <c>major_outage</c>.</summary>
        public string Status { get; init; } = "";
        public long UpdatedAt { get; init; }
        public List<string>? Regions { get; init; }
        public List<StatusGroup>? Groups { get; init; }
        public List<StatusComponent> Components { get; init; } = new List<StatusComponent>();
        public List<StatusIncidentSummary>? Incidents { get; init; }
    }

    public sealed class StatusAvailableComponent
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
    }

    /// <summary>The instance's incident subscription.</summary>
    public sealed class StatusSubscription
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>A subscription exists (it may be disabled).</summary>
        public bool Subscribed { get; init; }
        public bool Enabled { get; init; }
        /// <summary>Empty = every region.</summary>
        public List<string> Regions { get; init; } = new List<string>();
        /// <summary>Empty = every component.</summary>
        public List<string> ComponentIds { get; init; } = new List<string>();
        /// <summary><c>minor</c>, <c>major</c> or <c>critical</c>.</summary>
        public string? MinImpact { get; init; }
        public bool IncludeMaintenance { get; init; }
        public bool EmailOwners { get; init; }
        public bool Webhook { get; init; }
        /// <summary>The private feed; treat it as a secret (<see cref="StatusResource.RotateFeedAsync"/> replaces it).</summary>
        public string? RssUrl { get; init; }
        public string? AtomUrl { get; init; }
        public List<string>? AvailableRegions { get; init; }
        public List<StatusAvailableComponent>? AvailableComponents { get; init; }
        public long? UpdatedAt { get; init; }
    }

    /// <summary>The body of <c>PUT /v1/instance/status_subscription</c>: only the fields set change. Set <c>Regions</c> or <c>ComponentIds</c> to an empty list to mean "every".</summary>
    public sealed class UpdateStatusSubscriptionBody
    {
        public bool? Enabled { get; set; }
        public List<string>? Regions { get; set; }
        public List<string>? ComponentIds { get; set; }
        /// <summary><c>minor</c>, <c>major</c> or <c>critical</c>.</summary>
        public string? MinImpact { get; set; }
        public bool? IncludeMaintenance { get; set; }
        public bool? EmailOwners { get; set; }
        public bool? Webhook { get; set; }
    }

    public sealed class StatusIncidentUpdate
    {
        public string Id { get; init; } = "";
        public string Status { get; init; } = "";
        public string Body { get; init; } = "";
        public long CreatedAt { get; init; }
    }

    /// <summary>An incident that concerns this instance.</summary>
    public sealed class StatusIncident
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Kind { get; init; } = "";
        public string Title { get; init; } = "";
        public string Status { get; init; } = "";
        public string Impact { get; init; } = "";
        public List<string>? Regions { get; init; }
        public List<string>? ComponentIds { get; init; }
        public long StartedAt { get; init; }
        public long? ResolvedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary><c>atlas</c>, <c>import</c> or another source.</summary>
        public string? Source { get; init; }
        public long? ScheduledFor { get; init; }
        public long? ScheduledUntil { get; init; }
        /// <summary>Oldest first.</summary>
        public List<StatusIncidentUpdate> Updates { get; init; } = new List<StatusIncidentUpdate>();
    }

    /// <summary>The data of the <c>status.incident.created</c> / <c>updated</c> / <c>resolved</c> webhook events.</summary>
    public sealed class StatusIncidentEventData
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Kind { get; init; } = "";
        public string Title { get; init; } = "";
        public string Status { get; init; } = "";
        public string Impact { get; init; } = "";
        public List<string>? Regions { get; init; }
        public List<string>? ComponentIds { get; init; }
        /// <summary>The part of the incident that matches your subscription.</summary>
        public List<string>? AffectedRegions { get; init; }
        public List<string>? AffectedComponentIds { get; init; }
        public long StartedAt { get; init; }
        public long? ResolvedAt { get; init; }
        public StatusIncidentUpdate? Update { get; init; }
        /// <summary>The incident on the status page.</summary>
        public string? Url { get; init; }
    }

    public sealed class StatusDependencyComponent
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Status { get; init; } = "";
    }

    public sealed class StatusDependencyIncident
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Status { get; init; } = "";
        public string Impact { get; init; } = "";
        [JsonPropertyName("updatedAt")] public string? UpdatedAt { get; init; }
    }

    /// <summary>An upstream status page your app depends on.</summary>
    public sealed class StatusDependency
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary>The base URL of the Statuspage-compatible page (Atlas reads <c>{url}/api/v2/summary.json</c>).</summary>
        public string Url { get; init; } = "";
        public List<string>? ComponentFilter { get; init; }
        public bool Enabled { get; init; }
        /// <summary><c>operational</c>, <c>degraded</c>, <c>partial_outage</c>, <c>major_outage</c>, <c>maintenance</c> or <c>unknown</c>.</summary>
        public string State { get; init; } = "";
        public string? Indicator { get; init; }
        public string? Description { get; init; }
        public List<StatusDependencyComponent>? Components { get; init; }
        public List<StatusDependencyIncident>? Incidents { get; init; }
        public long? LastCheckedAt { get; init; }
        public long? LastSuccessAt { get; init; }
        public string? LastError { get; init; }
        /// <summary>The last check failed or none has succeeded yet, so <see cref="State"/> may be out of date.</summary>
        public bool Stale { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>The dependency list with the worst state among the enabled ones.</summary>
    public sealed class StatusDependencyList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public List<StatusDependency> Data { get; init; } = new List<StatusDependency>();
        public string State { get; init; } = "";
    }

    public sealed class StatusIncidentList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public List<StatusIncident> Data { get; init; } = new List<StatusIncident>();
    }

    /// <summary>The body of <c>POST /v1/instance/status_dependencies</c>.</summary>
    public sealed class CreateStatusDependencyBody
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public List<string>? ComponentFilter { get; set; }
        public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/instance/status_dependencies/{id}</c>.</summary>
    public sealed class UpdateStatusDependencyBody
    {
        public string? Name { get; set; }
        public List<string>? ComponentFilter { get; set; }
        public bool? Enabled { get; set; }
    }

    /// <summary>The service status, the instance's incident subscription and its status dependencies.</summary>
    public sealed class StatusResource : ResourceBase
    {
        public StatusResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/status</c> — the public service status (components with group and regions, open incidents).</summary>
        public Task<ServiceStatus> ServiceAsync(CancellationToken ct = default)
            => Req<ServiceStatus>(HttpVerb.Get, "/v1/status", cancellationToken: ct);

        /// <summary><c>GET /v1/instance/status_subscription</c></summary>
        public Task<StatusSubscription> GetSubscriptionAsync(CancellationToken ct = default)
            => Req<StatusSubscription>(HttpVerb.Get, "/v1/instance/status_subscription", cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/status_subscription</c> — create or change the subscription.</summary>
        public Task<StatusSubscription> UpdateSubscriptionAsync(UpdateStatusSubscriptionBody body, CancellationToken ct = default)
            => Req<StatusSubscription>(HttpVerb.Put, "/v1/instance/status_subscription", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/status_subscription</c> — turn the subscription off (settings are kept).</summary>
        public Task<StatusSubscription> DisableSubscriptionAsync(CancellationToken ct = default)
            => Req<StatusSubscription>(HttpVerb.Delete, "/v1/instance/status_subscription", cancellationToken: ct);

        /// <summary><c>POST /v1/instance/status_subscription/rotate_feed</c> — issue a new private feed URL; the old one 404s.</summary>
        public Task<StatusSubscription> RotateFeedAsync(CancellationToken ct = default)
            => Req<StatusSubscription>(HttpVerb.Post, "/v1/instance/status_subscription/rotate_feed", cancellationToken: ct);

        /// <summary><c>GET /v1/instance/status_incidents</c> — the incidents that concern this instance, newest first. <paramref name="open"/> lists only unresolved ones.</summary>
        public Task<StatusIncidentList> ListIncidentsAsync(bool? open = null, int? limit = null, CancellationToken ct = default)
            => Req<StatusIncidentList>(HttpVerb.Get, "/v1/instance/status_incidents", query: Q(("open", open), ("limit", limit)), cancellationToken: ct);

        /// <summary><c>GET /v1/instance/status_dependencies</c> — the dependencies, with <c>State</c> the worst among the enabled ones.</summary>
        public Task<StatusDependencyList> ListDependenciesAsync(CancellationToken ct = default)
            => Req<StatusDependencyList>(HttpVerb.Get, "/v1/instance/status_dependencies", cancellationToken: ct);

        /// <summary><c>POST /v1/instance/status_dependencies</c> — add a dependency; Atlas reads it at once, so the answer carries the state or <c>LastError</c>.</summary>
        public Task<StatusDependency> CreateDependencyAsync(CreateStatusDependencyBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<StatusDependency>(HttpVerb.Post, "/v1/instance/status_dependencies", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/instance/status_dependencies/{id}</c></summary>
        public Task<StatusDependency> GetDependencyAsync(string id, CancellationToken ct = default)
            => Req<StatusDependency>(HttpVerb.Get, $"/v1/instance/status_dependencies/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/instance/status_dependencies/{id}</c></summary>
        public Task<StatusDependency> UpdateDependencyAsync(string id, UpdateStatusDependencyBody body, CancellationToken ct = default)
            => Req<StatusDependency>(HttpVerb.Patch, $"/v1/instance/status_dependencies/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/status_dependencies/{id}</c></summary>
        public Task<DeletedObject> DeleteDependencyAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/instance/status_dependencies/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/instance/status_dependencies/{id}/refresh</c> — read the upstream page now.</summary>
        public Task<StatusDependency> RefreshDependencyAsync(string id, CancellationToken ct = default)
            => Req<StatusDependency>(HttpVerb.Post, $"/v1/instance/status_dependencies/{Enc(id)}/refresh", cancellationToken: ct);
    }
}
