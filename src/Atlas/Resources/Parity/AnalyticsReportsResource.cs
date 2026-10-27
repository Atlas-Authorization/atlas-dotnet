using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Operations of <c>analytics.reports</c>.</summary>
    public sealed partial class AnalyticsReportsResource : ResourceBase
    {
        public AnalyticsReportsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/analytics/reports/inactive_users</c> - Compliance report: users who have not signed in for `inactive_days` (default 90), oldest id first, cursor-paginated (an export returns up to 50,000 rows).</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> InactiveUsersAsync(long? inactiveDays = null, long? limit = null, string? startingAfter = null, object? from = null, object? to = null, long? days = null, string? granularity = null, string? organizationId = null, string? format = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/reports/inactive_users", query: Q(("inactive_days", inactiveDays), ("limit", limit), ("starting_after", startingAfter), ("from", from), ("to", to), ("days", days), ("granularity", granularity), ("organization_id", organizationId), ("format", format)), cancellationToken: ct);

        /// <summary><c>GET /v1/analytics/reports/mfa_coverage</c> - Compliance report: how many users are protected by a second factor, a passkey, either, or neither.</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> MfaCoverageAsync(object? from = null, object? to = null, long? days = null, string? granularity = null, string? organizationId = null, string? format = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/reports/mfa_coverage", query: Q(("from", from), ("to", to), ("days", days), ("granularity", granularity), ("organization_id", organizationId), ("format", format), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/analytics/reports/access_by_app</c> - Compliance report: for each application (OAuth client), how many users have access, how many grants are active and how many users signed in during the window.</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> AccessByAppAsync(object? from = null, object? to = null, long? days = null, string? granularity = null, string? organizationId = null, string? format = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/reports/access_by_app", query: Q(("from", from), ("to", to), ("days", days), ("granularity", granularity), ("organization_id", organizationId), ("format", format), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
