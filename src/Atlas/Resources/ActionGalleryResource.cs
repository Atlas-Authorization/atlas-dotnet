using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>A secret a template reads, declared by NAME; values are never part of a template.</summary>
    public sealed class ActionTemplateSecret
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Required { get; set; }
    }

    /// <summary>A template in the Actions gallery (the detail view adds code, changelog and versions).</summary>
    public sealed class ActionGalleryEntry
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "action_template";
        /// <summary><c>builtin</c> or <c>community</c>.</summary>
        public string Source { get; init; } = "";
        public string Slug { get; init; } = "";
        public string Name { get; init; } = "";
        public string Summary { get; init; } = "";
        public string Trigger { get; init; } = "";
        public string Category { get; init; } = "";
        public List<string> Tags { get; init; } = new List<string>();
        /// <summary>SPDX licence id.</summary>
        public string License { get; init; } = "";
        public JsonElement? Author { get; init; }
        public bool Verified { get; init; }
        public bool Featured { get; init; }
        public long InstallCount { get; init; }
        public string LatestVersion { get; init; } = "";
        /// <summary>The code sends outbound requests with <c>api.http</c>.</summary>
        public bool UsesHttp { get; init; }
        public List<ActionTemplateSecret> Secrets { get; init; } = new List<ActionTemplateSecret>();
        public long? UpdatedAt { get; init; }
        public string? Description { get; init; }
        public string? Code { get; init; }
        public string? Changelog { get; init; }
        public string? RepositoryUrl { get; init; }
        public JsonElement? Versions { get; init; }
    }

    /// <summary>An action installed from the gallery, with its update status.</summary>
    public sealed class ActionGalleryInstallation
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "action_gallery_installation";
        public string ActionId { get; init; } = "";
        public string ActionName { get; init; } = "";
        public bool Enabled { get; init; }
        public string Trigger { get; init; } = "";
        public string Source { get; init; } = "";
        public string TemplateSlug { get; init; } = "";
        public string InstalledVersion { get; init; } = "";
        public string? LatestVersion { get; init; }
        public bool UpdateAvailable { get; init; }
        public List<string> MissingSecrets { get; init; } = new List<string>();
        public List<string> UpdateRequiresSecrets { get; init; } = new List<string>();
        public long InstalledAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    /// <summary>One version of a template you publish (<c>draft</c>, <c>pending_review</c>, <c>approved</c> or <c>rejected</c>).</summary>
    public sealed class ActionTemplateVersion
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "action_template_version";
        public string Version { get; init; } = "";
        public string Status { get; init; } = "";
        public string Trigger { get; init; } = "";
        public List<ActionTemplateSecret> Secrets { get; init; } = new List<ActionTemplateSecret>();
        public string? Changelog { get; init; }
        /// <summary>Why staff rejected it, when they did.</summary>
        public string? ReviewNote { get; init; }
        public string Code { get; init; } = "";
        public long? SubmittedAt { get; init; }
        public long? ReviewedAt { get; init; }
        public long? CreatedAt { get; init; }
        public bool Deleted { get; init; }
    }

    /// <summary>A template this instance publishes.</summary>
    public sealed class ActionTemplate
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "action_template";
        public string Id { get; init; } = "";
        public string Slug { get; init; } = "";
        public string Name { get; init; } = "";
        public string AuthorName { get; init; } = "";
        public string Summary { get; init; } = "";
        public string? Description { get; init; }
        public string Category { get; init; } = "";
        public List<string> Tags { get; init; } = new List<string>();
        public string Trigger { get; init; } = "";
        public string License { get; init; } = "";
        public string? RepositoryUrl { get; init; }
        /// <summary><c>draft</c>, <c>in_review</c>, <c>published</c>, <c>unlisted</c>, <c>rejected</c> or <c>banned</c>.</summary>
        public string Status { get; init; } = "";
        public string? LatestVersion { get; init; }
        public bool Verified { get; init; }
        public bool Featured { get; init; }
        public long InstallCount { get; init; }
        public long ViewCount { get; init; }
        public long? PublishedAt { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        public List<ActionTemplateVersion> Versions { get; init; } = new List<ActionTemplateVersion>();
        public bool Deleted { get; init; }
    }

    /// <summary>
    /// A template listing (create needs slug, name, author name and trigger). On create the first version may come in
    /// the same call (<see cref="Version"/>, <see cref="Code"/>, <see cref="Secrets"/>, <see cref="Changelog"/>,
    /// <see cref="Submit"/>).
    /// </summary>
    public sealed class ActionTemplateBody
    {
        public string? Slug { get; set; }
        public string? Name { get; set; }
        public string? AuthorName { get; set; }
        public string? Summary { get; set; }
        /// <summary>Markdown.</summary>
        public string? Description { get; set; }
        public string? Category { get; set; }
        public List<string>? Tags { get; set; }
        public string? Trigger { get; set; }
        /// <summary>SPDX id.</summary>
        public string? License { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? Version { get; set; }
        public string? Code { get; set; }
        public List<ActionTemplateSecret>? Secrets { get; set; }
        public string? Changelog { get; set; }
        public bool? Submit { get; set; }
    }

    /// <summary>
    /// The community Actions gallery: browse reusable Action templates (first-party and community), install one as a
    /// DISABLED action, and keep installed actions up to date.
    /// </summary>
    public sealed class ActionGalleryResource : ResourceBase
    {
        public ActionGalleryResource(AtlasTransport transport) : base(transport) { }

        /// <summary>The gallery, best first. <paramref name="source"/> is <c>builtin</c> or <c>community</c>.</summary>
        public Task<ListPage<ActionGalleryEntry>> ListAsync(string? q = null, string? category = null, string? trigger = null, string? source = null, CancellationToken ct = default)
            => Req<ListPage<ActionGalleryEntry>>(HttpVerb.Get, "/v1/action_gallery", query: Q(("q", q), ("category", category), ("trigger", trigger), ("source", source)), cancellationToken: ct);

        /// <summary>One template with its code, secret names and versions.</summary>
        public Task<ActionGalleryEntry> GetAsync(string slug, CancellationToken ct = default)
            => Req<ActionGalleryEntry>(HttpVerb.Get, $"/v1/action_gallery/{Enc(slug)}", cancellationToken: ct);

        /// <summary>Install as a new, disabled action (<c>action</c>, <c>template</c>, <c>install_id</c>, <c>missing_secrets</c>).</summary>
        public Task<JsonElement> InstallAsync(string slug, string? version = null, string? name = null, Dictionary<string, string>? secrets = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/action_gallery/{Enc(slug)}/install", Body(("version", version), ("name", name), ("secrets", secrets)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>The actions installed from the gallery, with update status.</summary>
        public Task<ListPage<ActionGalleryInstallation>> InstallsAsync(CancellationToken ct = default)
            => Req<ListPage<ActionGalleryInstallation>>(HttpVerb.Get, "/v1/action_gallery_installs", cancellationToken: ct);

        /// <summary>Move an installed action to its template's latest version.</summary>
        public Task<JsonElement> UpdateAsync(string actionId, Dictionary<string, string>? secrets = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/actions/{Enc(actionId)}/gallery_update", Body(("secrets", secrets)), cancellationToken: ct);
    }

    /// <summary>Action templates this instance publishes to the gallery (staff review them).</summary>
    public sealed class ActionTemplatesResource : ResourceBase
    {
        public ActionTemplatesResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<ActionTemplate>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<ActionTemplate>>(HttpVerb.Get, "/v1/action_templates", cancellationToken: ct);

        public Task<ActionTemplate> CreateAsync(ActionTemplateBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ActionTemplate>(HttpVerb.Post, "/v1/action_templates", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<ActionTemplate> GetAsync(string id, CancellationToken ct = default)
            => Req<ActionTemplate>(HttpVerb.Get, $"/v1/action_templates/{Enc(id)}", cancellationToken: ct);

        public Task<ActionTemplate> UpdateAsync(string id, ActionTemplateBody body, CancellationToken ct = default)
            => Req<ActionTemplate>(HttpVerb.Patch, $"/v1/action_templates/{Enc(id)}", body, cancellationToken: ct);

        public Task<JsonElement> DeleteAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/action_templates/{Enc(id)}", cancellationToken: ct);

        /// <summary>Add a version (MAJOR.MINOR.PATCH, higher than every existing one); <paramref name="submit"/> sends it to review.</summary>
        public Task<ActionTemplateVersion> AddVersionAsync(string id, string version, string code, List<ActionTemplateSecret>? secrets = null, string? changelog = null, bool? submit = null, CancellationToken ct = default)
            => Req<ActionTemplateVersion>(HttpVerb.Post, $"/v1/action_templates/{Enc(id)}/versions", Body(("version", version), ("code", code), ("secrets", secrets), ("changelog", changelog), ("submit", submit)), cancellationToken: ct);

        public Task<ActionTemplateVersion> SubmitVersionAsync(string id, string version, CancellationToken ct = default)
            => Req<ActionTemplateVersion>(HttpVerb.Post, $"/v1/action_templates/{Enc(id)}/versions/{Enc(version)}/submit", cancellationToken: ct);

        public Task<JsonElement> DeleteVersionAsync(string id, string version, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/action_templates/{Enc(id)}/versions/{Enc(version)}", cancellationToken: ct);

        /// <summary>List an approved template again after unlisting it.</summary>
        public Task<ActionTemplate> PublishAsync(string id, CancellationToken ct = default)
            => Req<ActionTemplate>(HttpVerb.Post, $"/v1/action_templates/{Enc(id)}/publish", cancellationToken: ct);

        /// <summary>Hide a template from the gallery (installs keep working).</summary>
        public Task<ActionTemplate> UnlistAsync(string id, CancellationToken ct = default)
            => Req<ActionTemplate>(HttpVerb.Post, $"/v1/action_templates/{Enc(id)}/unlist", cancellationToken: ct);
    }
}
