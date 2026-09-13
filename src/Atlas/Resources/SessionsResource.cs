using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class Session
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "session";
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        public string Status { get; init; } = "";
        public string? LastActiveOrganizationId { get; init; }
        public string? ImpersonatedBy { get; init; }
        public long LastActiveAt { get; init; }
        public long ExpireAt { get; init; }
        public long AbandonAt { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class SessionActor
    {
        public string Sub { get; set; } = "";
    }

    public sealed class CreateSessionBody
    {
        /// <summary>The user to mint a session for.</summary>
        public string UserId { get; set; } = "";
        /// <summary>Optional impersonation actor — records who is acting as the user.</summary>
        public SessionActor? Actor { get; set; }
    }

    /// <summary>
    /// A freshly-minted session, carrying the bearer <c>jwt</c> and
    /// <c>refresh_token</c> in-body for headless use. Store the refresh token to
    /// keep the session alive.
    /// </summary>
    public sealed class MintedSession
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "session";
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        public string Jwt { get; init; } = "";
        public string RefreshToken { get; init; } = "";
        public int ExpiresIn { get; init; }
        public SessionActor? ImpersonatedBy { get; init; }
    }

    public sealed class RevokedSession
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "session";
        public string Id { get; init; } = "";
        public string Status { get; init; } = "";
    }

    /// <summary>The session-management namespace (<c>/v1/sessions</c>).</summary>
    public sealed class SessionsResource : ResourceBase
    {
        public SessionsResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>POST /v1/sessions</c> — mint a session for a user without the sign-in flow.</summary>
        public Task<MintedSession> CreateAsync(CreateSessionBody body, CancellationToken ct = default)
            => Req<MintedSession>(HttpVerb.Post, "/v1/sessions", body, cancellationToken: ct);

        /// <summary><c>GET /v1/sessions?user_id=</c> — requires a user id.</summary>
        public Task<ListPage<Session>> ListAsync(string userId, CancellationToken ct = default)
            => Req<ListPage<Session>>(HttpVerb.Get, "/v1/sessions", query: Q(("user_id", userId)), cancellationToken: ct);

        public Task<Session> GetAsync(string id, CancellationToken ct = default)
            => Req<Session>(HttpVerb.Get, $"/v1/sessions/{Enc(id)}", cancellationToken: ct);

        public Task<RevokedSession> RevokeAsync(string id, CancellationToken ct = default)
            => Req<RevokedSession>(HttpVerb.Post, $"/v1/sessions/{Enc(id)}/revoke", cancellationToken: ct);
    }
}
