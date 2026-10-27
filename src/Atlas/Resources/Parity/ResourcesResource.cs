using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/resources</c>. Only the fields set are sent.</summary>
    public sealed class ResourcesUpsertBody
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("external_id")] public string? ExternalId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("parent")] public Dictionary<string, object?>? Parent { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, object?>? Metadata { get; set; }
    }

    /// <summary>The body of <c>POST /v1/resources/sync</c>. Only the fields set are sent.</summary>
    public sealed class ResourcesSyncBody
    {
        [JsonPropertyName("resources")] public List<object?>? Resources { get; set; }
    }

    /// <summary>The body of <c>POST /v1/resource_checks</c>. Only the fields set are sent.</summary>
    public sealed class ResourcesCheckBody
    {
        [JsonPropertyName("subject")] public Dictionary<string, object?>? Subject { get; set; }
        [JsonPropertyName("permission")] public string? Permission { get; set; }
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("groups")] public List<string>? Groups { get; set; }
    }

    /// <summary>The body of <c>POST /v1/resource_checks/batch</c>. Only the fields set are sent.</summary>
    public sealed class ResourcesCheckBatchBody
    {
        [JsonPropertyName("checks")] public List<object?>? Checks { get; set; }
    }

    /// <summary>The body of <c>POST /v1/resource_checks/list</c>. Only the fields set are sent.</summary>
    public sealed class ResourcesAccessibleBody
    {
        [JsonPropertyName("subject")] public Dictionary<string, object?>? Subject { get; set; }
        [JsonPropertyName("permission")] public string? Permission { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("groups")] public List<string>? Groups { get; set; }
        [JsonPropertyName("limit")] public long? Limit { get; set; }
        [JsonPropertyName("starting_after")] public string? StartingAfter { get; set; }
    }

    /// <summary>Operations of <c>resources</c>.</summary>
    public sealed partial class ResourcesResource : ResourceBase
    {
        public ResourcesResource(AtlasTransport t) : base(t) { }

        private ResourcesTypesResource? _types;
        /// <summary>The <c>resources.types</c> operations.</summary>
        public ResourcesTypesResource Types => _types ??= new ResourcesTypesResource(Transport);

        private ResourcesAssignmentsResource? _assignments;
        /// <summary>The <c>resources.assignments</c> operations.</summary>
        public ResourcesAssignmentsResource Assignments => _assignments ??= new ResourcesAssignmentsResource(Transport);

        /// <summary><c>GET /v1/resources</c> - List resources, optionally filtered by `type` or `parent_id`.</summary>
        /// <remarks>Needs the <c>resources:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? type = null, string? parentId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/resources", query: Q(("type", type), ("parent_id", parentId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>PUT /v1/resources</c> - Create or update one resource by `type` + `external_id` (your own id for it). `parent` is `{id}` or `{type, external_id}` of an existing resource, or null; it must match the type’s `parent_type`.</summary>
        /// <remarks>Needs the <c>resources:write</c> scope.</remarks>
        public Task<JsonElement> UpsertAsync(ResourcesUpsertBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/resources", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/resources/sync</c> - Discovery sync: upsert up to 500 resources in order (list parents before children). Idempotent; call it whenever your app creates or moves resources.</summary>
        /// <remarks>Needs the <c>resources:write</c> scope.</remarks>
        public Task<JsonElement> SyncAsync(ResourcesSyncBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/resources/sync", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/resources/{id}</c> - Read one resource, with its type and parent.</summary>
        /// <remarks>Needs the <c>resources:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/resources/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/resources/{id}</c> - Delete a resource and its role assignments. Its children are detached to the root.</summary>
        /// <remarks>Needs the <c>resources:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/resources/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/resource_checks</c> - Does `subject` hold `permission` on `resource`? A user subject also holds the roles of the organizations they belong to, plus any `groups` ids you pass. The response lists the grants that allow it.</summary>
        /// <remarks>Needs the <c>resources:check</c> scope.</remarks>
        public Task<JsonElement> CheckAsync(ResourcesCheckBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/resource_checks", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/resource_checks/batch</c> - Up to 100 resource checks in one call.</summary>
        /// <remarks>Needs the <c>resources:check</c> scope.</remarks>
        public Task<JsonElement> CheckBatchAsync(ResourcesCheckBatchBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/resource_checks/batch", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/resource_checks/list</c> - List the resources of an optional `type` on which `subject` holds `permission`, paged with `limit` and `starting_after` (the response carries `has_more` and `next_cursor`).</summary>
        /// <remarks>Needs the <c>resources:check</c> scope.</remarks>
        public Task<JsonElement> AccessibleAsync(ResourcesAccessibleBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/resource_checks/list", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
