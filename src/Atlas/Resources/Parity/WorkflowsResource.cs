using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/workflows</c>. Only the fields set are sent.</summary>
    public sealed class WorkflowsCreateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("trigger")] public Dictionary<string, object?>? Trigger { get; set; }
        [JsonPropertyName("steps")] public List<object?>? Steps { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/workflows/{id}</c>. Only the fields set are sent.</summary>
    public sealed class WorkflowsUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("trigger")] public Dictionary<string, object?>? Trigger { get; set; }
        [JsonPropertyName("steps")] public List<object?>? Steps { get; set; }
    }

    /// <summary>The body of <c>POST /v1/workflows/{id}/dry_run</c>. Only the fields set are sent.</summary>
    public sealed class WorkflowsDryRunBody
    {
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("organization_id")] public string? OrganizationId { get; set; }
    }

    /// <summary>The body of <c>POST /v1/workflows/{id}/run</c>. Only the fields set are sent.</summary>
    public sealed class WorkflowsRunBody
    {
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("organization_id")] public string? OrganizationId { get; set; }
    }

    /// <summary>Operations of <c>workflows</c>.</summary>
    public sealed partial class WorkflowsResource : ResourceBase
    {
        public WorkflowsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/workflows</c> - List lifecycle workflows: a trigger (cron, user created, inactive for N days, membership age, trial end, webhook event) plus ordered steps, run by the worker.</summary>
        /// <remarks>Needs the <c>actions:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/workflows", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/workflows</c> - Create a lifecycle workflow. Steps are notify, disable, delete, invite, add_to_org, remove_from_org, add_to_team, remove_from_team, add_to_group, remove_from_group (an instance group by id or slug) and call_action (an Action with the `workflow_step` trigger, which may be an `http` Action). Each subject runs once per workflow; the first failing step stops the run.</summary>
        /// <remarks>Needs the <c>actions:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(WorkflowsCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/workflows", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/workflows/{id}</c> - Read one lifecycle workflow.</summary>
        /// <remarks>Needs the <c>actions:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/workflows/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/workflows/{id}</c> - Update a workflow's name, enabled flag, trigger or steps. Changing the trigger resets its cron slot and event cursor.</summary>
        /// <remarks>Needs the <c>actions:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, WorkflowsUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/workflows/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/workflows/{id}</c> - Delete a workflow and its run history.</summary>
        /// <remarks>Needs the <c>actions:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/workflows/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/workflows/{id}/dry_run</c> - Check what a workflow would do for a user (`user_id`; none for cron) without changing anything. Every step is validated against live data and reported as `would_run` or `failed`.</summary>
        /// <remarks>Needs the <c>actions:write</c> scope.</remarks>
        public Task<JsonElement> DryRunAsync(string id, WorkflowsDryRunBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/workflows/{Enc(id)}/dry_run", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/workflows/{id}/run</c> - Run a workflow now for one user (`user_id`; none for cron), outside its trigger. Returns the finished run.</summary>
        /// <remarks>Needs the <c>actions:write</c> scope.</remarks>
        public Task<JsonElement> RunAsync(string id, WorkflowsRunBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/workflows/{Enc(id)}/run", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/workflows/{id}/runs</c> - Run history, newest first, dry runs included (`dry_run`).</summary>
        /// <remarks>Needs the <c>actions:read</c> scope.</remarks>
        public Task<JsonElement> RunsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/workflows/{Enc(id)}/runs", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
