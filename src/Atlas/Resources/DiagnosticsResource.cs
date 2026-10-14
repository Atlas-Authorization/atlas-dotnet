using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class LoginReadiness
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Origin { get; init; } = "";
        public bool Live { get; init; }
        public string? DataRegion { get; init; }
        public bool Ready { get; init; }
        public List<LoginReadinessChecks> Checks { get; init; } = new List<LoginReadinessChecks>();
    }

    public sealed class LoginReadinessChecks
    {
        public string? Name { get; init; }
        public bool? Pass { get; init; }
        public JsonElement? Observed { get; init; }
        public string? Detail { get; init; }
    }

    public sealed class RequestInspection
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Origin { get; init; } = "";
        public string Method { get; init; } = "";
        public string Path { get; init; } = "";
        public bool WouldBeRejected { get; init; }
        public List<RequestInspectionFindings> Findings { get; init; } = new List<RequestInspectionFindings>();
    }

    public sealed class RequestInspectionFindings
    {
        public string? Check { get; init; }
        public bool? Blocks { get; init; }
        public JsonElement? Observed { get; init; }
        public string? Detail { get; init; }
    }

    /// <summary>Setup diagnostics (<c>/v1/diagnostics</c>).</summary>
    public sealed class DiagnosticsResource : ResourceBase
    {
        public DiagnosticsResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/diagnostics/login_readiness</c> — End-to-end login-readiness probe for a given `origin` (CORS, cookie domain, email claim, JWKS), reporting the observed effective runtime values with a per-hop pass/fail and a specific fix.</summary>
        public Task<LoginReadiness> LoginReadinessAsync(string? origin = null, bool? live = null, CancellationToken ct = default)
            => Req<LoginReadiness>(HttpVerb.Get, "/v1/diagnostics/login_readiness", query: Q(("origin", origin), ("live", live)), cancellationToken: ct);

        /// <summary><c>GET /v1/diagnostics/request_inspector</c> — Explain why a specific browser request would be rejected: CORS origin, CORS method (the DELETE/PATCH class), session-cookie delivery, rate-limit bucket, captcha requirement, and — with an optional `token` — JWT verification against this instance.</summary>
        public Task<RequestInspection> InspectRequestAsync(InspectRequestParams? @params = null, CancellationToken ct = default)
            => Req<RequestInspection>(HttpVerb.Get, "/v1/diagnostics/request_inspector", query: (@params ?? new InspectRequestParams()).ToQuery(), cancellationToken: ct);
    }
}
