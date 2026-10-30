using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Verification;

namespace Atlas.Resources
{
    // The data-rights executor protocol (v2): the routes a DSAR orchestrator calls to find a person and carry out a request
    // against their identity data (needs a key with the privacy:execute scope; privacy:grants:revoke lets the erasure also
    // revoke the Sign in with Atlas grant), and the instance's own orchestrator connection, where Atlas opens a case when an
    // account deletion is scheduled.

    /// <summary>Canonical privacy request types.</summary>
    public static class PrivacyTypes
    {
        public const string Access = "access";
        public const string Deletion = "deletion";
        public const string Closure = "closure";
        public const string Restriction = "restriction";
        public const string Rectification = "rectification";
    }

    /// <summary>Privacy request statuses.</summary>
    public static class PrivacyStatuses
    {
        public const string Pending = "pending";
        public const string Scheduled = "scheduled";
        public const string Processing = "processing";
        public const string Completed = "completed";
        public const string CompletedWithRetention = "completed_with_retention";
        public const string Rejected = "rejected";
        public const string NoMatch = "no_match";
        public const string Cancelled = "cancelled";
    }

    /// <summary>Webhook event names and scopes of the data-rights executor.</summary>
    public static class PrivacyEvents
    {
        /// <summary>A privacy request moved forward; <c>data</c> is the same <see cref="PrivacyRequest"/> the GET returns.</summary>
        public const string RequestUpdated = "privacy_request.updated";
        /// <summary>A privacy request finished.</summary>
        public const string RequestCompleted = "privacy_request.completed";
        /// <summary>An account deletion was scheduled (also sent to Sign in with Atlas app webhooks).</summary>
        public const string UserDeletionScheduled = "user.deletion_scheduled";
        /// <summary>A scheduled account deletion was cancelled inside its grace window.</summary>
        public const string UserDeletionCanceled = "user.deletion_canceled";
        /// <summary>Lets a key call <c>/v1/privacy/*</c>.</summary>
        public const string ScopeExecute = "privacy:execute";
        /// <summary>Lets an erasure revoke the Sign in with Atlas grant for the app this instance manages.</summary>
        public const string ScopeGrantsRevoke = "privacy:grants:revoke";
    }

    /// <summary>Identifies a person for a lookup. <c>atlas.sub</c> alone wins, then <c>atlas.ext</c>, then the emails.</summary>
    public sealed class PrivacySubject
    {
        [JsonPropertyName("iss")] public string? Iss { get; set; }
        [JsonPropertyName("sub")] public string? Sub { get; set; }
        [JsonPropertyName("ext")] public string? Ext { get; set; }
        [JsonPropertyName("principal")] public string? Principal { get; set; }
    }

    /// <summary>The body of <c>POST /v1/privacy/lookup</c>.</summary>
    public sealed class PrivacyLookupBody
    {
        [JsonPropertyName("emails")] public List<string>? Emails { get; set; }
        [JsonPropertyName("atlas")] public PrivacySubject? Atlas { get; set; }
    }

    /// <summary>The answer to a lookup.</summary>
    public sealed class PrivacyLookupResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary><c>found</c> or <c>no_match</c>.</summary>
        [JsonPropertyName("status")] public string Status { get; init; } = "";
        [JsonPropertyName("subjectId")] public string? SubjectId { get; init; }
        /// <summary><c>atlas.sub</c>, <c>atlas.ext</c>, <c>email</c> or <c>email_unverified</c>.</summary>
        [JsonPropertyName("matchedBy")] public string? MatchedBy { get; init; }
        [JsonPropertyName("detail")] public string? Detail { get; init; }
        [JsonPropertyName("principal")] public string? Principal { get; init; }
        /// <summary>Whether the <c>principal</c> the caller sent matched the person found.</summary>
        [JsonPropertyName("principalMatched")] public bool? PrincipalMatched { get; init; }
    }

    /// <summary>A category of data kept after an erasure, with its basis.</summary>
    public sealed class PrivacyRetention
    {
        [JsonPropertyName("category")] public string Category { get; init; } = "";
        [JsonPropertyName("basis")] public string Basis { get; init; } = "";
        [JsonPropertyName("until")] public string? Until { get; init; }
    }

    /// <summary>One step of carrying out a request.</summary>
    public sealed class PrivacyStep
    {
        [JsonPropertyName("name")] public string Name { get; init; } = "";
        [JsonPropertyName("status")] public string Status { get; init; } = "";
        [JsonPropertyName("reason")] public string? Reason { get; init; }
        [JsonPropertyName("at")] public long? At { get; init; }
    }

    /// <summary>A data-rights request.</summary>
    public sealed class PrivacyRequest
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        [JsonPropertyName("id")] public string Id { get; init; } = "";
        [JsonPropertyName("type")] public string Type { get; init; } = "";
        [JsonPropertyName("canonicalType")] public string? CanonicalType { get; init; }
        [JsonPropertyName("requestedType")] public string? RequestedType { get; init; }
        [JsonPropertyName("status")] public string Status { get; init; } = "";
        [JsonPropertyName("subjectId")] public string? SubjectId { get; init; }
        [JsonPropertyName("caseId")] public string? CaseId { get; init; }
        [JsonPropertyName("executeAfter")] public string? ExecuteAfter { get; init; }
        [JsonPropertyName("eta")] public string? Eta { get; init; }
        [JsonPropertyName("detail")] public string? Detail { get; init; }
        [JsonPropertyName("retention")] public List<PrivacyRetention>? Retention { get; init; }
        [JsonPropertyName("steps")] public List<PrivacyStep>? Steps { get; init; }
        [JsonPropertyName("trace")] public string? Trace { get; init; }
        [JsonPropertyName("createdAt")] public string? CreatedAt { get; init; }
        [JsonPropertyName("updatedAt")] public string? UpdatedAt { get; init; }
        [JsonPropertyName("completedAt")] public string? CompletedAt { get; init; }
        /// <summary>Set on a finished request (GET).</summary>
        [JsonPropertyName("result")] public JsonElement? Result { get; init; }
    }

    /// <summary>
    /// The body of <c>POST /v1/privacy/requests</c>. <see cref="Type"/> is access, deletion, closure, restriction or
    /// rectification (<c>export</c>, <c>erasure</c> and <c>erase</c> are accepted aliases).
    /// </summary>
    public sealed class CreatePrivacyRequestBody
    {
        [JsonPropertyName("subjectId")] public string SubjectId { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("caseId")] public string CaseId { get; set; } = "";
        /// <summary>ISO 8601 string, or epoch milliseconds as a <c>long</c>: nothing is erased before it.</summary>
        [JsonPropertyName("executeAfter")] public object? ExecuteAfter { get; set; }
        [JsonPropertyName("reason")] public string? Reason { get; set; }
    }

    /// <summary>The instance's data-rights orchestrator connection. Credentials are never returned; a signing secret appears once, on the write that mints it.</summary>
    public sealed class PrivacyOrchestrator
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public bool Configured { get; init; }
        public bool Enabled { get; init; }
        public string? Provider { get; init; }
        public string? Name { get; init; }
        public string? CaseUrl { get; init; }
        public string? CancelUrl { get; init; }
        public string? AuthHeader { get; init; }
        public string? AuthScheme { get; init; }
        public bool ApiKeySet { get; init; }
        public bool SigningSecretSet { get; init; }
        public bool UsePartnerLink { get; init; }
        public bool OpenClosureCases { get; init; }
        public JsonElement? CompletionWebhook { get; init; }
        public long? LastCaseAt { get; init; }
        public string? LastError { get; init; }
        public long? LastErrorAt { get; init; }
        public List<JsonElement>? Presets { get; init; }
        /// <summary>A <c>whsec_…</c> secret returned once.</summary>
        public string? SigningSecret { get; init; }
        public string? CompletionWebhookSecret { get; init; }
        public bool? Deleted { get; init; }
    }

    /// <summary>The body of <c>PUT /v1/instance/privacy_orchestrator</c>. Use <see cref="Set"/> to send an explicit <c>null</c> (case_url, cancel_url, api_key, completion_webhook_url).</summary>
    public sealed class SetPrivacyOrchestratorBody
    {
        /// <summary>A preset id (<c>cookiemunch</c>, <c>custom</c>) or your own lower-case id.</summary>
        public string? Provider { get; set; }
        public string? Name { get; set; }
        public bool? Enabled { get; set; }
        public string? CaseUrl { get; set; }
        /// <summary>Must contain <c>{id}</c>.</summary>
        public string? CancelUrl { get; set; }
        public string? AuthHeader { get; set; }
        /// <summary><c>bearer</c> or <c>raw</c>.</summary>
        public string? AuthScheme { get; set; }
        /// <summary>Write-only.</summary>
        public string? ApiKey { get; set; }
        /// <summary>Call through the partner link the orchestrator granted Atlas (partner id = provider).</summary>
        public bool? UsePartnerLink { get; set; }
        /// <summary>Call through this partner link (implies <c>UsePartnerLink</c>); <c>Set("partner_link_id", null)</c> clears it.</summary>
        public string? PartnerLinkId { get; set; }
        /// <summary>The orchestrator-side site a case is for; <c>Set("site_id", null)</c> clears it.</summary>
        public string? SiteId { get; set; }
        /// <summary>Cancel a self-scheduled deletion when the person signs back in during the grace period.</summary>
        public bool? CancelDeletionOnSignIn { get; set; }
        public bool? OpenClosureCases { get; set; }
        /// <summary>Mints (true) a secret and signs every case call; true again rotates it; false stops signing.</summary>
        public bool? SignRequests { get; set; }
        public string? CompletionWebhookUrl { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>.</summary>
        public SetPrivacyOrchestratorBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>
    /// The body of <c>POST /v1/instance/privacy_integrations/connect</c>: one call that wires a consent manager / orchestrator
    /// both ways (banner, orchestrator connection, completion webhook and an executor key).
    /// </summary>
    public sealed class PrivacyIntegrationConnectBody
    {
        /// <summary>An orchestrator preset (<c>cookiemunch</c>) or <c>custom</c>.</summary>
        public string Preset { get; set; } = "";
        /// <summary>The orchestrator-side site id. Required by the cookiemunch preset.</summary>
        public string? SiteId { get; set; }
        /// <summary>The key the orchestrator issued to Atlas for opening cases (optional on a re-run). Write-only.</summary>
        public string? ApiKey { get; set; }
        public string? PartnerLinkId { get; set; }
        /// <summary>The webhook URL the orchestrator shows for this instance's executor connection (optional on a re-run).</summary>
        public string? CompletionWebhookUrl { get; set; }
        /// <summary>Overrides for <c>PUT /v1/instance/cookie_consent</c> (merged over the preset), or <c>false</c> to leave it alone.</summary>
        public object? CookieConsent { get; set; }
        /// <summary>Further orchestrator fields (name, case_url, cancel_url, sign_requests, ...), by wire name.</summary>
        public Dictionary<string, object?>? Orchestrator { get; set; }
        /// <summary>The executor key: privacy:* scopes only (default privacy:execute + privacy:grants:revoke).</summary>
        public PrivacyIntegrationKeyInput? Key { get; set; }

        public override string ToString() => $"PrivacyIntegrationConnectBody {{ Preset = {Preset}, SiteId = {SiteId} }}";
    }

    /// <summary>The executor key options of a privacy integration connect.</summary>
    public sealed class PrivacyIntegrationKeyInput
    {
        public string? Name { get; set; }
        public List<string>? Scopes { get; set; }
        /// <summary>Epoch ms or an ISO 8601 string.</summary>
        public object? ExpiresAt { get; set; }
        public List<string>? AllowedIps { get; set; }
        public List<string>? AllowedOrigins { get; set; }
    }

    /// <summary>The consent manager, orchestrator, completion webhook and executor key, in one view.</summary>
    public sealed class PrivacyIntegration
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "privacy_integration";
        public string? Preset { get; init; }
        /// <summary>Banner on, orchestrator and its completion webhook set, executor key live.</summary>
        public bool Connected { get; init; }
        public JsonElement? CookieConsent { get; init; }
        public PrivacyOrchestrator? PrivacyOrchestrator { get; init; }
        public JsonElement? DsrWebhook { get; init; }
        /// <summary>The executor key: <c>secret</c> only on the connect that minted it; <c>idempotent_replay</c> on a re-run.</summary>
        public PrivacyIntegrationExecutorKey? ExecutorKey { get; init; }
        /// <summary>Connect only: the consent-manager site check.</summary>
        public List<PrivacyIntegrationWarning>? Warnings { get; init; }
    }

    public sealed class PrivacyIntegrationExecutorKey
    {
        public string Id { get; init; } = "";
        public List<string> Scopes { get; init; } = new List<string>();
        /// <summary>Shown once, on the connect that minted the key.</summary>
        public string? Secret { get; init; }
        public bool? IdempotentReplay { get; init; }
        /// <summary>The rest of the key's fields.</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }

        public override string ToString() => $"PrivacyIntegrationExecutorKey {{ Id = {Id} }}";
    }

    public sealed class PrivacyIntegrationWarning
    {
        public string Code { get; init; } = "";
        public string Message { get; init; } = "";
    }

    /// <summary><c>/v1/instance/privacy_integrations</c>.</summary>
    public sealed class PrivacyIntegrationResource : ResourceBase
    {
        public PrivacyIntegrationResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/instance/privacy_integrations</c></summary>
        public Task<PrivacyIntegration> GetAsync(CancellationToken ct = default)
            => Req<PrivacyIntegration>(HttpVerb.Get, "/v1/instance/privacy_integrations", cancellationToken: ct);

        /// <summary><c>POST /v1/instance/privacy_integrations/connect</c> — wire all of it in one idempotent call; the executor key's secret comes back once.</summary>
        public Task<PrivacyIntegration> ConnectAsync(PrivacyIntegrationConnectBody body, CancellationToken ct = default)
            => Req<PrivacyIntegration>(HttpVerb.Post, "/v1/instance/privacy_integrations/connect", body, cancellationToken: ct);
    }

    /// <summary>The data-rights executor protocol (<c>/v1/privacy</c>). <see cref="Orchestrator"/> hangs off it.</summary>
    public sealed class PrivacyResource : ResourceBase
    {
        public PrivacyResource(AtlasTransport t) : base(t)
        {
            Orchestrator = new PrivacyOrchestratorResource(t);
            Integration = new PrivacyIntegrationResource(t);
        }

        /// <summary>The instance's orchestrator connection (<c>/v1/instance/privacy_orchestrator</c>).</summary>
        public PrivacyOrchestratorResource Orchestrator { get; }

        /// <summary>The consent manager + orchestrator + executor key in one view (<c>/v1/instance/privacy_integrations</c>).</summary>
        public PrivacyIntegrationResource Integration { get; }

        /// <summary><c>GET /v1/privacy</c> — what this executor speaks (public).</summary>
        public Task<JsonElement> DiscoverAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/privacy", cancellationToken: ct);

        /// <summary><c>GET /v1/privacy/presets/{id}</c> — a ready-made orchestrator connection profile such as <c>cookiemunch</c> (public).</summary>
        public Task<JsonElement> PresetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/privacy/presets/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/privacy/lookup</c> — find a person.</summary>
        public Task<PrivacyLookupResult> LookupAsync(PrivacyLookupBody body, CancellationToken ct = default)
            => Req<PrivacyLookupResult>(HttpVerb.Post, "/v1/privacy/lookup", body, cancellationToken: ct);

        /// <summary><c>POST /v1/privacy/requests</c> — open a request, or return the existing one for the same case. Pass an idempotency key.</summary>
        public Task<PrivacyRequest> CreateRequestAsync(CreatePrivacyRequestBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<PrivacyRequest>(HttpVerb.Post, "/v1/privacy/requests", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/privacy/requests/{id}</c></summary>
        public Task<PrivacyRequest> GetRequestAsync(string id, CancellationToken ct = default)
            => Req<PrivacyRequest>(HttpVerb.Get, $"/v1/privacy/requests/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/privacy/requests/{id}/cancel</c> — cancel before it runs: nothing is erased, the account stays.</summary>
        public Task<PrivacyRequest> CancelRequestAsync(string id, CancellationToken ct = default)
            => Req<PrivacyRequest>(HttpVerb.Post, $"/v1/privacy/requests/{Enc(id)}/cancel", cancellationToken: ct);
    }

    /// <summary><c>/v1/instance/privacy_orchestrator</c>.</summary>
    public sealed class PrivacyOrchestratorResource : ResourceBase
    {
        public PrivacyOrchestratorResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/instance/privacy_orchestrator</c></summary>
        public Task<PrivacyOrchestrator> GetAsync(CancellationToken ct = default)
            => Req<PrivacyOrchestrator>(HttpVerb.Get, "/v1/instance/privacy_orchestrator", cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/privacy_orchestrator</c> — connect or update the orchestrator; new secrets come back once.</summary>
        public Task<PrivacyOrchestrator> SetAsync(SetPrivacyOrchestratorBody body, CancellationToken ct = default)
            => Req<PrivacyOrchestrator>(HttpVerb.Put, "/v1/instance/privacy_orchestrator", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/privacy_orchestrator</c> — remove the connection.</summary>
        public Task<PrivacyOrchestrator> DeleteAsync(CancellationToken ct = default)
            => Req<PrivacyOrchestrator>(HttpVerb.Delete, "/v1/instance/privacy_orchestrator", cancellationToken: ct);
    }

    /// <summary>
    /// A verified call from Atlas to your orchestrator or executor receiver: the signed case request (when Atlas opens a case for
    /// a scheduled deletion), its cancellation, or a <c>privacy_request.*</c> event.
    /// </summary>
    public sealed class PrivacyCall
    {
        /// <summary>The signed <c>Atlas-Id</c> header (also the Idempotency-Key Atlas sends); deduplicate retries on it.</summary>
        public string DeliveryId { get; init; } = "";
        /// <summary>The signed <c>Atlas-Timestamp</c> (epoch ms).</summary>
        public long Timestamp { get; init; }
        /// <summary>The W3C <c>traceparent</c> Atlas sent, or null.</summary>
        public string? Traceparent { get; init; }
        /// <summary>The raw JSON body.</summary>
        public byte[] Body { get; init; } = Array.Empty<byte>();
        /// <summary>Set when the body is a webhook envelope (<c>privacy_request.*</c>).</summary>
        public JsonElement? Event { get; init; }

        /// <summary>
        /// Verify a call Atlas makes to a data-rights receiver. With <c>sign_requests</c> on, case calls carry <c>Atlas-Id</c>,
        /// <c>Atlas-Timestamp</c> and <c>Atlas-Signature</c> exactly like a webhook (HMAC-SHA256 over <c>id.timestamp.body</c> with the
        /// signing secret); the completion endpoint's <c>privacy_request.*</c> events are ordinary signed webhooks. Verify the RAW body.
        /// Returns null when the signature does not verify; <paramref name="reason"/> says why.
        /// </summary>
        public static PrivacyCall? Verify(byte[] rawBody, IEnumerable<KeyValuePair<string, string>> headers, string secret, out string? reason, long? nowMs = null)
        {
            var v = new WebhookVerifier(secret).Verify(rawBody, headers, nowMs);
            reason = v.Reason;
            if (!v.Verified) return null;
            JsonElement? ev = null;
            if (v.Event is JsonElement el && el.ValueKind == JsonValueKind.Object
                && el.TryGetProperty("type", out var ty) && ty.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(ty.GetString()))
                ev = el;
            return new PrivacyCall
            {
                DeliveryId = v.DeliveryId ?? "",
                Timestamp = v.Timestamp ?? 0,
                Traceparent = AtlasTrace.TraceparentFromHeaders(headers),
                Body = rawBody,
                Event = ev,
            };
        }

        /// <summary>Decode the <c>data</c> of a <c>privacy_request.updated</c> / <c>privacy_request.completed</c> event.</summary>
        public PrivacyRequest? RequestFromEvent()
        {
            if (Event is JsonElement el && el.TryGetProperty("data", out var data))
                return AtlasJson.Deserialize<PrivacyRequest>(data.GetRawText());
            return null;
        }
    }
}
