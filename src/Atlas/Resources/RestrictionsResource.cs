using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>
    /// One allowlist or blocklist identifier. Allowlist and blocklist share an
    /// identical route shape, so they share this type and the
    /// <see cref="RestrictionResource"/> — only the path and the <c>object</c>
    /// literal differ.
    /// </summary>
    public sealed class RestrictionIdentifier
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Identifier { get; init; } = "";
        public long CreatedAt { get; init; }
    }

    /// <summary>
    /// The allowlist / blocklist namespace. One class serves both — the path is
    /// bound at construction (<c>/v1/allowlist_identifiers</c> or
    /// <c>/v1/blocklist_identifiers</c>).
    /// </summary>
    public sealed class RestrictionResource : ResourceBase
    {
        private readonly string _path;

        public RestrictionResource(AtlasTransport transport, string path) : base(transport)
        {
            _path = path;
        }

        public Task<ListPage<RestrictionIdentifier>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<RestrictionIdentifier>>(HttpVerb.Get, _path, cancellationToken: ct);

        public Task<RestrictionIdentifier> AddAsync(string identifier, CancellationToken ct = default)
            => Req<RestrictionIdentifier>(HttpVerb.Post, _path, body: Body(("identifier", identifier)), cancellationToken: ct);

        public Task<DeletedObject> RemoveAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"{_path}/{Enc(id)}", cancellationToken: ct);
    }
}
