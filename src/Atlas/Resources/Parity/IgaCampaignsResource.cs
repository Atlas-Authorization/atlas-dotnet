using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/iga/campaigns</c>. Only the fields set are sent.</summary>
    public sealed class IgaCampaignsCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("scope_type")] public string? ScopeType { get; set; }
        [JsonPropertyName("scope_ids")] public List<string>? ScopeIds { get; set; }
        [JsonPropertyName("reviewer_type")] public string? ReviewerType { get; set; }
        [JsonPropertyName("reviewer_ids")] public List<string>? ReviewerIds { get; set; }
        [JsonPropertyName("due_at")] public long? DueAt { get; set; }
        [JsonPropertyName("reminder_days")] public List<long>? ReminderDays { get; set; }
        [JsonPropertyName("auto_revoke")] public bool? AutoRevoke { get; set; }
    }

    /// <summary>The body of <c>POST /v1/iga/campaign_items/{id}/decide</c>. Only the fields set are sent.</summary>
    public sealed class IgaCampaignsDecideBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("decision")] public string? Decision { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
        [JsonPropertyName("reviewer_user_id")] public string? ReviewerUserId { get; set; }
    }

    /// <summary>Operations of <c>iga.campaigns</c>.</summary>
    public sealed partial class IgaCampaignsResource : ResourceBase
    {
        public IgaCampaignsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/iga/campaigns</c> - List certification (access review) campaigns.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? status = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/iga/campaigns", query: Q(("status", status), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/campaigns</c> - Create a draft campaign scoped to everything, specific entitlements, or the entitlements for an app, role, group or organization, with reviewers (the person's manager, the entitlement owner, or named users), a due date and reminder days. With `auto_revoke` the access reviewers mark for revocation is removed when the campaign closes.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(IgaCampaignsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/iga/campaigns", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/iga/campaigns/{id}</c> - Read a campaign with its progress.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/iga/campaigns/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/iga/campaigns/{id}</c> - Delete a draft or closed campaign.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/iga/campaigns/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/campaigns/{id}/launch</c> - Launch a draft campaign: it adopts access that exists in Atlas but was never granted through governance, freezes the review list and notifies reviewers.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> LaunchAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/iga/campaigns/{Enc(id)}/launch", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/iga/campaigns/{id}/close</c> - Close a campaign and apply its revoke decisions (with `auto_revoke`). Items nobody reviewed are left in place and reported as pending.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> CloseAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/iga/campaigns/{Enc(id)}/close", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/iga/campaigns/{id}/items</c> - The review items of a campaign.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ItemsAsync(string id, string? decision = null, string? reviewerUserId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/iga/campaigns/{Enc(id)}/items", query: Q(("decision", decision), ("reviewer_user_id", reviewerUserId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/iga/campaigns/{id}/progress</c> - Progress: totals, percent complete, whether it is overdue, and per-reviewer counts.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> ProgressAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/iga/campaigns/{Enc(id)}/progress", cancellationToken: ct);

        /// <summary><c>GET /v1/iga/campaigns/{id}/evidence</c> - The audit evidence for a campaign as JSON, or CSV with `format=csv`. People are identified by id only (an erased user is flagged), free-text comments are never included, and `masked_emails=true` adds an address reduced to its first letter and domain.</summary>
        /// <remarks>Needs the <c>iga:read</c> scope.</remarks>
        public Task<JsonElement> EvidenceAsync(string id, string? format = null, string? maskedEmails = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/iga/campaigns/{Enc(id)}/evidence", query: Q(("format", format), ("masked_emails", maskedEmails)), cancellationToken: ct);

        /// <summary><c>POST /v1/iga/campaign_items/{id}/decide</c> - Certify or revoke one review item. With `reviewer_user_id` the decision is the reviewer's (they must be allowed to review it, and may not review their own access); without it it is an administrator decision.</summary>
        /// <remarks>Needs the <c>iga:write</c> scope.</remarks>
        public Task<JsonElement> DecideAsync(string id, IgaCampaignsDecideBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/iga/campaign_items/{Enc(id)}/decide", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
