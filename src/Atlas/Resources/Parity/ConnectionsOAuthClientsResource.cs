using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/connections/oauth_clients</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsOAuthClientsSetBody
    {
        [JsonPropertyName("owner")] public Dictionary<string, object?>? Owner { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("provider")] public string? Provider { get; set; }
        /// <summary>The provider environment the credential belongs to. sandbox selects the provider's test hosts (Salesforce test.salesforce.com, PayPal and QuickBooks sandboxes, DocuSign demo...).</summary>
        [JsonPropertyName("environment")] public string? Environment { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("client_id")] public string? ClientId { get; set; }
        /// <summary>Write-only. Required when the client is first created; omit it to keep the stored secret.</summary>
        [JsonPropertyName("client_secret")] public string? ClientSecret { get; set; }
        [JsonPropertyName("scopes")] public List<string>? Scopes { get; set; }
        /// <summary>Values that fill the provider's host templates for this one connection, for example {"subdomain":"acme"} (Zendesk), {"shop":"acme"} (Shopify), {"domain":"acme.my.salesforce.com"} (Salesforce My Domain), {"data_center":"eu"} (Zoho), {"tenant":"contoso.onmicrosoft.com"} (Microsoft), {"host":"git.examp</summary>
        [JsonPropertyName("host_params")] public Dictionary<string, object?>? HostParams { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>Operations of <c>connections.oauthClients</c>.</summary>
    public sealed partial class ConnectionsOAuthClientsResource : ResourceBase
    {
        public ConnectionsOAuthClientsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>PUT /v1/connections/oauth_clients</c> - Set an owner's own OAuth client (their registered app) for a provider and environment. It overrides the instance client for that owner's connections to connect and to refresh. Write-only secret, sealed in the vault. Creates the client or replaces it.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> SetAsync(ConnectionsOAuthClientsSetBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/connections/oauth_clients", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/connections/oauth_clients</c> - List owner OAuth clients. The secret is never returned.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? ownerType = null, string? ownerId = null, string? provider = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/connections/oauth_clients", query: Q(("owner_type", ownerType), ("owner_id", ownerId), ("provider", provider), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/connections/oauth_clients/{id}</c> - Read one owner OAuth client.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/connections/oauth_clients/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/connections/oauth_clients/{id}</c> - Delete an owner OAuth client and its sealed secret. 409 while connections still hold tokens it issued (they could no longer refresh).</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/connections/oauth_clients/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
