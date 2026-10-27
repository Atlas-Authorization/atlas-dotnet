using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/abac_schema</c>. Only the fields set are sent.</summary>
    public sealed class AbacSchemaSetBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("schema")] public Dictionary<string, object?>? Schema { get; set; }
    }

    /// <summary>The body of <c>POST /v1/abac_schema/validate</c>. Only the fields set are sent.</summary>
    public sealed class AbacSchemaValidateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("schema")] public Dictionary<string, object?>? Schema { get; set; }
        [JsonPropertyName("template")] public bool? Template { get; set; }
    }

    /// <summary>Operations of <c>abac.schema</c>.</summary>
    public sealed partial class AbacSchemaResource : ResourceBase
    {
        public AbacSchemaResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/abac_schema</c> - The instance’s ABAC entity schema (`schema` is null and `version` 0 when none is set).</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/abac_schema", cancellationToken: ct);

        /// <summary><c>PUT /v1/abac_schema</c> - Set the ABAC entity schema: `entity_types` (attributes with `boolean`, `long`, `string`, `set`, `entity` and `record` types, `required: false` for optional ones, and `member_of`) and `actions` (`applies_to` principal and resource types and a `context`). Every problem is reported with its path. Once set, policies and templates are type-checked when saved: unknown attributes, optional attributes read without a `has` guard, wrong operand types, unknown entity types and actions, and actions that do not apply to the policy’s principal and resource types. The response lists the errors the schema would give the policies and links already stored.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task<JsonElement> SetAsync(AbacSchemaSetBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/abac_schema", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/abac_schema</c> - Remove the entity schema; policies are no longer type-checked.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task DeleteAsync(bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, "/v1/abac_schema", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/abac_schema/validate</c> - Type-check policy (or, with `template: true`, template) source against the saved schema, or one sent as `schema`, without saving anything. Returns every issue with its policy number, clause, character offset, line and column. A syntax error is reported as an issue too.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> ValidateAsync(AbacSchemaValidateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/abac_schema/validate", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
