using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    // ---- partner platforms ---------------------------------------------------------
    // This instance as a partner platform of another product: the partner signs its deliveries with a
    // published Ed25519 key set and delivers signed consent events to the instance's receiver. The partner
    // token it hands back is stored once, encrypted, and never appears in a response.

    /// <summary>Whether the partner has registered with the receiver.</summary>
    public sealed class PartnerPlatformRegistrationState
    {
        /// <summary><c>unregistered</c> or <c>registered</c>.</summary>
        public string Status { get; init; } = "";
        public long? RegisteredAt { get; init; }
    }

    /// <summary>A partner platform. The partner token is never part of it: only <see cref="TokenSet"/> and <see cref="TokenPrefix"/> show it.</summary>
    public sealed class PartnerPlatform
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "partner_platform";
        public string Id { get; init; } = "";
        public string Partner { get; init; } = "";
        public string Name { get; init; } = "";
        public bool Preset { get; init; }
        public string PlatformId { get; init; } = "";
        public bool Enabled { get; init; }
        public string JwksUrl { get; init; } = "";
        public string SignatureHeader { get; init; } = "";
        /// <summary><c>t_kid_ed25519</c>.</summary>
        public string SignatureScheme { get; init; } = "";
        public int ToleranceSeconds { get; init; }
        public List<string> AllowedSources { get; init; } = new List<string>();
        public bool RecordConsents { get; init; }
        public string ReceiverUrl { get; init; } = "";
        public string? ReceiverUrlTemplate { get; init; }
        public bool ServesDeployment { get; init; }
        public PartnerPlatformRegistrationState Registration { get; init; } = new PartnerPlatformRegistrationState();
        public bool TokenSet { get; init; }
        public string? TokenPrefix { get; init; }
        public long? LastDeliveryAt { get; init; }
        public string? LastDeliveryError { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    /// <summary><c>GET /v1/partner_platforms</c>: the configured partner platforms plus the ready-made presets.</summary>
    public sealed class PartnerPlatformList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<PartnerPlatform> Data { get; init; } = new List<PartnerPlatform>();
        public bool? HasMore { get; init; }
        public List<JsonElement> Presets { get; init; } = new List<JsonElement>();
    }

    public sealed class PartnerPlatformSignature
    {
        public string Header { get; init; } = "";
        public string Scheme { get; init; } = "";
        public string Format { get; init; } = "";
        public string JwksUrl { get; init; } = "";
        public int ToleranceSeconds { get; init; }
    }

    public sealed class PartnerPlatformSendTo
    {
        public string Contact { get; init; } = "";
        public string Docs { get; init; } = "";
    }

    /// <summary>What to send the partner so it can be registered.</summary>
    public sealed class PartnerPlatformRegistration
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "partner_platform_registration";
        public string Partner { get; init; } = "";
        public string PlatformId { get; init; } = "";
        public string ReceiverUrl { get; init; } = "";
        public bool ReceiverUrlIsTemplate { get; init; }
        public string? ExternalId { get; init; }
        public List<string> Events { get; init; } = new List<string>();
        public PartnerPlatformSignature Signature { get; init; } = new PartnerPlatformSignature();
        public PartnerPlatformSendTo? SendTo { get; init; }
        public string? TokenPrefix { get; init; }
        public string NextStep { get; init; } = "";
    }

    /// <summary>One delivery a partner made to the receiver.</summary>
    public sealed class PartnerDelivery
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "partner_delivery";
        public string? EventId { get; init; }
        public string? EventType { get; init; }
        /// <summary><c>recorded</c>, <c>unchanged</c>, <c>no_subject</c>, <c>no_user</c>, <c>source_not_allowed</c> or <c>ignored</c>.</summary>
        public string Outcome { get; init; } = "";
        public string? UserId { get; init; }
        public long ReceivedAt { get; init; }
    }

    public sealed class PartnerPlatformDeleted
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "partner_platform";
        public string Partner { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary>The body of <c>PUT /v1/partner_platforms/{partner}</c> (a preset fills the defaults). <see cref="Set"/> sends an explicit <c>null</c> (name).</summary>
    public sealed class SetPartnerPlatformBody
    {
        public string? Name { get; set; }
        public string? PlatformId { get; set; }
        public bool? Enabled { get; set; }
        public string? JwksUrl { get; set; }
        public string? SignatureHeader { get; set; }
        /// <summary><c>t_kid_ed25519</c>.</summary>
        public string? SignatureScheme { get; set; }
        public int? ToleranceSeconds { get; set; }
        public List<string>? AllowedSources { get; set; }
        public bool? RecordConsents { get; set; }
        public bool? ServesDeployment { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>.</summary>
        public SetPartnerPlatformBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>The body of <c>POST /v1/partner_platforms/{partner}/token</c>. The token is write-only: stored encrypted, never returned, and not printed by <see cref="ToString"/>.</summary>
    public sealed class StorePartnerPlatformTokenBody
    {
        /// <summary>Write-only.</summary>
        public string Token { get; set; } = "";
        /// <summary>true replaces a token that is already stored.</summary>
        public bool? Rotate { get; set; }
        public string? ApiBaseUrl { get; set; }

        public override string ToString() => "StorePartnerPlatformTokenBody { Token = ***, Rotate = " + Rotate + " }";
    }

    /// <summary><c>/v1/partner_platforms</c>: this instance as a partner platform of another product.</summary>
    public sealed class PartnerPlatformsResource : ResourceBase
    {
        public PartnerPlatformsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/partner_platforms</c></summary>
        public Task<PartnerPlatformList> ListAsync(CancellationToken ct = default)
            => Req<PartnerPlatformList>(HttpVerb.Get, "/v1/partner_platforms", cancellationToken: ct);

        /// <summary><c>GET /v1/partner_platforms/{partner}</c></summary>
        public Task<PartnerPlatform> GetAsync(string partner, CancellationToken ct = default)
            => Req<PartnerPlatform>(HttpVerb.Get, $"/v1/partner_platforms/{Enc(partner)}", cancellationToken: ct);

        /// <summary><c>PUT /v1/partner_platforms/{partner}</c> — set up or change (a preset fills the defaults).</summary>
        public Task<PartnerPlatform> SetAsync(string partner, SetPartnerPlatformBody? body = null, CancellationToken ct = default)
            => Req<PartnerPlatform>(HttpVerb.Put, $"/v1/partner_platforms/{Enc(partner)}", body ?? new SetPartnerPlatformBody(), cancellationToken: ct);

        /// <summary><c>DELETE /v1/partner_platforms/{partner}</c></summary>
        public Task<PartnerPlatformDeleted> RemoveAsync(string partner, CancellationToken ct = default)
            => Req<PartnerPlatformDeleted>(HttpVerb.Delete, $"/v1/partner_platforms/{Enc(partner)}", cancellationToken: ct);

        /// <summary><c>GET /v1/partner_platforms/{partner}/registration</c> — what to send the partner to be registered.</summary>
        public Task<PartnerPlatformRegistration> RegistrationAsync(string partner, CancellationToken ct = default)
            => Req<PartnerPlatformRegistration>(HttpVerb.Get, $"/v1/partner_platforms/{Enc(partner)}/registration", cancellationToken: ct);

        /// <summary><c>POST /v1/partner_platforms/{partner}/token</c> — store the token the partner handed over (once; <c>Rotate</c> replaces it). The token is never returned.</summary>
        public Task<PartnerPlatform> StoreTokenAsync(string partner, StorePartnerPlatformTokenBody body, CancellationToken ct = default)
            => Req<PartnerPlatform>(HttpVerb.Post, $"/v1/partner_platforms/{Enc(partner)}/token", body, cancellationToken: ct);

        /// <summary><c>GET /v1/partner_platforms/{partner}/deliveries</c></summary>
        public Task<ListPage<PartnerDelivery>> DeliveriesAsync(string partner, int? limit = null, CancellationToken ct = default)
            => Req<ListPage<PartnerDelivery>>(HttpVerb.Get, $"/v1/partner_platforms/{Enc(partner)}/deliveries", query: Q(("limit", limit)), cancellationToken: ct);
    }
}
