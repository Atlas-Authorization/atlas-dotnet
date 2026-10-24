using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atlas.Resources
{
    /// <summary>A finding that does not apply to how the instance integrates, and why.</summary>
    public sealed class PreflightSuppressedFinding
    {
        public string Code { get; init; } = "";
        public string Severity { get; init; } = "";
        public Dictionary<string, string>? Subject { get; init; }
        public string Reason { get; init; } = "";
    }

    /// <summary>How one allowed https origin signs in, as preflight judged it.</summary>
    public sealed class PreflightOriginIntegration
    {
        public string Origin { get; init; } = "";
        /// <summary><c>redirect</c> or <c>embedded</c>.</summary>
        public string Integration { get; init; } = "";
        /// <summary><c>declared</c>, <c>embedded_traffic</c>, <c>redirect_clients</c> or <c>default</c>.</summary>
        public string Source { get; init; } = "";
        /// <summary>False only for the default (nothing says how the origin signs in).</summary>
        public bool Confident { get; init; }
        public string Reason { get; init; } = "";
    }

    /// <summary>Preflight codes added with the security-posture and integration checks.</summary>
    public static class PreflightCodes
    {
        public const string OAuthClientNoBackchannelLogoutUri = "oauth_client_no_backchannel_logout_uri";
        public const string PkceNotRequired = "pkce_not_required";
        public const string PasskeyWithoutUv = "passkey_without_uv";
        public const string AutoLinkUnverified = "auto_link_unverified";
        public const string IntegrationEnabledNeverMinted = "integration_enabled_never_minted";
        public const string IntegrationVerifyErrors = "integration_verify_errors";
        public const string IntegrationReceiverFailing = "integration_receiver_failing";
        public const string CookieConsentNotEnabled = "cookie_consent_not_enabled";
        public const string StatusDependencyUnreachable = "status_dependency_unreachable";
        public const string StatusDependencyOutage = "status_dependency_outage";
    }

    /// <summary>One unknown body field your writes sent, aggregated over the report window.</summary>
    public sealed class UnknownFieldReportRow
    {
        public string Method { get; init; } = "";
        /// <summary>The route template, e.g. <c>/v1/users/{id}</c>.</summary>
        public string Path { get; init; } = "";
        /// <summary>The field path, nested as <c>parent.child</c>.</summary>
        public string Field { get; init; } = "";
        public int Count { get; init; }
        /// <summary>Lenient requests: the field was ignored and named in <c>Atlas-Unknown-Fields</c>.</summary>
        public int IgnoredCount { get; init; }
        /// <summary>Strict requests: refused with 422 <c>UNKNOWN_FIELD</c>.</summary>
        public int RejectedCount { get; init; }
        public long LastSeenAt { get; init; }
        public string? ApiKeyId { get; init; }
    }

    /// <summary><c>GET /v1/instance/unknown_fields_report</c>.</summary>
    public sealed class UnknownFieldsReport
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public int Days { get; init; }
        public int RetentionDays { get; init; }
        public bool StrictRequestBodies { get; init; }
        /// <summary>True when nothing in the window would be refused by strict request bodies.</summary>
        public bool SafeToEnableStrict { get; init; }
        public List<UnknownFieldReportRow> Data { get; init; } = new List<UnknownFieldReportRow>();
    }

    /// <summary>One operation in an OpenAPI diff.</summary>
    public sealed class OpenApiDiffItem
    {
        public string Method { get; init; } = "";
        public string Path { get; init; } = "";
        public string? OperationId { get; init; }
        public string? Summary { get; init; }
        /// <summary>The YYYY-MM-DD of the change.</summary>
        public string? At { get; init; }
    }

    /// <summary>What changed in the API since a date.</summary>
    public sealed class OpenApiDiff
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Since { get; init; } = "";
        /// <summary>The API build the diff was computed on (<c>&lt;date&gt;+&lt;sha&gt;</c>).</summary>
        public string? Version { get; init; }
        public List<OpenApiDiffItem> Added { get; init; } = new List<OpenApiDiffItem>();
        public List<OpenApiDiffItem> Deprecated { get; init; } = new List<OpenApiDiffItem>();
        public List<OpenApiDiffItem> Removed { get; init; } = new List<OpenApiDiffItem>();
    }

    /// <summary>One row of the integration-health report.</summary>
    public sealed class IntegrationHealthCheck
    {
        /// <summary>A preflight code verbatim, or <c>&lt;source&gt;.&lt;check&gt;</c>.</summary>
        public string Id { get; init; } = "";
        /// <summary>False when failing, null when not known or not configured.</summary>
        public bool? Ok { get; init; }
        public string? Detail { get; init; }
        public string? Fix { get; init; }
        public string? Docs { get; init; }
        /// <summary>For a check that calls your server, the hop that failed: <c>dns</c>, <c>tls</c>, <c>http</c>, <c>challenge</c> or <c>auth</c>.</summary>
        public string? Hop { get; init; }
        public int? Status { get; init; }
        [JsonPropertyName("elapsedMs")] public long? ElapsedMs { get; init; }
        public string Source { get; init; } = "";
        public string? Severity { get; init; }
        public Dictionary<string, string>? Subject { get; init; }
        [JsonPropertyName("observedAt")] public long? ObservedAt { get; init; }
    }

    public sealed class IntegrationHealthSummary
    {
        public int Failing { get; init; }
        public int Passing { get; init; }
        public int Unknown { get; init; }
    }

    /// <summary><c>GET /v1/integration-health</c>.</summary>
    public sealed class IntegrationHealthReport
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public bool Ok { get; init; }
        public IntegrationHealthSummary Summary { get; init; } = new IntegrationHealthSummary();
        public List<IntegrationHealthCheck> Checks { get; init; } = new List<IntegrationHealthCheck>();
        /// <summary>ISO 8601.</summary>
        [JsonPropertyName("generatedAt")] public string? GeneratedAtIso { get; init; }
        /// <summary>The same instant, epoch ms.</summary>
        public long GeneratedAt { get; init; }
    }

    /// <summary>A check source this deployment reports.</summary>
    public sealed class IntegrationHealthProvider
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Source { get; init; } = "";
        public string Description { get; init; } = "";
    }

    /// <summary>One surface of the embed manifest.</summary>
    public sealed class EmbedSurface
    {
        public bool? Tenant { get; init; }
        [JsonPropertyName("servedBy")] public string? ServedBy { get; init; }
        [JsonPropertyName("csp")] public Dictionary<string, List<string>>? Csp { get; init; }
        public Dictionary<string, List<string>>? Requires { get; init; }
        [JsonPropertyName("trustedTypes")] public List<string>? TrustedTypes { get; init; }
        [JsonPropertyName("consentCategory")] public string? ConsentCategory { get; init; }
        public List<JsonElement>? Storage { get; init; }
        [JsonPropertyName("sri")] public Dictionary<string, string>? Sri { get; init; }
        public List<string>? Notes { get; init; }
    }

    /// <summary>The per-instance part of the embed manifest.</summary>
    public sealed class EmbedManifestTenant
    {
        public string Instance { get; init; } = "";
        public string? Environment { get; init; }
        [JsonPropertyName("frontendApiHosts")] public List<string>? FrontendApiHosts { get; init; }
        /// <summary><c>redirect</c>, <c>panel</c>, <c>button</c> or null.</summary>
        public string? Shape { get; init; }
        public string? Origin { get; init; }
        [JsonPropertyName("originAllowed")] public bool? OriginAllowed { get; init; }
        /// <summary>The merged sources for your pages, as one <c>Content-Security-Policy</c> value.</summary>
        public string Policy { get; init; } = "";
        public Dictionary<string, List<string>>? Sources { get; init; }
    }

    /// <summary>A dated change to a surface's CSP sources.</summary>
    public sealed class EmbedChange
    {
        public string Effective { get; init; } = "";
        public string Surface { get; init; } = "";
        public Dictionary<string, List<string>>? Add { get; init; }
        public Dictionary<string, List<string>>? Remove { get; init; }
        public string? Note { get; init; }
    }

    /// <summary>The callers that probe your pages.</summary>
    public sealed class EmbedManifestCheckers
    {
        [JsonPropertyName("userAgent")] public string UserAgent { get; init; } = "";
        [JsonPropertyName("userAgents")] public List<string>? UserAgents { get; init; }
        public List<string>? Egress { get; init; }
    }

    /// <summary>The embed manifest (embed-v1) for one instance: CSP sources per surface, storage, SRI and the merged policy.</summary>
    public sealed class EmbedManifest
    {
        public string Schema { get; init; } = "";
        public string Product { get; init; } = "";
        public string Updated { get; init; } = "";
        [JsonPropertyName("openapi")] public string? OpenApi { get; init; }
        [JsonPropertyName("llms")] public string? Llms { get; init; }
        public string? Status { get; init; }
        public string? Docs { get; init; }
        public EmbedManifestCheckers? Checkers { get; init; }
        public Dictionary<string, EmbedSurface> Surfaces { get; init; } = new Dictionary<string, EmbedSurface>();
        public List<EmbedChange>? Changes { get; init; }
        public EmbedManifestTenant? Tenant { get; init; }
    }
}
