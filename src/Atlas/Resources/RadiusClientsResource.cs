using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class RadiusClient
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "radius_client";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string NasIdentifier { get; init; } = "";
        public string? IpAddress { get; init; }
        /// <summary>Whether a shared secret is stored — never the secret itself.</summary>
        public bool HasSecret { get; init; }
        /// <summary>Blast-RADIUS (CVE-2024-3596) hardening: require attr 80 on Access-Requests.</summary>
        public bool RequireMessageAuthenticator { get; init; }
        public bool Enabled { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class CreateRadiusClientBody
    {
        public string Name { get; set; } = "";
        public string NasIdentifier { get; set; } = "";
        public string SharedSecret { get; set; } = "";
        /// <summary>An optional exact source-IP match — a single address, not a CIDR range.</summary>
        public string? IpAddress { get; set; }
        public bool? RequireMessageAuthenticator { get; set; }
        public bool? Enabled { get; set; }
    }

    public sealed class UpdateRadiusClientBody
    {
        public string? Name { get; set; }
        public string? NasIdentifier { get; set; }
        /// <summary>Omit or send empty to keep the stored secret (write-only edit).</summary>
        public string? SharedSecret { get; set; }
        public string? IpAddress { get; set; }
        public bool? RequireMessageAuthenticator { get; set; }
        public bool? Enabled { get; set; }
    }

    /// <summary>The RADIUS-clients namespace (<c>/v1/radius_clients</c>) — NAS registrations.</summary>
    public sealed class RadiusClientsResource : ResourceBase
    {
        public RadiusClientsResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<RadiusClient>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<RadiusClient>>(HttpVerb.Get, "/v1/radius_clients", cancellationToken: ct);

        public Task<RadiusClient> CreateAsync(CreateRadiusClientBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<RadiusClient>(HttpVerb.Post, "/v1/radius_clients", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<RadiusClient> GetAsync(string id, CancellationToken ct = default)
            => Req<RadiusClient>(HttpVerb.Get, $"/v1/radius_clients/{Enc(id)}", cancellationToken: ct);

        public Task<RadiusClient> UpdateAsync(string id, UpdateRadiusClientBody body, CancellationToken ct = default)
            => Req<RadiusClient>(HttpVerb.Patch, $"/v1/radius_clients/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/radius_clients/{Enc(id)}", cancellationToken: ct);
    }
}
