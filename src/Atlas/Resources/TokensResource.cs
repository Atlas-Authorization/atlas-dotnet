using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class VerifyTokenBody
    {
        public string Token { get; set; } = "";
        /// <summary>Expected <c>azp</c> values, if the token carries an authorized-party claim.</summary>
        public List<string>? AuthorizedParties { get; set; }
    }

    /// <summary>
    /// The authoritative (revocation-aware) token verification verdict. A negative
    /// is reported as <see cref="Verified"/> false with a coarse <see cref="Reason"/>
    /// (<c>invalid</c> | <c>revoked</c>); the identity fields are populated only when
    /// <see cref="Verified"/> is true.
    /// </summary>
    public sealed class TokenVerification
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "token_verification";
        public bool Verified { get; init; }
        public string? Reason { get; init; }
        public string? UserId { get; init; }
        public string? SessionId { get; init; }
        public string? OrganizationId { get; init; }
        public string? OrganizationRole { get; init; }
        public List<string>? OrganizationPermissions { get; init; }
        public bool? Mfa { get; init; }
        /// <summary>True when the cache could not answer and the database was consulted.</summary>
        public bool? CheckedAuthoritatively { get; init; }
    }

    /// <summary>
    /// The authoritative token-verification namespace (<c>/v1/tokens/verify</c>) —
    /// the revocation-aware slow path. Prefer local verification
    /// (<see cref="Atlas.Verification.AtlasBackend"/>) on the hot path.
    /// </summary>
    public sealed class TokensResource : ResourceBase
    {
        public TokensResource(AtlasTransport transport) : base(transport) { }

        public Task<TokenVerification> VerifyAsync(VerifyTokenBody body, CancellationToken ct = default)
            => Req<TokenVerification>(HttpVerb.Post, "/v1/tokens/verify", body, cancellationToken: ct);

        /// <summary>Convenience overload: verify a bare token string.</summary>
        public Task<TokenVerification> VerifyAsync(string token, CancellationToken ct = default)
            => VerifyAsync(new VerifyTokenBody { Token = token }, ct);
    }
}
