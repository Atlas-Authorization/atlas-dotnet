using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class JwtTemplate
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "jwt_template";
        public string Name { get; init; } = "";
        public Dictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
    }

    public sealed class CreateJwtTemplateBody
    {
        public string Name { get; set; } = "";
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
    }

    public sealed class DeletedJwtTemplate
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "jwt_template";
        public string Name { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary>The JWT-templates namespace (<c>/v1/jwt_templates</c>), keyed by template name.</summary>
    public sealed class JwtTemplatesResource : ResourceBase
    {
        public JwtTemplatesResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<JwtTemplate>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<JwtTemplate>>(HttpVerb.Get, "/v1/jwt_templates", cancellationToken: ct);

        /// <summary>Fetched by template name.</summary>
        public Task<JwtTemplate> GetAsync(string name, CancellationToken ct = default)
            => Req<JwtTemplate>(HttpVerb.Get, $"/v1/jwt_templates/{Enc(name)}", cancellationToken: ct);

        public Task<JwtTemplate> CreateAsync(CreateJwtTemplateBody body, CancellationToken ct = default)
            => Req<JwtTemplate>(HttpVerb.Post, "/v1/jwt_templates", body, cancellationToken: ct);

        /// <summary>Only the claims are updatable; the name is the key.</summary>
        public Task<JwtTemplate> UpdateAsync(string name, Dictionary<string, string> claims, CancellationToken ct = default)
            => Req<JwtTemplate>(HttpVerb.Patch, $"/v1/jwt_templates/{Enc(name)}", body: Body(("claims", claims)), cancellationToken: ct);

        public Task<DeletedJwtTemplate> DeleteAsync(string name, CancellationToken ct = default)
            => Req<DeletedJwtTemplate>(HttpVerb.Delete, $"/v1/jwt_templates/{Enc(name)}", cancellationToken: ct);
    }
}
