using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class Invitation
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "invitation";
        public string Id { get; init; } = "";
        public string EmailAddress { get; init; } = "";
        public string Status { get; init; } = "";
        public Metadata PublicMetadata { get; init; } = new Metadata();
        public long ExpiresAt { get; init; }
        public long? AcceptedAt { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class CreateInvitationBody
    {
        public string EmailAddress { get; set; } = "";
        public Metadata? PublicMetadata { get; set; }
    }

    /// <summary>The instance-level invitations namespace (<c>/v1/invitations</c>).</summary>
    public sealed class InvitationsResource : ResourceBase
    {
        public InvitationsResource(AtlasTransport transport) : base(transport) { }

        public Task<CursorPage<Invitation>> ListAsync(CursorParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<Invitation>>(HttpVerb.Get, "/v1/invitations", query: (@params ?? new CursorParams()).ToQuery(), cancellationToken: ct);

        public Task<Invitation> CreateAsync(CreateInvitationBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Invitation>(HttpVerb.Post, "/v1/invitations", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<Invitation> RevokeAsync(string id, CancellationToken ct = default)
            => Req<Invitation>(HttpVerb.Post, $"/v1/invitations/{Enc(id)}/revoke", cancellationToken: ct);

        /// <summary><c>POST /v1/invitations/bulk</c> — Bulk-create instance invitations (≤100).</summary>
        public Task<DataList<Invitation>> CreateBulkAsync(BulkInvitationsBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<DataList<Invitation>>(HttpVerb.Post, "/v1/invitations/bulk", body, idempotencyKey: idempotencyKey, cancellationToken: ct);
    }

    public sealed class BulkInvitation
    {
        public string? EmailAddress { get; set; }
        public Dictionary<string, object?>? PublicMetadata { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public BulkInvitation Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class BulkInvitationsBody
    {
        public List<BulkInvitation>? Invitations { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public BulkInvitationsBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }
}
