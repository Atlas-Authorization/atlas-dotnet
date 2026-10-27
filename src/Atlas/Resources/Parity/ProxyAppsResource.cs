using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/proxy_apps</c>. Only the fields set are sent.</summary>
    public sealed class ProxyAppsCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("hostname")] public string? Hostname { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("upstream_url")] public string? UpstreamUrl { get; set; }
        /// <summary>Path / method rules requiring a role, permission or entitlement.</summary>
        [JsonPropertyName("rules")] public List<object?>? Rules { get; set; }
        [JsonPropertyName("public_paths")] public List<string>? PublicPaths { get; set; }
        [JsonPropertyName("inject_headers")] public bool? InjectHeaders { get; set; }
        [JsonPropertyName("inject_token")] public bool? InjectToken { get; set; }
        /// <summary>Also let an active agent through with its machine token (for example to front an MCP gateway). Its scopes count as the permissions rules check. Default false.</summary>
        [JsonPropertyName("allow_agent_tokens")] public bool? AllowAgentTokens { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/proxy_apps/{id}</c>. Only the fields set are sent.</summary>
    public sealed class ProxyAppsUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("hostname")] public string? Hostname { get; set; }
        [JsonPropertyName("upstream_url")] public string? UpstreamUrl { get; set; }
        /// <summary>Path / method rules requiring a role, permission or entitlement.</summary>
        [JsonPropertyName("rules")] public List<object?>? Rules { get; set; }
        [JsonPropertyName("public_paths")] public List<string>? PublicPaths { get; set; }
        [JsonPropertyName("inject_headers")] public bool? InjectHeaders { get; set; }
        [JsonPropertyName("inject_token")] public bool? InjectToken { get; set; }
        /// <summary>Also let an active agent through with its machine token (for example to front an MCP gateway). Its scopes count as the permissions rules check. Default false.</summary>
        [JsonPropertyName("allow_agent_tokens")] public bool? AllowAgentTokens { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>Operations of <c>proxyApps</c>.</summary>
    public sealed partial class ProxyAppsResource : ResourceBase
    {
        public ProxyAppsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/proxy_apps</c> - List the identity-aware proxy apps — hostnames protected by the forward-auth endpoint, with their path rules.</summary>
        /// <remarks>Needs the <c>proxy_apps:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/proxy_apps", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/proxy_apps</c> - Create a proxy app: a hostname, optional upstream URL, ordered path/method rules requiring a role, permission or entitlement, public paths, and header/token injection. The hostname is unique across the deployment.</summary>
        /// <remarks>Needs the <c>proxy_apps:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(ProxyAppsCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/proxy_apps", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/proxy_apps/{id}</c> - Read one identity-aware proxy app.</summary>
        /// <remarks>Needs the <c>proxy_apps:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/proxy_apps/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/proxy_apps/{id}</c> - Update a proxy app. Omitted fields keep their value.</summary>
        /// <remarks>Needs the <c>proxy_apps:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, ProxyAppsUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/proxy_apps/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/proxy_apps/{id}</c> - Delete a proxy app. Its hostname stops resolving at the forward-auth endpoint.</summary>
        /// <remarks>Needs the <c>proxy_apps:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/proxy_apps/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
