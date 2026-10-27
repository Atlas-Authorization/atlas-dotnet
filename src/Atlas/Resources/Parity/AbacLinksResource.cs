using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/abac_policy_links</c>. Only the fields set are sent.</summary>
    public sealed class AbacLinksCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("template_id")] public string? TemplateId { get; set; }
        /// <summary>Required. Shown as the deciding policy in decisions.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>An entity such as `{ "type": "User", "id": "alice" }`.</summary>
        [JsonPropertyName("principal")] public Dictionary<string, object?>? Principal { get; set; }
        /// <summary>An entity such as `{ "type": "User", "id": "alice" }`.</summary>
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/abac_policy_links/{id}</c>. Only the fields set are sent.</summary>
    public sealed class AbacLinksUpdateBody
    {
        [JsonPropertyName("template_id")] public string? TemplateId { get; set; }
        /// <summary>Shown as the deciding policy in decisions.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>An entity such as `{ "type": "User", "id": "alice" }`.</summary>
        [JsonPropertyName("principal")] public Dictionary<string, object?>? Principal { get; set; }
        /// <summary>An entity such as `{ "type": "User", "id": "alice" }`.</summary>
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>Operations of <c>abac.links</c>.</summary>
    public sealed partial class AbacLinksResource : ResourceBase
    {
        public AbacLinksResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/abac_policy_links</c> - List template links: a template with its slots filled by entities. Each link’s `source` is what the policy engine evaluates.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? templateId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/abac_policy_links", query: Q(("template_id", templateId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/abac_policy_links</c> - Link a template to entities. Every slot the template uses needs its entity (`principal` for `?principal`, `resource` for `?resource`); one it does not use is refused. The link is evaluated together with the other enabled policies, always from the template’s current source, and is type-checked against the entity schema when one is set.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(AbacLinksCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/abac_policy_links", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/abac_policy_links/{id}</c> - Read one template link.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/abac_policy_links/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/abac_policy_links/{id}</c> - Rename, enable or disable a link, or point its slots at other entities.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, AbacLinksUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/abac_policy_links/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/abac_policy_links/{id}</c> - Delete a link; the template stays.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/abac_policy_links/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
