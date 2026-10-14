using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

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
    /// <summary>
    /// Deprecated (Sunset 2026-04-01): POST /v1/sign_in_tokens is superseded by
    /// SessionsResource.CreateAsync (POST /v1/sessions), which mints a real
    /// redeemable session in one call.
    /// </summary>
    public sealed class SignInTokensResource : ResourceBase
    {
        public SignInTokensResource(AtlasTransport transport) : base(transport) { }

        public Task<SignInToken> CreateAsync(CreateSignInTokenBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<SignInToken>(HttpVerb.Post, "/v1/sign_in_tokens", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/sign_in_tokens/{id}/revoke</c> — DEPRECATED (Sunset 2027-04-01) — revoke an unused sign-in token before its 60s window elapses.</summary>
        public Task<RevokedSignInToken> RevokeAsync(string id, CancellationToken ct = default)
            => Req<RevokedSignInToken>(HttpVerb.Post, $"/v1/sign_in_tokens/{Enc(id)}/revoke", cancellationToken: ct);
    }

    public sealed class RevokedSignInToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public bool Revoked { get; init; }
    }
}
