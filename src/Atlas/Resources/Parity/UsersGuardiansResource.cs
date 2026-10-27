using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/users/{id}/guardians</c>. Only the fields set are sent.</summary>
    public sealed class UsersGuardiansRequestBody
    {
        /// <summary>The guardian's email address.</summary>
        [JsonPropertyName("email")] public string? Email { get; set; }
        /// <summary>A user of this application who is the guardian.</summary>
        [JsonPropertyName("guardian_user_id")] public string? GuardianUserId { get; set; }
        /// <summary>Free label such as mother or legal guardian.</summary>
        [JsonPropertyName("relationship")] public string? Relationship { get; set; }
        /// <summary>Email the consent link to the guardian (default false).</summary>
        [JsonPropertyName("send_email")] public bool? SendEmail { get; set; }
    }

    /// <summary>The body of <c>POST /v1/users/{id}/guardians/{linkId}/approve</c>. Only the fields set are sent.</summary>
    public sealed class UsersGuardiansApproveBody
    {
        /// <summary>How consent was obtained (kept on the link).</summary>
        [JsonPropertyName("note")] public string? Note { get; set; }
    }

    /// <summary>Operations of <c>users.guardians</c>.</summary>
    public sealed partial class UsersGuardiansResource : ResourceBase
    {
        public UsersGuardiansResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/users/{id}/guardians</c> - Ask a parent or guardian to approve a minor's account. Name them by email or by guardian_user_id (their address is used). Returns the pending link with a one-time consent_url that expires after compliance.minors.consentTtlHours. With send_email: true Atlas mails it through the application's own email provider (422 when none is set up); otherwise deliver consent_url yourself. -&gt; 201.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> RequestAsync(string id, UsersGuardiansRequestBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(id)}/guardians", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/guardians/{linkId}/approve</c> - Record consent obtained another way (a signed form, a verified call): the link becomes active with consent_method admin and the account is consented. 409 for a revoked link.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> ApproveAsync(string id, string linkId, UsersGuardiansApproveBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(id)}/guardians/{Enc(linkId)}/approve", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/guardians/{linkId}</c> - Withdraw a guardian link. When no active guardian is left, the account needs consent again.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> RevokeAsync(string id, string linkId, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/guardians/{Enc(linkId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
