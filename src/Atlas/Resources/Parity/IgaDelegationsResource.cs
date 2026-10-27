using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/iga/delegations</c>. Only the fields set are sent.</summary>
    public sealed class IgaDelegationsCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("delegator_user_id")] public string? DelegatorUserId { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("delegate_user_id")] public string? DelegateUserId { get; set; }
        [JsonPropertyName("starts_at")] public long? StartsAt { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("ends_at")] public long? EndsAt { get; set; }
    }

    /// <summary>Operations of <c>iga.delegations</c>.</summary>
    public sealed partial class IgaDelegationsResource : ResourceBase
    {
        public IgaDelegationsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/iga/delegations</c> - List approval delegations.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? delegatorUserId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/delegations", query: Q(("delegator_user_id", delegatorUserId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/delegations</c> - Let another user decide a person's approvals (and reviews) for a period of up to a year.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(IgaDelegationsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/iga/delegations", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/iga/delegations/{id}</c> - End an approval delegation.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/iga/delegations/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
