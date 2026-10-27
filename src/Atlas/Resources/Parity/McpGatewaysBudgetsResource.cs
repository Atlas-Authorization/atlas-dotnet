using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/mcp_gateways/{id}/budgets</c>. Only the fields set are sent.</summary>
    public sealed class McpGatewaysBudgetsCreateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("scope")] public string? Scope { get; set; }
        [JsonPropertyName("metric")] public string? Metric { get; set; }
        /// <summary>The ceiling, in calls or cost units.</summary>
        [JsonPropertyName("limit")] public double? Limit { get; set; }
        /// <summary>Fixed window length in seconds; null never resets. Required for rate.</summary>
        [JsonPropertyName("window_seconds")] public long? WindowSeconds { get; set; }
        [JsonPropertyName("agent_id")] public string? AgentId { get; set; }
        [JsonPropertyName("tool")] public string? Tool { get; set; }
        /// <summary>cost: what one call costs when cost_arg is not set. Default 1.</summary>
        [JsonPropertyName("cost_per_call")] public double? CostPerCall { get; set; }
        /// <summary>cost: dot path to a number in the call arguments that is the cost of the call.</summary>
        [JsonPropertyName("cost_arg")] public string? CostArg { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/mcp_gateways/{id}/budgets/{budgetId}</c>. Only the fields set are sent.</summary>
    public sealed class McpGatewaysBudgetsUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("scope")] public string? Scope { get; set; }
        [JsonPropertyName("metric")] public string? Metric { get; set; }
        /// <summary>The ceiling, in calls or cost units.</summary>
        [JsonPropertyName("limit")] public double? Limit { get; set; }
        /// <summary>Fixed window length in seconds; null never resets. Required for rate.</summary>
        [JsonPropertyName("window_seconds")] public long? WindowSeconds { get; set; }
        [JsonPropertyName("agent_id")] public string? AgentId { get; set; }
        [JsonPropertyName("tool")] public string? Tool { get; set; }
        /// <summary>cost: what one call costs when cost_arg is not set. Default 1.</summary>
        [JsonPropertyName("cost_per_call")] public double? CostPerCall { get; set; }
        /// <summary>cost: dot path to a number in the call arguments that is the cost of the call.</summary>
        [JsonPropertyName("cost_arg")] public string? CostArg { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>Operations of <c>mcpGateways.budgets</c>.</summary>
    public sealed partial class McpGatewaysBudgetsResource : ResourceBase
    {
        public McpGatewaysBudgetsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/mcp_gateways/{id}/budgets</c> - The cross-call budgets of a gateway, newest first (at most 50): ceilings on how many calls, how much cost, or how many calls per window one agent or one session may make.</summary>
        /// <remarks>Needs the <c>machines:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/mcp_gateways/{Enc(id)}/budgets", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/mcp_gateways/{id}/budgets</c> - Add a budget. `metric` is `calls` (a count), `cost` (the sum of each call's cost: `cost_per_call`, or the number found at `cost_arg` in the call arguments, such as an amount) or `rate` (calls per `window_seconds`, required). `limit` is the ceiling. `window_seconds` makes the counter reset in fixed windows; without it a counter never resets (a per-session one ends with the session). `scope` is `agent` (each agent counts separately) or `session` (each session of each agent; the client must echo the `Mcp-Session-Id` from initialize). `agent_id` limits it to one agent and `tool` to one tool. A call is charged against every budget it touches at once, or against none; only a call that is allowed and about to run is charged.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(string id, McpGatewaysBudgetsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/mcp_gateways/{Enc(id)}/budgets", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/mcp_gateways/{id}/budgets/{budgetId}</c> - Read one budget of an MCP gateway.</summary>
        /// <remarks>Needs the <c>machines:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, string budgetId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/mcp_gateways/{Enc(id)}/budgets/{Enc(budgetId)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/mcp_gateways/{id}/budgets/{budgetId}</c> - Change a budget. Omitted fields keep their value. Raising the limit takes effect on the next call; counters are kept.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, string budgetId, McpGatewaysBudgetsUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/mcp_gateways/{Enc(id)}/budgets/{Enc(budgetId)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/mcp_gateways/{id}/budgets/{budgetId}</c> - Delete a budget and stop counting against it.</summary>
        /// <remarks>Needs the <c>machines:write</c> scope.</remarks>
        public Task DeleteAsync(string id, string budgetId, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/mcp_gateways/{Enc(id)}/budgets/{Enc(budgetId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/mcp_gateways/{id}/budgets/{budgetId}/usage</c> - What one agent has used of a budget right now, with the room left and when the window resets. A per-session budget also needs the `session_id` (the Mcp-Session-Id of that session).</summary>
        /// <remarks>Needs the <c>machines:read</c> scope.</remarks>
        public Task<JsonElement> UsageAsync(string id, string budgetId, string? agentId = null, string? sessionId = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/mcp_gateways/{Enc(id)}/budgets/{Enc(budgetId)}/usage", query: Q(("agent_id", agentId), ("session_id", sessionId)), cancellationToken: ct);
    }
}
