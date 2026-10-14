using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class CreateTicketBody
    {
        /// <summary>Arbitrary JSON returned to whoever redeems the ticket. Defaults to {}.</summary>
        public object? Payload { get; set; }
        /// <summary>Ticket lifetime in seconds. Must be positive; clamped to a maximum of 86400 (24h).</summary>
        public long? TtlSeconds { get; set; }
        /// <summary>When true (default), the first successful redeem consumes the ticket and all later redeems are refused.</summary>
        public bool? SingleUse { get; set; }
        /// <summary>If set, only a redeemer acting as this user may redeem (FAPI: the session user; BAPI: for_user).</summary>
        public string? BoundUser { get; set; }
        /// <summary>If set, only a redeemer acting for this organization may redeem (FAPI: a proven member; BAPI: for_org).</summary>
        public string? BoundOrg { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateTicketBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class RedeemTicketBody
    {
        /// <summary>The opaque `tkt_…` secret to redeem.</summary>
        public string? Ticket { get; set; }
        /// <summary>The user this redemption is on behalf of, matched against bound_user.</summary>
        public string? ForUser { get; set; }
        /// <summary>The organization this redemption is for, matched against bound_org.</summary>
        public string? ForOrg { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public RedeemTicketBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class IssuedTicket
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        /// <summary>The opaque `tkt_…` secret — returned only here, never again.</summary>
        public string? Ticket { get; init; }
        public bool? SingleUse { get; init; }
        public string? BoundUser { get; init; }
        public string? BoundOrg { get; init; }
        public long? CreatedAt { get; init; }
        public long? ExpiresAt { get; init; }
    }

    public sealed class RedeemedTicket
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        /// <summary>The redeemed ticket id.</summary>
        public string? Id { get; init; }
        /// <summary>The arbitrary JSON the ticket carried, returned verbatim.</summary>
        public JsonElement? Payload { get; init; }
        public bool? SingleUse { get; init; }
    }

    /// <summary>Signed, short-lived tickets (<c>/v1/tickets</c>) that carry a payload between your own services.</summary>
    public sealed class TicketsResource : ResourceBase
    {
        public TicketsResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>POST /v1/tickets</c> — Mint a one-time capability ticket: an opaque, high-entropy handle carrying an arbitrary JSON payload a holder can later redeem (e.g.</summary>
        public Task<IssuedTicket> CreateAsync(CreateTicketBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<IssuedTicket>(HttpVerb.Post, "/v1/tickets", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/tickets/redeem</c> — Redeem a capability ticket server-to-server and return its payload.</summary>
        public Task<RedeemedTicket> RedeemAsync(RedeemTicketBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<RedeemedTicket>(HttpVerb.Post, "/v1/tickets/redeem", body, idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
