using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class ActorTokenActor
    {
        public string Sub { get; set; } = "";
    }

    public sealed class ActorToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "actor_token";
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        public ActorTokenActor Actor { get; init; } = new ActorTokenActor();
        /// <summary>The one-time opaque secret, revealed once.</summary>
        public string Token { get; init; } = "";
        public int ExpiresIn { get; init; }
    }

    public sealed class CreateActorTokenBody
    {
        public string UserId { get; set; } = "";
        public ActorTokenActor Actor { get; set; } = new ActorTokenActor();
        /// <summary>Clamped server-side to [1, 3600]; defaults to 60.</summary>
        public int? ExpiresInSeconds { get; set; }
    }

    /// <summary>The actor-tokens namespace (<c>/v1/actor_tokens</c>) — impersonation grants.</summary>
    public sealed class ActorTokensResource : ResourceBase
    {
        public ActorTokensResource(AtlasTransport transport) : base(transport) { }

        public Task<ActorToken> CreateAsync(CreateActorTokenBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ActorToken>(HttpVerb.Post, "/v1/actor_tokens", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<RevokedObject> RevokeAsync(string id, CancellationToken ct = default)
            => Req<RevokedObject>(HttpVerb.Post, $"/v1/actor_tokens/{Enc(id)}/revoke", cancellationToken: ct);
    }
}
