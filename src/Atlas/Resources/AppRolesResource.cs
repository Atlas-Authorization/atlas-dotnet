using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>
    /// <c>/v1/app_roles</c> — instance-wide user roles, not tied to any organization. A user's app roles ride on
    /// the session token as <c>app_roles</c> / <c>app_permissions</c>; check them offline with
    /// <c>SessionClaims.HasAppRole</c> / <c>HasAppPermission</c>.
    /// </summary>
    public sealed class AppRolesResource : ResourceBase
    {
        public AppRolesResource(AtlasTransport transport) : base(transport) { }

        public Task<JsonElement> ListAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/app_roles", cancellationToken: ct);

        /// <summary><paramref name="key"/>: 2-64 lowercase letters, digits, _ or -, starting with a letter.</summary>
        public Task<JsonElement> CreateAsync(string key, string name, string? description = null, IEnumerable<string>? permissions = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/app_roles",
                body: Body(("key", key), ("name", name), ("description", description), ("permissions", permissions == null ? null : new List<string>(permissions))),
                cancellationToken: ct);

        /// <summary>By id or key.</summary>
        public Task<JsonElement> GetAsync(string idOrKey, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/app_roles/{Enc(idOrKey)}", cancellationToken: ct);

        public Task<JsonElement> UpdateAsync(string idOrKey, string? name = null, string? description = null, IEnumerable<string>? permissions = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/app_roles/{Enc(idOrKey)}",
                body: Body(("name", name), ("description", description), ("permissions", permissions == null ? null : new List<string>(permissions))),
                cancellationToken: ct);

        public Task<JsonElement> DeleteAsync(string idOrKey, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/app_roles/{Enc(idOrKey)}", cancellationToken: ct);

        /// <summary>The users holding a role, oldest assignment first.</summary>
        public Task<JsonElement> ListUsersAsync(string idOrKey, int? limit = null, int? offset = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/app_roles/{Enc(idOrKey)}/users", query: Q(("limit", limit), ("offset", offset)), cancellationToken: ct);

        /// <summary>A user's app roles and effective permissions.</summary>
        public Task<JsonElement> GetForUserAsync(string userId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/users/{Enc(userId)}/app_roles", cancellationToken: ct);

        /// <summary>Replace the user's app roles with <paramref name="roles"/> (keys or ids).</summary>
        public Task<JsonElement> SetForUserAsync(string userId, IEnumerable<string> roles, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(userId)}/app_roles", body: Body(("roles", new List<string>(roles))), cancellationToken: ct);

        /// <summary>Add and/or remove app roles (keys or ids).</summary>
        public Task<JsonElement> ChangeForUserAsync(string userId, IEnumerable<string>? add = null, IEnumerable<string>? remove = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(userId)}/app_roles",
                body: Body(("add", add == null ? null : new List<string>(add)), ("remove", remove == null ? null : new List<string>(remove))),
                cancellationToken: ct);

        public Task<JsonElement> RemoveFromUserAsync(string userId, string idOrKey, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/users/{Enc(userId)}/app_roles/{Enc(idOrKey)}", cancellationToken: ct);
    }

    /// <summary>
    /// <c>/v1/oauth_initial_access_tokens</c> — tokens that gate OAuth dynamic client registration (RFC 7591 §3)
    /// once <c>oauthProvider.requireInitialAccessToken</c> is on.
    /// </summary>
    public sealed class OAuthInitialAccessTokensResource : ResourceBase
    {
        public OAuthInitialAccessTokensResource(AtlasTransport transport) : base(transport) { }

        public Task<JsonElement> ListAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/oauth_initial_access_tokens", cancellationToken: ct);

        /// <summary>Mint a token; <c>token</c> is returned exactly once.</summary>
        public Task<JsonElement> CreateAsync(string? description = null, long? expiresInSeconds = null, int? maxUses = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/oauth_initial_access_tokens",
                body: Body(("description", description), ("expires_in_seconds", expiresInSeconds), ("max_uses", maxUses)), cancellationToken: ct);

        public Task<JsonElement> RevokeAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/oauth_initial_access_tokens/{Enc(id)}", cancellationToken: ct);
    }

    /// <summary><c>/v1/organizations/:id/teams</c>.</summary>
    public sealed class OrganizationTeamsResource : ResourceBase
    {
        public OrganizationTeamsResource(AtlasTransport transport) : base(transport) { }

        private static string Base(string orgId) => $"/v1/organizations/{Enc(orgId)}/teams";

        /// <summary><paramref name="userId"/> narrows to the teams that user is in.</summary>
        public Task<JsonElement> ListAsync(string orgId, string? userId = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, Base(orgId), query: Q(("user_id", userId)), cancellationToken: ct);

        public Task<JsonElement> CreateAsync(string orgId, string name, string? description = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, Base(orgId), body: Body(("name", name), ("description", description)), cancellationToken: ct);

        public Task<JsonElement> GetAsync(string orgId, string teamId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"{Base(orgId)}/{Enc(teamId)}", cancellationToken: ct);

        public Task<JsonElement> UpdateAsync(string orgId, string teamId, string? name = null, string? description = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"{Base(orgId)}/{Enc(teamId)}", body: Body(("name", name), ("description", description)), cancellationToken: ct);

        public Task<JsonElement> DeleteAsync(string orgId, string teamId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"{Base(orgId)}/{Enc(teamId)}", cancellationToken: ct);

        public Task<JsonElement> ListMembersAsync(string orgId, string teamId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"{Base(orgId)}/{Enc(teamId)}/members", cancellationToken: ct);

        public Task<JsonElement> AddMemberAsync(string orgId, string teamId, string userId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"{Base(orgId)}/{Enc(teamId)}/members", body: Body(("user_id", userId)), cancellationToken: ct);

        public Task<JsonElement> RemoveMemberAsync(string orgId, string teamId, string userId, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"{Base(orgId)}/{Enc(teamId)}/members/{Enc(userId)}", cancellationToken: ct);
    }

    /// <summary><c>/v1/organizations/:id/roles</c> — instance roles plus the organization's own custom roles.</summary>
    public sealed class OrganizationRolesResource : ResourceBase
    {
        public OrganizationRolesResource(AtlasTransport transport) : base(transport) { }

        private static string Base(string orgId) => $"/v1/organizations/{Enc(orgId)}/roles";

        /// <summary><paramref name="own"/> lists only the organization's own custom roles.</summary>
        public Task<JsonElement> ListAsync(string orgId, bool own = false, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, Base(orgId), query: Q(("own", own ? "true" : null)), cancellationToken: ct);

        public Task<JsonElement> CreateAsync(string orgId, string key, string name, string? description = null, IEnumerable<string>? permissions = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, Base(orgId),
                body: Body(("key", key), ("name", name), ("description", description), ("permissions", permissions == null ? null : new List<string>(permissions))),
                cancellationToken: ct);

        public Task<JsonElement> UpdateAsync(string orgId, string roleId, string? name = null, string? description = null, IEnumerable<string>? permissions = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"{Base(orgId)}/{Enc(roleId)}",
                body: Body(("name", name), ("description", description), ("permissions", permissions == null ? null : new List<string>(permissions))),
                cancellationToken: ct);

        /// <summary><paramref name="reassignTo"/> (a role key) moves members and pending invitations first; without it a role in use is refused (409 ROLE_IN_USE).</summary>
        public Task<JsonElement> DeleteAsync(string orgId, string roleId, string? reassignTo = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"{Base(orgId)}/{Enc(roleId)}", query: Q(("reassign_to", reassignTo)), cancellationToken: ct);
    }
}
