using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/marketing_settings</c>. Only the fields set are sent.</summary>
    public sealed class MarketingUpdateSettingsBody
    {
        [JsonPropertyName("capture")] public Dictionary<string, object?>? Capture { get; set; }
        [JsonPropertyName("scripts")] public Dictionary<string, object?>? Scripts { get; set; }
    }

    /// <summary>Operations of <c>marketing</c>.</summary>
    public sealed partial class MarketingResource : ResourceBase
    {
        public MarketingResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/marketing_settings</c> - The marketing settings: which UTM / ad-click-id parameters the SDK captures onto new users, and the GA4, GTM and Hotjar ids hosted pages load after consent.</summary>
        /// <remarks>Needs the <c>marketing:read</c> scope.</remarks>
        public Task<JsonElement> GetSettingsAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/marketing_settings", cancellationToken: ct);

        /// <summary><c>PUT /v1/marketing_settings</c> - Change the marketing settings. `capture.params` empty means the standard set (utm_source, utm_medium, utm_campaign, utm_term, utm_content, utm_id and the common ad click ids). Hosted pages load a configured script only when the visitor's consent manager grants `scripts.consent_category` (default analytics); with no consent signal nothing loads.</summary>
        /// <remarks>Needs the <c>marketing:write</c> scope.</remarks>
        public Task<JsonElement> UpdateSettingsAsync(MarketingUpdateSettingsBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, "/v1/marketing_settings", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/leads</c> - The lead / subscribe form's submissions, newest first, with the first-touch attribution captured with each. `format=csv` exports up to 50,000.</summary>
        /// <remarks>Needs the <c>marketing:read</c> scope.</remarks>
        public Task<JsonElement> ListLeadsAsync(long? limit = null, string? startingAfter = null, string? form = null, string? format = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/leads", query: Q(("limit", limit), ("starting_after", startingAfter), ("form", form), ("format", format)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/leads/{id}</c> - Delete one captured lead.</summary>
        /// <remarks>Needs the <c>marketing:write</c> scope.</remarks>
        public Task<JsonElement> DeleteLeadAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/leads/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
