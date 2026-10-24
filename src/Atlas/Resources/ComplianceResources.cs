using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    // Notices, security advisories, sub-processors, audit export and the standard entitlements document.

    // ---- notices & advisories ------------------------------------------------------

    /// <summary>A behaviour change that does (or does not) affect this instance.</summary>
    public sealed class InstanceNotice
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Date { get; init; } = "";
        /// <summary><c>behavior_change</c>, <c>deprecation</c> or <c>new</c>.</summary>
        public string Type { get; init; } = "";
        /// <summary><c>breaking</c>, <c>behaviour</c> or <c>security</c>.</summary>
        public List<string> Tags { get; init; } = new List<string>();
        public string Title { get; init; } = "";
        public string Summary { get; init; } = "";
        public List<string>? Affects { get; init; }
        public string? DocsUrl { get; init; }
        public string? ChangelogId { get; init; }
        public bool Applies { get; init; }
        /// <summary>One sentence about this instance.</summary>
        public string? Detail { get; init; }
        public int? Count { get; init; }
        public long? FirstSeenAt { get; init; }
        public long? EmailedAt { get; init; }
        public long? AcknowledgedAt { get; init; }
    }

    /// <summary>The notice list with its counts.</summary>
    public sealed class InstanceNoticeList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public List<InstanceNotice> Data { get; init; } = new List<InstanceNotice>();
        public bool HasMore { get; init; }
        public int Unacknowledged { get; init; }
        public string? ChangelogUrl { get; init; }
    }

    /// <summary><c>/v1/instance/notices</c>.</summary>
    public sealed class NoticesResource : ResourceBase
    {
        public NoticesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/instance/notices</c> — the behaviour changes that affect this instance; <paramref name="all"/> also lists the ones that do not apply.</summary>
        public Task<InstanceNoticeList> ListAsync(bool? all = null, CancellationToken ct = default)
            => Req<InstanceNoticeList>(HttpVerb.Get, "/v1/instance/notices", query: Q(("all", all)), cancellationToken: ct);

        /// <summary><c>POST /v1/instance/notices/{id}/acknowledge</c> — mark a notice as read.</summary>
        public Task<InstanceNotice> AcknowledgeAsync(string id, CancellationToken ct = default)
            => Req<InstanceNotice>(HttpVerb.Post, $"/v1/instance/notices/{Enc(id)}/acknowledge", cancellationToken: ct);
    }

    /// <summary>An advisory, with whether it applies to this instance (absent on the public catalog).</summary>
    public sealed class SecurityAdvisory
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Published { get; init; } = "";
        /// <summary><c>low</c>, <c>medium</c>, <c>high</c> or <c>critical</c>.</summary>
        public string Severity { get; init; } = "";
        public string Title { get; init; } = "";
        public string Summary { get; init; } = "";
        public List<string>? Affects { get; init; }
        public string? FixedOn { get; init; }
        public string? Action { get; init; }
        public string? DocsUrl { get; init; }
        public bool? Applies { get; init; }
        public string? Detail { get; init; }
        public int? Count { get; init; }
        public long? FirstSeenAt { get; init; }
        public long? EmailedAt { get; init; }
        public long? AcknowledgedAt { get; init; }
    }

    /// <summary>The advisory list with the count that applies.</summary>
    public sealed class SecurityAdvisoryList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public List<SecurityAdvisory> Data { get; init; } = new List<SecurityAdvisory>();
        public bool HasMore { get; init; }
        public int Applying { get; init; }
    }

    /// <summary><c>/v1/security_advisories</c>.</summary>
    public sealed class SecurityAdvisoriesResource : ResourceBase
    {
        public SecurityAdvisoriesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/security_advisories</c> — every advisory with whether it applies to this instance.</summary>
        public Task<SecurityAdvisoryList> ListAsync(CancellationToken ct = default)
            => Req<SecurityAdvisoryList>(HttpVerb.Get, "/v1/security_advisories", cancellationToken: ct);

        /// <summary><c>POST /v1/security_advisories/{id}/acknowledge</c> — mark an advisory as handled.</summary>
        public Task<SecurityAdvisory> AcknowledgeAsync(string id, CancellationToken ct = default)
            => Req<SecurityAdvisory>(HttpVerb.Post, $"/v1/security_advisories/{Enc(id)}/acknowledge", cancellationToken: ct);

        /// <summary><c>GET /v1/public/security_advisories</c> — the public catalog (no instance data).</summary>
        public Task<ListPage<SecurityAdvisory>> CatalogAsync(CancellationToken ct = default)
            => Req<ListPage<SecurityAdvisory>>(HttpVerb.Get, "/v1/public/security_advisories", cancellationToken: ct);
    }

    // ---- sub-processors ------------------------------------------------------------

    /// <summary>A party that processes the instance's end-user data.</summary>
    public sealed class SubProcessor
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary><c>atlas_sub_processor</c> or <c>engaged_by_you</c>.</summary>
        public string Role { get; init; } = "";
        /// <summary><c>atlas</c>, <c>integration</c>, <c>referral_platform</c>, <c>consent_manager</c>, <c>email_provider</c>, <c>sms_provider</c>, <c>log_stream</c>, <c>audit_export</c> or <c>webhook_endpoint</c>.</summary>
        public string Source { get; init; } = "";
        public string? Purpose { get; init; }
        public List<string>? Data { get; init; }
        public string? Location { get; init; }
        public string? EnabledBy { get; init; }
        public string? Url { get; init; }
    }

    public sealed class SubProcessorProcessor
    {
        public string Name { get; init; } = "";
        public string Role { get; init; } = "";
        public string? Url { get; init; }
    }

    /// <summary>Who processes this instance's end-user data, and in which role.</summary>
    public sealed class SubProcessorList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string InstanceId { get; init; } = "";
        public Dictionary<string, string>? Roles { get; init; }
        public string? DataRegion { get; init; }
        public SubProcessorProcessor? Processor { get; init; }
        public List<SubProcessor> AtlasSubProcessors { get; init; } = new List<SubProcessor>();
        public List<SubProcessor> EngagedByYou { get; init; } = new List<SubProcessor>();
        public string? DpaUrl { get; init; }
    }

    /// <summary><c>/v1/instance/sub_processors</c>.</summary>
    public sealed class SubProcessorsResource : ResourceBase
    {
        public SubProcessorsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/instance/sub_processors</c></summary>
        public Task<SubProcessorList> GetAsync(CancellationToken ct = default)
            => Req<SubProcessorList>(HttpVerb.Get, "/v1/instance/sub_processors", cancellationToken: ct);
    }

    // ---- audit export --------------------------------------------------------------

    /// <summary>How far an export has read.</summary>
    public sealed class AuditExportCursor
    {
        public string? Id { get; init; }
        public long? CreatedAt { get; init; }
    }

    /// <summary>Forwards the audit log to a webhook or an S3-compatible bucket.</summary>
    public sealed class AuditExport
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary><c>webhook</c> or <c>s3</c>.</summary>
        public string Type { get; init; } = "";
        public bool Enabled { get; init; }
        /// <summary><c>active</c> or <c>failing</c>.</summary>
        public string Status { get; init; } = "";
        public List<string>? ActionFilter { get; init; }
        /// <summary>The non-secret fields plus <c>has_*</c> markers.</summary>
        public Dictionary<string, JsonElement>? Destination { get; init; }
        public AuditExportCursor? Cursor { get; init; }
        public int ConsecutiveFailures { get; init; }
        public string? LastError { get; init; }
        public long? LastDeliveredAt { get; init; }
        public long ExportedCount { get; init; }
        public List<string>? MandatoryActions { get; init; }
        /// <summary>A webhook export's signing secret, on create and rotate only.</summary>
        public string? Secret { get; init; }
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    /// <summary>The export list plus the always-exported actions.</summary>
    public sealed class AuditExportList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public List<AuditExport> Data { get; init; } = new List<AuditExport>();
        public bool HasMore { get; init; }
        public List<string>? MandatoryActions { get; init; }
    }

    /// <summary>The body of <c>POST /v1/audit_exports</c>. <c>Destination</c> is <c>{url}</c> for a webhook, or <c>{endpoint, bucket, region?, prefix?, access_key_id, secret_access_key}</c> for s3.</summary>
    public sealed class CreateAuditExportBody
    {
        public string Name { get; set; } = "";
        /// <summary><c>webhook</c> or <c>s3</c>.</summary>
        public string Type { get; set; } = "";
        public Dictionary<string, object?> Destination { get; set; } = new Dictionary<string, object?>();
        /// <summary>Action names or prefixes; key and data-rights events are always exported. Empty = everything.</summary>
        public List<string>? ActionFilter { get; set; }
        public bool? Enabled { get; set; }
        /// <summary>Also export the last N hours (0-720).</summary>
        public int? BackfillHours { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/audit_exports/{id}</c>. Destination is merged; a secret left out is kept. <see cref="ClearActionFilter"/> sends <c>action_filter</c> as null (everything).</summary>
    public sealed class UpdateAuditExportBody
    {
        public string? Name { get; set; }
        public Dictionary<string, object?>? Destination { get; set; }
        public List<string>? ActionFilter { get; set; }
        public bool? Enabled { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Send <c>action_filter: null</c> (export everything).</summary>
        public UpdateAuditExportBody ClearActionFilter()
        {
            ActionFilter = null;
            (AdditionalFields ??= new Dictionary<string, object?>())["action_filter"] = null;
            return this;
        }
    }

    public sealed class AuditCloudEventActor
    {
        public string Type { get; init; } = "";
        public string? Id { get; init; }
        public string? KeyName { get; init; }
        public string? KeyPrefix { get; init; }
    }

    public sealed class AuditCloudEventRef
    {
        public string? Type { get; init; }
        public string? Id { get; init; }
    }

    /// <summary>The data of an exported audit event.</summary>
    public sealed class AuditCloudEventData
    {
        public AuditCloudEventActor? Actor { get; init; }
        public string Action { get; init; } = "";
        public AuditCloudEventRef? Resource { get; init; }
        public AuditCloudEventRef? Org { get; init; }
        public string? Trace { get; init; }
        public string? Ip { get; init; }
        public Dictionary<string, JsonElement>? Metadata { get; init; }
    }

    /// <summary>An audit entry as an export sends it (CloudEvents 1.0, structured JSON; <c>Type</c> is <c>net.atlasauth.audit.&lt;action&gt;</c>).</summary>
    public sealed class AuditCloudEvent
    {
        [JsonPropertyName("specversion")] public string SpecVersion { get; init; } = "";
        [JsonPropertyName("id")] public string Id { get; init; } = "";
        [JsonPropertyName("source")] public string Source { get; init; } = "";
        [JsonPropertyName("type")] public string Type { get; init; } = "";
        [JsonPropertyName("subject")] public string? Subject { get; init; }
        [JsonPropertyName("time")] public string Time { get; init; } = "";
        [JsonPropertyName("datacontenttype")] public string? DataContentType { get; init; }
        [JsonPropertyName("atlasinstance")] public string? AtlasInstance { get; init; }
        [JsonPropertyName("data")] public AuditCloudEventData? Data { get; init; }
    }

    /// <summary>The answer to a test send.</summary>
    public sealed class AuditExportTestResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public bool Ok { get; init; }
        public int? Status { get; init; }
        public string? Error { get; init; }
        public string? Location { get; init; }
        public AuditCloudEvent? Event { get; init; }
    }

    /// <summary><c>/v1/audit_exports</c>.</summary>
    public sealed class AuditExportsResource : ResourceBase
    {
        public AuditExportsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/audit_exports</c></summary>
        public Task<AuditExportList> ListAsync(CancellationToken ct = default)
            => Req<AuditExportList>(HttpVerb.Get, "/v1/audit_exports", cancellationToken: ct);

        /// <summary><c>GET /v1/audit_exports/{id}</c></summary>
        public Task<AuditExport> GetAsync(string id, CancellationToken ct = default)
            => Req<AuditExport>(HttpVerb.Get, $"/v1/audit_exports/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/audit_exports</c> — add an export; a webhook export's signing secret is in this response only.</summary>
        public Task<AuditExport> CreateAsync(CreateAuditExportBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<AuditExport>(HttpVerb.Post, "/v1/audit_exports", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PATCH /v1/audit_exports/{id}</c></summary>
        public Task<AuditExport> UpdateAsync(string id, UpdateAuditExportBody body, CancellationToken ct = default)
            => Req<AuditExport>(HttpVerb.Patch, $"/v1/audit_exports/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/audit_exports/{id}</c></summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/audit_exports/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/audit_exports/{id}/test</c> — send one synthetic <c>audit_export.test</c> event now.</summary>
        public Task<AuditExportTestResult> TestAsync(string id, CancellationToken ct = default)
            => Req<AuditExportTestResult>(HttpVerb.Post, $"/v1/audit_exports/{Enc(id)}/test", cancellationToken: ct);

        /// <summary><c>POST /v1/audit_exports/{id}/rotate_secret</c> — rotate a webhook export's signing secret.</summary>
        public Task<AuditExport> RotateSecretAsync(string id, CancellationToken ct = default)
            => Req<AuditExport>(HttpVerb.Post, $"/v1/audit_exports/{Enc(id)}/rotate_secret", cancellationToken: ct);
    }

    // ---- entitlements --------------------------------------------------------------

    /// <summary>One entitlement of a subject.</summary>
    public sealed class EntitlementItem
    {
        public string Key { get; init; } = "";
        public bool Enabled { get; init; }
        public long? Limit { get; init; }
        public long? Used { get; init; }
        /// <summary><c>plan</c>, <c>override</c> or <c>internal</c>.</summary>
        public string? Source { get; init; }
    }

    public sealed class EntitlementSubject
    {
        public string Type { get; init; } = "";
        public string Id { get; init; } = "";
    }

    /// <summary>The standard entitlements document (JSON Schema <c>https://atlasauth.net/schemas/entitlements-v1.json</c>): one shape any product can serve.</summary>
    public sealed class Entitlements
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        [JsonPropertyName("$schema")] public string? Schema { get; init; }
        [JsonPropertyName("subject")] public EntitlementSubject Subject { get; init; } = new EntitlementSubject();
        [JsonPropertyName("plan")] public string? Plan { get; init; }
        /// <summary><c>plan</c> (a subscription), <c>override</c> (a sales-led grant), <c>internal</c> (an owner account, never revenue) or <c>none</c>.</summary>
        [JsonPropertyName("planSource")] public string? PlanSource { get; init; }
        [JsonPropertyName("entitlements")] public List<EntitlementItem> Items { get; init; } = new List<EntitlementItem>();
    }

    /// <summary><c>/v1/entitlements</c>.</summary>
    public sealed class EntitlementsResource : ResourceBase
    {
        public EntitlementsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/entitlements</c> — the entitlements of a user or organization. <paramref name="subjectType"/> is <c>user</c> or <c>org</c>.</summary>
        public Task<Entitlements> GetAsync(string subjectType, string subjectId, CancellationToken ct = default)
            => Req<Entitlements>(HttpVerb.Get, "/v1/entitlements", query: Q(("subject_type", subjectType), ("subject_id", subjectId)), cancellationToken: ct);
    }
}
