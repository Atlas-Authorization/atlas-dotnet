using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class McpRequest
    {
        public string? Jsonrpc { get; set; }
        public object? Id { get; set; }
        public string? Method { get; set; }
        public object? Params { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public McpRequest Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class McpServer
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string ProtocolVersion { get; init; } = "";
        public McpServerServer? Server { get; init; }
        public Dictionary<string, object?> Capabilities { get; init; } = new Dictionary<string, object?>();
        public List<Dictionary<string, object?>> Tools { get; init; } = new List<Dictionary<string, object?>>();
        public JsonElement? RateLimit { get; init; }
        public List<string>? RecommendedAgentScopes { get; init; }
    }

    public sealed class McpServerServer
    {
        public string? Name { get; init; }
        public string? Version { get; init; }
    }

    public sealed class McpError
    {
        public long? Code { get; init; }
        public string? Message { get; init; }
    }

    /// <summary>JSON-RPC 2.0 envelope.</summary>
    public sealed class McpResponse
    {
        public string? Jsonrpc { get; init; }
        public JsonElement? Id { get; init; }
        public Dictionary<string, object?>? Result { get; init; }
        public McpError? Error { get; init; }
    }

    /// <summary>The Atlas MCP server endpoint (<c>/v1/mcp</c>).</summary>
    public sealed class McpResource : ResourceBase
    {
        public McpResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/mcp</c> — MCP server descriptor and the tools this key may call.</summary>
        public Task<McpServer> DescribeAsync(CancellationToken ct = default)
            => Req<McpServer>(HttpVerb.Get, "/v1/mcp", cancellationToken: ct);

        /// <summary><c>POST /v1/mcp</c> — MCP JSON-RPC endpoint: initialize, tools/list, tools/call, ping.</summary>
        public Task<McpResponse> CallAsync(McpRequest body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<McpResponse>(HttpVerb.Post, "/v1/mcp", body, idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
