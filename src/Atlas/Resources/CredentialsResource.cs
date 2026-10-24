using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    // The instance's own credentials (scoped secret keys, the standard key list, whoami), partner links,
    // and Sign in with Atlas grant revocation for the apps the instance manages.

    /// <summary>What a credential is scoped to.</summary>
    public sealed class ResourceRef
    {
        /// <summary><c>instance</c>, <c>account</c>, <c>application</c> or <c>webhook_endpoint</c>.</summary>
        public string Type { get; init; } = "";
        public string Id { get; init; } = "";
    }

    /// <summary>The shape every credential listing uses.</summary>
    public class StandardKey
    {
        /// <summary><c>key</c> or <c>secret_key</c>.</summary>
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        /// <summary><c>secret_key</c>, <c>platform_key</c>, <c>account_key</c>, <c>webhook_secret</c>, <c>partner_link</c> or <c>access_token</c>.</summary>
        public string Kind { get; init; } = "";
        /// <summary><c>atlas</c> (<c>atlas_&lt;env&gt;_&lt;kind&gt;_…</c>) or <c>legacy</c> (<c>sk_</c> / <c>psk_</c> / <c>whsec_</c> / <c>dsk_</c>).</summary>
        public string Format { get; init; } = "";
        public string Prefix { get; init; } = "";
        public string? Name { get; init; }
        public string? Owner { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        /// <summary><c>live</c>, <c>test</c> or null.</summary>
        public string? Environment { get; init; }
        public ResourceRef? Resource { get; init; }
        public long? ExpiresAt { get; init; }
        public int? ExpiresInDays { get; init; }
        /// <summary><c>active</c>, <c>expiring</c>, <c>expired</c> or <c>revoked</c>.</summary>
        public string Status { get; init; } = "";
        public List<string>? IpAllowlist { get; init; }
        /// <summary>Browser origins a request carrying an <c>Origin</c> header must come from (empty = any).</summary>
        public List<string>? OriginAllowlist { get; init; }
        public long? LastUsedAt { get; init; }
        public string? LastUsedIp { get; init; }
        public long CreatedAt { get; init; }
        public long? RevokedAt { get; init; }
        /// <summary>A heads-up Atlas adds to a key that is close to expiry.</summary>
        public JsonElement? ExpiryWarning { get; init; }
    }

    /// <summary>A scoped instance secret key.</summary>
    public class SecretKey : StandardKey
    {
        public List<string>? AllowedIps { get; init; }
        /// <summary>Alias of <see cref="StandardKey.OriginAllowlist"/>.</summary>
        public List<string>? AllowedOrigins { get; init; }
        public string? RotatedFrom { get; init; }
        /// <summary>Provisioning tags.</summary>
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        /// <summary>The idempotent-create key, if any.</summary>
        public string? ExternalRef { get; init; }
        /// <summary>Create only: true when an existing key with this <c>external_ref</c> was returned (without its secret).</summary>
        public bool? IdempotentReplay { get; init; }
    }

    /// <summary>A create or rotate response: the secret is present once (or sealed, with a secret recipient).</summary>
    public sealed class SecretKeyWithSecret : SecretKey
    {
        public string? Secret { get; init; }
        public string? SealedSecret { get; init; }
        public string? Note { get; init; }
        /// <summary>On a rotation: the id of the key this one replaces.</summary>
        public string? Replaces { get; init; }
        /// <summary>On a rotation: when the old key stops working (epoch ms).</summary>
        public long? PreviousKeyExpiresAt { get; init; }
    }

    /// <summary>The body of <c>POST /v1/secret_keys</c>.</summary>
    public sealed class CreateSecretKeyBody
    {
        public string? Name { get; set; }
        /// <summary>Instance scopes, or the presets <c>read_only</c>, <c>privacy:execute</c> and <c>privacy:grants:revoke</c>.</summary>
        public List<string> Scopes { get; set; } = new List<string>();
        /// <summary>Unix ms (a <c>long</c>) or an ISO 8601 string; at most one year away (default one year).</summary>
        public object? ExpiresAt { get; set; }
        /// <summary>CIDRs the key may be used from.</summary>
        public List<string>? AllowedIps { get; set; }
        /// <summary>Origins like <c>https://app.example.com</c> or <c>https://*.example.com</c> a browser request must come from.</summary>
        public List<string>? AllowedOrigins { get; set; }
        public string? Owner { get; set; }
        /// <summary><c>atlas</c> (default) or <c>legacy</c> (for validators that check for <c>sk_</c>).</summary>
        public string? KeyFormat { get; set; }
        /// <summary>Idempotent-create key (up to 200 characters).</summary>
        public string? ExternalRef { get; set; }
        /// <summary>Provisioning tags (up to 20).</summary>
        public Dictionary<string, object?>? Metadata { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/secret_keys/{id}</c>; scopes never change. Use <see cref="Set"/> to send an explicit <c>null</c> (clear name, owner, expires_at).</summary>
    public sealed class UpdateSecretKeyBody
    {
        public string? Name { get; set; }
        public string? Owner { get; set; }
        public object? ExpiresAt { get; set; }
        public List<string>? AllowedIps { get; set; }
        public List<string>? AllowedOrigins { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>.</summary>
        public UpdateSecretKeyBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>The body of <c>POST /v1/secret_keys/{id}/rotate</c>.</summary>
    public sealed class RotateSecretKeyBody
    {
        /// <summary>How long the old key keeps working (default 72 h, at most 7 days; 0 = it stops now).</summary>
        public int? OverlapSeconds { get; set; }
        public string? KeyFormat { get; set; }
    }

    /// <summary><c>/v1/secret_keys</c>.</summary>
    public sealed class SecretKeysResource : ResourceBase
    {
        public SecretKeysResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/secret_keys</c> — mint a scoped secret key; the secret is in this response only. Send an <c>ExternalRef</c> to make the create idempotent.</summary>
        public Task<SecretKeyWithSecret> CreateAsync(CreateSecretKeyBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<SecretKeyWithSecret>(HttpVerb.Post, "/v1/secret_keys", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/secret_keys</c> — the instance's secret keys (revoked ones stay listed).</summary>
        public Task<ListPage<SecretKey>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<SecretKey>>(HttpVerb.Get, "/v1/secret_keys", cancellationToken: ct);

        /// <summary><c>GET /v1/secret_keys?metadata[key]=value</c> — the keys whose provisioning tags match every pair.</summary>
        public Task<ListPage<SecretKey>> ListAsync(IDictionary<string, object?> metadata, CancellationToken ct = default)
            => Req<ListPage<SecretKey>>(HttpVerb.Get, "/v1/secret_keys", query: AtlasProvisioning.MetadataQuery(metadata), cancellationToken: ct);

        /// <summary><c>GET /v1/secret_keys/{id}</c></summary>
        public Task<SecretKey> GetAsync(string id, CancellationToken ct = default)
            => Req<SecretKey>(HttpVerb.Get, $"/v1/secret_keys/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/secret_keys/{id}</c> — change a key's name, owner, expiry, IP or origin allowlist, or tags.</summary>
        public Task<SecretKey> UpdateAsync(string id, UpdateSecretKeyBody body, CancellationToken ct = default)
            => Req<SecretKey>(HttpVerb.Patch, $"/v1/secret_keys/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>POST /v1/secret_keys/{id}/rotate</c> — mint a replacement with the same scopes; the old key keeps working for the overlap.</summary>
        public Task<SecretKeyWithSecret> RotateAsync(string id, RotateSecretKeyBody? body = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<SecretKeyWithSecret>(HttpVerb.Post, $"/v1/secret_keys/{Enc(id)}/rotate", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/secret_keys/{id}/revoke</c> — revoke at once (a second revoke is a 409).</summary>
        public Task<SecretKey> RevokeAsync(string id, CancellationToken ct = default)
            => Req<SecretKey>(HttpVerb.Post, $"/v1/secret_keys/{Enc(id)}/revoke", cancellationToken: ct);
    }

    /// <summary><c>/v1/keys</c>.</summary>
    public sealed class KeysResource : ResourceBase
    {
        public KeysResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/keys</c> — every credential on the instance, in the standard shape.</summary>
        public Task<ListPage<StandardKey>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<StandardKey>>(HttpVerb.Get, "/v1/keys", cancellationToken: ct);
    }

    // ---- whoami ------------------------------------------------------------------

    /// <summary>The credential a request was signed with.</summary>
    public sealed class WhoamiKey
    {
        public string? Id { get; init; }
        /// <summary><c>secret</c>, <c>publishable</c>, <c>platform</c>, <c>partner_link</c> or <c>access_token</c>.</summary>
        public string Type { get; init; } = "";
        public string? Prefix { get; init; }
        public string? Environment { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        /// <summary>Presets (<c>read_only</c>, <c>privacy:execute</c>, …) expanded.</summary>
        public List<string> EffectiveScopes { get; init; } = new List<string>();
        public string? Owner { get; init; }
        public long? CreatedAt { get; init; }
        public long? ExpiresAt { get; init; }
        public long? LastUsedAt { get; init; }
        public string? LastUsedIp { get; init; }
        public List<string>? ApplicationIds { get; init; }
    }

    /// <summary>The instance the key belongs to.</summary>
    public sealed class WhoamiInstance
    {
        public string Id { get; init; } = "";
        public string? Environment { get; init; }
        public string? Issuer { get; init; }
        public string? DiscoveryUrl { get; init; }
        public string? JwksUri { get; init; }
    }

    public sealed class WhoamiAccount
    {
        public string? Id { get; init; }
    }

    public sealed class WhoamiPartner
    {
        public string LinkId { get; init; } = "";
        public string Id { get; init; } = "";
        public string? Name { get; init; }
    }

    public sealed class WhoamiOperator
    {
        public string Sub { get; init; } = "";
        public long? AuthTime { get; init; }
        public string? Acr { get; init; }
        public List<string>? Amr { get; init; }
    }

    /// <summary><c>GET /v1/whoami</c> (and <c>/v1/platform/whoami</c>): what the credential in use is and what it may do.</summary>
    public sealed class Whoami
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "whoami";
        public WhoamiKey Key { get; init; } = new WhoamiKey();
        public WhoamiInstance? Instance { get; init; }
        public WhoamiAccount? Account { get; init; }
        public WhoamiPartner? Partner { get; init; }
        public WhoamiOperator? Operator { get; init; }
        public JsonElement? Platform { get; init; }
    }

    /// <summary><c>GET /v1/whoami</c>.</summary>
    public sealed class WhoamiResource : ResourceBase
    {
        public WhoamiResource(AtlasTransport t) : base(t) { }

        /// <summary>Introspect the key this client uses: its type, scopes, owner, expiry and the instance it belongs to.</summary>
        public Task<Whoami> GetAsync(CancellationToken ct = default)
            => Req<Whoami>(HttpVerb.Get, "/v1/whoami", cancellationToken: ct);
    }

    // ---- partner links -------------------------------------------------------------

    /// <summary>A credential an integrated partner service holds on this instance.</summary>
    public class PartnerLink
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "partner_link";
        public string Id { get; init; } = "";
        public string ClientId { get; init; } = "";
        /// <summary>Any lowercase id; <c>cookiemunch</c>, <c>chatly</c>, <c>forumulate</c> and <c>referalli</c> are presets.</summary>
        public string Partner { get; init; } = "";
        public string Name { get; init; } = "";
        public bool Preset { get; init; }
        public string? Resource { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        public string? ClientSecretPrefix { get; init; }
        public string? TokenEndpoint { get; init; }
        /// <summary>mTLS: SHA-256 fingerprints (base64url) of the client certificates every use must present. Empty = none needed.</summary>
        public List<string>? CertFingerprints { get; init; }
        /// <summary>How the partner may authenticate: <c>client_secret_basic</c>, <c>client_secret_post</c>, <c>bearer</c> and <c>tls_client_auth</c> when certificates are pinned.</summary>
        public List<string>? AuthMethods { get; init; }
        /// <summary>The partner link request this link was approved from; null when created by hand.</summary>
        public string? RequestId { get; init; }
        /// <summary><c>active</c>, <c>expired</c> or <c>revoked</c>.</summary>
        public string Status { get; init; } = "";
        public long? ExpiresAt { get; init; }
        public long? RevokedAt { get; init; }
        public long? LastUsedAt { get; init; }
        public string? LastUsedIp { get; init; }
        [JsonPropertyName("calls_30d")] public int Calls30d { get; init; }
        public long CreatedAt { get; init; }
        /// <summary>Set once, on create and rotate.</summary>
        public string? ClientSecret { get; init; }
        public string? SealedClientSecret { get; init; }
        public bool? Revoked { get; init; }
    }

    /// <summary>The body of <c>POST /v1/partner_links</c>.</summary>
    public sealed class CreatePartnerLinkBody
    {
        public string Partner { get; set; } = "";
        public string? Name { get; set; }
        public List<string> Scopes { get; set; } = new List<string>();
        /// <summary>Unix ms or ISO 8601, at most one year away (default one year).</summary>
        public object? ExpiresAt { get; set; }
        /// <summary>mTLS: SHA-256 fingerprints (hex or base64url) of the client certificates the partner must present.</summary>
        public List<string>? CertFingerprints { get; set; }
    }

    /// <summary>A partner link request: a partner asking this instance (inbound) or Atlas asking a partner (outbound).</summary>
    public sealed class PartnerLinkRequest
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "partner_link_request";
        public string Id { get; init; } = "";
        /// <summary><c>inbound</c> or <c>outbound</c>.</summary>
        public string? Direction { get; init; }
        public string Partner { get; init; } = "";
        public string? Name { get; init; }
        public bool Preset { get; init; }
        public string? Resource { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        public string? CallbackUrl { get; init; }
        /// <summary>Where the credentials go; check before approving.</summary>
        public string? CallbackHost { get; init; }
        public List<string>? CertFingerprints { get; init; }
        /// <summary><c>pending</c>, <c>approved</c>, <c>denied</c>, <c>expired</c>, <c>delivery_failed</c>, <c>installed</c> or <c>failed</c>.</summary>
        public string Status { get; init; } = "";
        public string? LinkId { get; init; }
        public string? RemoteRequestId { get; init; }
        public string? DecidedBy { get; init; }
        public long? DecidedAt { get; init; }
        public int DeliveryAttempts { get; init; }
        public long? DeliveredAt { get; init; }
        public string? LastError { get; init; }
        public long ExpiresAt { get; init; }
        public long CreatedAt { get; init; }
        /// <summary>On approve: whether the credentials reached the partner's callback.</summary>
        public bool? Delivered { get; init; }
        /// <summary>On approve: the created link (null when delivery failed).</summary>
        public PartnerLink? Link { get; init; }
        public string? Note { get; init; }
        /// <summary>The partner-side create answer: the secret the credentials are signed with, shown once.</summary>
        public string? RequestSecret { get; init; }
        public string? Protocol { get; init; }
        /// <summary>The partner-side read answer: the id of the link once approved.</summary>
        public string? ClientId { get; init; }
    }

    /// <summary>The body of <c>POST /v1/partner_links/requests/{id}/approve</c>.</summary>
    public sealed class ApprovePartnerLinkRequestBody
    {
        /// <summary>Narrow what was requested.</summary>
        public List<string>? Scopes { get; set; }
        /// <summary>Unix ms or ISO 8601 (default one year).</summary>
        public object? ExpiresAt { get; set; }
    }

    /// <summary>The partner side of the request protocol: <c>POST /v1/partner_link_requests</c> (no Atlas credential).</summary>
    public sealed class CreatePartnerLinkRequestBody
    {
        /// <summary>Your product id.</summary>
        public string Partner { get; set; } = "";
        public string? Name { get; set; }
        /// <summary>The instance id or its publishable key.</summary>
        public string Resource { get; set; } = "";
        public List<string> Scopes { get; set; } = new List<string>();
        /// <summary>https; a preset product's callback must be on its own domain.</summary>
        public string CallbackUrl { get; set; } = "";
        public string? State { get; set; }
        /// <summary>Ask for a certificate-locked link.</summary>
        public List<string>? CertFingerprints { get; set; }
    }

    /// <summary>An outbound partner link: a credential this instance holds for a partner's API.</summary>
    public sealed class OutboundPartnerLink
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "outbound_partner_link";
        public string Id { get; init; } = "";
        public string Partner { get; init; } = "";
        public string Name { get; init; } = "";
        public bool Preset { get; init; }
        public string? Resource { get; init; }
        public List<string> Scopes { get; init; } = new List<string>();
        public string? BaseUrl { get; init; }
        public string? TokenUrl { get; init; }
        public string? ClientId { get; init; }
        /// <summary><c>client_credentials</c> or <c>bearer_secret</c>.</summary>
        public string? AuthMethod { get; init; }
        public bool HasClientSecret { get; init; }
        /// <summary><c>manual</c> or <c>oauth_install</c>.</summary>
        public string? Source { get; init; }
        /// <summary><c>active</c>, <c>revoked</c> or <c>failing</c>.</summary>
        public string? Status { get; init; }
        public long? ExpiresAt { get; init; }
        public long? LastUsedAt { get; init; }
        public string? LastError { get; init; }
        public long? LastErrorAt { get; init; }
        [JsonPropertyName("calls_30d")] public int Calls30d { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>The consent-manager block of the partner (cmp), when it has one.</summary>
        public JsonElement? Cmp { get; init; }
        public bool? CmpDefault { get; init; }
    }

    /// <summary>The body of <c>PUT /v1/partner_links/outbound/{partner}</c>.</summary>
    public sealed class SaveOutboundPartnerLinkBody
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public List<string>? Scopes { get; set; }
        public string? Resource { get; set; }
        /// <summary>Required for a partner that is not a preset.</summary>
        public string? BaseUrl { get; set; }
        public string? TokenUrl { get; set; }
        public string? AuthMethod { get; set; }
        public string? Name { get; set; }
        public long? ExpiresAt { get; set; }
    }

    /// <summary>The body of <c>POST /v1/partner_links/outbound/{partner}/request</c>.</summary>
    public sealed class RequestOutboundPartnerLinkBody
    {
        /// <summary>Partner-side scopes.</summary>
        public List<string> Scopes { get; set; } = new List<string>();
        public string? Resource { get; set; }
        /// <summary>Required for a partner that is not a preset.</summary>
        public string? BaseUrl { get; set; }
        public string? TokenUrl { get; set; }
        public string? Name { get; set; }
    }

    public sealed class OutboundPartnerLinkTest
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Partner { get; init; }
        public bool Ok { get; init; }
        public string? Error { get; init; }
    }

    public sealed class OutboundPartnerLinkDeleted
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string Partner { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary><c>/v1/partner_links</c>. Outbound links and link requests hang off it.</summary>
    public sealed class PartnerLinksResource : ResourceBase
    {
        public PartnerLinksResource(AtlasTransport t) : base(t)
        {
            Outbound = new OutboundPartnerLinksResource(t);
            Requests = new PartnerLinkRequestsResource(t);
        }

        /// <summary>Links this instance holds for a partner's API (Atlas calling them).</summary>
        public OutboundPartnerLinksResource Outbound { get; }

        /// <summary>Partner link requests (the owner approves or denies them).</summary>
        public PartnerLinkRequestsResource Requests { get; }

        /// <summary><c>POST /v1/partner_links</c> — grant a partner a credential on this instance; the secret is shown once.</summary>
        public Task<PartnerLink> CreateAsync(CreatePartnerLinkBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<PartnerLink>(HttpVerb.Post, "/v1/partner_links", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/partner_links</c></summary>
        public Task<ListPage<PartnerLink>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<PartnerLink>>(HttpVerb.Get, "/v1/partner_links", cancellationToken: ct);

        /// <summary><c>GET /v1/partner_links/{id}</c></summary>
        public Task<PartnerLink> GetAsync(string id, CancellationToken ct = default)
            => Req<PartnerLink>(HttpVerb.Get, $"/v1/partner_links/{Enc(id)}", cancellationToken: ct);

        /// <summary>
        /// <c>PATCH /v1/partner_links/{id}</c> — lock a link to client certificates (mTLS): SHA-256 fingerprints,
        /// hex or base64url, at most 5; an empty list removes the lock. Tokens minted before the change stop working.
        /// </summary>
        public Task<PartnerLink> SetCertFingerprintsAsync(string id, IEnumerable<string> certFingerprints, CancellationToken ct = default)
            => Req<PartnerLink>(HttpVerb.Patch, $"/v1/partner_links/{Enc(id)}", Body(("cert_fingerprints", certFingerprints)), cancellationToken: ct);

        /// <summary><c>POST /v1/partner_links/{id}/rotate_secret</c> — issue a new client secret.</summary>
        public Task<PartnerLink> RotateSecretAsync(string id, CancellationToken ct = default)
            => Req<PartnerLink>(HttpVerb.Post, $"/v1/partner_links/{Enc(id)}/rotate_secret", cancellationToken: ct);

        /// <summary><c>DELETE /v1/partner_links/{id}</c> — revoke a link; its secret and every token minted from it stop now.</summary>
        public Task<PartnerLink> RevokeAsync(string id, CancellationToken ct = default)
            => Req<PartnerLink>(HttpVerb.Delete, $"/v1/partner_links/{Enc(id)}", cancellationToken: ct);
    }

    /// <summary><c>/v1/partner_links/requests</c>.</summary>
    public sealed class PartnerLinkRequestsResource : ResourceBase
    {
        public PartnerLinkRequestsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/partner_links/requests</c> — <c>direction</c> is <c>inbound</c> (default; partners asking this instance) or <c>outbound</c> (Atlas asking partners); <c>status</c> is comma-separated.</summary>
        public Task<ListPage<PartnerLinkRequest>> ListAsync(string? direction = null, string? status = null, CancellationToken ct = default)
            => Req<ListPage<PartnerLinkRequest>>(HttpVerb.Get, "/v1/partner_links/requests", query: Q(("direction", direction), ("status", status)), cancellationToken: ct);

        /// <summary><c>GET /v1/partner_links/requests/{id}</c></summary>
        public Task<PartnerLinkRequest> GetAsync(string id, CancellationToken ct = default)
            => Req<PartnerLinkRequest>(HttpVerb.Get, $"/v1/partner_links/requests/{Enc(id)}", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/partner_links/requests/{id}/approve</c> — Atlas creates the link and delivers its credentials server to
        /// server to the partner's callback; the owner never sees a secret. If the callback does not answer 2xx the new link is
        /// revoked and the request is <c>delivery_failed</c>; approve again to retry.
        /// </summary>
        public Task<PartnerLinkRequest> ApproveAsync(string id, ApprovePartnerLinkRequestBody? body = null, CancellationToken ct = default)
            => Req<PartnerLinkRequest>(HttpVerb.Post, $"/v1/partner_links/requests/{Enc(id)}/approve", body, cancellationToken: ct);

        /// <summary><c>POST /v1/partner_links/requests/{id}/deny</c> — the partner's callback is told (best effort).</summary>
        public Task<PartnerLinkRequest> DenyAsync(string id, CancellationToken ct = default)
            => Req<PartnerLinkRequest>(HttpVerb.Post, $"/v1/partner_links/requests/{Enc(id)}/deny", cancellationToken: ct);
    }

    /// <summary><c>/v1/partner_links/outbound</c>.</summary>
    public sealed class OutboundPartnerLinksResource : ResourceBase
    {
        public OutboundPartnerLinksResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/partner_links/outbound</c></summary>
        public Task<ListPage<OutboundPartnerLink>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<OutboundPartnerLink>>(HttpVerb.Get, "/v1/partner_links/outbound", cancellationToken: ct);

        /// <summary><c>PUT /v1/partner_links/outbound/{partner}</c> — create or replace the outbound link for a partner.</summary>
        public Task<OutboundPartnerLink> SaveAsync(string partner, SaveOutboundPartnerLinkBody body, CancellationToken ct = default)
            => Req<OutboundPartnerLink>(HttpVerb.Put, $"/v1/partner_links/outbound/{Enc(partner)}", body, cancellationToken: ct);

        /// <summary><c>POST /v1/partner_links/outbound/{partner}/test</c> — fetch a fresh token from the partner to prove the stored credentials work.</summary>
        public Task<OutboundPartnerLinkTest> TestAsync(string partner, CancellationToken ct = default)
            => Req<OutboundPartnerLinkTest>(HttpVerb.Post, $"/v1/partner_links/outbound/{Enc(partner)}/test", cancellationToken: ct);

        /// <summary><c>DELETE /v1/partner_links/outbound/{partner}</c></summary>
        public Task<OutboundPartnerLinkDeleted> RemoveAsync(string partner, CancellationToken ct = default)
            => Req<OutboundPartnerLinkDeleted>(HttpVerb.Delete, $"/v1/partner_links/outbound/{Enc(partner)}", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/partner_links/outbound/{partner}/request</c> — ask a partner for a link on this instance's behalf instead of
        /// pasting the credentials it issues. Once the partner's owner approves, the credentials are delivered to Atlas and stored
        /// as an outbound link (<c>source: oauth_install</c>).
        /// </summary>
        public Task<PartnerLinkRequest> RequestAsync(string partner, RequestOutboundPartnerLinkBody body, CancellationToken ct = default)
            => Req<PartnerLinkRequest>(HttpVerb.Post, $"/v1/partner_links/outbound/{Enc(partner)}/request", body, cancellationToken: ct);
    }

    // ---- Sign in with Atlas grants for the apps the instance manages ------------------

    /// <summary>A Sign in with Atlas app this instance manages.</summary>
    public sealed class ManagedAtlasIdApp
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? ManagedByInstanceId { get; init; }
    }

    /// <summary>A verified email is a user of an app.</summary>
    public sealed class AtlasIdGrantLookup
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Sub { get; init; } = "";
        public string GrantId { get; init; } = "";
        public List<string> Scopes { get; init; } = new List<string>();
        public long GrantedAt { get; init; }
    }

    /// <summary>The result of revoking one user's grant.</summary>
    public sealed class InstanceAtlasIdGrantRevocation
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Sub { get; init; } = "";
        public bool Revoked { get; init; }
        public int TokensRevoked { get; init; }
        public int BackchannelLogouts { get; init; }
    }

    /// <summary><c>/v1/atlas_id_apps</c>.</summary>
    public sealed class AtlasIdGrantsResource : ResourceBase
    {
        public AtlasIdGrantsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/atlas_id_apps</c> — the Sign in with Atlas apps this instance manages.</summary>
        public Task<ListPage<ManagedAtlasIdApp>> ListAppsAsync(CancellationToken ct = default)
            => Req<ListPage<ManagedAtlasIdApp>>(HttpVerb.Get, "/v1/atlas_id_apps", cancellationToken: ct);

        /// <summary><c>GET /v1/atlas_id_apps/{id}/users?email=</c> — whether a verified email is a user of the app (an empty list when not).</summary>
        public Task<ListPage<AtlasIdGrantLookup>> LookupAsync(string appId, string email, CancellationToken ct = default)
            => Req<ListPage<AtlasIdGrantLookup>>(HttpVerb.Get, $"/v1/atlas_id_apps/{Enc(appId)}/users", query: Q(("email", email)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/atlas_id_apps/{id}/grants/{grantId}</c> — revoke one user's grant. Needs <c>privacy:grants:revoke</c> or the per-app grant scope.</summary>
        public Task<InstanceAtlasIdGrantRevocation> RevokeAsync(string appId, string grantId, CancellationToken ct = default)
            => Req<InstanceAtlasIdGrantRevocation>(HttpVerb.Delete, $"/v1/atlas_id_apps/{Enc(appId)}/grants/{Enc(grantId)}", cancellationToken: ct);
    }
}
