using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/network_zones</c>. Only the fields set are sent.</summary>
    public sealed class NetworkZonesCreateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("cidrs")] public List<string>? Cidrs { get; set; }
        [JsonPropertyName("countries")] public List<string>? Countries { get; set; }
        [JsonPropertyName("asns")] public List<long>? Asns { get; set; }
        [JsonPropertyName("anonymizers")] public List<string>? Anonymizers { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/network_zones/{id}</c>. Only the fields set are sent.</summary>
    public sealed class NetworkZonesUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("cidrs")] public List<string>? Cidrs { get; set; }
        [JsonPropertyName("countries")] public List<string>? Countries { get; set; }
        [JsonPropertyName("asns")] public List<long>? Asns { get; set; }
        [JsonPropertyName("anonymizers")] public List<string>? Anonymizers { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>The body of <c>POST /v1/network_zones/{id}/check</c>. Only the fields set are sent.</summary>
    public sealed class NetworkZonesCheckBody
    {
        [JsonPropertyName("ip")] public string? Ip { get; set; }
    }

    /// <summary>Operations of <c>networkZones</c>.</summary>
    public sealed partial class NetworkZonesResource : ResourceBase
    {
        public NetworkZonesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/network_zones</c> - List reusable network zones — named sets of CIDRs, countries, ASNs and anonymizer classes (tor, datacenter, vpn) that network ACL rules reference by zone_id.</summary>
        /// <remarks>Needs the <c>network_acls:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/network_zones", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/network_zones</c> - Create a network zone. `name` is unique per instance; `cidrs`, `countries` (ISO alpha-2), `asns` and `anonymizers` are unioned (any match = in the zone). Malformed entries are refused.</summary>
        /// <remarks>Needs the <c>network_acls:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(NetworkZonesCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/network_zones", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/network_zones/{id}</c> - Read one network zone.</summary>
        /// <remarks>Needs the <c>network_acls:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/network_zones/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/network_zones/{id}</c> - Update a network zone. Sending any of cidrs/countries/asns/anonymizers replaces that list; omitted lists keep their value.</summary>
        /// <remarks>Needs the <c>network_acls:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, NetworkZonesUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/network_zones/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/network_zones/{id}</c> - Delete a network zone. The network ACL rules that reference it are deleted with it.</summary>
        /// <remarks>Needs the <c>network_acls:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/network_zones/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/network_zones/{id}/check</c> - What-if: does an IP address fall inside this zone? Returns the resolved country / ASN / Tor / datacenter facts alongside the answer. Facts the instance has no IP-intelligence data for are null and never match.</summary>
        /// <remarks>Needs the <c>network_acls:read</c> scope.</remarks>
        public Task<JsonElement> CheckAsync(string id, NetworkZonesCheckBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/network_zones/{Enc(id)}/check", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
