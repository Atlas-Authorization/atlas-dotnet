using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/iga/grants</c>. Only the fields set are sent.</summary>
    public sealed class IgaGrantsCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("entitlement")] public string? Entitlement { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("expires_at")] public long? ExpiresAt { get; set; }
        [JsonPropertyName("override_reason")] public string? OverrideReason { get; set; }
    }

    /// <summary>The body of <c>POST /v1/iga/grants/{id}/revoke</c>. Only the fields set are sent.</summary>
    public sealed class IgaGrantsRevokeBody
    {
        [JsonPropertyName("reason")] public string? Reason { get; set; }
    }

    /// <summary>Operations of <c>iga.grants</c>.</summary>
    public sealed partial class IgaGrantsResource : ResourceBase
    {
        public IgaGrantsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/iga/grants</c> - List grants (what each user was given through governance), newest first.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? userId = null, string? entitlement = null, string? status = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/grants", query: Q(("user_id", userId), ("entitlement", entitlement), ("status", status), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/grants</c> - Give a user an entitlement now, without a request. `expires_at` makes it time-bound. A blocking separation-of-duties rule refuses it; a require-approval rule refuses unless `override_reason` is given, which records an accepted violation.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(IgaGrantsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/iga/grants", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/iga/grants/{id}/revoke</c> - Take the access away and close the grant, including access that existed before governance adopted it (an expiry, by contrast, leaves adopted access in place).</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> RevokeAsync(string id, IgaGrantsRevokeBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/iga/grants/{Enc(id)}/revoke", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
