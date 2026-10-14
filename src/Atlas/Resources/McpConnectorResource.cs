using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class McpConnectorBody
    {
        /// <summary>Master switch for the connector and its OAuth discovery.</summary>
        public bool? Enabled { get; set; }
        /// <summary>Display name shown on the consent screen (max 80 chars).</summary>
        public string? Name { get; set; }
        /// <summary>Which user-scoped tool groups the connector exposes.</summary>
        public List<string>? ExposedScopes { get; set; }
        /// <summary>Let MCP clients self-register an OAuth client (RFC 7591) for the MCP resource.</summary>
        public bool? DynamicRegistration { get; set; }
        /// <summary>Extra redirect-URI hostnames allowed for dynamic registration beyond the built-in MCP clients.</summary>
        public List<string>? AllowedRedirectHosts { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public McpConnectorBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class McpConnectorConfig
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public bool? Enabled { get; init; }
        public string? Name { get; init; }
        public List<string>? ExposedScopes { get; init; }
        public bool? DynamicRegistration { get; init; }
        public List<string>? AllowedRedirectHosts { get; init; }
        /// <summary>The RFC 9728 protected-resource metadata URL.</summary>
        public string? DiscoveryUrl { get; init; }
        /// <summary>The RFC 8707 resource identifier (token audience).</summary>
        public string? Resource { get; init; }
        /// <summary>Whether the MCP resource server has been provisioned.</summary>
        public bool? ResourceServerExists { get; init; }
    }

    /// <summary>The instance's remote MCP connector (<c>/v1/mcp_connector</c>).</summary>
    public sealed class McpConnectorResource : ResourceBase
    {
        public McpConnectorResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/mcp_connector</c> — Read the instance's remote MCP connector: enabled state, exposed tool scopes, dynamic-registration policy, plus the computed discovery URL, resource identifier and whether the resource server exists.</summary>
        public Task<McpConnectorConfig> GetAsync(CancellationToken ct = default)
            => Req<McpConnectorConfig>(HttpVerb.Get, "/v1/mcp_connector", cancellationToken: ct);

        /// <summary><c>PUT /v1/mcp_connector</c> — Configure the remote MCP connector (enabled, name, exposed_scopes, dynamic_registration, allowed_redirect_hosts).</summary>
        public Task<McpConnectorConfig> ReplaceAsync(McpConnectorBody body, CancellationToken ct = default)
            => Req<McpConnectorConfig>(HttpVerb.Put, "/v1/mcp_connector", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/mcp_connector</c> — Disable the remote MCP connector.</summary>
        public Task<McpConnectorConfig> DisableAsync(CancellationToken ct = default)
            => Req<McpConnectorConfig>(HttpVerb.Delete, "/v1/mcp_connector", cancellationToken: ct);
    }
}
