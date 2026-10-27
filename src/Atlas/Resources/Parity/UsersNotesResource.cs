using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/users/{id}/notes</c>. Only the fields set are sent.</summary>
    public sealed class UsersNotesCreateBody
    {
        /// <summary>Required. The note text.</summary>
        [JsonPropertyName("body")] public string? Body { get; set; }
        /// <summary>Pinned notes are listed first.</summary>
        [JsonPropertyName("pinned")] public bool? Pinned { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/users/{id}/notes/{noteId}</c>. Only the fields set are sent.</summary>
    public sealed class UsersNotesUpdateBody
    {
        /// <summary>The note text.</summary>
        [JsonPropertyName("body")] public string? Body { get; set; }
        /// <summary>Pinned notes are listed first.</summary>
        [JsonPropertyName("pinned")] public bool? Pinned { get; set; }
    }

    /// <summary>Operations of <c>users.notes</c>.</summary>
    public sealed partial class UsersNotesResource : ResourceBase
    {
        public UsersNotesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/users/{id}/notes</c> - The notes on a user: pinned notes first, then newest first.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/users/{Enc(id)}/notes", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/notes</c> - Add a note to a user (up to 5000 characters; at most 500 notes per user). The audit log records that a note was added, never its text. -&gt; 201.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(string id, UsersNotesCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(id)}/notes", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PATCH /v1/users/{id}/notes/{noteId}</c> - Edit a note: its text, its pin, or both.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, string noteId, UsersNotesUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/users/{Enc(id)}/notes/{Enc(noteId)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/notes/{noteId}</c> - Delete a note from a user. The audit log keeps that it was deleted.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string id, string noteId, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/notes/{Enc(noteId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
