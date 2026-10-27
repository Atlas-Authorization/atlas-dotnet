using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/resource_types</c>. Only the fields set are sent.</summary>
    public sealed class ResourcesTypesCreateBody
    {
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("parent_type")] public string? ParentType { get; set; }
        [JsonPropertyName("inherit")] public bool? Inherit { get; set; }
        [JsonPropertyName("roles")] public List<object?>? Roles { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/resource_types/{key}</c>. Only the fields set are sent.</summary>
    public sealed class ResourcesTypesUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("parent_type")] public string? ParentType { get; set; }
        [JsonPropertyName("inherit")] public bool? Inherit { get; set; }
        [JsonPropertyName("roles")] public List<object?>? Roles { get; set; }
    }

    /// <summary>Operations of <c>resources.types</c>.</summary>
    public sealed partial class ResourcesTypesResource : ResourceBase
    {
        public ResourcesTypesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/resource_types</c> - List the resource types (roles, permissions, parent type).</summary>
        /// <remarks>Needs the <c>resources:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/resource_types", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/resource_types</c> - Define a resource type: a `key`, its `roles` (each a set of permissions, optionally `includes` other roles), an optional `parent_type` resources nest under, and `inherit` (default true) controlling whether assignments on a parent flow down.</summary>
        /// <remarks>Needs the <c>resources:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(ResourcesTypesCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/resource_types", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/resource_types/{key}</c> - Read one resource type.</summary>
        /// <remarks>Needs the <c>resources:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string key, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/resource_types/{Enc(key)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/resource_types/{key}</c> - Change a resource type’s name, roles, parent type or inheritance.</summary>
        /// <remarks>Needs the <c>resources:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string key, ResourcesTypesUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/resource_types/{Enc(key)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/resource_types/{key}</c> - Delete a resource type together with its resources and their role assignments.</summary>
        /// <remarks>Needs the <c>resources:write</c> scope.</remarks>
        public Task DeleteAsync(string key, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/resource_types/{Enc(key)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
