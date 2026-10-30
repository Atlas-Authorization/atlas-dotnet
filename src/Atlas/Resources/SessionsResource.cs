using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

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
        /// <summary>Epoch ms the session ends from inactivity unless there is activity first (same value as <see cref="AbandonAt"/>).</summary>
        public long? IdleExpiresAt { get; init; }
        /// <summary>Custom per-session data your backend (or a post_login Action) attached; null when none.</summary>
        public Dictionary<string, JsonElement>? CustomData { get; init; }
    }

    /// <summary>The session after a keep-alive touch (<c>POST /v1/sessions/{id}/touch</c>).</summary>
    public sealed class SessionTouch
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
        /// <summary>Epoch ms the session ends from inactivity unless there is activity first; never past <see cref="ExpireAt"/>.</summary>
        public long IdleExpiresAt { get; init; }
        public long CreatedAt { get; init; }
        /// <summary>True when this touch moved the inactivity window; false when a touch within the last 60 seconds already did.</summary>
        public bool Extended { get; init; }
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
        /// <summary>Custom per-session data to attach (a JSON object, at most 2 KB).</summary>
        public Dictionary<string, object?>? CustomData { get; set; }
        /// <summary><c>session</c> (the default) or <c>browser_ticket</c>; use <see cref="SessionsResource.CreateBrowserTicketAsync"/> for the latter.</summary>
        public string? Mode { get; set; }
        /// <summary>Browser ticket only: where the ready <c>url</c> lands (one of the instance's allowed origins).</summary>
        public string? RedirectUrl { get; set; }
        /// <summary>Browser ticket only: 1 to 300 (default 60).</summary>
        public int? ExpiresInSeconds { get; set; }
    }

    /// <summary>
    /// A one-time ticket that signs a BROWSER in as the user (<c>POST /v1/sessions</c> with <c>mode: browser_ticket</c>,
    /// or <c>object: testing_sign_in_url</c> from <see cref="TestingResource.CreateSignInUrlAsync"/>). Open <see cref="Url"/>
    /// in the browser, or exchange <see cref="Ticket"/> through the sign-in ticket strategy. No session exists until then.
    /// </summary>
    public sealed class BrowserTicket
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "browser_ticket";
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        /// <summary>The one-time ticket. Single use; on development instances only, a repeat exchange from the same client within 10 seconds returns the first answer.</summary>
        public string Ticket { get; init; } = "";
        /// <summary>The ready sign-in URL; null without a redirect URL.</summary>
        public string? Url { get; init; }
        public string? RedirectUrl { get; init; }
        /// <summary>Seconds.</summary>
        public int ExpiresIn { get; init; }
        /// <summary>ISO 8601.</summary>
        public string ExpiresAt { get; init; } = "";
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
        public Dictionary<string, JsonElement>? CustomData { get; init; }
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

        /// <summary>
        /// <c>POST /v1/sessions</c> with <c>mode: browser_ticket</c> — a one-time ticket that signs a browser in as the user
        /// (support handoffs, E2E). <paramref name="redirectUrl"/> (an allowed origin) makes the answer carry a ready <c>url</c>;
        /// <paramref name="expiresInSeconds"/> is 1 to 300 (default 60). Replaces the deprecated sign-in tokens.
        /// </summary>
        public Task<BrowserTicket> CreateBrowserTicketAsync(string userId, string? redirectUrl = null, int? expiresInSeconds = null, CancellationToken ct = default)
            => Req<BrowserTicket>(HttpVerb.Post, "/v1/sessions", new CreateSessionBody { UserId = userId, Mode = "browser_ticket", RedirectUrl = redirectUrl, ExpiresInSeconds = expiresInSeconds }, cancellationToken: ct);

        /// <summary><c>GET /v1/sessions?user_id=</c> — requires a user id.</summary>
        public Task<ListPage<Session>> ListAsync(string userId, CancellationToken ct = default)
            => Req<ListPage<Session>>(HttpVerb.Get, "/v1/sessions", query: Q(("user_id", userId)), cancellationToken: ct);

        public Task<Session> GetAsync(string id, CancellationToken ct = default)
            => Req<Session>(HttpVerb.Get, $"/v1/sessions/{Enc(id)}", cancellationToken: ct);

        public Task<RevokedSession> RevokeAsync(string id, CancellationToken ct = default)
            => Req<RevokedSession>(HttpVerb.Post, $"/v1/sessions/{Enc(id)}/revoke", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/sessions/{id}/touch</c> — keep a session alive from your backend: slides its
        /// inactivity window as if the user had just loaded a page, never past the absolute expiry.
        /// <see cref="SessionTouch.Extended"/> is false when a touch within the last minute already moved
        /// it, so calling it on every request is safe. An ended session throws
        /// <see cref="AtlasConflictException"/> (<c>SESSION_REVOKED</c>, <c>SESSION_IDLE_EXPIRED</c> or <c>SESSION_EXPIRED</c>).
        /// </summary>
        public Task<SessionTouch> TouchAsync(string id, CancellationToken ct = default)
            => Req<SessionTouch>(HttpVerb.Post, $"/v1/sessions/{Enc(id)}/touch", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/sessions/{id}/integration_tokens/{integration}</c> — mint an identity-integration token for this session, the
        /// server-side form of <c>getToken({ for })</c>: a preset such as <c>chatly</c>, or a custom integration id.
        /// </summary>
        public Task<IntegrationToken> GetIntegrationTokenAsync(string id, string integration, CancellationToken ct = default)
            => Req<IntegrationToken>(HttpVerb.Post, $"/v1/sessions/{Enc(id)}/integration_tokens/{Enc(integration)}", cancellationToken: ct);

        /// <summary>
        /// <see cref="GetIntegrationTokenAsync(string, string, CancellationToken)"/> for a per-call audience. <paramref name="audience"/> is the
        /// full audience; <paramref name="audienceParams"/> carries the integration's own parameter (a site id, a channel, an organization id; the one
        /// named in its <c>per_call_audience.param</c>). The token's <c>aud</c> is that audience when the integration permits it; a value it does not permit is refused (403).
        /// </summary>
        public Task<IntegrationToken> GetIntegrationTokenAsync(string id, string integration, string? audience, IDictionary<string, string>? audienceParams, CancellationToken ct = default)
        {
            var pairs = new List<KeyValuePair<string, object?>>();
            if (audienceParams != null) foreach (var kv in audienceParams) pairs.Add(new KeyValuePair<string, object?>(kv.Key, kv.Value));
            if (!string.IsNullOrEmpty(audience)) pairs.Add(new KeyValuePair<string, object?>("audience", audience));
            return Req<IntegrationToken>(HttpVerb.Post, $"/v1/sessions/{Enc(id)}/integration_tokens/{Enc(integration)}", query: pairs, cancellationToken: ct);
        }

        /// <summary><c>POST /v1/sessions/{id}/tokens/{template}</c> — Mint a named JWT-template token for a session (Clerk SessionApi.getToken).</summary>
        public Task<SessionTemplateToken> CreateTokenAsync(string id, string template, CancellationToken ct = default)
            => Req<SessionTemplateToken>(HttpVerb.Post, $"/v1/sessions/{Enc(id)}/tokens/{Enc(template)}", cancellationToken: ct);

        /// <summary>
        /// <c>PUT /v1/sessions/{id}/custom_data</c> — replace the session's custom data (null clears it).
        /// At most 2 KB; only an active session can be changed. Not for secrets: the signed-in user can
        /// read it. Sends the <c>session.updated</c> webhook.
        /// </summary>
        public Task<Session> SetCustomDataAsync(string id, Dictionary<string, object?>? customData, CancellationToken ct = default)
            => Req<Session>(HttpVerb.Put, $"/v1/sessions/{Enc(id)}/custom_data", new Dictionary<string, object?> { ["custom_data"] = customData }, cancellationToken: ct);

        /// <summary><c>PATCH /v1/sessions/{id}/custom_data</c> — merge keys; a key set to null is removed.</summary>
        public Task<Session> UpdateCustomDataAsync(string id, Dictionary<string, object?> patch, CancellationToken ct = default)
            => Req<Session>(HttpVerb.Patch, $"/v1/sessions/{Enc(id)}/custom_data", new Dictionary<string, object?> { ["custom_data"] = patch }, cancellationToken: ct);
    }

    public sealed class SessionTemplateToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Jwt { get; init; } = "";
        public long ExpiresIn { get; init; }
    }
}
