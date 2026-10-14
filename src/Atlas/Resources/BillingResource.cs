using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class BillingPlan
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "billing_plan";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Slug { get; init; } = "";
        public string StripePriceId { get; init; } = "";
        public string Interval { get; init; } = "";
        public string? Amount { get; init; }
        public string Currency { get; init; } = "";
        public List<string> Features { get; init; } = new List<string>();
        public string Audience { get; init; } = "";
        public bool Active { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class BillingSubscription
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "billing_subscription";
        public string Id { get; init; } = "";
        public string SubjectType { get; init; } = "";
        public string SubjectId { get; init; } = "";
        public string PlanId { get; init; } = "";
        public string Status { get; init; } = "";
        public string? StripeSubscriptionId { get; init; }
        public string? StripeCustomerId { get; init; }
        public long? CurrentPeriodEnd { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class CreateBillingPlanBody
    {
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string StripePriceId { get; set; } = "";
        /// <summary>One of <c>month</c>, <c>year</c>. Defaults to <c>month</c>.</summary>
        public string? Interval { get; set; }
        public string? Amount { get; set; }
        public string? Currency { get; set; }
        public List<string>? Features { get; set; }
        /// <summary>One of <c>user</c>, <c>org</c>. Defaults to <c>user</c>.</summary>
        public string? Audience { get; set; }
        public bool? Active { get; set; }
    }

    public sealed class UpdateBillingPlanBody
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? StripePriceId { get; set; }
        public string? Interval { get; set; }
        public string? Amount { get; set; }
        public string? Currency { get; set; }
        public List<string>? Features { get; set; }
        public string? Audience { get; set; }
        public bool? Active { get; set; }
    }

    public sealed class ListSubscriptionsParams
    {
        /// <summary>One of <c>user</c>, <c>org</c>.</summary>
        public string? SubjectType { get; set; }
        public string? SubjectId { get; set; }

        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("subject_type", SubjectType);
            yield return new KeyValuePair<string, object?>("subject_id", SubjectId);
        }
    }

    /// <summary>The billing namespace (<c>/v1/billing</c>) — plans a tenant defines for their users.</summary>
    public sealed class BillingResource : ResourceBase
    {
        public BillingResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<BillingPlan>> ListPlansAsync(CancellationToken ct = default)
            => Req<ListPage<BillingPlan>>(HttpVerb.Get, "/v1/billing/plans", cancellationToken: ct);

        public Task<BillingPlan> CreatePlanAsync(CreateBillingPlanBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<BillingPlan>(HttpVerb.Post, "/v1/billing/plans", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<BillingPlan> GetPlanAsync(string id, CancellationToken ct = default)
            => Req<BillingPlan>(HttpVerb.Get, $"/v1/billing/plans/{Enc(id)}", cancellationToken: ct);

        public Task<BillingPlan> UpdatePlanAsync(string id, UpdateBillingPlanBody body, CancellationToken ct = default)
            => Req<BillingPlan>(HttpVerb.Patch, $"/v1/billing/plans/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeletePlanAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/billing/plans/{Enc(id)}", cancellationToken: ct);

        public Task<ListPage<BillingSubscription>> ListSubscriptionsAsync(ListSubscriptionsParams? @params = null, CancellationToken ct = default)
            => Req<ListPage<BillingSubscription>>(HttpVerb.Get, "/v1/billing/subscriptions", query: (@params ?? new ListSubscriptionsParams()).ToQuery(), cancellationToken: ct);

        /// <summary><c>POST /v1/billing/subscriptions/import</c> — Adopt an EXISTING Stripe subscription (same Stripe account) onto an Atlas user/org + plan, for migrating billing into Atlas without re-charging.</summary>
        public Task<BillingSubscription> ImportSubscriptionAsync(ImportSubscriptionBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<BillingSubscription>(HttpVerb.Post, "/v1/billing/subscriptions/import", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/billing/usage</c> — Report metered usage for a subject (idempotent on idempotency_key); mirrored to a Stripe meter event when the plan is metered and configured.</summary>
        public Task<BillingUsageRecorded> ReportUsageAsync(ReportBillingUsageBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<BillingUsageRecorded>(HttpVerb.Post, "/v1/billing/usage", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/billing/subjects/{type}/{id}/sync_seats</c> — Sync a per-seat subscription's Stripe quantity to the org's current member count (proration applied).</summary>
        public Task<BillingSeatSync> SyncSeatsAsync(string type, string id, CancellationToken ct = default)
            => Req<BillingSeatSync>(HttpVerb.Post, $"/v1/billing/subjects/{Enc(type)}/{Enc(id)}/sync_seats", cancellationToken: ct);
    }

    public sealed class ImportSubscriptionBody
    {
        public string? SubjectType { get; set; }
        public string? SubjectId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? PlanId { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public ImportSubscriptionBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class ReportBillingUsageBody
    {
        public string? SubjectType { get; set; }
        public string? SubjectId { get; set; }
        public double? Quantity { get; set; }
        public string? IdempotencyKey { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public ReportBillingUsageBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class BillingUsageRecorded
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public bool Recorded { get; init; }
        public bool Metered { get; init; }
        public double Total { get; init; }
    }

    public sealed class BillingSeatSync
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string SubjectType { get; init; } = "";
        public string SubjectId { get; init; } = "";
        public long Quantity { get; init; }
    }
}
