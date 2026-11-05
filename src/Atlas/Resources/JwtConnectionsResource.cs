using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>A trusted-JWT connection: your system signs a short-lived JWT and sends the person to <see cref="LoginUrl"/>.</summary>
    public sealed class JwtConnection
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "jwt_connection";
        public string Id { get; init; } = "";
        public string? OrganizationId { get; init; }
        /// <summary><c>draft</c>, <c>active</c> or <c>disabled</c>.</summary>
        public string Status { get; init; } = "";
        public string? JwksUri { get; init; }
        public string? Issuer { get; init; }
        public string? Audience { get; init; }
        public string EmailClaim { get; init; } = "email";
        public int MaxAgeSeconds { get; init; }
        public string? DefaultRoleId { get; init; }
        /// <summary>Send the person here with <c>?token=&lt;jwt&gt;&amp;redirect_url=&lt;your app URL&gt;</c>.</summary>
        public string LoginUrl { get; init; } = "";
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class JwtConnectionBody
    {
        public string? OrganizationId { get; set; }
        /// <summary><c>draft</c>, <c>active</c> or <c>disabled</c>.</summary>
        public string? Status { get; set; }
        /// <summary>An https URL serving your public signing keys (RS / PS / ES / EdDSA).</summary>
        public string? JwksUri { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        /// <summary>Default <c>email</c>.</summary>
        public string? EmailClaim { get; set; }
        /// <summary><c>exp</c> − <c>iat</c> at most this; 30-3600, default 300.</summary>
        public int? MaxAgeSeconds { get; set; }
        public string? DefaultRoleId { get; set; }
    }

    /// <summary>The trusted-JWT connections namespace (<c>/v1/jwt_connections</c>; scopes <c>sso_connections:read</c> / <c>sso_connections:write</c>).</summary>
    public sealed class JwtConnectionsResource : ResourceBase
    {
        public JwtConnectionsResource(AtlasTransport transport) : base(transport) { }

        /// <summary>Where to send the person's browser to sign in with a trusted JWT (<c>GET /v1/sso/jwt/{connectionId}</c>). <paramref name="loginUrl"/> is the connection's <c>login_url</c>. A URL, not an API call: the token works once, and the 302 carries a one-time ticket that has to reach the browser.</summary>
        public static string SignInUrl(string loginUrl, string token, string redirectUrl)
            => loginUrl + (loginUrl.Contains("?") ? "&" : "?") + "token=" + System.Uri.EscapeDataString(token) + "&redirect_url=" + System.Uri.EscapeDataString(redirectUrl);

        public Task<ListPage<JwtConnection>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<JwtConnection>>(HttpVerb.Get, "/v1/jwt_connections", cancellationToken: ct);

        public Task<JwtConnection> GetAsync(string id, CancellationToken ct = default)
            => Req<JwtConnection>(HttpVerb.Get, $"/v1/jwt_connections/{Enc(id)}", cancellationToken: ct);

        public Task<JwtConnection> CreateAsync(JwtConnectionBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JwtConnection>(HttpVerb.Post, "/v1/jwt_connections", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<JwtConnection> UpdateAsync(string id, JwtConnectionBody body, CancellationToken ct = default)
            => Req<JwtConnection>(HttpVerb.Patch, $"/v1/jwt_connections/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/jwt_connections/{Enc(id)}", cancellationToken: ct);
    }
}
