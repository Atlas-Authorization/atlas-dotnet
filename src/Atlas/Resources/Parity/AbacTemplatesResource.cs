using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/abac_templates</c>. Only the fields set are sent.</summary>
    public sealed class AbacTemplatesCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>Required. Policy source with `?principal` and/or `?resource` slots in the scope, e.g. `permit (principal == ?principal, action == Action::"read", resource in ?resource);`.</summary>
        [JsonPropertyName("source")] public string? Source { get; set; }
        /// <summary>The FGA store `fga.check(...)` in the source consults.</summary>
        [JsonPropertyName("fga_store_id")] public string? FgaStoreId { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/abac_templates/{id}</c>. Only the fields set are sent.</summary>
    public sealed class AbacTemplatesUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>Policy source with `?principal` and/or `?resource` slots in the scope, e.g. `permit (principal == ?principal, action == Action::"read", resource in ?resource);`.</summary>
        [JsonPropertyName("source")] public string? Source { get; set; }
        /// <summary>The FGA store `fga.check(...)` in the source consults.</summary>
        [JsonPropertyName("fga_store_id")] public string? FgaStoreId { get; set; }
    }

    /// <summary>Operations of <c>abac.templates</c>.</summary>
    public sealed partial class AbacTemplatesResource : ResourceBase
    {
        public AbacTemplatesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/abac_templates</c> - List the ABAC policy templates, with their slots and how many links each has.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/abac_templates", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/abac_templates</c> - Create a policy template: policy source whose scope uses the slots `?principal` and/or `?resource` (`principal == ?principal`, `resource in ?resource`, `resource is Doc in ?resource`). Slots are filled by a link. With an entity schema set, the template is type-checked on save (a slot stands for any entity type).</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(AbacTemplatesCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/abac_templates", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/abac_templates/{id}</c> - Read one policy template.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/abac_templates/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/abac_templates/{id}</c> - Change a template. A new `source` bumps `version` and takes effect on every link at once (`links_updated` counts them). It is refused, with one error per problem and the link named, if any link could no longer be filled in or would fail the entity schema.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, AbacTemplatesUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/abac_templates/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/abac_templates/{id}</c> - Delete a template. A template with links is refused with 409 unless `force=true`, which deletes the links too.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, bool? force = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/abac_templates/{Enc(id)}", query: Q(("dry_run", dryRun), ("force", force)), cancellationToken: ct);
    }
}
