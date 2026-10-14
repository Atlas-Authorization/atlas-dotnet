using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    /// <summary>A structured sign-in page design: OVERRIDES on top of a preset (every field optional; unset fields come from the preset, then the defaults). Unknown keys are refused. Colours are #RRGGBB; images are uploaded design asset ids (dasset_…), never URLs; fonts are ids from GET /v1/design/catalog.…</summary>
    public sealed class AtlasDesign
    {
        public long? Version { get; set; }
        public string? Preset { get; set; }
        [JsonPropertyName("colorScheme")] public string? ColorScheme { get; set; }
        public AtlasDesignPalette? Palette { get; set; }
        [JsonPropertyName("paletteDark")] public AtlasDesignPalette? PaletteDark { get; set; }
        public AtlasDesignTypography? Typography { get; set; }
        public AtlasDesignShape? Shape { get; set; }
        public AtlasDesignElevation? Elevation { get; set; }
        public AtlasDesignSpacing? Spacing { get; set; }
        public AtlasDesignCard? Card { get; set; }
        public AtlasDesignLayout? Layout { get; set; }
        public AtlasDesignButtons? Buttons { get; set; }
        public AtlasDesignInputs? Inputs { get; set; }
        public AtlasDesignBackground? Background { get; set; }
        [JsonPropertyName("backgroundDark")] public AtlasDesignBackground? BackgroundDark { get; set; }
        public AtlasDesignLogo? Logo { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesign Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignPalette
    {
        public string? Primary { get; set; }
        [JsonPropertyName("primaryInk")] public string? PrimaryInk { get; set; }
        public string? Accent { get; set; }
        public string? Background { get; set; }
        public string? Surface { get; set; }
        public string? Text { get; set; }
        public string? Muted { get; set; }
        public string? Border { get; set; }
        public string? Danger { get; set; }
        public string? Success { get; set; }
        [JsonPropertyName("inputBackground")] public string? InputBackground { get; set; }
        [JsonPropertyName("inputText")] public string? InputText { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignPalette Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignTypography
    {
        public string? Font { get; set; }
        [JsonPropertyName("headingFont")] public string? HeadingFont { get; set; }
        [JsonPropertyName("baseSize")] public long? BaseSize { get; set; }
        [JsonPropertyName("headingWeight")] public long? HeadingWeight { get; set; }
        [JsonPropertyName("bodyWeight")] public long? BodyWeight { get; set; }
        [JsonPropertyName("letterCase")] public string? LetterCase { get; set; }
        public string? Scale { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignTypography Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignShape
    {
        public long? Radius { get; set; }
        [JsonPropertyName("controlRadius")] public long? ControlRadius { get; set; }
        [JsonPropertyName("borderWidth")] public long? BorderWidth { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignShape Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignElevation
    {
        [JsonPropertyName("cardShadow")] public string? CardShadow { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignElevation Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignSpacing
    {
        public string? Density { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignSpacing Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignCard
    {
        public string? Style { get; set; }
        public long? Blur { get; set; }
        public string? Width { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignCard Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignLayout
    {
        public string? Position { get; set; }
        [JsonPropertyName("splitImageSide")] public string? SplitImageSide { get; set; }
        [JsonPropertyName("logoPlacement")] public string? LogoPlacement { get; set; }
        [JsonPropertyName("logoSize")] public string? LogoSize { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignLayout Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignButtons
    {
        public string? Style { get; set; }
        [JsonPropertyName("socialLayout")] public string? SocialLayout { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignButtons Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignInputs
    {
        public string? Style { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignInputs Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignBackgroundGradientStops
    {
        public string? Color { get; set; }
        public double? At { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignBackgroundGradientStops Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignBackgroundGradient
    {
        public string? Type { get; set; }
        public long? Angle { get; set; }
        public List<AtlasDesignBackgroundGradientStops>? Stops { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignBackgroundGradient Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignBackgroundImageOverlay
    {
        public string? Color { get; set; }
        public double? Opacity { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignBackgroundImageOverlay Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignBackgroundImage
    {
        [JsonPropertyName("assetId")] public string? AssetId { get; set; }
        public string? Fit { get; set; }
        public string? Position { get; set; }
        public AtlasDesignBackgroundImageOverlay? Overlay { get; set; }
        public long? Blur { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignBackgroundImage Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignBackground
    {
        public string? Color { get; set; }
        public AtlasDesignBackgroundGradient? Gradient { get; set; }
        public AtlasDesignBackgroundImage? Image { get; set; }
        public string? Pattern { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignBackground Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class AtlasDesignLogo
    {
        [JsonPropertyName("assetId")] public string? AssetId { get; set; }
        [JsonPropertyName("darkAssetId")] public string? DarkAssetId { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AtlasDesignLogo Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class DesignBody
    {
        public AtlasDesign? Design { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public DesignBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    /// <summary>An uploaded design image. Use asset_id in a design (logo.assetId, background.image.assetId, ...). Stored re-encoded as WebP, scaled to fit 2560 px (1024 px for a logo).</summary>
    public sealed class DesignAsset
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>dasset_...</summary>
        public string AssetId { get; init; } = "";
        public string Kind { get; init; } = "";
        /// <summary>Where Atlas serves the stored image.</summary>
        public string Url { get; init; } = "";
        public long Width { get; init; }
        public long Height { get; init; }
        /// <summary>#rrggbb for an opaque image, else null.</summary>
        public string? DominantColor { get; init; }
        /// <summary>Size of the stored image.</summary>
        public long Bytes { get; init; }
        public string CreatedAt { get; init; } = "";
    }

    public sealed class DeletedDesignAsset
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string AssetId { get; init; } = "";
        public bool Deleted { get; init; }
    }

    public sealed class DesignSettings
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>Organization designs only.</summary>
        public string? OrganizationId { get; init; }
        /// <summary>The stored overrides ({} when nothing is stored).</summary>
        public AtlasDesign? Design { get; init; }
        public ResolvedDesign? Resolved { get; init; }
        public List<DesignWarning> Warnings { get; init; } = new List<DesignWarning>();
        /// <summary>True when nothing is stored and the page is drawn from the older branding settings.</summary>
        public bool Legacy { get; init; }
        /// <summary>When legacy is true: the design derived from the older branding settings (a good starting point for an editor). Null otherwise.</summary>
        public AtlasDesign? LegacyDesign { get; init; }
    }

    /// <summary>The fully-resolved design every surface renders (defaults ← preset ← overrides): the same shape as AtlasDesign with every field present.</summary>
    public sealed class ResolvedDesign
    {
        public long Version { get; init; }
        public string Preset { get; init; } = "";
        [JsonPropertyName("colorScheme")] public string ColorScheme { get; init; } = "";
        /// <summary>Every palette slot, as #rrggbb.</summary>
        public AtlasDesignPalette? Palette { get; init; }
        /// <summary>Every palette slot, as #rrggbb.</summary>
        [JsonPropertyName("paletteDark")] public AtlasDesignPalette? PaletteDark { get; init; }
        public AtlasDesignTypography? Typography { get; init; }
        public AtlasDesignShape? Shape { get; init; }
        public AtlasDesignElevation? Elevation { get; init; }
        public AtlasDesignSpacing? Spacing { get; init; }
        public AtlasDesignCard? Card { get; init; }
        public AtlasDesignLayout? Layout { get; init; }
        public AtlasDesignButtons? Buttons { get; init; }
        public AtlasDesignInputs? Inputs { get; init; }
        public AtlasDesignBackground? Background { get; init; }
        [JsonPropertyName("backgroundDark")] public AtlasDesignBackground? BackgroundDark { get; init; }
        public AtlasDesignLogo? Logo { get; init; }
    }

    /// <summary>The public design-system catalog: every preset (fully resolved), the curated fonts, and the documented CSS variables.</summary>
    public sealed class DesignCatalog
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "design_catalog";
        public long Version { get; init; }
        public List<DesignCatalogPreset> Presets { get; init; } = new List<DesignCatalogPreset>();
        public List<DesignCatalogFont> Fonts { get; init; } = new List<DesignCatalogFont>();
        public List<DesignCssVariable> CssVariables { get; init; } = new List<DesignCssVariable>();
    }

    /// <summary>One preset: its id (use it as <c>AtlasDesign.Preset</c>) and the design it resolves to.</summary>
    public sealed class DesignCatalogPreset
    {
        public string Id { get; init; } = "";
        public string Label { get; init; } = "";
        public string Description { get; init; } = "";
        public ResolvedDesign? Resolved { get; init; }
    }

    /// <summary>A curated font. <c>Stylesheet</c> is a path on the API origin, or null for system fonts.</summary>
    public sealed class DesignCatalogFont
    {
        public string Id { get; init; } = "";
        public string Label { get; init; } = "";
        public string Family { get; init; } = "";
        public string Category { get; init; } = "";
        public bool WebFont { get; init; }
        public string? Stylesheet { get; init; }
    }

    /// <summary>A documented <c>--atlas-*</c> CSS custom property.</summary>
    public sealed class DesignCssVariable
    {
        public string Name { get; init; } = "";
        public string Type { get; init; } = "";
        [JsonPropertyName("perMode")] public bool? PerMode { get; init; }
        public string Description { get; init; } = "";
    }

    /// <summary>A contrast warning (advice, never a refusal): a text/background pair below WCAG AA 4.5:1.</summary>
    public sealed class DesignWarning
    {
        public string Code { get; init; } = "";
        public string Mode { get; init; } = "";
        public List<string> Pair { get; init; } = new List<string>();
        public double Ratio { get; init; }
        public double Required { get; init; }
        /// <summary>The setting to change, e.g. palette.primaryInk.</summary>
        public string Path { get; init; } = "";
        public string Message { get; init; } = "";
    }

    public sealed class DesignPreview
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>The selector the CSS is scoped to: ".atlas-root".</summary>
        public string Scope { get; init; } = "";
        /// <summary>CSS custom properties (--atlas-*) for the scope, including light/dark blocks.</summary>
        public string Css { get; init; } = "";
        /// <summary>Attributes to set on the scoped element (data-atlas-layout, data-atlas-card, …).</summary>
        public Dictionary<string, string> DataAttributes { get; init; } = new Dictionary<string, string>();
        /// <summary>Absolute URLs of the self-hosted font stylesheets to link.</summary>
        public List<string> FontStylesheets { get; init; } = new List<string>();
        public ResolvedDesign? Resolved { get; init; }
        public List<DesignWarning> Warnings { get; init; } = new List<DesignWarning>();
        /// <summary>The sign-in page rendered with the draft design (inert: for a sandboxed preview frame).</summary>
        public string Html { get; init; } = "";
    }

    /// <summary>Sign-in page design: the instance design, per-organization overrides, previews, uploaded images and the public design catalog.</summary>
    public sealed class DesignResource : ResourceBase
    {
        public DesignResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/design/catalog</c> — The public design catalog: presets (each fully resolved), the curated fonts, and the <c>--atlas-*</c> CSS variables.</summary>
        public Task<DesignCatalog> CatalogAsync(CancellationToken ct = default)
            => Req<DesignCatalog>(HttpVerb.Get, "/v1/design/catalog", cancellationToken: ct);

        /// <summary><c>POST /v1/design/assets</c> — Upload a design image (a background or a logo) for this instance.</summary>
        public Task<DesignAsset> UploadAssetAsync(byte[] data, string contentType, string? kind = null, CancellationToken ct = default)
            => ReqBytes<DesignAsset>(HttpVerb.Post, "/v1/design/assets", data, contentType, Q(("kind", kind)), ct);

        /// <summary><c>GET /v1/design/assets</c> — List this instance's uploaded design images, newest first.</summary>
        public Task<DataList<DesignAsset>> ListAssetsAsync(CancellationToken ct = default)
            => Req<DataList<DesignAsset>>(HttpVerb.Get, "/v1/design/assets", cancellationToken: ct);

        /// <summary><c>DELETE /v1/design/assets/{id}</c> — Delete one of this instance's design images.</summary>
        public Task<DeletedDesignAsset> DeleteAssetAsync(string id, CancellationToken ct = default)
            => Req<DeletedDesignAsset>(HttpVerb.Delete, $"/v1/design/assets/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>GET /v1/instance/design</c> — Read the instance's sign-in page design: stored overrides, the resolved design, contrast warnings and the legacy flag.</summary>
        public Task<DesignSettings> GetAsync(CancellationToken ct = default)
            => Req<DesignSettings>(HttpVerb.Get, "/v1/instance/design", cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/design</c> — Replace the instance design ({ design: AtlasDesign }).</summary>
        public Task<DesignSettings> ReplaceAsync(DesignBody body, CancellationToken ct = default)
            => Req<DesignSettings>(HttpVerb.Put, "/v1/instance/design", body, cancellationToken: ct);

        /// <summary>Shorthand: wraps <paramref name="design"/> in a <see cref="DesignBody"/>.</summary>
        public Task<DesignSettings> ReplaceAsync(AtlasDesign design, CancellationToken ct = default)
            => ReplaceAsync(new DesignBody { Design = design }, ct);

        /// <summary><c>PATCH /v1/instance/design</c> — Change part of the instance design ({ design: AtlasDesignPatch }): nested settings merge, null removes a setting.</summary>
        public Task<DesignSettings> UpdateAsync(DesignBody body, CancellationToken ct = default)
            => Req<DesignSettings>(HttpVerb.Patch, "/v1/instance/design", body, cancellationToken: ct);

        /// <summary>Shorthand: wraps <paramref name="design"/> in a <see cref="DesignBody"/>.</summary>
        public Task<DesignSettings> UpdateAsync(AtlasDesign design, CancellationToken ct = default)
            => UpdateAsync(new DesignBody { Design = design }, ct);

        /// <summary><c>POST /v1/instance/design/preview</c> — Compile an unsaved design: CSS variables scoped to .atlas-root, data attributes, font stylesheet URLs, warnings, the resolved design and the rendered sign-in page.</summary>
        public Task<DesignPreview> PreviewAsync(DesignBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<DesignPreview>(HttpVerb.Post, "/v1/instance/design/preview", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Shorthand: wraps <paramref name="design"/> in a <see cref="DesignBody"/>.</summary>
        public Task<DesignPreview> PreviewAsync(AtlasDesign design, string? idempotencyKey = null, CancellationToken ct = default)
            => PreviewAsync(new DesignBody { Design = design }, idempotencyKey, ct);

        /// <summary><c>GET /v1/organizations/{id}/design</c> — Read an organization's design override and the result of layering it over the instance design.</summary>
        public Task<DesignSettings> GetForOrganizationAsync(string id, CancellationToken ct = default)
            => Req<DesignSettings>(HttpVerb.Get, $"/v1/organizations/{Enc(id)}/design", cancellationToken: ct);

        /// <summary><c>PUT /v1/organizations/{id}/design</c> — Replace an organization's design override ({ design: AtlasDesign }); { design: {} } removes it.</summary>
        public Task<DesignSettings> ReplaceForOrganizationAsync(string id, DesignBody body, CancellationToken ct = default)
            => Req<DesignSettings>(HttpVerb.Put, $"/v1/organizations/{Enc(id)}/design", body, cancellationToken: ct);

        /// <summary>Shorthand: wraps <paramref name="design"/> in a <see cref="DesignBody"/>.</summary>
        public Task<DesignSettings> ReplaceForOrganizationAsync(string id, AtlasDesign design, CancellationToken ct = default)
            => ReplaceForOrganizationAsync(id, new DesignBody { Design = design }, ct);

        /// <summary><c>PATCH /v1/organizations/{id}/design</c> — Change part of an organization's design override; null removes a setting.</summary>
        public Task<DesignSettings> UpdateForOrganizationAsync(string id, DesignBody body, CancellationToken ct = default)
            => Req<DesignSettings>(HttpVerb.Patch, $"/v1/organizations/{Enc(id)}/design", body, cancellationToken: ct);

        /// <summary>Shorthand: wraps <paramref name="design"/> in a <see cref="DesignBody"/>.</summary>
        public Task<DesignSettings> UpdateForOrganizationAsync(string id, AtlasDesign design, CancellationToken ct = default)
            => UpdateForOrganizationAsync(id, new DesignBody { Design = design }, ct);
    }
}
