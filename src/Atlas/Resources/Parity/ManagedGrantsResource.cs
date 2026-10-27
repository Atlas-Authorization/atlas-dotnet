using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PATCH /v1/grant_management/grants/{id}</c>. Only the fields set are sent.</summary>
    public sealed class ManagedGrantsNarrowBody
    {
        [JsonPropertyName("scopes")] public List<string>? Scopes { get; set; }
        [JsonPropertyName("authorization_details")] public List<object?>? AuthorizationDetails { get; set; }
    }

    /// <summary>Operations of <c>managedGrants</c>.</summary>
    public sealed partial class ManagedGrantsResource : ResourceBase
    {
        public ManagedGrantsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/grant_management/grants</c> - List OpenID Grant Management grants: what each OAuth client holds for each user under a `grant_id`, newest first. Filter by `user_id`, `client_id` (the public client id) and `status`.</summary>
        /// <remarks>Needs the <c>grants:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? userId = null, string? clientId = null, string? status = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/grant_management/grants", query: Q(("user_id", userId), ("client_id", clientId), ("status", status), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/grant_management/grants/{id}</c> - Read one grant: its scopes, resource, authorization_details, status and revision.</summary>
        /// <remarks>Needs the <c>grants:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/grant_management/grants/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/grant_management/grants/{id}</c> - Narrow a grant: send the `scopes` and/or `authorization_details` it should keep. Anything it does not already hold is a 400 (only the user widens a grant, by authorizing again with `grant_management_action=merge`). Refresh tokens under the grant are clamped to the narrower scope; a revoked grant is a 404.</summary>
        /// <remarks>Needs the <c>grants:write</c> scope.</remarks>
        public Task<JsonElement> NarrowAsync(string id, ManagedGrantsNarrowBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/grant_management/grants/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/grant_management/grants/{id}</c> - Revoke a grant and everything issued under it: refresh tokens stop refreshing and access tokens that carry the `grant_id` stop validating at once. `tokens_ended` counts the refresh grants that ended with it. Revoking an already-revoked grant answers 200 with `tokens_ended: 0`.</summary>
        /// <remarks>Needs the <c>grants:write</c> scope.</remarks>
        public Task<JsonElement> RevokeAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/grant_management/grants/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
