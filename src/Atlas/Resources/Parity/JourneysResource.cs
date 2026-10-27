using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/flows</c>. Only the fields set are sent.</summary>
    public sealed class JourneysCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>The journey for every sign-in without an application-specific one (only one is default).</summary>
        [JsonPropertyName("is_default")] public bool? IsDefault { get; set; }
        /// <summary>OAuth client ids whose sign-ins use this journey instead of the default.</summary>
        [JsonPropertyName("applications")] public List<string>? Applications { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("definition")] public Dictionary<string, object?>? Definition { get; set; }
        /// <summary>Make the new version active (default true).</summary>
        [JsonPropertyName("activate")] public bool? Activate { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/flows/{id}</c>. Only the fields set are sent.</summary>
    public sealed class JourneysUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        /// <summary>The journey for every sign-in without an application-specific one (only one is default).</summary>
        [JsonPropertyName("is_default")] public bool? IsDefault { get; set; }
        /// <summary>OAuth client ids whose sign-ins use this journey instead of the default.</summary>
        [JsonPropertyName("applications")] public List<string>? Applications { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("definition")] public Dictionary<string, object?>? Definition { get; set; }
        /// <summary>Make the new version active (default true).</summary>
        [JsonPropertyName("activate")] public bool? Activate { get; set; }
    }

    /// <summary>The body of <c>POST /v1/flows/{id}/activate</c>. Only the fields set are sent.</summary>
    public sealed class JourneysActivateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("version")] public long? Version { get; set; }
    }

    /// <summary>The body of <c>POST /v1/flows/{id}/simulate</c>. Only the fields set are sent.</summary>
    public sealed class JourneysSimulateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("method")] public string? Method { get; set; }
        [JsonPropertyName("mfa_verified")] public bool? MfaVerified { get; set; }
        [JsonPropertyName("ip")] public string? Ip { get; set; }
        [JsonPropertyName("application_id")] public string? ApplicationId { get; set; }
        [JsonPropertyName("version")] public long? Version { get; set; }
    }

    /// <summary>Operations of <c>journeys</c>.</summary>
    public sealed partial class JourneysResource : ResourceBase
    {
        public JourneysResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/flows</c> - The sign-in journeys.</summary>
        /// <remarks>Needs the <c>actions:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/flows", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/flows</c> - Create a sign-in journey (version 1, active unless activate is false). Steps run top to bottom once a sign-in has passed its factors: refusals (deny, require_method, require_mfa), required actions the user must finish first, Action calls that may refuse, session claims and the redirect after sign-in. Action steps must name workflow_step Actions. -&gt; 201.</summary>
        /// <remarks>Needs the <c>actions:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(JourneysCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/flows", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/flows/{id}</c> - One journey with its active definition (else the latest).</summary>
        /// <remarks>Needs the <c>actions:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/flows/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/flows/{id}</c> - Change a journey. A definition becomes the next version (immutable), active unless activate is false.</summary>
        /// <remarks>Needs the <c>actions:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, JourneysUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/flows/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/flows/{id}</c> - Delete a journey and its versions.</summary>
        /// <remarks>Needs the <c>actions:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/flows/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/flows/{id}/versions</c> - Every version of a journey, newest first.</summary>
        /// <remarks>Needs the <c>actions:read</c> scope.</remarks>
        public Task<JsonElement> VersionsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/flows/{Enc(id)}/versions", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/flows/{id}/activate</c> - Make a version the active one (roll forward or back).</summary>
        /// <remarks>Needs the <c>actions:write</c> scope.</remarks>
        public Task<JsonElement> ActivateAsync(string id, JourneysActivateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/flows/{Enc(id)}/activate", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/flows/{id}/simulate</c> - What-if: run a version (default the active one) for a user and a described sign-in (method, mfa_verified, ip, application_id) without changing anything. Returns the outcome (allowed, held or refused), the effects and a per-step trace.</summary>
        /// <remarks>Needs the <c>actions:read</c> scope.</remarks>
        public Task<JsonElement> SimulateAsync(string id, JourneysSimulateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/flows/{Enc(id)}/simulate", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
