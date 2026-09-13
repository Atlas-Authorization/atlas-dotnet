using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class SignInToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "sign_in_token";
        public string UserId { get; init; } = "";
        public string Token { get; init; } = "";
        public int ExpiresIn { get; init; }
    }

    public sealed class CreateSignInTokenBody
    {
        public string UserId { get; set; } = "";
        /// <summary>
        /// Accepted by the wire type but currently ignored server-side — the token
        /// always lives 60 seconds. Kept so the shape matches the BAPI.
        /// </summary>
        public int? ExpiresInSeconds { get; set; }
    }

    /// <summary>The sign-in-tokens namespace (<c>/v1/sign_in_tokens</c>).</summary>
    public sealed class SignInTokensResource : ResourceBase
    {
        public SignInTokensResource(AtlasTransport transport) : base(transport) { }

        public Task<SignInToken> CreateAsync(CreateSignInTokenBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<SignInToken>(HttpVerb.Post, "/v1/sign_in_tokens", body, idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
