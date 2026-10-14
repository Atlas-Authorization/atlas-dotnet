using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class CreateEntitlementOverrideBody
    {
        public string? SubjectType { get; set; }
        public string? SubjectId { get; set; }
        /// <summary>Plan slug to pin, or null to override features/limits only.</summary>
        public string? Plan { get; set; }
        /// <summary>Replaces (not adds to) the resolved feature set.</summary>
        public List<string>? Features { get; set; }
        /// <summary>Numeric limits, e.g. {"max_devices":50}.</summary>
        public Dictionary<string, object?>? Limits { get; set; }
        /// <summary>Epoch ms when the override lapses; omit/null for open-ended.</summary>
        public long? ExpiresAt { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateEntitlementOverrideBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>A sales-led entitlement override (P1-6): a non-Stripe grant pinning a plan / features / limits onto a user or org. A non-expired override wins over the Stripe-derived entitlement.</summary>
    public sealed class EntitlementOverride
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string SubjectType { get; init; } = "";
        public string SubjectId { get; init; } = "";
        /// <summary>Plan slug to pin, or null to override features/limits only.</summary>
        public string? Plan { get; init; }
        public List<string>? Features { get; init; }
        public Dictionary<string, object?>? Limits { get; init; }
        /// <summary>Epoch ms when the override lapses, or null for open-ended.</summary>
        public long? ExpiresAt { get; init; }
        public string? CreatedBy { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
    }

    /// <summary>Sales-led entitlement overrides (<c>/v1/entitlement_overrides</c>).</summary>
    public sealed class EntitlementOverridesResource : ResourceBase
    {
        public EntitlementOverridesResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>POST /v1/entitlement_overrides</c> — Grant a SALES-LED entitlement override (P1-6): pin a plan / features / limits onto a user or org without Stripe (e.g.</summary>
        public Task<EntitlementOverride> CreateAsync(CreateEntitlementOverrideBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<EntitlementOverride>(HttpVerb.Post, "/v1/entitlement_overrides", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/entitlement_overrides</c> — List sales-led entitlement overrides, optionally filtered by subject_type (user/org) and subject_id.</summary>
        public Task<ListPage<EntitlementOverride>> ListAsync(string? subjectType = null, string? subjectId = null, CancellationToken ct = default)
            => Req<ListPage<EntitlementOverride>>(HttpVerb.Get, "/v1/entitlement_overrides", query: Q(("subject_type", subjectType), ("subject_id", subjectId)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/entitlement_overrides/{id}</c> — Revoke (hard-delete) a sales-led entitlement override.</summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/entitlement_overrides/{Enc(id)}", cancellationToken: ct);
    }
}
