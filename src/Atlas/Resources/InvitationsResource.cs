using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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
    }
}
