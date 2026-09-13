using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>A user as the BAPI serves it. <c>private_metadata</c> is never returned.</summary>
    public sealed class User
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "user";
        public string Id { get; init; } = "";
        public string? Username { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? ImageUrl { get; init; }
        public Metadata PublicMetadata { get; init; } = new Metadata();
        public bool MfaEnabled { get; init; }
        public bool Banned { get; init; }
        public bool Locked { get; init; }
        /// <summary>Epoch milliseconds, or null if never signed in.</summary>
        public long? LastSignInAt { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class EmailAddress
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "email_address";
        public string Id { get; init; } = "";
        [JsonPropertyName("email_address")] public string Address { get; init; } = "";
        public bool Verified { get; init; }
        public bool Primary { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class UserSession
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "session";
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        public string Status { get; init; } = "";
        public long LastActiveAt { get; init; }
        public long ExpireAt { get; init; }
        public long AbandonAt { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>One sign-in identity on a user: the base Atlas anchor, or a linked provider.</summary>
    public sealed class Identity
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "identity";
        public string Id { get; init; } = "";
        public string Type { get; init; } = "";
        public string Provider { get; init; } = "";
        public string? ProviderUserId { get; init; }
        public string? Email { get; init; }
        public bool? EmailVerified { get; init; }
        public bool IsPrimary { get; init; }
        public bool? HasPassword { get; init; }
    }

    /// <summary>An OAuth consent grant — the scopes a user authorized a client for. Never a token.</summary>
    public sealed class Grant
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "grant";
        public string Id { get; init; } = "";
        public string? ClientId { get; init; }
        public string? ClientName { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        public long GrantedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class OAuthAccessToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "oauth_access_token";
        public string Provider { get; init; } = "";
        /// <summary>The provider access token itself. Field is <c>token</c>, not <c>access_token</c>.</summary>
        public string Token { get; init; } = "";
        public long? ExpiresAt { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        public bool Refreshed { get; init; }
    }

    public sealed class ListUsersParams : CursorParams { }

    public sealed class CreateUserBody
    {
        public string EmailAddress { get; set; } = "";
        public string? Password { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? EmailVerified { get; set; }
        public Metadata? PublicMetadata { get; set; }
        public Metadata? PrivateMetadata { get; set; }
        public Metadata? UnsafeMetadata { get; set; }
    }

    public sealed class UpdateUserBody
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Metadata? PublicMetadata { get; set; }
        public Metadata? PrivateMetadata { get; set; }
    }

    public sealed class ReplaceUserMetadataBody
    {
        public Metadata? PublicMetadata { get; set; }
        public Metadata? PrivateMetadata { get; set; }
        public Metadata? UnsafeMetadata { get; set; }
    }

    public sealed class ResetMfaResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "user";
        public string Id { get; init; } = "";
        public bool MfaEnabled { get; init; }
    }

    public sealed class DeletedMfaFactor
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "mfa_factor";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
    }

    public sealed class RevokeUserSessionsResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "user";
        public string Id { get; init; } = "";
        public int SessionsRevoked { get; init; }
    }

    public sealed class IdentityCollision
    {
        public string Type { get; init; } = "";
        public string Detail { get; init; } = "";
    }

    public sealed class LinkIdentityResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<Identity> Data { get; init; } = new List<Identity>();
        public List<IdentityCollision> Collisions { get; init; } = new List<IdentityCollision>();
    }

    public sealed class ExternalAccountConnection
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "external_account_connection";
        public string Provider { get; init; } = "";
        public string UserId { get; init; } = "";
        public string AttemptId { get; init; } = "";
        public string AuthorizationUrl { get; init; } = "";
        public List<string> Scopes { get; init; } = new List<string>();
    }

    public sealed class UnlinkedIdentity
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "identity";
        public string Id { get; init; } = "";
        public string Provider { get; init; } = "";
        public bool Unlinked { get; init; }
        public string NewUserId { get; init; } = "";
    }

    public sealed class RevokeAllGrantsResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "user";
        public string Id { get; init; } = "";
        public int GrantsRevoked { get; init; }
        public int TokensRevoked { get; init; }
    }

    public sealed class RevokeGrantResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "grant";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
        public int TokensRevoked { get; init; }
    }

    public sealed class ConnectExternalAccountBody
    {
        public string Provider { get; set; } = "";
        public string RedirectUrl { get; set; } = "";
        public List<string>? AdditionalScopes { get; set; }
    }

    /// <summary>The user-management namespace (<c>/v1/users</c>).</summary>
    public sealed class UsersResource : ResourceBase
    {
        public UsersResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>GET /v1/users</c> — cursor-paginated.</summary>
        public Task<CursorPage<User>> ListAsync(ListUsersParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<User>>(HttpVerb.Get, "/v1/users", query: (@params ?? new ListUsersParams()).ToQuery(), cancellationToken: ct);

        public Task<User> GetAsync(string id, CancellationToken ct = default)
            => Req<User>(HttpVerb.Get, $"/v1/users/{Enc(id)}", cancellationToken: ct);

        public Task<User> CreateAsync(CreateUserBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<User>(HttpVerb.Post, "/v1/users", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<User> UpdateAsync(string id, UpdateUserBody body, CancellationToken ct = default)
            => Req<User>(HttpVerb.Patch, $"/v1/users/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>PUT /v1/users/:id/metadata</c> — replaces the named bags wholesale.</summary>
        public Task<User> ReplaceMetadataAsync(string id, ReplaceUserMetadataBody body, CancellationToken ct = default)
            => Req<User>(HttpVerb.Put, $"/v1/users/{Enc(id)}/metadata", body, cancellationToken: ct);

        public Task<User> BanAsync(string id, CancellationToken ct = default)
            => Req<User>(HttpVerb.Post, $"/v1/users/{Enc(id)}/ban", cancellationToken: ct);

        public Task<User> UnbanAsync(string id, CancellationToken ct = default)
            => Req<User>(HttpVerb.Post, $"/v1/users/{Enc(id)}/unban", cancellationToken: ct);

        public Task<User> LockAsync(string id, int? durationInSeconds = null, CancellationToken ct = default)
            => Req<User>(HttpVerb.Post, $"/v1/users/{Enc(id)}/lock",
                body: Body(("duration_in_seconds", durationInSeconds)), cancellationToken: ct);

        public Task<User> UnlockAsync(string id, CancellationToken ct = default)
            => Req<User>(HttpVerb.Post, $"/v1/users/{Enc(id)}/unlock", cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/users/{Enc(id)}", cancellationToken: ct);

        public Task<ResetMfaResult> ResetMfaAsync(string id, CancellationToken ct = default)
            => Req<ResetMfaResult>(HttpVerb.Post, $"/v1/users/{Enc(id)}/reset_mfa", cancellationToken: ct);

        public Task<DeletedMfaFactor> DeleteMfaFactorAsync(string id, string factorId, CancellationToken ct = default)
            => Req<DeletedMfaFactor>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/mfa/{Enc(factorId)}", cancellationToken: ct);

        public Task<ListPage<UserSession>> ListSessionsAsync(string id, CancellationToken ct = default)
            => Req<ListPage<UserSession>>(HttpVerb.Get, $"/v1/users/{Enc(id)}/sessions", cancellationToken: ct);

        public Task<RevokeUserSessionsResult> RevokeSessionsAsync(string id, CancellationToken ct = default)
            => Req<RevokeUserSessionsResult>(HttpVerb.Post, $"/v1/users/{Enc(id)}/sessions/revoke", cancellationToken: ct);

        public Task<EmailAddress> AddEmailAsync(string id, string emailAddress, CancellationToken ct = default)
            => Req<EmailAddress>(HttpVerb.Post, $"/v1/users/{Enc(id)}/email_addresses",
                body: Body(("email_address", emailAddress)), cancellationToken: ct);

        public Task<EmailAddress> VerifyEmailAsync(string id, string emailId, CancellationToken ct = default)
            => Req<EmailAddress>(HttpVerb.Post, $"/v1/users/{Enc(id)}/email_addresses/{Enc(emailId)}/verify", cancellationToken: ct);

        public Task<EmailAddress> SetPrimaryEmailAsync(string id, string emailId, CancellationToken ct = default)
            => Req<EmailAddress>(HttpVerb.Post, $"/v1/users/{Enc(id)}/email_addresses/{Enc(emailId)}/primary", cancellationToken: ct);

        /// <summary><c>GET /v1/users/:id/oauth_access_tokens/:provider</c> — a live provider credential.</summary>
        public Task<OAuthAccessToken> GetOAuthAccessTokenAsync(string id, string provider, CancellationToken ct = default)
            => Req<OAuthAccessToken>(HttpVerb.Get, $"/v1/users/{Enc(id)}/oauth_access_tokens/{Enc(provider)}", cancellationToken: ct);

        /// <summary><c>GET /v1/users/:id/identities</c> — base identity plus one entry per linked account.</summary>
        public Task<ListPage<Identity>> ListIdentitiesAsync(string id, CancellationToken ct = default)
            => Req<ListPage<Identity>>(HttpVerb.Get, $"/v1/users/{Enc(id)}/identities", cancellationToken: ct);

        /// <summary><c>POST /v1/users/:id/identities</c> — merge a secondary user INTO this one.</summary>
        public Task<LinkIdentityResult> LinkIdentityAsync(string id, string secondaryUserId, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<LinkIdentityResult>(HttpVerb.Post, $"/v1/users/{Enc(id)}/identities",
                body: Body(("secondary_user_id", secondaryUserId)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/users/:id/external_accounts/connect</c> — backend-initiated provider link.</summary>
        public Task<ExternalAccountConnection> ConnectExternalAccountAsync(string id, ConnectExternalAccountBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ExternalAccountConnection>(HttpVerb.Post, $"/v1/users/{Enc(id)}/external_accounts/connect", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/:id/identities/:identityId</c> — extract an identity into a new user.</summary>
        public Task<UnlinkedIdentity> UnlinkIdentityAsync(string id, string identityId, CancellationToken ct = default)
            => Req<UnlinkedIdentity>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/identities/{Enc(identityId)}", cancellationToken: ct);

        /// <summary><c>GET /v1/users/:id/grants</c> — the OAuth clients this user has authorized.</summary>
        public Task<ListPage<Grant>> ListGrantsAsync(string id, CancellationToken ct = default)
            => Req<ListPage<Grant>>(HttpVerb.Get, $"/v1/users/{Enc(id)}/grants", cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/:id/grants</c> — revoke every consent grant the user holds.</summary>
        public Task<RevokeAllGrantsResult> RevokeAllGrantsAsync(string id, CancellationToken ct = default)
            => Req<RevokeAllGrantsResult>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/grants", cancellationToken: ct);

        /// <summary><c>DELETE /v1/grants/:id</c> — revoke ONE consent grant (and its live tokens).</summary>
        public Task<RevokeGrantResult> RevokeGrantAsync(string grantId, CancellationToken ct = default)
            => Req<RevokeGrantResult>(HttpVerb.Delete, $"/v1/grants/{Enc(grantId)}", cancellationToken: ct);
    }
}
