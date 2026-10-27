using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>PUT /v1/instance/page_templates/{page}</c>. Only the fields set are sent.</summary>
    public sealed class PageTemplatesSaveDraftBody
    {
        [JsonPropertyName("definition")] public Dictionary<string, object?>? Definition { get; set; }
    }

    /// <summary>Operations of <c>pageTemplates</c>.</summary>
    public sealed partial class PageTemplatesResource : ResourceBase
    {
        public PageTemplatesResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/instance/page_templates</c> - The hosted-page templates of the instance: for each page (sign-in, sign-up, user-profile) the live template and the draft waiting to be previewed or promoted.</summary>
        /// <remarks>Needs the <c>instance:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/instance/page_templates", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/instance/page_templates/{page}</c> - Read one page's live and draft template.</summary>
        /// <remarks>Needs the <c>instance:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string page, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/instance/page_templates/{Enc(page)}", cancellationToken: ct);

        /// <summary><c>PUT /v1/instance/page_templates/{page}</c> - Save a page template as the page's draft. A template is data: components (heading, text, image, link, button_link, list, divider, spacer, columns) in four regions (header, above_form, below_form, footer). It is validated on save: no raw HTML, CSS or script, https and mailto links only. The live page is unchanged until you promote.</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> SaveDraftAsync(string page, PageTemplatesSaveDraftBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/instance/page_templates/{Enc(page)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/instance/page_templates/{page}</c> - Drop the draft or take the live template down (`status`).</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> DiscardAsync(string page, bool? dryRun = null, string? status = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/instance/page_templates/{Enc(page)}", query: Q(("dry_run", dryRun), ("status", status)), cancellationToken: ct);

        /// <summary><c>POST /v1/instance/page_templates/{page}/preview</c> - A signed link to the hosted page with the draft template applied (valid for one hour, noindex, with a preview banner). 409 when the page has no template.</summary>
        /// <remarks>Needs the <c>instance:read</c> scope.</remarks>
        public Task<JsonElement> PreviewAsync(string page, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/instance/page_templates/{Enc(page)}/preview", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/instance/page_templates/{page}/promote</c> - Make the draft the live template.</summary>
        /// <remarks>Needs the <c>instance:write</c> scope.</remarks>
        public Task<JsonElement> PromoteAsync(string page, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/instance/page_templates/{Enc(page)}/promote", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
