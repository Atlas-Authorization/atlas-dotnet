using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/service_connectors</c>. Only the fields set are sent.</summary>
    public sealed class ServiceConnectorsCreateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("kind")] public string? Kind { get; set; }
        [JsonPropertyName("config")] public Dictionary<string, object?>? Config { get; set; }
        [JsonPropertyName("credentials")] public Dictionary<string, object?>? Credentials { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/service_connectors/{id}</c>. Only the fields set are sent.</summary>
    public sealed class ServiceConnectorsUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("config")] public Dictionary<string, object?>? Config { get; set; }
        [JsonPropertyName("credentials")] public Dictionary<string, object?>? Credentials { get; set; }
    }

    /// <summary>The body of <c>POST /v1/service_connectors/{id}/invoke</c>. Only the fields set are sent.</summary>
    public sealed class ServiceConnectorsInvokeBody
    {
        [JsonPropertyName("operation")] public string? Operation { get; set; }
        [JsonPropertyName("input")] public Dictionary<string, object?>? Input { get; set; }
    }

    /// <summary>Operations of <c>serviceConnectors</c>.</summary>
    public sealed partial class ServiceConnectorsResource : ResourceBase
    {
        public ServiceConnectorsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/service_connector_catalog</c> - The services you can connect: for each kind the credential fields, the config fields and the operations with their inputs. Messaging (Twilio, SendGrid, Slack), CRM (HubSpot, Salesforce), analytics (Segment), fraud (Fingerprint, Arkose Labs, Forter, Sardine), identity verification (Persona, Onfido, Veriff, Stripe Identity) and a generic HTTPS connector.</summary>
        /// <remarks>Needs the <c>service_connectors:read</c> scope.</remarks>
        public Task<JsonElement> CatalogAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/service_connector_catalog", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/service_connectors</c> - List the instance's service connectors. Credentials are never returned.</summary>
        /// <remarks>Needs the <c>service_connectors:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/service_connectors", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/service_connectors</c> - Connect a service. `credentials` are checked against the kind's fields and stored in the vault (never returned again); `config` holds the non-secret settings. 409 when the name is taken.</summary>
        /// <remarks>Needs the <c>service_connectors:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(ServiceConnectorsCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/service_connectors", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/service_connectors/{id}</c> - Read one service connector.</summary>
        /// <remarks>Needs the <c>service_connectors:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/service_connectors/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/service_connectors/{id}</c> - Rename, enable or disable a connector, merge new `config` values, or replace individual `credentials` (written as a new vault version; the others are kept).</summary>
        /// <remarks>Needs the <c>service_connectors:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, ServiceConnectorsUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/service_connectors/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/service_connectors/{id}</c> - Delete a connector and its stored credentials.</summary>
        /// <remarks>Needs the <c>service_connectors:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/service_connectors/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/service_connectors/{id}/invoke</c> - Run one operation of the connector (send a message, read a fraud signal, start an identity check). `input` is checked against the operation's fields. A refusal or failure from the service is 502 CONNECTOR_UPSTREAM_ERROR with its own message. Each call is audited without its input or output.</summary>
        /// <remarks>Needs the <c>service_connectors:invoke</c> scope.</remarks>
        public Task<JsonElement> InvokeAsync(string id, ServiceConnectorsInvokeBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/service_connectors/{Enc(id)}/invoke", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
