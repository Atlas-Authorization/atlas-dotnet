using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>A policy version as referenced from a <see cref="Policy"/>.</summary>
    public sealed class PolicyVersionRef
    {
        public string Id { get; init; } = "";
        public string Version { get; init; } = "";
        /// <summary>draft, published or retired.</summary>
        public string Status { get; init; } = "";
        public string ContentSha256 { get; init; } = "";
        /// <summary>Epoch ms.</summary>
        public long? PublishedAt { get; init; }
        /// <summary>Epoch ms.</summary>
        public long? EffectiveAt { get; init; }
    }

    /// <summary>A policy (terms of service, privacy policy, any key) users accept. Timestamps are epoch milliseconds.</summary>
    public sealed class Policy
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy";
        public string Id { get; init; } = "";
        public string Key { get; init; } = "";
        public string Title { get; init; } = "";
        /// <summary>checkbox, scroll or link.</summary>
        public string Display { get; init; } = "";
        public bool Required { get; init; }
        /// <summary>sign_up or sign_in_and_sign_up.</summary>
        public string AppliesTo { get; init; } = "";
        /// <summary>none, next_sign_in or grace.</summary>
        public string Reaccept { get; init; } = "";
        public int? GraceDays { get; init; }
        /// <summary>atlas or external.</summary>
        public string Source { get; init; } = "";
        public int Position { get; init; }
        /// <summary>active or retired.</summary>
        public string Status { get; init; } = "";
        /// <summary>External policies: fields changed in Atlas that a sync keeps.</summary>
        public List<string> Overrides { get; init; } = new List<string>();
        /// <summary>The version in effect now.</summary>
        public PolicyVersionRef? CurrentVersion { get; init; }
        /// <summary>A newer published version that is not in effect yet.</summary>
        public PolicyVersionRef? UpcomingVersion { get; init; }
        /// <summary>The newest version of any status.</summary>
        public PolicyVersionRef? LatestVersion { get; init; }
        public long? RetiredAt { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    /// <summary>One version of a policy: draft, published (immutable) or retired. <c>ContentSha256</c> is the hex SHA-256 of <c>ContentHtml</c>.</summary>
    public sealed class PolicyVersion
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy_version";
        public string Id { get; init; } = "";
        public string PolicyId { get; init; } = "";
        /// <summary>The policy key.</summary>
        public string Policy { get; init; } = "";
        public string Version { get; init; } = "";
        public string Status { get; init; } = "";
        public string? ContentMarkdown { get; init; }
        /// <summary>The sanitised HTML clients render.</summary>
        public string ContentHtml { get; init; } = "";
        public string? ContentUrl { get; init; }
        public string ContentSha256 { get; init; } = "";
        public string? SummaryOfChanges { get; init; }
        public string? Locale { get; init; }
        public long? PublishedAt { get; init; }
        public long? EffectiveAt { get; init; }
        public long? RetiredAt { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>On list: how many users accepted this version.</summary>
        public int? AcceptanceCount { get; init; }
    }

    /// <summary>A user's acceptance of a policy version, with the reading evidence.</summary>
    public sealed class PolicyAcceptance
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy_acceptance";
        public string Id { get; init; } = "";
        public string UserId { get; init; } = "";
        public string Policy { get; init; } = "";
        public string Version { get; init; } = "";
        public string? PolicyVersionId { get; init; }
        /// <summary>accepted or withdrawn.</summary>
        public string Status { get; init; } = "";
        /// <summary>signup, signin, reaccept, api or import.</summary>
        public string? Source { get; init; }
        public string? ContentSha256 { get; init; }
        public string? Display { get; init; }
        public bool? ScrolledToEnd { get; init; }
        public int? ViewMs { get; init; }
        public string? Locale { get; init; }
        public string? Ip { get; init; }
        public string? UserAgent { get; init; }
        public long AcceptedAt { get; init; }
    }

    /// <summary>One user's standing on a policy. <c>Blocking</c>: accepting is required before the next sign-in completes.</summary>
    public sealed class PolicyStanding
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy_standing";
        public string Policy { get; init; } = "";
        public string PolicyId { get; init; } = "";
        public string Title { get; init; } = "";
        public string Display { get; init; } = "";
        public bool Required { get; init; }
        public string AppliesTo { get; init; } = "";
        public string Reaccept { get; init; } = "";
        public string? CurrentVersionId { get; init; }
        public string? CurrentVersion { get; init; }
        public string? ContentSha256 { get; init; }
        public string? SummaryOfChanges { get; init; }
        public string? AcceptedVersionId { get; init; }
        public string? AcceptedVersion { get; init; }
        public long? AcceptedAt { get; init; }
        public bool NeedsReaccept { get; init; }
        public bool Blocking { get; init; }
        public long? GraceEndsAt { get; init; }
    }

    /// <summary><c>GET /v1/users/:id/policies</c>: a user's standing on every policy.</summary>
    public sealed class UserPolicies
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public string UserId { get; init; } = "";
        public List<PolicyStanding> Data { get; init; } = new List<PolicyStanding>();
        public bool NeedsAcceptance { get; init; }
    }

    /// <summary><c>DELETE /v1/policies/:id</c>: a never-published policy is deleted, one with history is retired.</summary>
    public sealed class DeletedPolicy
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy";
        public string Id { get; init; } = "";
        public string Key { get; init; } = "";
        public bool Deleted { get; init; }
        public bool Retired { get; init; }
        public Policy? Policy { get; init; }
    }

    /// <summary>A deleted draft version.</summary>
    public sealed class DeletedPolicyVersion
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy_version";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary><c>SessionClaim</c>: put <c>pol</c> (accepted versions of required policies) on session tokens.</summary>
    public sealed class PolicySettings
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy_settings";
        public bool SessionClaim { get; init; }
    }

    /// <summary>The external policy source (policy source protocol). The signing secret is write-only; <c>SigningSecretSet</c> says whether one is stored.</summary>
    public sealed class PolicySource
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy_source";
        public bool Enabled { get; init; }
        public string? EndpointUrl { get; init; }
        public bool SigningSecretSet { get; init; }
        /// <summary>webhook or poll.</summary>
        public string Sync { get; init; } = "";
        public int PollIntervalMinutes { get; init; }
        /// <summary>Where your source sends policy.published / policy.retired events.</summary>
        public string? WebhookUrl { get; init; }
        public string ProtocolVersion { get; init; } = "";
        public long? LastSyncedAt { get; init; }
        public bool? LastSyncOk { get; init; }
        public string? LastSyncError { get; init; }
        public long? NextPollAt { get; init; }
    }

    /// <summary>One fetch, test, poll or event exchanged with the policy source.</summary>
    public sealed class PolicySourceDelivery
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy_source_delivery";
        public string Id { get; init; } = "";
        /// <summary>outbound or inbound.</summary>
        public string Direction { get; init; } = "";
        /// <summary>fetch, test, poll or webhook.</summary>
        public string Kind { get; init; } = "";
        /// <summary>ok, failed or rejected.</summary>
        public string Status { get; init; } = "";
        public int? HttpStatus { get; init; }
        public string? Error { get; init; }
        public string? EventType { get; init; }
        public string? Policy { get; init; }
        public string? Version { get; init; }
        public string? Url { get; init; }
        public Dictionary<string, JsonElement>? Changes { get; init; }
        public int? LatencyMs { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>A policy found in the feed by a source test.</summary>
    public sealed class PolicySourceTestPolicy
    {
        public string Key { get; init; } = "";
        public string Title { get; init; } = "";
        public List<string> Versions { get; init; } = new List<string>();
    }

    /// <summary><c>POST /v1/instance/policy_source/test</c>: a signed fetch of the feed, validated but not stored.</summary>
    public sealed class PolicySourceTest
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy_source_test";
        public bool Ok { get; init; }
        public int? HttpStatus { get; init; }
        public int? LatencyMs { get; init; }
        public string? Error { get; init; }
        public List<string> Warnings { get; init; } = new List<string>();
        public List<PolicySourceTestPolicy> Policies { get; init; } = new List<PolicySourceTestPolicy>();
        public PolicySourceDelivery? Delivery { get; init; }
    }

    /// <summary>What a sync changed, as policy keys.</summary>
    public sealed class PolicySourceSyncChanges
    {
        public List<string> Created { get; init; } = new List<string>();
        public List<string> Updated { get; init; } = new List<string>();
        public List<string> Published { get; init; } = new List<string>();
        public List<string> Unchanged { get; init; } = new List<string>();
        public List<string> Rejected { get; init; } = new List<string>();
        public List<string> Retired { get; init; } = new List<string>();
    }

    /// <summary><c>POST /v1/instance/policy_source/sync</c>: fetch and store now.</summary>
    public sealed class PolicySourceSync
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "policy_source_sync";
        public bool Ok { get; init; }
        public string? Error { get; init; }
        public List<string> Warnings { get; init; } = new List<string>();
        public PolicySourceSyncChanges? Changes { get; init; }
        public PolicySourceDelivery? Delivery { get; init; }
    }

    /// <summary>Body for <c>POST /v1/policies</c>.</summary>
    public sealed class CreatePolicyBody
    {
        /// <summary>Slug (lowercase letters, digits, _ and -), unique per instance. Becomes the consent record's policy.</summary>
        public string? Key { get; set; }
        public string? Title { get; set; }
        /// <summary>checkbox, scroll or link.</summary>
        public string? Display { get; set; }
        public bool? Required { get; set; }
        /// <summary>sign_up or sign_in_and_sign_up.</summary>
        public string? AppliesTo { get; set; }
        /// <summary>none, next_sign_in or grace.</summary>
        public string? Reaccept { get; set; }
        /// <summary>Required with reaccept grace (1-365).</summary>
        public int? GraceDays { get; set; }
        public int? Position { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name, including one this SDK does not model yet.</summary>
        public CreatePolicyBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }

        /// <summary>Clear a nullable field by its wire name (it is sent as <c>null</c>).</summary>
        public CreatePolicyBody Clear(string key) => Set(key, null);
    }

    /// <summary>Body for <c>PATCH /v1/policies/:id</c>: only the fields set change.</summary>
    public sealed class UpdatePolicyBody
    {
        public string? Title { get; set; }
        public string? Display { get; set; }
        public bool? Required { get; set; }
        public string? AppliesTo { get; set; }
        public string? Reaccept { get; set; }
        /// <summary>Clear with <c>Clear("grace_days")</c>.</summary>
        public int? GraceDays { get; set; }
        public int? Position { get; set; }
        /// <summary>active or retired.</summary>
        public string? Status { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name, including one this SDK does not model yet.</summary>
        public UpdatePolicyBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }

        /// <summary>Clear a nullable field by its wire name (it is sent as <c>null</c>).</summary>
        public UpdatePolicyBody Clear(string key) => Set(key, null);
    }

    /// <summary>Body for <c>POST /v1/policies/:id/versions</c>: a draft, or published at once with <c>Publish</c>.</summary>
    public sealed class CreatePolicyVersionBody
    {
        /// <summary>A label such as 2026-10-24 or 1.2.0, unique per policy.</summary>
        public string? Version { get; set; }
        /// <summary>Rendered and sanitised by Atlas; or <c>ContentHtml</c>.</summary>
        public string? ContentMarkdown { get; set; }
        public string? ContentHtml { get; set; }
        public string? ContentUrl { get; set; }
        public string? SummaryOfChanges { get; set; }
        public string? Locale { get; set; }
        /// <summary>Publish immediately instead of saving a draft.</summary>
        public bool? Publish { get; set; }
        /// <summary>With Publish: epoch ms when the gate starts asking for it (default now). For ISO 8601, use <c>Set("effective_at", "...")</c>.</summary>
        public long? EffectiveAt { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name, including one this SDK does not model yet.</summary>
        public CreatePolicyVersionBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }

        /// <summary>Clear a nullable field by its wire name (it is sent as <c>null</c>).</summary>
        public CreatePolicyVersionBody Clear(string key) => Set(key, null);
    }

    /// <summary>Body for <c>PATCH /v1/policies/:id/versions/:versionId</c> (drafts only).</summary>
    public sealed class UpdatePolicyVersionBody
    {
        public string? Version { get; set; }
        public string? ContentMarkdown { get; set; }
        public string? ContentHtml { get; set; }
        public string? ContentUrl { get; set; }
        public string? SummaryOfChanges { get; set; }
        public string? Locale { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name, including one this SDK does not model yet.</summary>
        public UpdatePolicyVersionBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }

        /// <summary>Clear a nullable field by its wire name (it is sent as <c>null</c>).</summary>
        public UpdatePolicyVersionBody Clear(string key) => Set(key, null);
    }

    /// <summary>Body for <c>PUT /v1/instance/policy_source</c>: partial. <c>SigningSecret</c> is write-only (a whsec_ secret works).</summary>
    public sealed class UpdatePolicySourceBody
    {
        public bool? Enabled { get; set; }
        /// <summary>Base URL; Atlas calls GET {endpoint_url}/policies.</summary>
        public string? EndpointUrl { get; set; }
        /// <summary>Write-only.</summary>
        public string? SigningSecret { get; set; }
        /// <summary>webhook or poll.</summary>
        public string? Sync { get; set; }
        /// <summary>Poll mode: 5-1440.</summary>
        public int? PollIntervalMinutes { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name, including one this SDK does not model yet.</summary>
        public UpdatePolicySourceBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }

        /// <summary>Clear a nullable field by its wire name (it is sent as <c>null</c>).</summary>
        public UpdatePolicySourceBody Clear(string key) => Set(key, null);
    }

    /// <summary>A page of acceptances; continue with <c>NextCursor</c> as <c>starting_after</c>.</summary>
    public sealed class PolicyAcceptancePage
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<PolicyAcceptance> Data { get; init; } = new();
        public bool HasMore { get; init; }
        public string? NextCursor { get; init; }
    }

    /// <summary>A page of policy source deliveries.</summary>
    public sealed class PolicySourceDeliveryPage
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<PolicySourceDelivery> Data { get; init; } = new();
        public bool HasMore { get; init; }
        public string? NextCursor { get; init; }
    }

    /// <summary>
    /// Policy acceptance (<c>/v1/policies</c>): the policies (terms of service, privacy policy, any key) your users
    /// accept at sign-up and, per policy, again at sign-in when a new version takes effect. A policy's versions go
    /// draft, published (with an <c>effective_at</c>), retired; a published version is immutable and its
    /// <c>ContentSha256</c> is the SHA-256 of the sanitised HTML users are shown. A policy is addressed by id or key,
    /// a version by id or label. Content can also come from an external system: see <see cref="PolicySourceResource"/>
    /// and <see cref="Atlas.Verification.PolicySourceProtocol"/>.
    /// </summary>
    public sealed class PoliciesResource : ResourceBase
    {
        public PoliciesResource(AtlasTransport transport) : base(transport) { }

        private static string VersionsPath(string idOrKey) => "/v1/policies/" + Enc(idOrKey) + "/versions";
        private static string VersionPath(string idOrKey, string version) => VersionsPath(idOrKey) + "/" + Enc(version);

        /// <summary><c>GET /v1/policies</c> — active policies (<paramref name="includeRetired"/> adds retired ones).</summary>
        public Task<ListPage<Policy>> ListAsync(bool? includeRetired = null, CancellationToken ct = default)
            => Req<ListPage<Policy>>(HttpVerb.Get, "/v1/policies", query: Q(("include_retired", includeRetired)), cancellationToken: ct);

        /// <summary><c>POST /v1/policies</c>.</summary>
        public Task<Policy> CreateAsync(CreatePolicyBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Policy>(HttpVerb.Post, "/v1/policies", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/policies/:id</c> — by id or key.</summary>
        public Task<Policy> GetAsync(string idOrKey, CancellationToken ct = default)
            => Req<Policy>(HttpVerb.Get, "/v1/policies/" + Enc(idOrKey), cancellationToken: ct);

        /// <summary><c>PATCH /v1/policies/:id</c>.</summary>
        public Task<Policy> UpdateAsync(string idOrKey, UpdatePolicyBody body, CancellationToken ct = default)
            => Req<Policy>(HttpVerb.Patch, "/v1/policies/" + Enc(idOrKey), body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/policies/:id</c> — deletes a never-published policy, retires one with history.</summary>
        public Task<DeletedPolicy> DeleteAsync(string idOrKey, CancellationToken ct = default)
            => Req<DeletedPolicy>(HttpVerb.Delete, "/v1/policies/" + Enc(idOrKey), cancellationToken: ct);

        /// <summary><c>GET /v1/policies/:id/versions</c>.</summary>
        public Task<ListPage<PolicyVersion>> ListVersionsAsync(string idOrKey, CancellationToken ct = default)
            => Req<ListPage<PolicyVersion>>(HttpVerb.Get, VersionsPath(idOrKey), cancellationToken: ct);

        /// <summary><c>POST /v1/policies/:id/versions</c> — a draft, or published at once with <c>Publish</c>.</summary>
        public Task<PolicyVersion> CreateVersionAsync(string idOrKey, CreatePolicyVersionBody body, CancellationToken ct = default)
            => Req<PolicyVersion>(HttpVerb.Post, VersionsPath(idOrKey), body, cancellationToken: ct);

        /// <summary><c>GET /v1/policies/:id/versions/:versionId</c> — by version id or label.</summary>
        public Task<PolicyVersion> GetVersionAsync(string idOrKey, string versionIdOrLabel, CancellationToken ct = default)
            => Req<PolicyVersion>(HttpVerb.Get, VersionPath(idOrKey, versionIdOrLabel), cancellationToken: ct);

        /// <summary><c>PATCH /v1/policies/:id/versions/:versionId</c> — drafts only.</summary>
        public Task<PolicyVersion> UpdateVersionAsync(string idOrKey, string versionIdOrLabel, UpdatePolicyVersionBody body, CancellationToken ct = default)
            => Req<PolicyVersion>(HttpVerb.Patch, VersionPath(idOrKey, versionIdOrLabel), body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/policies/:id/versions/:versionId</c> — drafts only.</summary>
        public Task<DeletedPolicyVersion> DeleteVersionAsync(string idOrKey, string versionIdOrLabel, CancellationToken ct = default)
            => Req<DeletedPolicyVersion>(HttpVerb.Delete, VersionPath(idOrKey, versionIdOrLabel), cancellationToken: ct);

        /// <summary>
        /// <c>POST …/publish</c> — <paramref name="effectiveAtMs"/> (epoch ms; null is now) is when the gate starts asking
        /// users for it; it is never earlier than the publish time.
        /// </summary>
        public Task<PolicyVersion> PublishVersionAsync(string idOrKey, string versionIdOrLabel, long? effectiveAtMs = null, CancellationToken ct = default)
            => Req<PolicyVersion>(HttpVerb.Post, VersionPath(idOrKey, versionIdOrLabel) + "/publish", Body(("effective_at", effectiveAtMs)), cancellationToken: ct);

        /// <summary><see cref="PublishVersionAsync"/> with an ISO 8601 <c>effective_at</c>.</summary>
        public Task<PolicyVersion> PublishVersionAtAsync(string idOrKey, string versionIdOrLabel, string effectiveAtIso, CancellationToken ct = default)
            => Req<PolicyVersion>(HttpVerb.Post, VersionPath(idOrKey, versionIdOrLabel) + "/publish", Body(("effective_at", effectiveAtIso)), cancellationToken: ct);

        /// <summary><c>POST …/retire</c>.</summary>
        public Task<PolicyVersion> RetireVersionAsync(string idOrKey, string versionIdOrLabel, CancellationToken ct = default)
            => Req<PolicyVersion>(HttpVerb.Post, VersionPath(idOrKey, versionIdOrLabel) + "/retire", cancellationToken: ct);

        /// <summary>
        /// <c>GET /v1/policies/:id/acceptances</c> — who accepted, newest first. <paramref name="version"/> is an id or label,
        /// <paramref name="status"/> is <c>accepted</c> (default), <c>withdrawn</c> or <c>all</c>.
        /// </summary>
        public Task<PolicyAcceptancePage> ListAcceptancesAsync(string idOrKey, string? version = null, string? status = null, int? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<PolicyAcceptancePage>(HttpVerb.Get, "/v1/policies/" + Enc(idOrKey) + "/acceptances",
                query: Q(("version", version), ("status", status), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary>The same as CSV (<c>format=csv</c>, up to 100,000 rows).</summary>
        public Task<string> ExportAcceptancesCsvAsync(string idOrKey, string? version = null, string? status = null, CancellationToken ct = default)
            => Req<string>(HttpVerb.Get, "/v1/policies/" + Enc(idOrKey) + "/acceptances",
                query: Q(("version", version), ("status", status), ("format", "csv")), raw: true, cancellationToken: ct);

        /// <summary><c>GET /v1/users/:id/policies</c> — a user's standing on every policy.</summary>
        public Task<UserPolicies> ForUserAsync(string userId, CancellationToken ct = default)
            => Req<UserPolicies>(HttpVerb.Get, "/v1/users/" + Enc(userId) + "/policies", cancellationToken: ct);

        /// <summary><c>GET /v1/instance/policy_settings</c>.</summary>
        public Task<PolicySettings> GetSettingsAsync(CancellationToken ct = default)
            => Req<PolicySettings>(HttpVerb.Get, "/v1/instance/policy_settings", cancellationToken: ct);

        /// <summary><c>PATCH /v1/instance/policy_settings</c> — <paramref name="sessionClaim"/> puts <c>pol</c> on session tokens.</summary>
        public Task<PolicySettings> UpdateSettingsAsync(bool sessionClaim, CancellationToken ct = default)
            => Req<PolicySettings>(HttpVerb.Patch, "/v1/instance/policy_settings", Body(("session_claim", sessionClaim)), cancellationToken: ct);
    }

    /// <summary>
    /// The external policy source (<c>/v1/instance/policy_source</c>): Atlas fetches <c>GET {endpoint_url}/policies</c>
    /// signed like an Atlas webhook, and your system tells Atlas about a new version with a signed
    /// <c>policy.published</c> / <c>policy.retired</c> event sent to <c>WebhookUrl</c>. The signing secret is
    /// write-only. See <see cref="Atlas.Verification.PolicySourceProtocol"/> for the verify and sign helpers.
    /// </summary>
    public sealed class PolicySourceResource : ResourceBase
    {
        public PolicySourceResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>GET /v1/instance/policy_source</c>.</summary>
        public Task<PolicySource> GetAsync(CancellationToken ct = default)
            => Req<PolicySource>(HttpVerb.Get, "/v1/instance/policy_source", cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/policy_source</c> — partial; the signing secret is write-only.</summary>
        public Task<PolicySource> UpdateAsync(UpdatePolicySourceBody body, CancellationToken ct = default)
            => Req<PolicySource>(HttpVerb.Put, "/v1/instance/policy_source", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/policy_source</c> — disconnect the source (its policies and history stay).</summary>
        public Task<PolicySource> DeleteAsync(CancellationToken ct = default)
            => Req<PolicySource>(HttpVerb.Delete, "/v1/instance/policy_source", cancellationToken: ct);

        /// <summary><c>POST /v1/instance/policy_source/test</c> — a signed fetch of the feed, validated but not stored.</summary>
        public Task<PolicySourceTest> TestAsync(CancellationToken ct = default)
            => Req<PolicySourceTest>(HttpVerb.Post, "/v1/instance/policy_source/test", cancellationToken: ct);

        /// <summary><c>POST /v1/instance/policy_source/sync</c> — fetch and store now.</summary>
        public Task<PolicySourceSync> SyncAsync(CancellationToken ct = default)
            => Req<PolicySourceSync>(HttpVerb.Post, "/v1/instance/policy_source/sync", cancellationToken: ct);

        /// <summary><c>GET /v1/instance/policy_source/deliveries</c>.</summary>
        public Task<PolicySourceDeliveryPage> ListDeliveriesAsync(int? limit = null, string? startingAfter = null, string? kind = null, string? status = null, CancellationToken ct = default)
            => Req<PolicySourceDeliveryPage>(HttpVerb.Get, "/v1/instance/policy_source/deliveries",
                query: Q(("limit", limit), ("starting_after", startingAfter), ("kind", kind), ("status", status)), cancellationToken: ct);
    }
}
