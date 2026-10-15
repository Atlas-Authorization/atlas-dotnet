using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

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
        public string? LastActiveOrganizationId { get; init; }
        /// <summary>The actor id when this session is an impersonation; null otherwise.</summary>
        public string? ImpersonatedBy { get; init; }
        public long LastActiveAt { get; init; }
        public long ExpireAt { get; init; }
        public long AbandonAt { get; init; }
        public long CreatedAt { get; init; }
        /// <summary>Epoch ms the session ends from inactivity unless there is activity first (same value as <see cref="AbandonAt"/>).</summary>
        public long? IdleExpiresAt { get; init; }
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
        /// <summary><c>false</c> (the default) guarantees no email, SMS or push is sent to the user as a consequence of the create.</summary>
        public bool? Notify { get; set; }
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

        /// <summary><c>POST /v1/users/{id}/password</c> — Set (reset) a user's password from the backend.</summary>
        public Task<User> SetPasswordAsync(string id, SetUserPasswordBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<User>(HttpVerb.Post, $"/v1/users/{Enc(id)}/password", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/directives</c> — Impose a cross-property directive on a user (e.g.</summary>
        public Task<Directive> SetDirectiveAsync(string id, SetUserDirectiveBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Directive>(HttpVerb.Post, $"/v1/users/{Enc(id)}/directives", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/directives</c> — Lift a previously imposed directive from a user.</summary>
        public Task<DirectiveLifted> LiftDirectiveAsync(string id, CancellationToken ct = default)
            => Req<DirectiveLifted>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/directives", cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/passkeys/{passkeyId}</c> — Remove one of a user’s passkeys (Clerk deleteUserPasskey).</summary>
        public Task<DeletedObject> DeletePasskeyAsync(string id, string passkeyId, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/passkeys/{Enc(passkeyId)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/profile_image</c> — Clear a user’s profile image (Clerk deleteUserProfileImage).</summary>
        public Task<User> DeleteProfileImageAsync(string id, CancellationToken ct = default)
            => Req<User>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/profile_image", cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/phone_numbers</c> — Add a phone number to a user.</summary>
        public Task<PhoneNumber> AddPhoneNumberAsync(string id, string phoneNumber, CancellationToken ct = default)
            => Req<PhoneNumber>(HttpVerb.Post, $"/v1/users/{Enc(id)}/phone_numbers", Body(("phone_number", phoneNumber)), cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/phone_numbers/{pid}/verify</c> — Mark a phone number verified on the backend authority.</summary>
        public Task<PhoneNumber> VerifyPhoneNumberAsync(string id, string pid, CancellationToken ct = default)
            => Req<PhoneNumber>(HttpVerb.Post, $"/v1/users/{Enc(id)}/phone_numbers/{Enc(pid)}/verify", cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/phone_numbers/{pid}</c> — Remove a phone number from a user.</summary>
        public Task<DeletedObject> DeletePhoneNumberAsync(string id, string pid, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/phone_numbers/{Enc(pid)}", cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/progression/events</c> — Append an idempotent progression event (XP / level / state) for a user, scoped to the calling source.</summary>
        public Task<ProgressionEvent> RecordProgressionEventAsync(string id, ProgressionEventBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ProgressionEvent>(HttpVerb.Post, $"/v1/users/{Enc(id)}/progression/events", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/state</c> — Read a user's portable state — progression, tier, flair, entitlements, directives and roles.</summary>
        public Task<UserState> GetStateAsync(string id, CancellationToken ct = default)
            => Req<UserState>(HttpVerb.Get, $"/v1/users/{Enc(id)}/state", cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/consents</c> — Record a versioned consent grant (or withdrawal, `status: "withdrawn"`) for a user.</summary>
        public Task<Consent> RecordConsentAsync(string id, RecordConsentBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Consent>(HttpVerb.Post, $"/v1/users/{Enc(id)}/consents", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/consents</c> — List a user's recorded consents with their versions and timestamps.</summary>
        public Task<DataList<Consent>> ListConsentsAsync(string id, CancellationToken ct = default)
            => Req<DataList<Consent>>(HttpVerb.Get, $"/v1/users/{Enc(id)}/consents", cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/organization_memberships</c> — List the organizations a user belongs to with each membership's role, the org's resolved plan, and the join time.</summary>
        public Task<CursorPage<UserOrganizationMembership>> ListOrganizationMembershipsAsync(string id, int? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<CursorPage<UserOrganizationMembership>>(HttpVerb.Get, $"/v1/users/{Enc(id)}/organization_memberships", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/deletion</c> — Schedule a GRACEFUL deletion of a user: opens a cancellation grace window (grace_seconds, default the instance erasure grace of 7 days) and emits user.deletion_scheduled.</summary>
        public Task<User> ScheduleDeletionAsync(string id, double? graceSeconds = null, CancellationToken ct = default)
            => Req<User>(HttpVerb.Post, $"/v1/users/{Enc(id)}/deletion", Body(("grace_seconds", graceSeconds)), cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/deletion/cancel</c> — Cancel a scheduled deletion inside its grace window, aborting the purge so the account is NOT erased; emits user.deletion_canceled.</summary>
        public Task<User> CancelDeletionAsync(string id, CancellationToken ct = default)
            => Req<User>(HttpVerb.Post, $"/v1/users/{Enc(id)}/deletion/cancel", cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/deletion/finalize</c> — Finalize a scheduled deletion whose grace window has lapsed — the idempotent purge trigger the worker calls on schedule (a cron/operator may too).</summary>
        public Task<DeletedObject> FinalizeDeletionAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Post, $"/v1/users/{Enc(id)}/deletion/finalize", cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/erasure_acks</c> — An integrating app acknowledges it has purged its copy of the user data during a scheduled deletion, identified by source (or the app_id alias), and may contribute a JSON payload Atlas folds into the DSAR erasure export.</summary>
        public Task<UserErasureAck> AcknowledgeErasureAsync(string id, ErasureAckBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<UserErasureAck>(HttpVerb.Post, $"/v1/users/{Enc(id)}/erasure_acks", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/erasure_export</c> — The DSAR erasure export: Atlas's own data about the subject (the same no-secret bundle the self-service export produces) PLUS every app's contributed blob from its erasure ack, aggregated into one package.</summary>
        public Task<UserErasureExport> GetErasureExportAsync(string id, CancellationToken ct = default)
            => Req<UserErasureExport>(HttpVerb.Get, $"/v1/users/{Enc(id)}/erasure_export", cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/approval_requests</c> — Request a transaction approval from a user: push a number-matching "approve this action" prompt (carrying a human-readable description and optional context) to the user's registered push devices.</summary>
        public Task<ApprovalRequestCreated> CreateApprovalRequestAsync(string id, CreateApprovalRequestBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ApprovalRequestCreated>(HttpVerb.Post, $"/v1/users/{Enc(id)}/approval_requests", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/approval_requests/{approvalId}</c> — Poll a transaction-approval request: pending | approved | denied | expired, with the approving device and resolution timestamp.</summary>
        public Task<ApprovalRequest> GetApprovalRequestAsync(string id, string approvalId, CancellationToken ct = default)
            => Req<ApprovalRequest>(HttpVerb.Get, $"/v1/users/{Enc(id)}/approval_requests/{Enc(approvalId)}", cancellationToken: ct);

        /// <summary><c>POST /v1/users/by_emails</c> — Resolve a batch of email addresses to user ids in one call.</summary>
        public Task<DataList<UserEmailMatch>> FindByEmailsAsync(FindUsersByEmailsBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<DataList<UserEmailMatch>>(HttpVerb.Post, "/v1/users/by_emails", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/resolved</c> — Resolve a user to a FLAT identity in one call: the real primary email (never the @no-email.invalid placeholder), email_verified, name, image_url, and the linked provider identities (provider + provider_user_id).</summary>
        public Task<ResolvedUser> ResolveAsync(string id, CancellationToken ct = default)
            => Req<ResolvedUser>(HttpVerb.Get, $"/v1/users/{Enc(id)}/resolved", cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/identities/import</c> — Import a KNOWN provider identity (migration) without an interactive OAuth flow — link { provider, provider_user_id, email_address?, verified?</summary>
        public Task<Identity> ImportIdentityAsync(string id, ImportIdentityBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Identity>(HttpVerb.Post, $"/v1/users/{Enc(id)}/identities/import", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/identities/by-provider/{provider}</c> — Hard-remove a provider identity from a user (undo a mis-import).</summary>
        public Task<IdentityRemoved> RemoveIdentityByProviderAsync(string id, string provider, CancellationToken ct = default)
            => Req<IdentityRemoved>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/identities/by-provider/{Enc(provider)}", cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/notification_preferences</c> — Read an end-user's security-notification preferences: every category with whether it is enabled and whether it is optional (security-critical categories are non-optional and always enabled).</summary>
        public Task<NotificationPreferences> GetNotificationPreferencesAsync(string id, CancellationToken ct = default)
            => Req<NotificationPreferences>(HttpVerb.Get, $"/v1/users/{Enc(id)}/notification_preferences", cancellationToken: ct);

        /// <summary><c>PATCH /v1/users/{id}/notification_preferences</c> — Change an end-user's security-notification preferences with a { preferences: { category: enabled } } patch.</summary>
        public Task<NotificationPreferences> UpdateNotificationPreferencesAsync(string id, UpdateNotificationPreferencesBody body, CancellationToken ct = default)
            => Req<NotificationPreferences>(HttpVerb.Patch, $"/v1/users/{Enc(id)}/notification_preferences", body, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/sign_in_history</c> — A user's successful sign-ins, newest first, keyset-paginated.</summary>
        public Task<CursorPage<SignInHistoryEntry>> ListSignInHistoryAsync(string id, int? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<CursorPage<SignInHistoryEntry>>(HttpVerb.Get, $"/v1/users/{Enc(id)}/sign_in_history", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }

    public sealed class SetUserPasswordBody
    {
        /// <summary>The new plaintext password; Argon2id-hashed server-side.</summary>
        public string? Password { get; set; }
        /// <summary>Clerk-parity flag; a no-op here since an admin set runs no policy/breach check.</summary>
        public bool? SkipPasswordChecks { get; set; }
        /// <summary>Revoke the user's live sessions and bump sessions_version (JWTs die within 60s).</summary>
        public bool? SignOutOfOtherSessions { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public SetUserPasswordBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class SetUserDirectiveBody
    {
        public string? SuspendedUntil { get; set; }
        public bool? ReadOnly { get; set; }
        public bool? ForcedLogout { get; set; }
        public string? Reason { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public SetUserDirectiveBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class ProgressionEventBody
    {
        public string? Track { get; set; }
        public double? Amount { get; set; }
        public string? Reason { get; set; }
        public string? IdempotencyKey { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public ProgressionEventBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class RecordConsentBody
    {
        public string? Policy { get; set; }
        public string? Version { get; set; }
        public string? Status { get; set; }
        public string? Source { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public RecordConsentBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class ErasureAckBody
    {
        /// <summary>The acknowledging app/system (its app id or a label).</summary>
        public string? Source { get; set; }
        /// <summary>Alias of source.</summary>
        public string? AppId { get; set; }
        /// <summary>The app's contributed data for the DSAR export; omit to only confirm a purge.</summary>
        public Dictionary<string, object?>? Payload { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public ErasureAckBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class CreateApprovalRequestBody
    {
        /// <summary>What is being approved, shown on the device (e.g. "Remote desktop connection from 203.0.113.9, Berlin").</summary>
        public string? Description { get; set; }
        /// <summary>Optional key/value details to render beneath the description. String values only.</summary>
        public Dictionary<string, string?>? Context { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateApprovalRequestBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class FindUsersByEmailsBody
    {
        /// <summary>Up to 1000 addresses, deduped. Case-insensitive.</summary>
        public List<string>? EmailAddresses { get; set; }
        /// <summary>Default false (verified only). Set true for rights fulfilment / DSAR to also match unverified addresses; each result is flagged with `verified`.</summary>
        public bool? IncludeUnverified { get; set; }
        /// <summary>Default false (live users only). Set true to also match soft-deleted users; each such result is flagged with `deleted:true`.</summary>
        public bool? IncludeDeleted { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public FindUsersByEmailsBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class ImportIdentityBody
    {
        public string? Provider { get; set; }
        public string? ProviderUserId { get; set; }
        public string? EmailAddress { get; set; }
        public bool? Verified { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public ImportIdentityBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class UpdateNotificationPreferencesBody
    {
        /// <summary>Map of category → enabled. Only the listed categories change.</summary>
        public Dictionary<string, bool?>? Preferences { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public UpdateNotificationPreferencesBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class Directive
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string UserId { get; init; } = "";
        /// <summary>ISO 8601; present only when a suspension was imposed.</summary>
        public string? SuspendedUntil { get; init; }
        /// <summary>Present only when set.</summary>
        public bool? ReadOnly { get; init; }
        /// <summary>Present only when set.</summary>
        public bool? ForcedLogout { get; init; }
        /// <summary>Present only when provided.</summary>
        public string? Reason { get; init; }
        /// <summary>Live sessions revoked by imposing the directive.</summary>
        public long SessionsRevoked { get; init; }
    }

    public sealed class DirectiveLifted
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string UserId { get; init; } = "";
        public bool Lifted { get; init; }
    }

    public sealed class PhoneNumber
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        [JsonPropertyName("phone_number")] public string PhoneNumberValue { get; init; } = "";
        public bool Verified { get; init; }
        /// <summary>When the number was added (epoch ms).</summary>
        public long CreatedAt { get; init; }
    }

    public sealed class ProgressionEvent
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Track { get; init; } = "";
        public long Xp { get; init; }
        public long Level { get; init; }
        public bool LevelChanged { get; init; }
        public long Delta { get; init; }
    }

    /// <summary>A user's portable state (progression / tier / flair / entitlements / directives / roles).</summary>
    public sealed class UserState
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public UserStateAttributes? Attributes { get; init; }
        public List<JsonElement?> Entitlements { get; init; } = new List<JsonElement?>();
        public List<JsonElement?> Directives { get; init; } = new List<JsonElement?>();
        public List<JsonElement?> Roles { get; init; } = new List<JsonElement?>();
    }

    public sealed class UserStateAttributesProgressionValue
    {
        public long Xp { get; init; }
        public long Level { get; init; }
    }

    public sealed class UserStateAttributes
    {
        /// <summary>Map keyed by track name.</summary>
        public Dictionary<string, UserStateAttributesProgressionValue?> Progression { get; init; } = new Dictionary<string, UserStateAttributesProgressionValue?>();
        /// <summary>From public_metadata.tier; present only when set.</summary>
        public JsonElement? Tier { get; init; }
        /// <summary>From public_metadata.flair; present only when set.</summary>
        public JsonElement? Flair { get; init; }
    }

    public sealed class Consent
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Policy { get; init; } = "";
        public string Version { get; init; } = "";
        /// <summary>accepted | withdrawn</summary>
        public string Status { get; init; } = "";
        public string? Source { get; init; }
        public JsonElement? Metadata { get; init; }
        /// <summary>Recorded (epoch ms).</summary>
        public long CreatedAt { get; init; }
    }

    public sealed class UserErasureAck
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        /// <summary>The acknowledging app/system.</summary>
        public string Source { get; init; } = "";
        /// <summary>Acknowledged (epoch ms).</summary>
        public long CreatedAt { get; init; }
    }

    public sealed class UserErasureExport
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public long FormatVersion { get; init; }
        /// <summary>Generated (epoch ms).</summary>
        public long GeneratedAt { get; init; }
        public string UserId { get; init; } = "";
        /// <summary>Atlas's own data about the subject — the no-secret self-service export bundle.</summary>
        public Dictionary<string, object?> Atlas { get; init; } = new Dictionary<string, object?>();
        /// <summary>One entry per app that acknowledged, with the blob it contributed.</summary>
        public List<UserErasureExportAppContributions> AppContributions { get; init; } = new List<UserErasureExportAppContributions>();
    }

    public sealed class UserErasureExportAppContributions
    {
        public string Source { get; init; } = "";
        /// <summary>Contributed (epoch ms).</summary>
        public long ContributedAt { get; init; }
        public JsonElement? Payload { get; init; }
    }

    public sealed class ApprovalRequestCreated
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public string? Status { get; init; }
        /// <summary>The number to display to the user; the device must send back the one that matches.</summary>
        public string? MatchNumber { get; init; }
        public long? ExpiresAt { get; init; }
    }

    public sealed class ApprovalRequest
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public string? Status { get; init; }
        public string? Description { get; init; }
        public Dictionary<string, string>? Context { get; init; }
        /// <summary>The push device that approved (if the responder named it).</summary>
        public string? ApprovedByDeviceId { get; init; }
        public long? CreatedAt { get; init; }
        public long? ExpiresAt { get; init; }
        public long? ResolvedAt { get; init; }
    }

    public sealed class UserEmailMatch
    {
        public string EmailAddress { get; init; } = "";
        public string UserId { get; init; } = "";
        public bool Verified { get; init; }
    }

    public sealed class ResolvedIdentity
    {
        public string Provider { get; init; } = "";
        public string ProviderUserId { get; init; } = "";
        public string? Email { get; init; }
    }

    public sealed class ResolvedUser
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string UserId { get; init; } = "";
        public string? Email { get; init; }
        public bool EmailVerified { get; init; }
        public string? Name { get; init; }
        public string? ImageUrl { get; init; }
        public List<ResolvedIdentity> Identities { get; init; } = new List<ResolvedIdentity>();
    }

    public sealed class IdentityRemoved
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Provider { get; init; } = "";
        public bool Removed { get; init; }
    }

    public sealed class NotificationPreference
    {
        public string Category { get; init; } = "";
        /// <summary>User-facing label for the toggle.</summary>
        public string Label { get; init; } = "";
        public bool Enabled { get; init; }
        public bool Optional { get; init; }
    }

    public sealed class NotificationPreferences
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public List<NotificationPreference> Preferences { get; init; } = new List<NotificationPreference>();
    }

    public sealed class SignInHistoryEntry
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        /// <summary>How the user signed in (password, email_code, oauth, passkey, …).</summary>
        public string? Method { get; init; }
        /// <summary>The social provider, for an oauth sign-in.</summary>
        public string? Provider { get; init; }
        public string? ProviderEmail { get; init; }
        public string? Ip { get; init; }
        /// <summary>The actor id when this sign-in was an impersonation.</summary>
        public string? ImpersonatedBy { get; init; }
        /// <summary>Epoch ms.</summary>
        public long? CreatedAt { get; init; }
    }
}
