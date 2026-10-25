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
        /// <summary>Default "Sign in" (the app is named on the "to continue to {app}" line under it).</summary>
        public string? SignInTitle { get; set; }
        public string? SignInSubtitle { get; set; }
        /// <summary>Default "Create your Atlas Account".</summary>
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
        /// <summary>Provisioning tags (strings, numbers or booleans). On a PATCH they replace the tags.</summary>
        public Dictionary<string, object?>? Metadata { get; set; }
        /// <summary>A stable key of your own: a repeat create with it returns the existing object (200, no secret) instead of creating another one.</summary>
        public string? ExternalRef { get; set; }
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
        /// <summary>Per-app webhook (public https): user.updated, user.deleted and grant.revoked for this app's users. The first set returns WebhookSecret once; <c>Set("webhook_url", null)</c> removes it.</summary>
        public string? WebhookUrl { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public UpdateAtlasIdAppBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
        /// <summary>Provisioning tags (strings, numbers or booleans). On a PATCH they replace the tags.</summary>
        public Dictionary<string, object?>? Metadata { get; set; }
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

        /// <summary>Send <c>allowed_origins</c> as entries that may also declare how each origin signs in (<see cref="AllowedOriginEntry"/>); replaces <see cref="AllowedOrigins"/>.</summary>
        public CreatePlatformAppBody WithAllowedOriginEntries(IEnumerable<AllowedOriginEntry> entries)
        {
            AllowedOrigins = null;
            return Set("allowed_origins", AllowedOriginEntry.ToWire(entries));
        }
        /// <summary>Provisioning tags (strings, numbers or booleans). On a PATCH they replace the tags.</summary>
        public Dictionary<string, object?>? Metadata { get; set; }
        /// <summary>A stable key of your own: a repeat create with it returns the existing object (200, no secret) instead of creating another one.</summary>
        public string? ExternalRef { get; set; }
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

        /// <summary>Send <c>allowed_origins</c> as entries that may also declare how each origin signs in (<see cref="AllowedOriginEntry"/>); replaces <see cref="AllowedOrigins"/>.</summary>
        public UpdatePlatformAppBody WithAllowedOriginEntries(IEnumerable<AllowedOriginEntry> entries)
        {
            AllowedOrigins = null;
            return Set("allowed_origins", AllowedOriginEntry.ToWire(entries));
        }
        /// <summary>Provisioning tags (strings, numbers or booleans). On a PATCH they replace the tags.</summary>
        public Dictionary<string, object?>? Metadata { get; set; }
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

        /// <summary>Send <c>allowed_origins</c> as entries that may also declare how each origin signs in (<see cref="AllowedOriginEntry"/>); replaces <see cref="AllowedOrigins"/>.</summary>
        public CreatePlatformInstanceBody WithAllowedOriginEntries(IEnumerable<AllowedOriginEntry> entries)
        {
            AllowedOrigins = null;
            return Set("allowed_origins", AllowedOriginEntry.ToWire(entries));
        }
        /// <summary>Provisioning tags (strings, numbers or booleans). On a PATCH they replace the tags.</summary>
        public Dictionary<string, object?>? Metadata { get; set; }
        /// <summary>A stable key of your own: a repeat create with it returns the existing object (200, no secret) instead of creating another one.</summary>
        public string? ExternalRef { get; set; }
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

        /// <summary>Send <c>allowed_origins</c> as entries that may also declare how each origin signs in (<see cref="AllowedOriginEntry"/>); replaces <see cref="AllowedOrigins"/>.</summary>
        public CreateAccountlessAppBody WithAllowedOriginEntries(IEnumerable<AllowedOriginEntry> entries)
        {
            AllowedOrigins = null;
            return Set("allowed_origins", AllowedOriginEntry.ToWire(entries));
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
        /// <summary><c>instance_toggle</c> (an instance's Sign in with Atlas toggle app) or <c>adopted</c> (handed to an instance with <c>SetManagedByAsync</c>); null otherwise.</summary>
        public string? ManagedVia { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>The per-app webhook URL, or null.</summary>
        public string? WebhookUrl { get; init; }
        public List<string>? WebhookEvents { get; init; }
        /// <summary>false once auto-disabled after repeated failures; null without a webhook.</summary>
        public bool? WebhookActive { get; init; }
        /// <summary>While a webhook secret rotation overlap is open: when the previous secret stops signing (epoch ms); else null.</summary>
        public long? WebhookPreviousSecretExpiresAt { get; init; }
        /// <summary>After a client secret rotation with an overlap: when the previous secret stops working (epoch ms); else null.</summary>
        public long? PreviousSecretExpiresAt { get; init; }
        /// <summary>When the current client secret was issued (epoch ms); null for a public app.</summary>
        public long? SecretIssuedAt { get; init; }
        /// <summary>Provisioning tags.</summary>
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        /// <summary>The idempotent-create key you sent, if any.</summary>
        public string? ExternalRef { get; init; }
        /// <summary>True on a create that sent an <c>external_ref</c> and got the existing object back (200, no secret).</summary>
        public bool? IdempotentReplay { get; init; }
        /// <summary>The page origins the Sign in with Atlas button script ran on in the last 7 days (Get only).</summary>
        public List<AtlasIdOriginSeen>? ButtonRenderedOrigins { get; init; }
        /// <summary>How the last 7 days' sign-ins started.</summary>
        [JsonPropertyName("authorize_requests_7d")] public AtlasIdAuthorizeSources? AuthorizeRequests7d { get; init; }
        /// <summary>Origins of sign-ins that came through the no-script fallback link (<c>?atlasid_fallback=1</c>).</summary>
        public List<AtlasIdOriginSeen>? FallbackOrigins { get; init; }
        /// <summary>Whether the app's homepage references the button script (null = could not check).</summary>
        public bool? HomepageIncludesScript { get; init; }
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
        /// <summary><c>instance_toggle</c> (an instance's Sign in with Atlas toggle app) or <c>adopted</c> (handed to an instance with <c>SetManagedByAsync</c>); null otherwise.</summary>
        public string? ManagedVia { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>The per-app webhook URL, or null.</summary>
        public string? WebhookUrl { get; init; }
        public List<string>? WebhookEvents { get; init; }
        /// <summary>false once auto-disabled after repeated failures; null without a webhook.</summary>
        public bool? WebhookActive { get; init; }
        /// <summary>Confidential apps only; returned exactly once.</summary>
        public string? ClientSecret { get; init; }
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        public string? Note { get; init; }
        /// <summary>The per-app webhook signing secret, returned exactly once (first set, or rotated).</summary>
        public string? WebhookSecret { get; init; }
        /// <summary>While a webhook secret rotation overlap is open: when the previous secret stops signing (epoch ms); else null.</summary>
        public long? WebhookPreviousSecretExpiresAt { get; init; }
        /// <summary>After a client secret rotation with an overlap: when the previous secret stops working (epoch ms); else null.</summary>
        public long? PreviousSecretExpiresAt { get; init; }
        /// <summary>When the current client secret was issued (epoch ms); null for a public app.</summary>
        public long? SecretIssuedAt { get; init; }
        /// <summary>On a rotation: the overlap applied, in seconds.</summary>
        public int? OverlapSeconds { get; init; }
        /// <summary>Provisioning tags.</summary>
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        /// <summary>The idempotent-create key you sent, if any.</summary>
        public string? ExternalRef { get; init; }
        /// <summary>True on a create that sent an <c>external_ref</c> and got the existing object back (200, no secret).</summary>
        public bool? IdempotentReplay { get; init; }
        /// <summary>The page origins the Sign in with Atlas button script ran on in the last 7 days (Get only).</summary>
        public List<AtlasIdOriginSeen>? ButtonRenderedOrigins { get; init; }
        /// <summary>How the last 7 days' sign-ins started.</summary>
        [JsonPropertyName("authorize_requests_7d")] public AtlasIdAuthorizeSources? AuthorizeRequests7d { get; init; }
        /// <summary>Origins of sign-ins that came through the no-script fallback link (<c>?atlasid_fallback=1</c>).</summary>
        public List<AtlasIdOriginSeen>? FallbackOrigins { get; init; }
        /// <summary>Whether the app's homepage references the button script (null = could not check).</summary>
        public bool? HomepageIncludesScript { get; init; }
    }

    /// <summary>The body of the Sign in with Atlas app rotate calls (client secret and webhook secret).</summary>
    public sealed class RotateAtlasIdAppSecretBody
    {
        /// <summary>How long the previous secret keeps working, in seconds; at most 604800 (7 days). Null uses the default: 86400 (24 hours) for the client secret, 0 (it stops at once) for the webhook secret. 0 stops it now.</summary>
        public int? OverlapSeconds { get; set; }
    }

    /// <summary>A page origin the button (or the fallback link) was seen on.</summary>
    public sealed class AtlasIdOriginSeen
    {
        public string Origin { get; init; } = "";
        public long LastSeenAt { get; init; }
        public int Requests { get; init; }
    }

    /// <summary>Counts how sign-ins started: the rendered button, the script API, One Tap, the no-script fallback, or unmarked.</summary>
    public sealed class AtlasIdAuthorizeSources
    {
        public int Button { get; init; }
        public int Js { get; init; }
        [JsonPropertyName("fedcm")] public int FedCm { get; init; }
        public int Fallback { get; init; }
        public int Unmarked { get; init; }
    }

    public sealed class PlatformAppProvisioned
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Environment { get; init; } = "";
        public string PublishableKey { get; init; } = "";
        /// <summary>The Frontend API host: the default per-app host, or the custom domain once it is active.</summary>
        public string? FrontendApiHost { get; init; }
        /// <summary>The <c>iss</c> the app's session tokens carry. It changes when a custom domain goes active.</summary>
        public string? Issuer { get; init; }
        /// <summary>The OpenID discovery document whose <c>issuer</c> equals <see cref="Issuer"/>.</summary>
        public string? DiscoveryUrl { get; init; }
        /// <summary>The JWKS holding the app's signing keys.</summary>
        public string? JwksUri { get; init; }
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        /// <summary>The declared integration per allowed origin (<c>redirect</c> or <c>embedded</c>).</summary>
        public Dictionary<string, string>? AllowedOriginIntegrations { get; init; }
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
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        public string? Note { get; init; }
        /// <summary>The auto-created test-mode sibling in the environment opposite the primary (development for a production app — the default and common case). Has distinct publishable_key and secret from the primary. Absent when create_development_sibling is false.</summary>
        public PlatformAppWithSecret? SiblingInstance { get; init; }
        /// <summary>Provisioning tags.</summary>
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        /// <summary>The idempotent-create key you sent, if any.</summary>
        public string? ExternalRef { get; init; }
        /// <summary>True on a create that sent an <c>external_ref</c> and got the existing object back (200, no secret).</summary>
        public bool? IdempotentReplay { get; init; }
    }

    public sealed class PlatformAppWithSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Environment { get; init; } = "";
        public string PublishableKey { get; init; } = "";
        /// <summary>The Frontend API host: the default per-app host, or the custom domain once it is active.</summary>
        public string? FrontendApiHost { get; init; }
        /// <summary>The <c>iss</c> the app's session tokens carry. It changes when a custom domain goes active.</summary>
        public string? Issuer { get; init; }
        /// <summary>The OpenID discovery document whose <c>issuer</c> equals <see cref="Issuer"/>.</summary>
        public string? DiscoveryUrl { get; init; }
        /// <summary>The JWKS holding the app's signing keys.</summary>
        public string? JwksUri { get; init; }
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        /// <summary>The declared integration per allowed origin (<c>redirect</c> or <c>embedded</c>).</summary>
        public Dictionary<string, string>? AllowedOriginIntegrations { get; init; }
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
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        public string? Note { get; init; }
        /// <summary>Provisioning tags.</summary>
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        /// <summary>The idempotent-create key you sent, if any.</summary>
        public string? ExternalRef { get; init; }
        /// <summary>True on a create that sent an <c>external_ref</c> and got the existing object back (200, no secret).</summary>
        public bool? IdempotentReplay { get; init; }
    }

    /// <summary>An app (instance) a platform owns. Never carries a secret except on create/rotate/claim.</summary>
    public sealed class PlatformApp
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Environment { get; init; } = "";
        public string PublishableKey { get; init; } = "";
        /// <summary>The Frontend API host: the default per-app host, or the custom domain once it is active.</summary>
        public string? FrontendApiHost { get; init; }
        /// <summary>The <c>iss</c> the app's session tokens carry. It changes when a custom domain goes active.</summary>
        public string? Issuer { get; init; }
        /// <summary>The OpenID discovery document whose <c>issuer</c> equals <see cref="Issuer"/>.</summary>
        public string? DiscoveryUrl { get; init; }
        /// <summary>The JWKS holding the app's signing keys.</summary>
        public string? JwksUri { get; init; }
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        /// <summary>The declared integration per allowed origin (<c>redirect</c> or <c>embedded</c>).</summary>
        public Dictionary<string, string>? AllowedOriginIntegrations { get; init; }
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
        /// <summary>Provisioning tags.</summary>
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        /// <summary>The idempotent-create key you sent, if any.</summary>
        public string? ExternalRef { get; init; }
        /// <summary>True on a create that sent an <c>external_ref</c> and got the existing object back (200, no secret).</summary>
        public bool? IdempotentReplay { get; init; }
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
        public string? Type { get; init; }
        public string? InstanceId { get; init; }
    }

    public sealed class PlatformAppBulkDeleteResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public List<PlatformAppBulkDeleteItem> Results { get; init; } = new List<PlatformAppBulkDeleteItem>();
        /// <summary><c>disable</c> (ids) or <c>hard_delete</c> (a sandbox metadata selection).</summary>
        public string? Mode { get; init; }
        public bool? DryRun { get; init; }
        public List<DryRunChange>? Changes { get; init; }
    }

    public sealed class PlatformAppSecretRotation
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string AppId { get; init; } = "";
        public string Secret { get; init; } = "";
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
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
        /// <summary>Provisioning tags.</summary>
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        /// <summary>The idempotent-create key you sent, if any.</summary>
        public string? ExternalRef { get; init; }
        /// <summary>True on a create that sent an <c>external_ref</c> and got the existing object back (200, no secret).</summary>
        public bool? IdempotentReplay { get; init; }
        public string? Name { get; init; }
    }

    public sealed class PlatformInstanceSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>Never sent by the API; read <see cref="InstanceId"/>.</summary>
        [System.Obsolete("Never sent by the API; read InstanceId.")]
        public string Id { get; init; } = "";
        public string AppId { get; init; } = "";
        public string InstanceId { get; init; } = "";
        /// <summary>The new secret key, revealed exactly once.</summary>
        public string Secret { get; init; } = "";
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        /// <summary>The overlap applied, in seconds.</summary>
        public long OverlapSeconds { get; init; }
        /// <summary>When the prior keys stop working (epoch ms); null on a hard cut.</summary>
        public long? PreviousKeysExpireAt { get; init; }
        public string Note { get; init; } = "";
    }

    /// <summary>The body of <c>POST /v1/platform/apps/{id}/instances/{instanceId}/rotate_secret</c>.</summary>
    public sealed class RotatePlatformSecretBody
    {
        /// <summary>How long the prior keys keep working, in seconds. Null uses the default, 259200 (72 hours); 0 revokes them now; at most 604800 (7 days).</summary>
        public int? OverlapSeconds { get; set; }
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
        /// <summary>The Frontend API host: the default per-app host, or the custom domain once it is active.</summary>
        public string? FrontendApiHost { get; init; }
        /// <summary>The <c>iss</c> the app's session tokens carry. It changes when a custom domain goes active.</summary>
        public string? Issuer { get; init; }
        /// <summary>The OpenID discovery document whose <c>issuer</c> equals <see cref="Issuer"/>.</summary>
        public string? DiscoveryUrl { get; init; }
        /// <summary>The JWKS holding the app's signing keys.</summary>
        public string? JwksUri { get; init; }
        public List<string> AllowedOrigins { get; init; } = new List<string>();
        /// <summary>The declared integration per allowed origin (<c>redirect</c> or <c>embedded</c>).</summary>
        public Dictionary<string, string>? AllowedOriginIntegrations { get; init; }
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

        /// <summary><c>GET /v1/platform/atlas_id_apps?metadata[key]=value</c> — the apps whose provisioning metadata matches every pair.</summary>
        public Task<DataList<AtlasIdApp>> ListAsync(IDictionary<string, object?> metadata, CancellationToken ct = default)
            => Req<DataList<AtlasIdApp>>(HttpVerb.Get, "/v1/platform/atlas_id_apps", query: AtlasProvisioning.MetadataQuery(metadata), cancellationToken: ct);

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

        /// <summary><c>POST /v1/platform/atlas_id_apps/{id}/rotate_secret</c> — Rotate a confidential app's client_secret; the previous one keeps working for 24 hours.</summary>
        public Task<AtlasIdAppWithSecret> RotateSecretAsync(string id, CancellationToken ct = default)
            => Req<AtlasIdAppWithSecret>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(id)}/rotate_secret", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_apps/{id}/rotate_secret</c> — Rotate the client_secret with a chosen overlap (default 86400; 0 revokes the previous one now).</summary>
        public Task<AtlasIdAppWithSecret> RotateSecretAsync(string id, RotateAtlasIdAppSecretBody body, CancellationToken ct = default)
            => Req<AtlasIdAppWithSecret>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(id)}/rotate_secret", body, cancellationToken: ct);

        /// <summary><c>GET /v1/platform/atlas_id_apps/{id}/backchannel_deliveries</c> — The app's back-channel logout delivery log, newest first.</summary>
        public Task<CursorPage<BackchannelLogoutDelivery>> ListBackchannelDeliveriesAsync(string id, int? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<CursorPage<BackchannelLogoutDelivery>>(HttpVerb.Get, $"/v1/platform/atlas_id_apps/{Enc(id)}/backchannel_deliveries",
                query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_apps/{id}/backchannel_logout/test</c> — Send a test back-channel logout to the app now.</summary>
        public Task<BackchannelLogoutTest> TestBackchannelLogoutAsync(string id, CancellationToken ct = default)
            => Req<BackchannelLogoutTest>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(id)}/backchannel_logout/test", cancellationToken: ct);

        /// <summary><c>GET /v1/platform/atlas_id_apps/{id}/preflight</c> — Check one app's setup.</summary>
        public Task<AtlasIdAppPreflight> PreflightAsync(string id, CancellationToken ct = default)
            => Req<AtlasIdAppPreflight>(HttpVerb.Get, $"/v1/platform/atlas_id_apps/{Enc(id)}/preflight", cancellationToken: ct);

        /// <summary><c>GET /v1/platform/atlas_id_apps/{id}/users</c> (an instance secret key is accepted too, see <c>AtlasIdGrants</c>) — Privacy requests: is this person (verified email, exact match) one of the app's users? Anyone else gets an empty list.</summary>
        public Task<ListPage<AtlasIdAppUser>> FindUsersAsync(string id, string email, CancellationToken ct = default)
            => Req<ListPage<AtlasIdAppUser>>(HttpVerb.Get, $"/v1/platform/atlas_id_apps/{Enc(id)}/users", query: Q(("email", email)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/platform/atlas_id_apps/{id}/grants/{grantId}</c> (an instance secret key is accepted too, see <c>AtlasIdGrants</c>) — Revoke one user's grant; the next sign-in shows the consent screen again.</summary>
        public Task<AtlasIdGrantRevocation> RevokeGrantAsync(string id, string grantId, CancellationToken ct = default)
            => Req<AtlasIdGrantRevocation>(HttpVerb.Delete, $"/v1/platform/atlas_id_apps/{Enc(id)}/grants/{Enc(grantId)}", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_apps/{id}/move_origin</c> — Move the app's URLs from one origin to another.</summary>
        public Task<OriginMove> MoveOriginAsync(string id, OriginMoveBody body, CancellationToken ct = default)
            => Req<OriginMove>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(id)}/move_origin", body, cancellationToken: ct);

        /// <summary>
        /// <c>PUT /v1/platform/atlas_id_apps/{id}/managed_by</c> — Hand the app's grant management to an instance of the same
        /// account (<c>managed_via: "adopted"</c>), or take it back with <c>null</c>. That instance's secret key, scoped to
        /// <c>privacy:grants:revoke</c> or <c>atlas_id_apps:*</c>, then lists and revokes the app's grants, so a data-rights tool
        /// never needs the platform key.
        /// </summary>
        public Task<AtlasIdAppManagedBy> SetManagedByAsync(string id, string? instanceId, CancellationToken ct = default)
            => Req<AtlasIdAppManagedBy>(HttpVerb.Put, $"/v1/platform/atlas_id_apps/{Enc(id)}/managed_by", new SetAtlasIdAppManagedByBody { InstanceId = instanceId }, cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_apps/{id}/webhook/rotate_secret</c> — Rotate the app's webhook signing secret (returned once).</summary>
        public Task<AtlasIdAppWithSecret> RotateWebhookSecretAsync(string id, CancellationToken ct = default)
            => Req<AtlasIdAppWithSecret>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(id)}/webhook/rotate_secret", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/platform/atlas_id_apps/{id}/webhook/rotate_secret</c> — Rotate the webhook signing secret with a chosen
        /// overlap: the previous secret signs each delivery too until <see cref="AtlasIdAppWithSecret.WebhookPreviousSecretExpiresAt"/>.
        /// </summary>
        public Task<AtlasIdAppWithSecret> RotateWebhookSecretAsync(string id, RotateAtlasIdAppSecretBody body, CancellationToken ct = default)
            => Req<AtlasIdAppWithSecret>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(id)}/webhook/rotate_secret", body, cancellationToken: ct);
    }

    public sealed class AtlasIdAppPreflight
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "preflight";
        public string AppId { get; init; } = "";
        public bool Ok { get; init; }
        public long CheckedAt { get; init; }
        /// <summary>People who have approved the app.</summary>
        public int UserCount { get; init; }
        public List<PreflightFinding> Findings { get; init; } = new List<PreflightFinding>();
        /// <summary>The page origins the Sign in with Atlas button script ran on in the last 7 days.</summary>
        public List<AtlasIdOriginSeen>? ButtonRenderedOrigins { get; init; }
        /// <summary>How the last 7 days' sign-ins started.</summary>
        [JsonPropertyName("authorize_requests_7d")] public AtlasIdAuthorizeSources? AuthorizeRequests7d { get; init; }
        /// <summary>Origins of sign-ins that came through the no-script fallback link.</summary>
        public List<AtlasIdOriginSeen>? FallbackOrigins { get; init; }
        /// <summary>Whether the app's homepage references the button script (null = could not check).</summary>
        public bool? HomepageIncludesScript { get; init; }
    }

    /// <summary>Body of <c>SetManagedByAsync</c>; a null <c>InstanceId</c> is sent as <c>null</c> and takes the app back.</summary>
    public sealed class SetAtlasIdAppManagedByBody
    {
        [JsonPropertyName("instance_id")] [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public string? InstanceId { get; init; }
    }

    /// <summary><c>PUT /v1/platform/atlas_id_apps/{id}/managed_by</c>: the app after the change, and whether anything changed.</summary>
    public sealed class AtlasIdAppManagedBy
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "atlas_id_app";
        public string Id { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? ManagedByInstanceId { get; init; }
        /// <summary><c>instance_toggle</c> or <c>adopted</c>; null once taken back.</summary>
        public string? ManagedVia { get; init; }
        public long? UpdatedAt { get; init; }
        public bool Changed { get; init; }
    }

    /// <summary>A person who has approved a Sign in with Atlas app.</summary>
    public sealed class AtlasIdAppUser
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "atlas_id_user";
        public bool AtlasIdUser { get; init; }
        /// <summary>The user id as this app sees it (the id_token sub).</summary>
        public string Sub { get; init; } = "";
        public string GrantId { get; init; } = "";
        public List<string> Scopes { get; init; } = new List<string>();
        public long GrantedAt { get; init; }
    }

    public sealed class AtlasIdGrantRevocation
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "atlas_id_grant";
        public string Id { get; init; } = "";
        public string Sub { get; init; } = "";
        public bool Revoked { get; init; }
        public int? TokensRevoked { get; init; }
        public int? BackchannelLogouts { get; init; }
    }

    public sealed class AtlasIdPublisherVerification
    {
        public string RecordName { get; init; } = "";
        public string RecordType { get; init; } = "";
        public string RecordValue { get; init; } = "";
    }

    /// <summary>The account's Sign in with Atlas publisher (name and DNS-verified domain).</summary>
    public sealed class AtlasIdPublisher
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "atlas_id_publisher";
        public string Name { get; init; } = "";
        public string Domain { get; init; } = "";
        public bool Verified { get; init; }
        public long? VerifiedAt { get; init; }
        /// <summary>Publisher-wide consent for verified apps (openid / email / profile only).</summary>
        public bool SkipConsent { get; init; }
        public AtlasIdPublisherVerification? Verification { get; init; }
        public long? LastCheckedAt { get; init; }
        public long? CreatedAt { get; init; }
        public long? UpdatedAt { get; init; }
        /// <summary>Set by verify: whether the TXT record matched.</summary>
        public bool? RecordFound { get; init; }
        /// <summary>False when the singleton has never been set (the GET answers 200, no longer 404).</summary>
        public bool? Configured { get; init; }
    }

    public sealed class AtlasIdPublisherBody
    {
        /// <summary>Shown on consent screens once verified (up to 100 characters).</summary>
        public string Name { get; set; } = "";
        /// <summary>The host your apps' homepage_url lives on, e.g. example.com.</summary>
        public string Domain { get; set; } = "";
        public bool? SkipConsent { get; set; }
    }

    /// <summary>The account's Sign in with Atlas publisher (<c>/v1/platform/atlas_id_publisher</c>).</summary>
    public sealed class PlatformAtlasIdPublisherResource : ResourceBase
    {
        public PlatformAtlasIdPublisherResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>GET /v1/platform/atlas_id_publisher</c> — The publisher (404 until claimed).</summary>
        public Task<AtlasIdPublisher> GetAsync(CancellationToken ct = default)
            => Req<AtlasIdPublisher>(HttpVerb.Get, "/v1/platform/atlas_id_publisher", cancellationToken: ct);

        /// <summary><c>PUT /v1/platform/atlas_id_publisher</c> — Claim a publisher name and domain; returns the TXT record to publish.</summary>
        public Task<AtlasIdPublisher> SetAsync(AtlasIdPublisherBody body, CancellationToken ct = default)
            => Req<AtlasIdPublisher>(HttpVerb.Put, "/v1/platform/atlas_id_publisher", body, cancellationToken: ct);

        /// <summary><c>PATCH /v1/platform/atlas_id_publisher</c> — Turn publisher-wide consent on or off.</summary>
        public Task<AtlasIdPublisher> SetSkipConsentAsync(bool skipConsent, CancellationToken ct = default)
            => Req<AtlasIdPublisher>(HttpVerb.Patch, "/v1/platform/atlas_id_publisher", Body(("skip_consent", skipConsent)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/platform/atlas_id_publisher</c> — Remove the publisher claim.</summary>
        public Task<DeletedObject> DeleteAsync(CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, "/v1/platform/atlas_id_publisher", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_publisher/verify</c> — Look up the TXT record now.</summary>
        public Task<AtlasIdPublisher> VerifyAsync(CancellationToken ct = default)
            => Req<AtlasIdPublisher>(HttpVerb.Post, "/v1/platform/atlas_id_publisher/verify", cancellationToken: ct);
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

        /// <summary><c>GET /v1/platform/apps/{id}/secret_keys?metadata[key]=value</c> — an app's secret keys whose provisioning metadata matches every pair.</summary>
        public Task<CursorPage<PlatformSecretKey>> ListSecretKeysAsync(string id, IDictionary<string, object?> metadata, CancellationToken ct = default)
            => Req<CursorPage<PlatformSecretKey>>(HttpVerb.Get, $"/v1/platform/apps/{Enc(id)}/secret_keys", query: AtlasProvisioning.MetadataQuery(metadata), cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/secret_keys</c> — mint an additional named, taggable secret key for an app (shown once; an <c>ExternalRef</c> makes it idempotent).</summary>
        public Task<CreatedPlatformSecretKey> CreateSecretKeyAsync(string id, CreatePlatformSecretKeyBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<CreatedPlatformSecretKey>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/secret_keys", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/clone_config</c> — copy an app's configuration into its development (or another) instance, secrets excluded.</summary>
        public Task<CloneConfigResult> CloneConfigAsync(string id, CloneConfigBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<CloneConfigResult>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/clone_config", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/platform/apps/bulk_delete</c> — hard-delete every sandbox-tagged app and resource of this platform. <paramref name="metadata"/> must carry
        /// <c>sandbox: true</c> (null = <c>{"sandbox": true}</c>); <paramref name="dryRun"/> lists what would go.
        /// </summary>
        public Task<PlatformAppBulkDeleteResult> SandboxTeardownAsync(IDictionary<string, object?>? metadata = null, bool dryRun = false, CancellationToken ct = default)
            => Req<PlatformAppBulkDeleteResult>(HttpVerb.Post, "/v1/platform/apps/bulk_delete",
                Body(("metadata", metadata ?? new Dictionary<string, object?> { ["sandbox"] = true }), ("dry_run", dryRun ? true : (bool?)null)), cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/instances</c> — Provision an additional instance (e.g.</summary>
        public Task<PlatformAppWithSecret> CreateInstanceAsync(string id, CreatePlatformInstanceBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<PlatformAppWithSecret>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/instances", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/platform/apps/{id}/instances</c> — List every instance under an owned app — its development/production siblings.</summary>
        public Task<CursorPage<PlatformApp>> ListInstancesAsync(string id, CancellationToken ct = default)
            => Req<CursorPage<PlatformApp>>(HttpVerb.Get, $"/v1/platform/apps/{Enc(id)}/instances", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/apps/{id}/instances/{instanceId}/rotate_secret</c> — Mint a new secret key for one instance under an owned app, revealed once; the prior keys stay valid for the default overlap (72 hours).</summary>
        public Task<PlatformInstanceSecret> RotateInstanceSecretAsync(string id, string instanceId, CancellationToken ct = default)
            => Req<PlatformInstanceSecret>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/instances/{Enc(instanceId)}/rotate_secret", cancellationToken: ct);

        /// <summary>
        /// <c>POST /v1/platform/apps/{id}/instances/{instanceId}/rotate_secret</c> — Rotate one instance's secret key with a chosen
        /// overlap: the prior keys stay valid for <c>OverlapSeconds</c> (default 259200, at most 604800; 0 revokes them now).
        /// </summary>
        public Task<PlatformInstanceSecret> RotateInstanceSecretAsync(string id, string instanceId, RotatePlatformSecretBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<PlatformInstanceSecret>(HttpVerb.Post, $"/v1/platform/apps/{Enc(id)}/instances/{Enc(instanceId)}/rotate_secret", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

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
            AtlasIdPublisher = new PlatformAtlasIdPublisherResource(transport);
            Keys = new PlatformKeysResource(transport);
            Audit = new PlatformAuditResource(transport);
        }

        /// <summary><c>/v1/platform/keys</c> — every credential on the account, and minting / revoking them.</summary>
        public PlatformKeysResource Keys { get; }

        /// <summary>The account audit log (<c>/v1/platform/audit_logs</c>) and its streams.</summary>
        public PlatformAuditResource Audit { get; }

        /// <summary><c>GET /v1/platform/whoami</c> — introspect the platform key or token this client uses.</summary>
        public Task<Whoami> WhoamiAsync(CancellationToken ct = default)
            => Req<Whoami>(HttpVerb.Get, "/v1/platform/whoami", cancellationToken: ct);

        /// <summary>The account's Sign in with Atlas publisher (<c>/v1/platform/atlas_id_publisher</c>).</summary>
        public PlatformAtlasIdPublisherResource AtlasIdPublisher { get; }

        /// <summary><c>GET /v1/platform/config_changelog</c> — Config change history for the account's Sign in with Atlas apps.</summary>
        public Task<CursorPage<ConfigChangelogEntry>> ConfigChangelogAsync(ListConfigChangelogParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<ConfigChangelogEntry>>(HttpVerb.Get, "/v1/platform/config_changelog", query: (@params ?? new ListConfigChangelogParams()).ToQuery(), cancellationToken: ct);

        /// <summary>Design images owned by the platform account (<c>/v1/platform/design/assets</c>).</summary>
        public PlatformDesignAssetsResource DesignAssets { get; }

        /// <summary>Sign in with Atlas apps (<c>/v1/platform/atlas_id_apps</c>).</summary>
        public PlatformAtlasIdAppsResource AtlasIdApps { get; }

        /// <summary>Apps (instances) owned by the platform (<c>/v1/platform/apps</c>).</summary>
        public PlatformAppsResource Apps { get; }
    }

    // ---- provisioning additions ----------------------------------------------------

    /// <summary>The body of <c>POST /v1/platform/apps/{id}/secret_keys</c>.</summary>
    public sealed class CreatePlatformSecretKeyBody
    {
        public string? Name { get; set; }
        /// <summary>Any instance of the same app (default the app's own).</summary>
        public string? InstanceId { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }
        public string? ExternalRef { get; set; }
    }

    /// <summary>A minted secret key. <c>Secret</c> is present only on a fresh create (never on an idempotent replay).</summary>
    public sealed class CreatedPlatformSecretKey
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string? Name { get; init; }
        public string Prefix { get; init; } = "";
        public long CreatedAt { get; init; }
        public long? ExpiresAt { get; init; }
        public string AppId { get; init; } = "";
        public string InstanceId { get; init; } = "";
        public string? Secret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        public string? SealedSecret { get; init; }
        public string? Note { get; init; }
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        public string? ExternalRef { get; init; }
        public bool? IdempotentReplay { get; init; }
    }

    /// <summary>The body of <c>POST /v1/platform/apps/{id}/clone_config</c>.</summary>
    public sealed class CloneConfigBody
    {
        /// <summary><c>development</c> (default) or <c>production</c>.</summary>
        public string? ToEnvironment { get; set; }
        public string? TargetInstanceId { get; set; }
        /// <summary><c>auth_config</c>, <c>allowed_origins</c>, <c>roles</c>, <c>redirect_urls</c> (default auth_config, roles, redirect_urls).</summary>
        public List<string>? Sections { get; set; }
        public bool? IncludeOrigins { get; set; }
        public bool? DryRun { get; set; }
    }

    /// <summary>A path that was not copied, and why (<c>secret</c> or <c>instance_specific</c>).</summary>
    public sealed class CloneConfigExcluded
    {
        public string Path { get; init; } = "";
        public string Reason { get; init; } = "";
    }

    /// <summary>The answer to a config clone.</summary>
    public sealed class CloneConfigResult
    {
        /// <summary><c>clone_config_result</c>, or <c>dry_run</c>.</summary>
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string SourceInstanceId { get; init; } = "";
        public string? TargetInstanceId { get; init; }
        public string? ToEnvironment { get; init; }
        public bool CreatedTarget { get; init; }
        public List<string>? Sections { get; init; }
        public List<CloneConfigExcluded>? Excluded { get; init; }
        public List<string>? NeverCopied { get; init; }
        public List<DryRunChange>? Changes { get; init; }
        public PlatformApp? Target { get; init; }
        /// <summary>When a new instance was created for the clone, its secret key, shown once.</summary>
        public string? Secret { get; init; }
        /// <summary>With a secret recipient: the secret sealed to it (then the plaintext field is absent).</summary>
        public string? SealedSecret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        public string? Note { get; init; }
    }

    // ---- platform credentials & account audit --------------------------------------

    /// <summary>The body of <c>POST /v1/platform/keys</c>. Scopes are a subset of the caller's; per-app grant scopes <c>atlas_id_apps:&lt;app id&gt;:grants:read|write</c> are allowed.</summary>
    public sealed class CreatePlatformKeyBody
    {
        public List<string> Scopes { get; set; } = new List<string>();
        public string? Name { get; set; }
        public List<string>? ApplicationIds { get; set; }
        /// <summary>Unix ms or ISO 8601, at most one year away.</summary>
        public object? ExpiresAt { get; set; }
        public List<string>? IpAllowlist { get; set; }
        /// <summary>Browser origins a request carrying an <c>Origin</c> header must come from.</summary>
        public List<string>? AllowedOrigins { get; set; }
        public string? Owner { get; set; }
        /// <summary><c>atlas</c> (default) or <c>legacy</c>.</summary>
        public string? KeyFormat { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/platform/keys/{id}</c>. Use <see cref="Set"/> to send an explicit <c>null</c> (name, owner, expires_at).</summary>
    public sealed class UpdatePlatformKeyBody
    {
        public string? Name { get; set; }
        public string? Owner { get; set; }
        public object? ExpiresAt { get; set; }
        public List<string>? AllowedOrigins { get; set; }

        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        public UpdatePlatformKeyBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>A platform key; <c>Secret</c> / <c>SealedSecret</c> are present once.</summary>
    public sealed class PlatformApiKey : StandardKey
    {
        public string? Secret { get; init; }
        public string? SealedSecret { get; init; }
        public List<string>? ApplicationIds { get; init; }
        public List<string>? AllowedOrigins { get; init; }
        public string? Note { get; init; }
    }

    /// <summary>Acknowledges a revocation.</summary>
    public sealed class PlatformKeyRevoked
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Kind { get; init; } = "";
        public bool Revoked { get; init; }
    }

    /// <summary><c>/v1/platform/keys</c>.</summary>
    public sealed class PlatformKeysResource : ResourceBase
    {
        public PlatformKeysResource(AtlasTransport t) : base(t) { }

        /// <summary>
        /// <c>GET /v1/platform/keys</c> — every credential on the account, revoked ones included. <paramref name="kind"/> is <c>secret_key</c>, <c>platform_key</c>,
        /// <c>account_key</c>, <c>webhook_secret</c>, <c>partner_link</c> or <c>access_token</c>; <paramref name="status"/> is <c>active</c>, <c>expiring</c>, <c>expired</c> or <c>revoked</c>.
        /// </summary>
        public Task<ListPage<StandardKey>> ListAsync(string? kind = null, string? status = null, string? instanceId = null, CancellationToken ct = default)
            => Req<ListPage<StandardKey>>(HttpVerb.Get, "/v1/platform/keys", query: Q(("kind", kind), ("status", status), ("instance_id", instanceId)), cancellationToken: ct);

        /// <summary><c>POST /v1/platform/keys</c> — mint a platform key; the secret is in this response only.</summary>
        public Task<PlatformApiKey> CreateAsync(CreatePlatformKeyBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<PlatformApiKey>(HttpVerb.Post, "/v1/platform/keys", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PATCH /v1/platform/keys/{id}</c> — change a key's name, owner or expiry.</summary>
        public Task<PlatformApiKey> UpdateAsync(string id, UpdatePlatformKeyBody body, CancellationToken ct = default)
            => Req<PlatformApiKey>(HttpVerb.Patch, $"/v1/platform/keys/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>POST /v1/platform/keys/{id}/revoke</c> — revoke a key at once.</summary>
        public Task<PlatformKeyRevoked> RevokeAsync(string id, CancellationToken ct = default)
            => Req<PlatformKeyRevoked>(HttpVerb.Post, $"/v1/platform/keys/{Enc(id)}/revoke", cancellationToken: ct);
    }

    public sealed class AccountAuditActor
    {
        public string Type { get; init; } = "";
        public string? Id { get; init; }
    }

    public sealed class AccountAuditTarget
    {
        public string? Type { get; init; }
        public string? Id { get; init; }
    }

    /// <summary>One entry of the account audit log.</summary>
    public sealed class AccountAuditRecord
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string AccountId { get; init; } = "";
        public AccountAuditActor? Actor { get; init; }
        public string Action { get; init; } = "";
        public AccountAuditTarget? Target { get; init; }
        public string? InstanceId { get; init; }
        public string? Ip { get; init; }
        public string? UserAgent { get; init; }
        public Dictionary<string, JsonElement>? Metadata { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>Forwards the account audit log to a URL.</summary>
    public sealed class AccountAuditStream
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Url { get; init; } = "";
        /// <summary><c>atlas</c> or <c>cloudevents</c>.</summary>
        public string Format { get; init; } = "";
        public bool Active { get; init; }
        public bool HasSecret { get; init; }
        public string? Cursor { get; init; }
        public int ConsecutiveFailures { get; init; }
        public long? FailingSince { get; init; }
        public long? NextAttemptAt { get; init; }
        public long? LastDeliveredAt { get; init; }
        public string? LastError { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>Present once, on create (or sealed).</summary>
        public string? Secret { get; init; }
        /// <summary>With a secret recipient: the client secret sealed to it.</summary>
        public string? SealedClientSecret { get; init; }
        /// <summary>With a secret recipient: the webhook secret sealed to it.</summary>
        public string? SealedWebhookSecret { get; init; }
        /// <summary>Names the fields that arrived sealed.</summary>
        public SecretsSealed? SecretsSealed { get; init; }
        public string? SealedSecret { get; init; }
    }

    /// <summary>The body of <c>POST /v1/platform/audit_streams</c>.</summary>
    public sealed class CreateAccountAuditStreamBody
    {
        public string Url { get; set; } = "";
        /// <summary><c>atlas</c> (default) or <c>cloudevents</c>.</summary>
        public string? Format { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/platform/audit_streams/{id}</c>.</summary>
    public sealed class UpdateAccountAuditStreamBody
    {
        public string? Url { get; set; }
        public string? Format { get; set; }
        public bool? Active { get; set; }
    }

    /// <summary>The answer to a test send.</summary>
    public sealed class AccountAuditStreamTest
    {
        public string Id { get; init; } = "";
        public bool Ok { get; init; }
        public int? Status { get; init; }
        public string? Error { get; init; }
    }

    /// <summary>The account audit log and its streams.</summary>
    public sealed class PlatformAuditResource : ResourceBase
    {
        public PlatformAuditResource(AtlasTransport t) : base(t)
        {
            Streams = new PlatformAuditStreamsResource(t);
        }

        /// <summary><c>/v1/platform/audit_streams</c>.</summary>
        public PlatformAuditStreamsResource Streams { get; }

        /// <summary><c>GET /v1/platform/audit_logs</c> — one cursor page of the account audit log.</summary>
        public Task<CursorPage<AccountAuditRecord>> ListAsync(int? limit = null, string? startingAfter = null, string? action = null, CancellationToken ct = default)
            => Req<CursorPage<AccountAuditRecord>>(HttpVerb.Get, "/v1/platform/audit_logs", query: Q(("limit", limit), ("starting_after", startingAfter), ("action", action)), cancellationToken: ct);
    }

    /// <summary><c>/v1/platform/audit_streams</c>.</summary>
    public sealed class PlatformAuditStreamsResource : ResourceBase
    {
        public PlatformAuditStreamsResource(AtlasTransport t) : base(t) { }

        public Task<ListPage<AccountAuditStream>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<AccountAuditStream>>(HttpVerb.Get, "/v1/platform/audit_streams", cancellationToken: ct);

        /// <summary>Add a stream; its signing secret is in this response only.</summary>
        public Task<AccountAuditStream> CreateAsync(CreateAccountAuditStreamBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<AccountAuditStream>(HttpVerb.Post, "/v1/platform/audit_streams", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<AccountAuditStream> UpdateAsync(string id, UpdateAccountAuditStreamBody body, CancellationToken ct = default)
            => Req<AccountAuditStream>(HttpVerb.Patch, $"/v1/platform/audit_streams/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/platform/audit_streams/{Enc(id)}", cancellationToken: ct);

        /// <summary>Send one synthetic event.</summary>
        public Task<AccountAuditStreamTest> TestAsync(string id, CancellationToken ct = default)
            => Req<AccountAuditStreamTest>(HttpVerb.Post, $"/v1/platform/audit_streams/{Enc(id)}/test", cancellationToken: ct);
    }
}
