using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/devices</c>. Only the fields set are sent.</summary>
    public sealed class DevicesReportBody
    {
        [JsonPropertyName("external_id")] public string? ExternalId { get; set; }
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("user_email")] public string? UserEmail { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("platform")] public string? Platform { get; set; }
        [JsonPropertyName("managed")] public bool? Managed { get; set; }
        [JsonPropertyName("compliant")] public bool? Compliant { get; set; }
        [JsonPropertyName("posture")] public Dictionary<string, object?>? Posture { get; set; }
    }

    /// <summary>The body of <c>POST /v1/device_posture</c>. Only the fields set are sent.</summary>
    public sealed class DevicesIngestPostureBody
    {
        [JsonPropertyName("devices")] public List<object?>? Devices { get; set; }
    }

    /// <summary>Operations of <c>devices</c>.</summary>
    public sealed partial class DevicesResource : ResourceBase
    {
        public DevicesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/devices</c> - List registered devices, filtered by user_id or source.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? userId = null, string? source = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/devices", query: Q(("user_id", userId), ("source", source), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>PUT /v1/devices</c> - Create or refresh one device by (source, external_id), reporting managed/compliant posture. Tie it to a user with user_id or a verified user_email. Access policies read the result through the deviceTrust condition: trusted = a managed, compliant device reported within 30 days.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> ReportAsync(DevicesReportBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/devices", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/devices/{id}</c> - Fetch a device and its latest posture.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/devices/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/devices/{id}</c> - Remove a device from the registry.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task RemoveAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/devices/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/device_posture</c> - Posture ingestion for your MDM/EDR or its relay: up to 500 device reports per call, same fields as PUT /v1/devices.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> IngestPostureAsync(DevicesIngestPostureBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/device_posture", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
