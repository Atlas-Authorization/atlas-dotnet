using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/users/{id}/links</c>. Only the fields set are sent.</summary>
    public sealed class UsersCreateLinkBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("type")] public string? Type { get; set; }
        /// <summary>One of the user's addresses. Default: the primary address.</summary>
        [JsonPropertyName("email_address_id")] public string? EmailAddressId { get; set; }
        /// <summary>Default 3600.</summary>
        [JsonPropertyName("expires_in_seconds")] public long? ExpiresInSeconds { get; set; }
    }

    /// <summary>More operations of <c>users</c>.</summary>
    public sealed partial class UsersResource
    {
        private UsersRequiredActionsResource? _requiredActions;
        /// <summary>The <c>users.requiredActions</c> operations.</summary>
        public UsersRequiredActionsResource RequiredActions => _requiredActions ??= new UsersRequiredActionsResource(Transport);

        private UsersNotesResource? _notes;
        /// <summary>The <c>users.notes</c> operations.</summary>
        public UsersNotesResource Notes => _notes ??= new UsersNotesResource(Transport);

        private UsersGuardiansResource? _guardians;
        /// <summary>The <c>users.guardians</c> operations.</summary>
        public UsersGuardiansResource Guardians => _guardians ??= new UsersGuardiansResource(Transport);

        private UsersSearchResource? _search;
        /// <summary>The <c>users.search</c> operations.</summary>
        public UsersSearchResource Search => _search ??= new UsersSearchResource(Transport);

        /// <summary><c>GET /v1/users/{id}/history</c> - The user change history, newest first: every profile, metadata and status change (ban, lock, directives and their reversal) with who made it, the reason and expiry, and each changed field with its value before and after. A password change and private_metadata values are recorded as redacted (the field name only). Pages with starting_after.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> HistoryAsync(string id, long? limit = null, string? startingAfter = null, string? action = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/users/{Enc(id)}/history", query: Q(("limit", limit), ("starting_after", startingAfter), ("action", action)), cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/minor_status</c> - A user's parental-consent state (not_required for anyone at or above the age of digital consent) and their guardian links, newest first.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> MinorStatusAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/users/{Enc(id)}/minor_status", cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/minor_status/assess</c> - Re-assess a user from their date-of-birth trait now (for users created before age gating was turned on). 400 when compliance.minors is off.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> AssessMinorAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(id)}/minor_status/assess", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/links</c> - Mint an email-verification or password-reset link (and code) for a user WITHOUT sending it, for support tools and invite flows that deliver it themselves. type "email_verification" needs an unverified address (default: the primary); the url opens GET /v1/client/email_addresses/verify and the code works wherever the address is verified by code. type "password_reset" needs a verified address and returns the url and/or code the instance's password-reset setting would have mailed, plus the reset attempt_id; redeeming it is the ordinary reset (it still asks for the second factor and sets a new password). Single-use, expires_in_seconds 60 to 86400 (default 3600). Audited as user.link_generated, never with the secret. Treat the response as a credential. -&gt; 201.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> CreateLinkAsync(string id, UsersCreateLinkBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(id)}/links", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/device_keys</c> - List a user’s device keys (native biometric sign-in). Never returns key material.</summary>
        /// <remarks>Needs the <c>users:read</c> scope.</remarks>
        public Task<JsonElement> ListDeviceKeysAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/users/{Enc(id)}/device_keys", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/users/{id}/device_keys/{keyId}</c> - Revoke one of a user’s device keys so that device can no longer sign in. Scoped to the user; the account holder is notified when account-change notices are on.</summary>
        /// <remarks>Needs the <c>users:write</c> scope.</remarks>
        public Task<JsonElement> DeleteDeviceKeyAsync(string id, string keyId, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/users/{Enc(id)}/device_keys/{Enc(keyId)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
