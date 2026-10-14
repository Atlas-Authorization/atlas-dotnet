using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    /// <summary>Page text. Plain text only (shown as typed, never as HTML), one line, no control characters; empty or null uses the default. `{app}` is replaced with the app name. Merged per field on PATCH.</summary>
    public sealed class AtlasIdAppCopy
    {
        /// <summary>Default "Sign in to continue to {app}".</summary>
        public string? SignInTitle { get; set; }
        public string? SignInSubtitle { get; set; }
        /// <summary>Default "Create your account to continue to {app}".</summary>
        public string? SignUpTitle { get; set; }
        public string? SignUpSubtitle { get; set; }
        /// <summary>Shown on the consent screen above the list of permissions.</summary>
        public string? ConsentMessage { get; set; }
        /// <summary>https URL, linked in the page footer.</summary>
        public string? SupportUrl { get; set; }
        /// <summary>Text of the support link. Default "Help".</summary>
        public string? SupportLabel { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasIdAppCopy Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>How the Sign in with Atlas page and consent screen look for this app. On PATCH each field merges with what is stored; a field set to null clears it, and branding: null clears all of them. The Atlas header and the app domain are always shown.</summary>
    public sealed class AtlasIdAppBrandingInput
    {
        /// <summary>Default auto (follows the visitor’s system setting).</summary>
        public string? Theme { get; set; }
        /// <summary>Button colour, #RRGGBB. The button text is black or white, whichever contrasts more.</summary>
        public string? AccentColor { get; set; }
        /// <summary>Page background behind the card, #RRGGBB.</summary>
        public string? BackgroundColor { get; set; }
        /// <summary>https URL, linked in the page footer.</summary>
        public string? PrivacyPolicyUrl { get; set; }
        /// <summary>https URL, linked in the page footer.</summary>
        public string? TermsOfServiceUrl { get; set; }
        /// <summary>The sign-in page design (AtlasDesign settings). Merged like a design PATCH over what is stored (null inside removes a setting); design: null removes it. When no design is set, theme/accent_color/background_color are used. logoPlacement "hidden" is refused; images must be design assets uploaded…</summary>
        public AtlasDesign? Design { get; set; }
        /// <summary>Page text. Plain text only (shown as typed, never as HTML), one line, no control characters; empty or null uses the default. `{app}` is replaced with the app name. Merged per field on PATCH.</summary>
        public AtlasIdAppCopy? Copy { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasIdAppBrandingInput Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class CreateAtlasIdAppBody
    {
        public string? Name { get; set; }
        public List<string>? RedirectUris { get; set; }
        /// <summary>Where /oauth2/logout may send the user after sign-out. Exact match; https, or http on a loopback host (localhost, 127.0.0.1, [::1]). Defaults to [].</summary>
        public List<string>? PostLogoutRedirectUris { get; set; }
        /// <summary>OIDC Back-Channel Logout: when the user signs out of Atlas, a signed logout_token is POSTed here (form field `logout_token`). Same URL rule as redirect_uris; empty or null clears it.</summary>
        public string? BackchannelLogoutUri { get; set; }
        /// <summary>Whether the app needs `sid` in the logout_token. Atlas always includes it. Default false.</summary>
        public bool? BackchannelLogoutSessionRequired { get; set; }
        /// <summary>OIDC Front-Channel Logout: loaded in a hidden iframe on the Atlas sign-out page, with `iss` (and `sid` if session-required). Same URL rule as redirect_uris; empty or null clears it.</summary>
        public string? FrontchannelLogoutUri { get; set; }
        /// <summary>Add `sid` to the front-channel logout URL. Default false.</summary>
        public bool? FrontchannelLogoutSessionRequired { get; set; }
        public string? LogoUrl { get; set; }
        public string? HomepageUrl { get; set; }
        public List<string>? AllowedScopes { get; set; }
        /// <summary>How the Sign in with Atlas page and consent screen look for this app. On PATCH each field merges with what is stored; a field set to null clears it, and branding: null clears all of them. The Atlas header and the app domain are always shown.</summary>
        public AtlasIdAppBrandingInput? Branding { get; set; }
        public string? Type { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateAtlasIdAppBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class UpdateAtlasIdAppBody
    {
        public string? Name { get; set; }
        public List<string>? RedirectUris { get; set; }
        /// <summary>Where /oauth2/logout may send the user after sign-out. Exact match; https, or http on a loopback host (localhost, 127.0.0.1, [::1]). Defaults to [].</summary>
        public List<string>? PostLogoutRedirectUris { get; set; }
        /// <summary>OIDC Back-Channel Logout: when the user signs out of Atlas, a signed logout_token is POSTed here (form field `logout_token`). Same URL rule as redirect_uris; empty or null clears it.</summary>
        public string? BackchannelLogoutUri { get; set; }
        /// <summary>Whether the app needs `sid` in the logout_token. Atlas always includes it. Default false.</summary>
        public bool? BackchannelLogoutSessionRequired { get; set; }
        /// <summary>OIDC Front-Channel Logout: loaded in a hidden iframe on the Atlas sign-out page, with `iss` (and `sid` if session-required). Same URL rule as redirect_uris; empty or null clears it.</summary>
        public string? FrontchannelLogoutUri { get; set; }
        /// <summary>Add `sid` to the front-channel logout URL. Default false.</summary>
        public bool? FrontchannelLogoutSessionRequired { get; set; }
        public string? LogoUrl { get; set; }
        public string? HomepageUrl { get; set; }
        public List<string>? AllowedScopes { get; set; }
        /// <summary>How the Sign in with Atlas page and consent screen look for this app. On PATCH each field merges with what is stored; a field set to null clears it, and branding: null clears all of them. The Atlas header and the app domain are always shown.</summary>
        public AtlasIdAppBrandingInput? Branding { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public UpdateAtlasIdAppBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class CreatePlatformAppBody
    {
        public string? Name { get; set; }
        public string? Environment { get; set; }
        public List<string>? AllowedOrigins { get; set; }
        public bool? CreateDevelopmentSibling { get; set; }
        public List<string>? Tags { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreatePlatformAppBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class UpdatePlatformAppBody
    {
        public string? Name { get; set; }
        public List<string>? AllowedOrigins { get; set; }
        public List<string>? Tags { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public UpdatePlatformAppBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class CreatePlatformInstanceBody
    {
        public string? Environment { get; set; }
        public List<string>? AllowedOrigins { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreatePlatformInstanceBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class CreateAccountlessAppBody
    {
        public string? Name { get; set; }
        public string? Environment { get; set; }
        public List<string>? AllowedOrigins { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateAccountlessAppBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasIdAppBranding
    {
        public string Theme { get; init; } = "";
        public string? AccentColor { get; init; }
        public string? BackgroundColor { get; init; }
        public string? PrivacyPolicyUrl { get; init; }
        public string? TermsOfServiceUrl { get; init; }
        /// <summary>The stored design overrides (null = none).</summary>
        public AtlasDesign? Design { get; init; }
        /// <summary>What the Sign in with Atlas page renders with.</summary>
        public ResolvedDesign? ResolvedDesign { get; init; }
        public AtlasIdAppCopy? Copy { get; init; }
    }

    public sealed class AtlasIdApp
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? LogoUrl { get; init; }
        public string? HomepageUrl { get; init; }
        public AtlasIdAppBranding? Branding { get; init; }
        public List<string> RedirectUris { get; init; } = new List<string>();
        public List<string> PostLogoutRedirectUris { get; init; } = new List<string>();
        public string? BackchannelLogoutUri { get; init; }
        public bool BackchannelLogoutSessionRequired { get; init; }
        public string? FrontchannelLogoutUri { get; init; }
        public bool FrontchannelLogoutSessionRequired { get; init; }
        public List<string> AllowedScopes { get; init; } = new List<string>();
        public string Type { get; init; } = "";
        public string? SecretPrefix { get; init; }
        public string? ManagedByInstanceId { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class AtlasIdAppWithSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? LogoUrl { get; init; }
        public string? HomepageUrl { get; init; }
        public AtlasIdAppBranding? Branding { get; init; }
        public List<string> RedirectUris { get; init; } = new List<string>();
        public List<string> PostLogoutRedirectUris { get; init; } = new List<string>();
        public string? BackchannelLogoutUri { get; init; }
        public bool BackchannelLogoutSessionRequired { get; init; }
        public string? FrontchannelLogoutUri { get; init; }
        public bool FrontchannelLogoutSessionRequired { get; init; }
        public List<string> AllowedScopes { get; init; } = new List<string>();
        public string Type { get; init; } = "";
        public string? SecretPrefix { get; init; }
        public string? ManagedByInstanceId { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>Confidential apps only; returned exactly once.</summary>
        public string? ClientSecret { get; init; }
        public string? Note { get; init; }
    }

    public sealed class PlatformAppProvisioned
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Environment { get; init; } = "";
        public string PublishableKey { get; init; } = "";
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        /// <summary>X8 housekeeping labels (≤20, each ≤40 chars).</summary>
        public List<string> Tags { get; init; } = new List<string>();
        /// <summary>X8 housekeeping: hidden from the default listing. A pure visibility flag — the app still serves auth.</summary>
        public bool Archived { get; init; }
        /// <summary>Epoch ms the app was archived, or null if not archived.</summary>
        public long? ArchivedAt { get; init; }
        public bool Disabled { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
        /// <summary>Last updated (epoch ms).</summary>
        public long UpdatedAt { get; init; }
        /// <summary>Reveal-once per-instance secret key.</summary>
        public string Secret { get; init; } = "";
        public string? Note { get; init; }
        /// <summary>The auto-created test-mode sibling in the environment opposite the primary (development for a production app — the default and common case). Has distinct publishable_key and secret from the primary. Absent when create_development_sibling is false.</summary>
        public PlatformAppWithSecret? SiblingInstance { get; init; }
    }

    public sealed class PlatformAppWithSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Environment { get; init; } = "";
        public string PublishableKey { get; init; } = "";
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        /// <summary>X8 housekeeping labels (≤20, each ≤40 chars).</summary>
        public List<string> Tags { get; init; } = new List<string>();
        /// <summary>X8 housekeeping: hidden from the default listing. A pure visibility flag — the app still serves auth.</summary>
        public bool Archived { get; init; }
        /// <summary>Epoch ms the app was archived, or null if not archived.</summary>
        public long? ArchivedAt { get; init; }
        public bool Disabled { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
        /// <summary>Last updated (epoch ms).</summary>
        public long UpdatedAt { get; init; }
        /// <summary>Reveal-once per-instance secret key.</summary>
        public string Secret { get; init; } = "";
        public string? Note { get; init; }
    }

    /// <summary>An app (instance) a platform owns. Never carries a secret except on create/rotate/claim.</summary>
    public sealed class PlatformApp
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Environment { get; init; } = "";
        public string PublishableKey { get; init; } = "";
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        /// <summary>X8 housekeeping labels (≤20, each ≤40 chars).</summary>
        public List<string> Tags { get; init; } = new List<string>();
        /// <summary>X8 housekeeping: hidden from the default listing. A pure visibility flag — the app still serves auth.</summary>
        public bool Archived { get; init; }
        /// <summary>Epoch ms the app was archived, or null if not archived.</summary>
        public long? ArchivedAt { get; init; }
        public bool Disabled { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
        /// <summary>Last updated (epoch ms).</summary>
        public long UpdatedAt { get; init; }
    }

    public sealed class PlatformAppDisabled
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public bool Disabled { get; init; }
        public string Note { get; init; } = "";
    }

    public sealed class PlatformAppBulkDeleteItem
    {
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
        public string? Reason { get; init; }
    }

    public sealed class PlatformAppBulkDeleteResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public List<PlatformAppBulkDeleteItem> Results { get; init; } = new List<PlatformAppBulkDeleteItem>();
    }

    public sealed class PlatformAppSecretRotation
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string AppId { get; init; } = "";
        public string Secret { get; init; } = "";
        public long OverlapSeconds { get; init; }
        public long? PreviousKeysExpireAt { get; init; }
        public string Note { get; init; } = "";
    }

    public sealed class PlatformSecretKey
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Prefix { get; init; } = "";
        public long CreatedAt { get; init; }
        public long? ExpiresAt { get; init; }
    }

    public sealed class PlatformInstanceSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Secret { get; init; } = "";
        public string Note { get; init; } = "";
    }

    public sealed class AccountlessApp
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Environment { get; init; } = "";
        public string PublishableKey { get; init; } = "";
        public string ClaimToken { get; init; } = "";
        public long ExpiresAt { get; init; }
        public string Note { get; init; } = "";
    }

    public sealed class PlatformAppTransfer
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string TransferToken { get; init; } = "";
        public long ExpiresAt { get; init; }
        public string Note { get; init; } = "";
    }

    public sealed class AcceptedPlatformApp
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Environment { get; init; } = "";
        public string PublishableKey { get; init; } = "";
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        /// <summary>X8 housekeeping labels (≤20, each ≤40 chars).</summary>
        public List<string> Tags { get; init; } = new List<string>();
        /// <summary>X8 housekeeping: hidden from the default listing. A pure visibility flag — the app still serves auth.</summary>
        public bool Archived { get; init; }
        /// <summary>Epoch ms the app was archived, or null if not archived.</summary>
        public long? ArchivedAt { get; init; }
        public bool Disabled { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
        /// <summary>Last updated (epoch ms).</summary>
        public long UpdatedAt { get; init; }
        public string Note { get; init; } = "";
    }

    /// <summary>Design images owned by the platform account (<c>/v1/platform/design/assets</c>).</summary>
    public sealed class PlatformDesignAssetsResource : ResourceBase
    {
        public PlatformDesignAssetsResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>POST /v1/platform/design/assets</c> — Upload a design image owned by the platform's account, for Sign in with Atlas app designs.</summary>
        public Task<DesignAsset> UploadAsync(byte[] data, string contentType, string? kind = null, CancellationToken ct = default)
            => ReqBytes<DesignAsset>(HttpVerb.Post, "/v1/platform/design/assets", data, contentType, Q(("kind", kind)), ct);

        /// <summary><c>GET /v1/platform/design/assets</c> — List the design images owned by the platform's account, newest first.</summary>
        public Task<DataList<DesignAsset>> ListAsync(CancellationToken ct = default)
            => Req<DataList<DesignAsset>>(HttpVerb.Get, "/v1/platform/design/assets", cancellationToken: ct);

        /// <summary><c>DELETE /v1/platform/design/assets/{id}</c> — Delete a design image owned by the platform's account.</summary>
        public Task<DeletedDesignAsset> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedDesignAsset>(HttpVerb.Delete, $"/v1/platform/design/assets/{Enc(id)}", cancellationToken: ct);
    }

    /// <summary>Sign in with Atlas apps (<c>/v1/platform/atlas_id_apps</c>).</summary>
    public sealed class PlatformAtlasIdAppsResource : ResourceBase
    {
        public PlatformAtlasIdAppsResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>POST /v1/platform/atlas_id_apps/design/preview</c> — Preview a Sign in with Atlas app design without saving it: the compiled bundle plus the rendered Sign in with Atlas page (the Atlas trust strip, the destination line and "Secured by Atlas" are always there).</summary>
        public Task<DesignPreview> PreviewDesignAsync(DesignBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<DesignPreview>(HttpVerb.Post, "/v1/platform/atlas_id_apps/design/preview", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Shorthand: wraps <paramref name="design"/> in a <see cref="DesignBody"/>.</summary>
        public Task<DesignPreview> PreviewDesignAsync(AtlasDesign design, string? idempotencyKey = null, CancellationToken ct = default)
            => PreviewDesignAsync(new DesignBody { Design = design }, idempotencyKey, ct);

        /// <summary><c>GET /v1/platform/atlas_id_apps</c> — List the Sign in with Atlas apps registered to this platform's account, with the issuer and discovery URL.</summary>
        public Task<DataList<AtlasIdApp>> ListAsync(CancellationToken ct = default)
            => Req<DataList<AtlasIdApp>>(HttpVerb.Get, "/v1/platform/atlas_id_apps", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_apps</c> — Register a Sign in with Atlas app (an OpenID Connect client on https://atlasauth.net) for this platform's account.</summary>
        public Task<AtlasIdAppWithSecret> CreateAsync(CreateAtlasIdAppBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<AtlasIdAppWithSecret>(HttpVerb.Post, "/v1/platform/atlas_id_apps", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/platform/atlas_id_apps/{id}</c> — Fetch one Sign in with Atlas app, including user_count (people who have approved it).</summary>
        public Task<AtlasIdApp> GetAsync(string id, CancellationToken ct = default)
            => Req<AtlasIdApp>(HttpVerb.Get, $"/v1/platform/atlas_id_apps/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/platform/atlas_id_apps/{id}</c> — Update a Sign in with Atlas app: name, redirect_uris, post_logout_redirect_uris ([] clears them), backchannel_logout_uri / frontchannel_logout_uri (empty or null clears) and their *_session_required flags, allowed_scopes, logo_url, homepage_url,…</summary>
        public Task<AtlasIdApp> UpdateAsync(string id, UpdateAtlasIdAppBody body, CancellationToken ct = default)
            => Req<AtlasIdApp>(HttpVerb.Patch, $"/v1/platform/atlas_id_apps/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/platform/atlas_id_apps/{id}</c> — Delete a Sign in with Atlas app.</summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/platform/atlas_id_apps/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_apps/{id}/rotate_secret</c> — Rotate a confidential app's client_secret.</summary>
        public Task<AtlasIdAppWithSecret> RotateSecretAsync(string id, CancellationToken ct = default)
            => Req<AtlasIdAppWithSecret>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(id)}/rotate_secret", cancellationToken: ct);
    }

    /// <summary>Apps (instances) owned by the platform (<c>/v1/platform/apps</c>).</summary>
    public sealed class PlatformAppsResource : ResourceBase
    {
        public PlatformAppsResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>POST /v1/platform/apps</c> — Provision a new app (instance) owned by the platform: mints its publishable key, signing key and first secret key.</summary>
        public Task<PlatformAppProvisioned> CreateAsync(CreatePlatformAppBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<PlatformAppProvisioned>(HttpVerb.Post, "/v1/platform/apps", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/platform/apps</c> — List the apps (instances) this platform owns.</summary>
        public Task<CursorPage<PlatformApp>> ListAsync(ListPlatformAppsParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<PlatformApp>>(HttpVerb.Get, "/v1/platform/apps", query: (@params ?? new ListPlatformAppsParams()).ToQuery(), cancellationToken: ct);

        /// <summary><c>GET /v1/platform/apps/{id}</c> — Fetch one owned app.</summary>
        public Task<PlatformApp> GetAsync(string id, CancellationToken ct = default)
            => Req<PlatformApp>(HttpVerb.Get, $"/v1/platform/apps/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/platform/apps/{id}</c> — Update an owned app's safe config subset (name, allowed_origins, and X8 housekeeping tags).</summary>
        public Task<PlatformApp> UpdateAsync(string id, UpdatePlatformAppBody body, CancellationToken ct = default)
            => Req<PlatformApp>(HttpVerb.Patch, $"/v1/platform/apps/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/platform/apps/{id}</c> — Disable an owned app: refuse sign-ins and sign-ups via its kill switches.</summary>
        public Task<PlatformAppDisabled> DisableAsync(string id, CancellationToken ct = default)
            => Req<PlatformAppDisabled>(HttpVerb.Delete, $"/v1/platform/apps/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/archive</c> — X8 housekeeping: archive an owned app so it drops out of the default app listing.</summary>
        public Task<PlatformApp> ArchiveAsync(string id, CancellationToken ct = default)
            => Req<PlatformApp>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/archive", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/unarchive</c> — X8 housekeeping: clear an owned app's archive flag, returning it to the default app listing.</summary>
        public Task<PlatformApp> UnarchiveAsync(string id, CancellationToken ct = default)
            => Req<PlatformApp>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/unarchive", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/bulk_delete</c> — X8 housekeeping: delete up to 100 owned apps in one call, for tidying throwaway probes.</summary>
        public Task<PlatformAppBulkDeleteResult> BulkDeleteAsync(List<string> ids, CancellationToken ct = default)
            => Req<PlatformAppBulkDeleteResult>(HttpVerb.Post, "/v1/platform/apps/bulk_delete", Body(("ids", ids)), cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/rotate_secret</c> — Rotate an owned app's secret key WITH OVERLAP (S3): mint a new sk_ (revealed exactly once) while the prior keys stay valid for a grace window so servers roll over without downtime.</summary>
        public Task<PlatformAppSecretRotation> RotateSecretAsync(string id, double? overlapSeconds = null, CancellationToken ct = default)
            => Req<PlatformAppSecretRotation>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/rotate_secret", Body(("overlap_seconds", overlapSeconds)), cancellationToken: ct);

        /// <summary><c>GET /v1/platform/apps/{id}/secret_keys</c> — List an owned app's currently-valid secret keys (S3) — the primary key plus any prior key still inside its rotation overlap window.</summary>
        public Task<CursorPage<PlatformSecretKey>> ListSecretKeysAsync(string id, CancellationToken ct = default)
            => Req<CursorPage<PlatformSecretKey>>(HttpVerb.Get, $"/v1/platform/apps/{Enc(id)}/secret_keys", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/instances</c> — Provision an additional instance (e.g.</summary>
        public Task<PlatformAppWithSecret> CreateInstanceAsync(string id, CreatePlatformInstanceBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<PlatformAppWithSecret>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/instances", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/platform/apps/{id}/instances</c> — List every instance under an owned app — its development/production siblings.</summary>
        public Task<CursorPage<PlatformApp>> ListInstancesAsync(string id, CancellationToken ct = default)
            => Req<CursorPage<PlatformApp>>(HttpVerb.Get, $"/v1/platform/apps/{Enc(id)}/instances", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/instances/{instanceId}/rotate_secret</c> — Rotate one specific instance's secret under an owned app: revoke its prior keys and mint a new one, revealed once.</summary>
        public Task<PlatformInstanceSecret> RotateInstanceSecretAsync(string id, string instanceId, CancellationToken ct = default)
            => Req<PlatformInstanceSecret>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/instances/{Enc(instanceId)}/rotate_secret", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/accountless_apps</c> — Mint an app owned by no platform and return a one-time claim_token (hashed at rest, single-use, expiring).</summary>
        public Task<AccountlessApp> CreateAccountlessAsync(CreateAccountlessAppBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<AccountlessApp>(HttpVerb.Post, "/v1/platform/accountless_apps", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/claim</c> — Claim an accountless app into the calling platform by its claim_token.</summary>
        public Task<PlatformAppWithSecret> ClaimAsync(string claimToken, CancellationToken ct = default)
            => Req<PlatformAppWithSecret>(HttpVerb.Post, "/v1/platform/apps/claim", Body(("claim_token", claimToken)), cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/transfer</c> — Initiate a transfer of an owned app: returns a one-time transfer_token bound to it (hashed, single-use, expiring).</summary>
        public Task<PlatformAppTransfer> StartTransferAsync(string id, CancellationToken ct = default)
            => Req<PlatformAppTransfer>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/transfer", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/accept_transfer</c> — Accept a transfer by transfer_token: move the app's ownership to the calling platform and re-parent it to its account.</summary>
        public Task<AcceptedPlatformApp> AcceptTransferAsync(string transferToken, CancellationToken ct = default)
            => Req<AcceptedPlatformApp>(HttpVerb.Post, "/v1/platform/apps/accept_transfer", Body(("transfer_token", transferToken)), cancellationToken: ct);
    }

    /// <summary>The platform (management-plane) API, authenticated by a platform key (<c>psk_...</c>): provision apps, register Sign in with Atlas apps, and manage account-owned design images.</summary>
    public sealed class PlatformResource : ResourceBase
    {
        public PlatformResource(AtlasTransport transport) : base(transport)
        {
            DesignAssets = new PlatformDesignAssetsResource(transport);
            AtlasIdApps = new PlatformAtlasIdAppsResource(transport);
            Apps = new PlatformAppsResource(transport);
        }

        /// <summary>Design images owned by the platform account (<c>/v1/platform/design/assets</c>).</summary>
        public PlatformDesignAssetsResource DesignAssets { get; }

        /// <summary>Sign in with Atlas apps (<c>/v1/platform/atlas_id_apps</c>).</summary>
        public PlatformAtlasIdAppsResource AtlasIdApps { get; }

        /// <summary>Apps (instances) owned by the platform (<c>/v1/platform/apps</c>).</summary>
        public PlatformAppsResource Apps { get; }
    }
}
