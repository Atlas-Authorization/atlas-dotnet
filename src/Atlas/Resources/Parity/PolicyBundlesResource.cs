using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/policy_bundles/verify</c>. Only the fields set are sent.</summary>
    public sealed class PolicyBundlesVerifyBody
    {
        /// <summary>Required. The bundle (tar.gz), base64-encoded.</summary>
        [JsonPropertyName("bundle")] public string? Bundle { get; set; }
        /// <summary>Required. The detached compact JWS (`header..signature`) over the archive bytes.</summary>
        [JsonPropertyName("signature")] public string? Signature { get; set; }
        /// <summary>Public keys to trust IN ADDITION to this instance's own, as a JWKS — for a bundle signed by another instance (promote staging to production). Private key material is rejected.</summary>
        [JsonPropertyName("trusted_jwks")] public Dictionary<string, object?>? TrustedJwks { get; set; }
    }

    /// <summary>The body of <c>POST /v1/policy_bundles/import</c>. Only the fields set are sent.</summary>
    public sealed class PolicyBundlesImportBody
    {
        /// <summary>Required. The bundle (tar.gz), base64-encoded.</summary>
        [JsonPropertyName("bundle")] public string? Bundle { get; set; }
        /// <summary>Required. The detached compact JWS (`header..signature`) over the archive bytes.</summary>
        [JsonPropertyName("signature")] public string? Signature { get; set; }
        /// <summary>Public keys to trust IN ADDITION to this instance's own, as a JWKS — for a bundle signed by another instance (promote staging to production). Private key material is rejected.</summary>
        [JsonPropertyName("trusted_jwks")] public Dictionary<string, object?>? TrustedJwks { get; set; }
        /// <summary>Return the diff and apply nothing.</summary>
        [JsonPropertyName("dry_run")] public bool? DryRun { get; set; }
        /// <summary>Also delete ABAC policies absent from the bundle.</summary>
        [JsonPropertyName("prune")] public bool? Prune { get; set; }
    }

    /// <summary>Operations of <c>policyBundles</c>.</summary>
    public sealed partial class PolicyBundlesResource : ResourceBase
    {
        public PolicyBundlesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/policy_bundles/current</c> - The bundle revision the instance policy is at right now. Compare it with a deployed bundle’s revision to detect drift.</summary>
        /// <remarks>Needs the <c>policy_bundles:read</c> scope.</remarks>
        public Task<JsonElement> CurrentAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/policy_bundles/current", cancellationToken: ct);

        /// <summary><c>GET /v1/policy_bundles</c> - List the signed policy bundles exported or imported on this instance, newest first (without the archives).</summary>
        /// <remarks>Needs the <c>policy_bundles:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/policy_bundles", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/policy_bundles</c> - Export the instance policy — every ABAC policy (current version), the ABAC entity schema, templates and template links, and every FGA store’s latest authorization model — as a versioned tar.gz with a manifest of file digests, signed with the instance signing key (detached JWS). The same policy always has the same revision.</summary>
        /// <remarks>Needs the <c>policy_bundles:write</c> scope.</remarks>
        public Task<JsonElement> ExportAsync(bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/policy_bundles", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/policy_bundles/{revision}</c> - Read a stored bundle by revision, with the base64 archive and its signature.</summary>
        /// <remarks>Needs the <c>policy_bundles:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string revision, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/policy_bundles/{Enc(revision)}", cancellationToken: ct);

        /// <summary><c>GET /v1/policy_bundles/{revision}/archive</c> - Download a stored bundle as the raw tar.gz (`application/gzip`). The detached signature is in the `Atlas-Bundle-Signature` header, with `Atlas-Bundle-Revision` and `Atlas-Bundle-Key-Id`.</summary>
        /// <remarks>Needs the <c>policy_bundles:read</c> scope.</remarks>
        public Task<byte[]> ArchiveAsync(string revision, CancellationToken ct = default)
            => Req<byte[]>(HttpVerb.Get, $"/v1/policy_bundles/{Enc(revision)}/archive", raw: true, cancellationToken: ct);

        /// <summary><c>POST /v1/policy_bundles/verify</c> - Check a bundle without importing it: the detached signature against this instance's signing keys (plus any `trusted_jwks` you supply) and the archive's integrity. The SDK can do the same check offline.</summary>
        /// <remarks>Needs the <c>policy_bundles:read</c> scope.</remarks>
        public Task<JsonElement> VerifyAsync(PolicyBundlesVerifyBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/policy_bundles/verify", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/policy_bundles/import</c> - Import a signed bundle. The signature and the archive integrity must both check out or nothing is touched (422). With `dry_run: true` it returns the diff of what would change and applies nothing (needs only policy_bundles:read). Otherwise it applies everything in one transaction: stores and models first, then the ABAC schema, templates, links and policies; a changed policy appends a version. `prune` also deletes ABAC policies, templates and links the bundle does not name (and the schema when it has none); FGA stores are never deleted.</summary>
        /// <remarks>Needs the <c>policy_bundles:write</c> scope.</remarks>
        public Task<JsonElement> ImportAsync(PolicyBundlesImportBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/policy_bundles/import", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
