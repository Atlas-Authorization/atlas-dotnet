using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/ssf/receivers</c>. Only the fields set are sent.</summary>
    public sealed class SsfReceiversCreateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("issuer")] public string? Issuer { get; set; }
        [JsonPropertyName("jwks_uri")] public string? JwksUri { get; set; }
        [JsonPropertyName("audience")] public string? Audience { get; set; }
        [JsonPropertyName("actions")] public Dictionary<string, object?>? Actions { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/ssf/receivers/{id}</c>. Only the fields set are sent.</summary>
    public sealed class SsfReceiversUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("jwks_uri")] public string? JwksUri { get; set; }
        [JsonPropertyName("audience")] public string? Audience { get; set; }
        [JsonPropertyName("actions")] public Dictionary<string, object?>? Actions { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    }

    /// <summary>Operations of <c>ssf.receivers</c>.</summary>
    public sealed partial class SsfReceiversResource : ResourceBase
    {
        public SsfReceiversResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/ssf/receivers</c> - List inbound Shared Signals receivers: transmitters whose signed events act on this instance's users.</summary>
        /// <remarks>Needs the <c>ssf:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/ssf/receivers", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/ssf/receivers</c> - Register a transmitter. Its SETs are POSTed (`application/secevent+jwt`) to the returned `receive_endpoint` and verified against `jwks_uri`, `issuer` and the optional `audience`. `actions` maps an event type URI to `revoke_sessions`, `refresh_claims` (invalidate minted tokens so the next refresh carries fresh claims, sessions stay), `ban` or `ignore`; unmapped events are ignored. The default maps session-revoked, credential-change, credential-compromise and device-compliance-change (only when the new status is not-compliant) to `revoke_sessions`, token-claims-change to `refresh_claims` and account-disabled to `ban`.</summary>
        /// <remarks>Needs the <c>ssf:write</c> scope.</remarks>
        public Task CreateAsync(SsfReceiversCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Post, "/v1/ssf/receivers", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PATCH /v1/ssf/receivers/{id}</c> - Change a receiver: name, JWKS URI, audience, action mapping, or enabled.</summary>
        /// <remarks>Needs the <c>ssf:write</c> scope.</remarks>
        public Task UpdateAsync(string id, SsfReceiversUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Patch, $"/v1/ssf/receivers/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/ssf/receivers/{id}</c> - Delete an inbound SSF receiver; its endpoint stops accepting events.</summary>
        /// <remarks>Needs the <c>ssf:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/ssf/receivers/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
