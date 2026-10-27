using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/users/{id}/required_actions</c>. Only the fields set are sent.</summary>
    public sealed class UsersRequiredActionsRequestBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("action")] public string? Action { get; set; }
        /// <summary>custom only: the https URL the user is sent to.</summary>
        [JsonPropertyName("url")] public string? Url { get; set; }
        /// <summary>Free note shown on the audit entry and the held attempt (max 500 characters).</summary>
        [JsonPropertyName("reason")] public string? Reason { get; set; }
    }

    /// <summary>The body of <c>POST /v1/required_actions/bulk</c>. Only the fields set are sent.</summary>
    public sealed class UsersRequiredActionsBulkBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("action")] public string? Action { get; set; }
        /// <summary>custom only: the https URL the user is sent to.</summary>
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("reason")] public string? Reason { get; set; }
        /// <summary>Exactly one of organization_id or user_ids.</summary>
        [JsonPropertyName("organization_id")] public string? OrganizationId { get; set; }
        [JsonPropertyName("user_ids")] public List<string>? UserIds { get; set; }
    }

    /// <summary>Operations of <c>users.requiredActions</c>.</summary>
    public sealed partial class UsersRequiredActionsResource : ResourceBase
    {
        public UsersRequiredActionsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/users/{id}/required_actions</c> - Make a user owe an action at their next sign-in, whatever the method: update_password, verify_email, accept_policy, configure_mfa, update_profile, or custom (sent to an https url; the app closes it with DELETE). The sign-in attempt holds at needs_required_action until it is done (configure_mfa alone routes through MFA enrollment; accept_policy is closed once the policy gate passes; verify_email and configure_mfa clear themselves when already true). Re-requesting an open action is a 200 with the same row. Emits user.required_action_requested. -&gt; 201.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> RequestAsync(string id, UsersRequiredActionsRequestBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(id)}/required_actions", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/required_actions</c> - The actions a user currently owes, oldest first.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/users/{Enc(id)}/required_actions", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/required_actions/{rid}</c> - Withdraw an owed action. This is also how an application closes a custom action once the user has finished its step. Emits user.required_action_cleared. 404 when it is not open.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> ClearAsync(string id, string rid, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/required_actions/{Enc(rid)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/required_actions/bulk</c> - Owe the same action for many users at once: every member of one organization (organization_id) or an explicit user_ids list, at most 5000. Users outside the instance are skipped, and an already-open action counts as already_open. Emits user.required_action_requested per newly opened action.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task BulkAsync(UsersRequiredActionsBulkBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, "/v1/required_actions/bulk", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
