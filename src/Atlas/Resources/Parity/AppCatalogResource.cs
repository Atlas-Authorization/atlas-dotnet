using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/app_catalog/{id}/install</c>. Only the fields set are sent.</summary>
    public sealed class AppCatalogInstallBody
    {
        /// <summary>The preset's parameters (GET /v1/app_catalog lists them).</summary>
        [JsonPropertyName("params")] public Dictionary<string, object?>? Params { get; set; }
        /// <summary>1-64 letters, digits, - or _. Defaults to the preset id.</summary>
        [JsonPropertyName("installed_id")] public string? InstalledId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("allowed_email_domains")] public List<string>? AllowedEmailDomains { get; set; }
        /// <summary>Who may open the app. mode everyone (default) or assigned; with assigned, only the listed users, members of the listed instance groups (id or slug, inherited), members of the listed organizations or holders of the listed organization roles. allowed_email_domains applies on top.</summary>
        [JsonPropertyName("assignment")] public Dictionary<string, object?>? Assignment { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/app_catalog/installed/{id}</c>. Only the fields set are sent.</summary>
    public sealed class AppCatalogUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("allowed_email_domains")] public List<string>? AllowedEmailDomains { get; set; }
        /// <summary>Who may open the app. mode everyone (default) or assigned; with assigned, only the listed users, members of the listed instance groups (id or slug, inherited), members of the listed organizations or holders of the listed organization roles. allowed_email_domains applies on top.</summary>
        [JsonPropertyName("assignment")] public Dictionary<string, object?>? Assignment { get; set; }
    }

    /// <summary>Operations of <c>appCatalog</c>.</summary>
    public sealed partial class AppCatalogResource : ResourceBase
    {
        public AppCatalogResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/app_catalog</c> - List the app presets: about 100 SAML apps (Slack, GitHub, Snowflake, Notion, HubSpot and more) and about 45 OpenID Connect apps (Grafana, GitLab, Argo CD, Vault, Cloudflare Zero Trust and more), each with its protocol, category, the parameters it needs, whether its values were confirmed in the vendor's docs (verified) and that page (source_url).</summary>
        /// <remarks>Needs the <c>app_catalog:read</c> scope.</remarks>
        public Task<JsonElement> PresetsAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/app_catalog", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>GET /v1/app_catalog/installed</c> - List the installed apps (SAML and OpenID Connect) with their email-domain allow-list and assignment.</summary>
        /// <remarks>Needs the <c>app_catalog:read</c> scope.</remarks>
        public Task<JsonElement> InstalledAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/app_catalog/installed", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/app_catalog/{id}/install</c> - Install a catalog app from its preset and params, with an optional name, installed_id, allowed_email_domains and assignment. A SAML preset becomes a SAML service provider; an OIDC preset registers a confidential OAuth client with the preset's redirect URIs and returns client_id, client_secret (shown once), redirect_uri and issuer to paste into the app. 409 if the installed_id exists. -&gt; 201.</summary>
        /// <remarks>Needs the <c>app_catalog:write</c> scope.</remarks>
        public Task<JsonElement> InstallAsync(string id, AppCatalogInstallBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/app_catalog/{Enc(id)}/install", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>PATCH /v1/app_catalog/installed/{id}</c> - Change an installed app's name, allowed_email_domains or assignment. Assignment applies to the launcher and to single sign-on: someone not assigned is refused with 403.</summary>
        /// <remarks>Needs the <c>app_catalog:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, AppCatalogUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/app_catalog/installed/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/app_catalog/installed/{id}</c> - Uninstall an app: it disappears from the launcher and Atlas stops signing people in to it (an OIDC app's OAuth client and its grants are deleted).</summary>
        /// <remarks>Needs the <c>app_catalog:write</c> scope.</remarks>
        public Task UninstallAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/app_catalog/installed/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);
    }
}
