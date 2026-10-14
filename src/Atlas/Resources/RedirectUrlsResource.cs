using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class RedirectUrl
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Url { get; init; } = "";
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
    }

    /// <summary>Allowlisted redirect URLs (<c>/v1/redirect_urls</c>).</summary>
    public sealed class RedirectUrlsResource : ResourceBase
    {
        public RedirectUrlsResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/redirect_urls</c> — List the allowlisted OAuth/SSO redirect URLs.</summary>
        public Task<ListPage<RedirectUrl>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<RedirectUrl>>(HttpVerb.Get, "/v1/redirect_urls", cancellationToken: ct);

        /// <summary><c>POST /v1/redirect_urls</c> — Add a permitted OAuth/SSO redirect URL (absolute http(s); idempotent on the URL).</summary>
        public Task<RedirectUrl> CreateAsync(string url, CancellationToken ct = default)
            => Req<RedirectUrl>(HttpVerb.Post, "/v1/redirect_urls", Body(("url", url)), cancellationToken: ct);

        /// <summary><c>GET /v1/redirect_urls/{id}</c> — Fetch one allowlisted redirect URL.</summary>
        public Task<RedirectUrl> GetAsync(string id, CancellationToken ct = default)
            => Req<RedirectUrl>(HttpVerb.Get, $"/v1/redirect_urls/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/redirect_urls/{id}</c> — Remove a redirect URL from the allowlist.</summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/redirect_urls/{Enc(id)}", cancellationToken: ct);
    }
}
