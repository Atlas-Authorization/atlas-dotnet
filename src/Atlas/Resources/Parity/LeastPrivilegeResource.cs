using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/least_privilege/apply</c>. Only the fields set are sent.</summary>
    public sealed class LeastPrivilegeApplyBody
    {
        [JsonPropertyName("subject_type")] public string? SubjectType { get; set; }
        [JsonPropertyName("subject_id")] public string? SubjectId { get; set; }
        [JsonPropertyName("digest")] public string? Digest { get; set; }
        [JsonPropertyName("days")] public long? Days { get; set; }
        [JsonPropertyName("acknowledge_short_window")] public bool? AcknowledgeShortWindow { get; set; }
    }

    /// <summary>Operations of <c>leastPrivilege</c>.</summary>
    public sealed partial class LeastPrivilegeResource : ResourceBase
    {
        public LeastPrivilegeResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/least_privilege/report</c> - The least-privilege report: for every active secret key, OAuth client, MCP gateway and agent, what it is allowed to do against what it used over the window, the permissions never used, and the narrower grant that would still cover its use. Credentials with something unused come first. `format=csv` exports one row per permission.</summary>
        /// <remarks>Needs the <c>least_privilege:read</c> scope.</remarks>
        public Task<JsonElement> ReportAsync(long? days = null, string? subjectType = null, string? subjectId = null, bool? onlyUnused = null, long? limit = null, string? format = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/least_privilege/report", query: Q(("days", days), ("subject_type", subjectType), ("subject_id", subjectId), ("only_unused", onlyUnused), ("limit", limit), ("format", format), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/least_privilege/report/{type}/{id}</c> - The least-privilege report of one credential (`type` is api_key, oauth_client, mcp_gateway or agent). 404 for a credential the report does not cover, such as a revoked key.</summary>
        /// <remarks>Needs the <c>least_privilege:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string type, string id, long? days = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/least_privilege/report/{Enc(type)}/{Enc(id)}", query: Q(("days", days)), cancellationToken: ct);

        /// <summary><c>POST /v1/least_privilege/apply</c> - Apply a credential's suggestion: narrow a secret key's scopes, an OAuth client's allowed scopes, an MCP gateway's tool policy, or revoke an agent's unused tool grants. Preview it first with `?dry_run=true`, then send the same body for real. `digest` (from the report) must match the current suggestion, else 409, so what you previewed is what is applied; the write is conditional on the credential being unchanged. A suggestion only ever removes permissions. Over a window the data does not fully cover (`confidence: low`) it needs `acknowledge_short_window: true`. Audited as `least_privilege.applied`.</summary>
        /// <remarks>Needs the <c>least_privilege:write</c> scope.</remarks>
        public Task<JsonElement> ApplyAsync(LeastPrivilegeApplyBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/least_privilege/apply", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
