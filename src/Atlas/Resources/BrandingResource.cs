using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    // The whole branding bag is camelCase on the wire, unlike the rest of the
    // BAPI — so every field carries an explicit [JsonPropertyName] that overrides
    // the SDK's default snake_case policy.

    public sealed class HostedBackground
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("color")] public string? Color { get; set; }
        [JsonPropertyName("from")] public string? From { get; set; }
        [JsonPropertyName("to")] public string? To { get; set; }
        [JsonPropertyName("angle")] public double? Angle { get; set; }
        [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
    }

    public sealed class BrandingProviders
    {
        [JsonPropertyName("order")] public List<string>? Order { get; set; }
        [JsonPropertyName("hidden")] public List<string>? Hidden { get; set; }
    }

    public sealed class BrandingCopy
    {
        [JsonPropertyName("headline")] public string? Headline { get; set; }
        [JsonPropertyName("subheadline")] public string? Subheadline { get; set; }
        [JsonPropertyName("footer")] public string? Footer { get; set; }
    }

    public sealed class BrandingCard
    {
        [JsonPropertyName("width")] public string? Width { get; set; }
        [JsonPropertyName("align")] public string? Align { get; set; }
        [JsonPropertyName("logoSize")] public string? LogoSize { get; set; }
    }

    public sealed class BrandingTypography
    {
        [JsonPropertyName("headingSize")] public string? HeadingSize { get; set; }
        [JsonPropertyName("headingWeight")] public string? HeadingWeight { get; set; }
    }

    public sealed class BrandingInteraction
    {
        [JsonPropertyName("buttonStyle")] public string? ButtonStyle { get; set; }
        [JsonPropertyName("buttonEffect")] public string? ButtonEffect { get; set; }
        [JsonPropertyName("shadow")] public string? Shadow { get; set; }
        [JsonPropertyName("density")] public string? Density { get; set; }
        [JsonPropertyName("inputStyle")] public string? InputStyle { get; set; }
        [JsonPropertyName("animations")] public bool? Animations { get; set; }
        [JsonPropertyName("motionSpeed")] public string? MotionSpeed { get; set; }
        [JsonPropertyName("motionEasing")] public string? MotionEasing { get; set; }
        [JsonPropertyName("loading")] public string? Loading { get; set; }
    }

    public sealed class BrandingSocialButtons
    {
        [JsonPropertyName("variant")] public string? Variant { get; set; }
        [JsonPropertyName("size")] public string? Size { get; set; }
        [JsonPropertyName("layout")] public string? Layout { get; set; }
        [JsonPropertyName("labels")] public Dictionary<string, string>? Labels { get; set; }
        [JsonPropertyName("search")] public string? Search { get; set; }
        [JsonPropertyName("styles")] public Dictionary<string, object?>? Styles { get; set; }
    }

    public sealed class BrandingLegal
    {
        [JsonPropertyName("termsUrl")] public string? TermsUrl { get; set; }
        [JsonPropertyName("privacyUrl")] public string? PrivacyUrl { get; set; }
        [JsonPropertyName("required")] public bool? Required { get; set; }
    }

    public sealed class BrandingSignUpField
    {
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("required")] public bool? Required { get; set; }
        [JsonPropertyName("placeholder")] public string? Placeholder { get; set; }
        [JsonPropertyName("options")] public List<string>? Options { get; set; }
    }

    /// <summary>
    /// The hosted-page / email appearance bag. Every field is optional and
    /// customer-authored; the render paths only ever read the SANITIZED resolution
    /// of it. Used both as the read echo (inside <see cref="BrandingResponse"/>)
    /// and as the partial update / preview body.
    /// </summary>
    public class Branding
    {
        [JsonPropertyName("applicationName")] public string? ApplicationName { get; set; }
        [JsonPropertyName("logoUrl")] public string? LogoUrl { get; set; }
        [JsonPropertyName("colorPrimary")] public string? ColorPrimary { get; set; }
        [JsonPropertyName("colorBackground")] public string? ColorBackground { get; set; }
        [JsonPropertyName("colorText")] public string? ColorText { get; set; }
        [JsonPropertyName("supportEmail")] public string? SupportEmail { get; set; }

        [JsonPropertyName("colorAccent")] public string? ColorAccent { get; set; }
        [JsonPropertyName("colorCard")] public string? ColorCard { get; set; }
        [JsonPropertyName("colorBorder")] public string? ColorBorder { get; set; }
        [JsonPropertyName("borderRadius")] public string? BorderRadius { get; set; }
        [JsonPropertyName("theme")] public string? Theme { get; set; }
        [JsonPropertyName("fontFamily")] public string? FontFamily { get; set; }
        [JsonPropertyName("fontUrl")] public string? FontUrl { get; set; }
        [JsonPropertyName("layout")] public string? Layout { get; set; }
        [JsonPropertyName("background")] public HostedBackground? Background { get; set; }
        [JsonPropertyName("providers")] public BrandingProviders? Providers { get; set; }
        [JsonPropertyName("copy")] public BrandingCopy? Copy { get; set; }
        [JsonPropertyName("socialButtons")] public BrandingSocialButtons? SocialButtons { get; set; }
        [JsonPropertyName("codeInput")] public string? CodeInput { get; set; }
        [JsonPropertyName("card")] public BrandingCard? Card { get; set; }
        [JsonPropertyName("typography")] public BrandingTypography? Typography { get; set; }
        [JsonPropertyName("buttonShape")] public string? ButtonShape { get; set; }
        [JsonPropertyName("cardBorder")] public string? CardBorder { get; set; }
        [JsonPropertyName("inputSize")] public string? InputSize { get; set; }
        [JsonPropertyName("interaction")] public BrandingInteraction? Interaction { get; set; }
        [JsonPropertyName("backgroundPattern")] public string? BackgroundPattern { get; set; }
        [JsonPropertyName("cardStyle")] public string? CardStyle { get; set; }
        [JsonPropertyName("textScale")] public string? TextScale { get; set; }
        [JsonPropertyName("socialPlacement")] public string? SocialPlacement { get; set; }
        [JsonPropertyName("sectionOrder")] public List<string>? SectionOrder { get; set; }
        [JsonPropertyName("legal")] public BrandingLegal? Legal { get; set; }
        [JsonPropertyName("signUpFields")] public List<BrandingSignUpField>? SignUpFields { get; set; }
        [JsonPropertyName("customCss")] public string? CustomCss { get; set; }
        [JsonPropertyName("customHtmlHeader")] public string? CustomHtmlHeader { get; set; }
        [JsonPropertyName("customHtmlFooter")] public string? CustomHtmlFooter { get; set; }
    }

    /// <summary>What the renderers actually use — everything already escaped or dropped.</summary>
    public sealed class ResolvedBranding
    {
        [JsonPropertyName("applicationName")] public string ApplicationName { get; init; } = "";
        [JsonPropertyName("escapedName")] public string EscapedName { get; init; } = "";
        [JsonPropertyName("logoUrl")] public string? LogoUrl { get; init; }
        [JsonPropertyName("colorPrimary")] public string ColorPrimary { get; init; } = "";
        [JsonPropertyName("colorBackground")] public string ColorBackground { get; init; } = "";
        [JsonPropertyName("colorText")] public string ColorText { get; init; } = "";
        [JsonPropertyName("supportEmail")] public string? SupportEmail { get; init; }
    }

    /// <summary>
    /// A read/update echoes the stored branding plus <c>resolved</c> — the
    /// sanitized shape the page will actually render.
    /// </summary>
    public sealed class BrandingResponse : Branding
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "branding";
        [JsonPropertyName("resolved")] public ResolvedBranding Resolved { get; init; } = new ResolvedBranding();
    }

    /// <summary>A draft rendered through the real hosted-page renderer, returned as HTML.</summary>
    public sealed class HostedPagePreview
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "hosted_page_preview";
        [JsonPropertyName("html")] public string Html { get; init; } = "";
    }

    /// <summary>The branding namespace (<c>/v1/branding</c>).</summary>
    public sealed class BrandingResource : ResourceBase
    {
        public BrandingResource(AtlasTransport transport) : base(transport) { }

        public Task<BrandingResponse> GetAsync(CancellationToken ct = default)
            => Req<BrandingResponse>(HttpVerb.Get, "/v1/branding", cancellationToken: ct);

        /// <summary>A PARTIAL merge — one field changes, the rest is preserved.</summary>
        public Task<BrandingResponse> UpdateAsync(Branding body, CancellationToken ct = default)
            => Req<BrandingResponse>(HttpVerb.Patch, "/v1/branding", body, cancellationToken: ct);

        /// <summary>Render a draft through the live sign-in renderer without saving it.</summary>
        public Task<HostedPagePreview> PreviewAsync(Branding body, CancellationToken ct = default)
            => Req<HostedPagePreview>(HttpVerb.Post, "/v1/branding/preview", body, cancellationToken: ct);
    }
}
