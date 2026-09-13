using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class NetworkAcl
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "network_acl";
        public string Id { get; init; } = "";
        public string Action { get; init; } = "";
        /// <summary>A single IP or CIDR range; refused at write time if malformed.</summary>
        public string Cidr { get; init; } = "";
        public string? Description { get; init; }
        public int Priority { get; init; }
        public bool Enabled { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class CreateNetworkAclBody
    {
        public string Action { get; set; } = "";
        public string Cidr { get; set; } = "";
        public string? Description { get; set; }
        public int? Priority { get; set; }
        public bool? Enabled { get; set; }
    }

    public sealed class UpdateNetworkAclBody
    {
        public string? Action { get; set; }
        public string? Cidr { get; set; }
        public string? Description { get; set; }
        public int? Priority { get; set; }
        public bool? Enabled { get; set; }
    }

    /// <summary>The Network-ACL namespace (<c>/v1/network_acls</c>) — per-instance IP allow/deny rules.</summary>
    public sealed class NetworkAclsResource : ResourceBase
    {
        public NetworkAclsResource(AtlasTransport transport) : base(transport) { }

        /// <summary>All rules for the instance, in priority order.</summary>
        public Task<ListPage<NetworkAcl>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<NetworkAcl>>(HttpVerb.Get, "/v1/network_acls", cancellationToken: ct);

        public Task<NetworkAcl> GetAsync(string id, CancellationToken ct = default)
            => Req<NetworkAcl>(HttpVerb.Get, $"/v1/network_acls/{Enc(id)}", cancellationToken: ct);

        public Task<NetworkAcl> CreateAsync(CreateNetworkAclBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<NetworkAcl>(HttpVerb.Post, "/v1/network_acls", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<NetworkAcl> UpdateAsync(string id, UpdateNetworkAclBody body, CancellationToken ct = default)
            => Req<NetworkAcl>(HttpVerb.Patch, $"/v1/network_acls/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/network_acls/{Enc(id)}", cancellationToken: ct);
    }
}
