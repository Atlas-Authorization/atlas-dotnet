using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/vc/credentials/{id}/revoke</c>. Only the fields set are sent.</summary>
    public sealed class VcCredentialsRevokeBody
    {
    }

    /// <summary>Operations of <c>vc.credentials</c>.</summary>
    public sealed partial class VcCredentialsResource : ResourceBase
    {
        public VcCredentialsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/vc/credentials</c> - List issued credentials (most recent 200), optionally for one user.</summary>
        /// <remarks>Needs the <c>vc:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? userId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/vc/credentials", query: Q(("user_id", userId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/vc/credentials/{id}/revoke</c> - Revoke an issued credential: its status-list bit is set and later presentations are refused.</summary>
        /// <remarks>Needs the <c>vc:write</c> scope.</remarks>
        public Task<JsonElement> RevokeAsync(string id, VcCredentialsRevokeBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/vc/credentials/{Enc(id)}/revoke", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
