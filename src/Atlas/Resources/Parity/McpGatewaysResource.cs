using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/mcp_gateways</c>. Only the fields set are sent.</summary>
    public sealed class McpGatewaysCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("upstream_url")] public string? UpstreamUrl { get; set; }
        [JsonPropertyName("upstream_authorization")] public string? UpstreamAuthorization { get; set; }
        [JsonPropertyName("default_policy")] public string? DefaultPolicy { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>Authorize every tool call through the ABAC policy engine as well as the tool policy. The principal is the agent (type Agent, with name, tier, owner_user_id, organization_id, scopes, data), the action is the tool name, and the resource is the tool (type McpTool, with gateway_id, gateway_name, tool an</summary>
        [JsonPropertyName("abac")] public Dictionary<string, object?>? Abac { get; set; }
        /// <summary>Tool name to { allow?, scopes?, conditions?: [{ arg, op: eq|neq|lte|gte|in|not_in|present, value }], approval?, mask?: { fields?, presets?: email|card|ssn|phone } }.</summary>
        [JsonPropertyName("tools")] public Dictionary<string, object?>? Tools { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/mcp_gateways/{id}</c>. Only the fields set are sent.</summary>
    public sealed class McpGatewaysUpdateBody
    {
        /// <summary>Authorize every tool call through the ABAC policy engine as well as the tool policy. The principal is the agent (type Agent, with name, tier, owner_user_id, organization_id, scopes, data), the action is the tool name, and the resource is the tool (type McpTool, with gateway_id, gateway_name, tool an</summary>
        [JsonPropertyName("abac")] public Dictionary<string, object?>? Abac { get; set; }
    }

    /// <summary>Operations of <c>mcpGateways</c>.</summary>
    public sealed partial class McpGatewaysResource : ResourceBase
    {
        public McpGatewaysResource(AtlasTransport t) : base(t) { }

        private McpGatewaysBudgetsResource? _budgets;
        /// <summary>The <c>mcpGateways.budgets</c> operations.</summary>
        public McpGatewaysBudgetsResource Budgets => _budgets ??= new McpGatewaysBudgetsResource(Transport);

        /// <summary><c>GET /v1/mcp_gateways</c> - List the MCP tool gateways of the instance.</summary>
        /// <remarks>Needs the <c>machines:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/mcp_gateways", cancellationToken: ct);

        /// <summary><c>POST /v1/mcp_gateways</c> - Create an MCP gateway: one URL (`url` in the response) that fronts your MCP server. `tools` maps a tool name to its policy (allow, scopes the agent token must hold, argument conditions, approval, response masking); `default_policy` (deny by default) decides tools not listed. upstream_authorization is write-only and sent to the upstream as its Authorization header.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(McpGatewaysCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/mcp_gateways", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/mcp_gateways/{id}</c> - Read one MCP gateway.</summary>
        /// <remarks>Needs the <c>machines:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/mcp_gateways/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/mcp_gateways/{id}</c> - Update an MCP gateway. Omitted fields keep their value; `tools` is replaced as a whole. `abac` turns on authorization of every tool call through the ABAC policy engine.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, McpGatewaysUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/mcp_gateways/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/mcp_gateways/{id}</c> - Delete an MCP gateway.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/mcp_gateways/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
