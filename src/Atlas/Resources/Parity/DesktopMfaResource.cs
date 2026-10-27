using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/desktop_mfa/policy</c>. Only the fields set are sent.</summary>
    public sealed class DesktopMfaUpdatePolicyBody
    {
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("applies_to")] public string? AppliesTo { get; set; }
        [JsonPropertyName("user_ids")] public List<string>? UserIds { get; set; }
        [JsonPropertyName("org_ids")] public List<string>? OrgIds { get; set; }
        [JsonPropertyName("allowed_factors")] public List<string>? AllowedFactors { get; set; }
        [JsonPropertyName("offline_enabled")] public bool? OfflineEnabled { get; set; }
        [JsonPropertyName("offline_grace_hours")] public double? OfflineGraceHours { get; set; }
        [JsonPropertyName("offline_pin_required")] public bool? OfflinePinRequired { get; set; }
    }

    /// <summary>The body of <c>POST /v1/desktop_mfa/enrolment_codes</c>. Only the fields set are sent.</summary>
    public sealed class DesktopMfaCreateEnrolmentCodeBody
    {
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("expires_in_minutes")] public double? ExpiresInMinutes { get; set; }
    }

    /// <summary>Operations of <c>desktopMfa</c>.</summary>
    public sealed partial class DesktopMfaResource : ResourceBase
    {
        public DesktopMfaResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/desktop_mfa/policy</c> - The desktop-login MFA policy: who must pass a second factor at OS login, the allowed factors and the offline grace window.</summary>
        /// <remarks>Needs the <c>desktop_mfa:read</c> scope.</remarks>
        public Task<JsonElement> GetPolicyAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/desktop_mfa/policy", cancellationToken: ct);

        /// <summary><c>PUT /v1/desktop_mfa/policy</c> - Change the desktop-login MFA policy. Send only the fields to change. `applies_to: "selected"` needs at least one user or organization while enabled.</summary>
        /// <remarks>Needs the <c>desktop_mfa:write</c> scope.</remarks>
        public Task<JsonElement> UpdatePolicyAsync(DesktopMfaUpdatePolicyBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/desktop_mfa/policy", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/desktop_mfa/enrolment_codes</c> - Issue a single-use code a user types into the workstation agent to enrol it. Valid for `expires_in_minutes` (default 60, at most 7 days). Shown once.</summary>
        /// <remarks>Needs the <c>desktop_mfa:write</c> scope.</remarks>
        public Task<JsonElement> CreateEnrolmentCodeAsync(DesktopMfaCreateEnrolmentCodeBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/desktop_mfa/enrolment_codes", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/desktop_mfa/devices</c> - List enrolled workstations, optionally for one `user_id`.</summary>
        /// <remarks>Needs the <c>desktop_mfa:read</c> scope.</remarks>
        public Task<JsonElement> ListDevicesAsync(string? userId = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/desktop_mfa/devices", query: Q(("user_id", userId), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/desktop_mfa/devices/{id}</c> - Read one workstation with the offline verifiers issued to it.</summary>
        /// <remarks>Needs the <c>desktop_mfa:read</c> scope.</remarks>
        public Task<JsonElement> GetDeviceAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/desktop_mfa/devices/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/desktop_mfa/devices/{id}</c> - Delete a workstation and its device key.</summary>
        /// <remarks>Needs the <c>desktop_mfa:write</c> scope.</remarks>
        public Task<JsonElement> DeleteDeviceAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/desktop_mfa/devices/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/desktop_mfa/devices/{id}/revoke</c> - Revoke a workstation. Its next call is refused with `device_status: revoked` and it wipes its offline cache; offline unlock stops working at the latest when the cached verifier expires.</summary>
        /// <remarks>Needs the <c>desktop_mfa:write</c> scope.</remarks>
        public Task<JsonElement> RevokeDeviceAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/desktop_mfa/devices/{Enc(id)}/revoke", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/desktop_mfa/devices/{id}/revoke_offline_cache</c> - Void every offline verifier issued to the workstation without revoking it. It receives a fresh one on its next sync.</summary>
        /// <remarks>Needs the <c>desktop_mfa:write</c> scope.</remarks>
        public Task<JsonElement> RevokeOfflineCacheAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/desktop_mfa/devices/{Enc(id)}/revoke_offline_cache", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
