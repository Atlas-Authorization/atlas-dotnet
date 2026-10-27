using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>More operations of <c>analytics</c>.</summary>
    public sealed partial class AnalyticsResource
    {
        private AnalyticsReportsResource? _reports;
        /// <summary>The <c>analytics.reports</c> operations.</summary>
        public AnalyticsReportsResource Reports => _reports ??= new AnalyticsReportsResource(Transport);

        private AnalyticsAlertRulesResource? _alertRules;
        /// <summary>The <c>analytics.alertRules</c> operations.</summary>
        public AnalyticsAlertRulesResource AlertRules => _alertRules ??= new AnalyticsAlertRulesResource(Transport);

        /// <summary><c>GET /v1/analytics/funnel</c> - Where sign-in or sign-up attempts end, per method: started, completed, abandoned, and the step each unfinished attempt stopped at, with the completion rate. Each attempt is counted once, at its final state.</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> FunnelAsync(string? kind = null, object? from = null, object? to = null, long? days = null, string? granularity = null, string? organizationId = null, string? format = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/funnel", query: Q(("kind", kind), ("from", from), ("to", to), ("days", days), ("granularity", granularity), ("organization_id", organizationId), ("format", format), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/analytics/journey_funnel</c> - What happened at each step of a sign-in journey: how often it held a sign-in (a required action or form), was completed, ran at the sign-in (an Action, claim or redirect) or refused it, with the completion rate of its holds. Steps follow the active version; a sub-flow's steps are prefixed with the sub-flow step id. No organization slice.</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> JourneyFunnelAsync(string journeyId, object? from = null, object? to = null, long? days = null, string? granularity = null, string? organizationId = null, string? format = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/journey_funnel", query: Q(("journey_id", journeyId), ("from", from), ("to", to), ("days", days), ("granularity", granularity), ("organization_id", organizationId), ("format", format), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/analytics/failures</c> - Failed first- and second-factor attempts by reason and method, most frequent first. No organization slice (a failed attempt belongs to no organization).</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> FailuresAsync(object? from = null, object? to = null, long? days = null, string? granularity = null, string? organizationId = null, string? format = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/failures", query: Q(("from", from), ("to", to), ("days", days), ("granularity", granularity), ("organization_id", organizationId), ("format", format), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/analytics/breakdown</c> - Sign-ins split by one dimension: method, device type, browser, operating system or country (country needs a GeoIP database on the deployment; otherwise it reports `unknown`).</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> BreakdownAsync(string? dimension = null, object? from = null, object? to = null, long? days = null, string? granularity = null, string? organizationId = null, string? format = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/breakdown", query: Q(("dimension", dimension), ("from", from), ("to", to), ("days", days), ("granularity", granularity), ("organization_id", organizationId), ("format", format), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/analytics/active_users</c> - Distinct active users and sign-ins per bucket, each bucket split into new users (signed up in that bucket) and returning users, plus trailing 1/7/30-day active-user counts (dau, wau, mau) ending at `to`.</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> ActiveUsersAsync(object? from = null, object? to = null, long? days = null, string? granularity = null, string? organizationId = null, string? format = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/active_users", query: Q(("from", from), ("to", to), ("days", days), ("granularity", granularity), ("organization_id", organizationId), ("format", format), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/analytics/passkey_adoption</c> - How many users hold a passkey (adoption rate), and the passkey share of sign-ins per bucket.</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> PasskeyAdoptionAsync(object? from = null, object? to = null, long? days = null, string? granularity = null, string? organizationId = null, string? format = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/passkey_adoption", query: Q(("from", from), ("to", to), ("days", days), ("granularity", granularity), ("organization_id", organizationId), ("format", format), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/analytics/api_usage</c> - API usage per subject against its own baseline: secret keys calling the Backend API, end-user API keys verified through /v1/api_keys/verify, and organizations (Backend API calls on the organization plus verifications of its keys). For each: requests and the failure rate in the window, the average hourly rate over the 7 days before, and the ratio between them, highest ratio first (at most 100). Counts are written in batches and can lag by up to a minute.</summary>
        /// <remarks>Needs the <c>analytics:read</c> scope.</remarks>
        public Task<JsonElement> ApiUsageAsync(string? subjectType = null, string? subjectId = null, long? windowMinutes = null, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/analytics/api_usage", query: Q(("subject_type", subjectType), ("subject_id", subjectId), ("window_minutes", windowMinutes), ("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
