using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class OrganizationSettingsSchemaDocument
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        /// <summary>A JSON Schema (OpenAPI 3.1 dialect). The validated subset is type/required/properties/enum/minimum/maximum/minLength/maxLength/minItems/maxItems/items/additionalProperties/pattern/format (format: email, uri/url, date-time, uuid).</summary>
        public Dictionary<string, object?>? Schema { get; init; }
        /// <summary>Bumped each time the schema is re-registered.</summary>
        public long? Version { get; init; }
        /// <summary>Last registered (epoch ms).</summary>
        public long? UpdatedAt { get; init; }
    }

    /// <summary>The instance-wide JSON Schema for organization settings (<c>/v1/organization_settings_schema</c>).</summary>
    public sealed class OrganizationSettingsSchemaResource : ResourceBase
    {
        public OrganizationSettingsSchemaResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/organization_settings_schema</c> — Fetch the instance-wide JSON Schema registered for organization settings.</summary>
        public Task<OrganizationSettingsSchemaDocument> GetAsync(CancellationToken ct = default)
            => Req<OrganizationSettingsSchemaDocument>(HttpVerb.Get, "/v1/organization_settings_schema", cancellationToken: ct);

        /// <summary><c>POST /v1/organization_settings_schema</c> — Register the instance-wide JSON Schema for organization settings (first registration).</summary>
        public Task<OrganizationSettingsSchemaDocument> CreateAsync(Dictionary<string, object?>? schema, CancellationToken ct = default)
            => Req<OrganizationSettingsSchemaDocument>(HttpVerb.Post, "/v1/organization_settings_schema", Body(("schema", schema)), cancellationToken: ct);

        /// <summary><c>PUT /v1/organization_settings_schema</c> — Replace the instance-wide organization-settings JSON Schema.</summary>
        public Task<OrganizationSettingsSchemaDocument> ReplaceAsync(Dictionary<string, object?>? schema, CancellationToken ct = default)
            => Req<OrganizationSettingsSchemaDocument>(HttpVerb.Put, "/v1/organization_settings_schema", Body(("schema", schema)), cancellationToken: ct);
    }
}
