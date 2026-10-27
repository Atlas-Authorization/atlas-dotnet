using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/identity_verification/settings</c>. Only the fields set are sent.</summary>
    public sealed class IdentityVerificationUpdateSettingsBody
    {
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("connector_id")] public string? ConnectorId { get; set; }
        [JsonPropertyName("as_challenge")] public bool? AsChallenge { get; set; }
    }

    /// <summary>The body of <c>POST /v1/identity_verifications/{id}/decide</c>. Only the fields set are sent.</summary>
    public sealed class IdentityVerificationDecideBody
    {
        [JsonPropertyName("decision")] public string? Decision { get; set; }
        [JsonPropertyName("reason")] public string? Reason { get; set; }
    }

    /// <summary>The body of <c>POST /v1/users/{id}/identity_verifications</c>. Only the fields set are sent.</summary>
    public sealed class IdentityVerificationStartBody
    {
        [JsonPropertyName("redirect_url")] public string? RedirectUrl { get; set; }
    }

    /// <summary>Operations of <c>identityVerification</c>.</summary>
    public sealed partial class IdentityVerificationResource : ResourceBase
    {
        public IdentityVerificationResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/identity_verification/settings</c> - Whether identity verification is on, and which connector does the checking.</summary>
        /// <remarks>Needs the <c>identity_verification:read</c> scope.</remarks>
        public Task<JsonElement> SettingsAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/identity_verification/settings", cancellationToken: ct);

        /// <summary><c>PUT /v1/identity_verification/settings</c> - Turn identity verification on or off and choose the connector (a Persona, Onfido, Veriff or Stripe Identity service connector; its credentials stay in the vault). `as_challenge` offers a verified identity as the custom second step of sign-in (the custom challenge factor must be enabled).</summary>
        /// <remarks>Needs the <c>identity_verification:write</c> scope.</remarks>
        public Task<JsonElement> UpdateSettingsAsync(IdentityVerificationUpdateSettingsBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/identity_verification/settings", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/identity_verifications</c> - List checks by status (default `needs_review`, the review queue), newest first.</summary>
        /// <remarks>Needs the <c>identity_verification:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? status = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/identity_verifications", query: Q(("status", status), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/identity_verifications/{id}</c> - Read one check. A pending check is refreshed from the verification service first.</summary>
        /// <remarks>Needs the <c>identity_verification:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/identity_verifications/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/identity_verifications/{id}/decide</c> - Decide a check that is waiting for review (or override a pending one) as `approved` or `declined`. The outcome is written to the user as `verification.identity`. 409 when the check is already approved or declined.</summary>
        /// <remarks>Needs the <c>identity_verification:write</c> scope.</remarks>
        public Task<JsonElement> DecideAsync(string id, IdentityVerificationDecideBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/identity_verifications/{Enc(id)}/decide", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/users/{id}/identity_verifications</c> - A user's identity checks, newest first.</summary>
        /// <remarks>Needs the <c>identity_verification:read</c> scope.</remarks>
        public Task<JsonElement> ForUserAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/users/{Enc(id)}/identity_verifications", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/users/{id}/identity_verifications</c> - Start an identity check for a user and get the verification service's hosted page (`url`) to send them to. A check started in the last 30 minutes and still pending is returned instead of a new one; at most 5 checks per user per hour. 409 when identity verification is not turned on.</summary>
        /// <remarks>Needs the <c>identity_verification:write</c> scope.</remarks>
        public Task<JsonElement> StartAsync(string id, IdentityVerificationStartBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/users/{Enc(id)}/identity_verifications", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
