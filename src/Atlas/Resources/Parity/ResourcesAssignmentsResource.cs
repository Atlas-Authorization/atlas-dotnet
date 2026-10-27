using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/resource_role_assignments</c>. Only the fields set are sent.</summary>
    public sealed class ResourcesAssignmentsAssignBody
    {
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("subject")] public Dictionary<string, object?>? Subject { get; set; }
        [JsonPropertyName("role")] public string? Role { get; set; }
        [JsonPropertyName("inherit")] public bool? Inherit { get; set; }
    }

    /// <summary>Operations of <c>resources.assignments</c>.</summary>
    public sealed partial class ResourcesAssignmentsResource : ResourceBase
    {
        public ResourcesAssignmentsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/resource_role_assignments</c> - List role assignments, filtered by `resource_id`, `subject_type` or `subject_id`.</summary>
        /// <remarks>Needs the <c>resources:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? resourceId = null, string? subjectType = null, string? subjectId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/resource_role_assignments", query: Q(("resource_id", resourceId), ("subject_type", subjectType), ("subject_id", subjectId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/resource_role_assignments</c> - Assign a `role` on a `resource` to a `subject` (`{type: user|group|org, id}`). `inherit` (default true) also applies it to the resource’s descendants. Re-assigning the same role updates `inherit`.</summary>
        /// <remarks>Needs the <c>resources:write</c> scope.</remarks>
        public Task<JsonElement> AssignAsync(ResourcesAssignmentsAssignBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/resource_role_assignments", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/resource_role_assignments/{id}</c> - Revoke a role assignment.</summary>
        /// <remarks>Needs the <c>resources:write</c> scope.</remarks>
        public Task RevokeAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/resource_role_assignments/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
