using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class EmailTemplateBody
    {
        public string? Subject { get; set; }
        public string? Text { get; set; }
    }

    public sealed class EmailTemplateDefault
    {
        public string Subject { get; init; } = "";
        public string Text { get; init; } = "";
    }

    /// <summary>A list row: the built-in <c>default</c> and the stored <c>override</c> are both returned.</summary>
    public sealed class EmailTemplate
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "email_template";
        public string Name { get; init; } = "";
        public EmailTemplateDefault Default { get; init; } = new EmailTemplateDefault();
        public EmailTemplateBody? Override { get; init; }
        public bool Customised { get; init; }
    }

    /// <summary>The lean shape an update/delete acknowledges with (no <c>default</c>).</summary>
    public sealed class EmailTemplateResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "email_template";
        public string Name { get; init; } = "";
        public EmailTemplateBody? Override { get; init; }
        public bool Customised { get; init; }
    }

    public sealed class EmailTemplatePreview
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "email_preview";
        public string Template { get; init; } = "";
        public bool Valid { get; init; }
        public string? Problem { get; init; }
        public string? Html { get; init; }
        public Dictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();
    }

    /// <summary>The email-templates namespace (<c>/v1/email_templates</c>), keyed by template name.</summary>
    public sealed class EmailTemplatesResource : ResourceBase
    {
        public EmailTemplatesResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<EmailTemplate>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<EmailTemplate>>(HttpVerb.Get, "/v1/email_templates", cancellationToken: ct);

        /// <summary>Render an unsaved draft (if a body is given) or the stored template.</summary>
        public Task<EmailTemplatePreview> PreviewAsync(string name, EmailTemplateBody? body = null, CancellationToken ct = default)
            => Req<EmailTemplatePreview>(HttpVerb.Post, $"/v1/email_templates/{Enc(name)}/preview", body ?? new EmailTemplateBody(), cancellationToken: ct);

        /// <summary>Save an override. Keyed by template name, not an id.</summary>
        public Task<EmailTemplateResult> UpdateAsync(string name, EmailTemplateBody body, CancellationToken ct = default)
            => Req<EmailTemplateResult>(HttpVerb.Put, $"/v1/email_templates/{Enc(name)}", body, cancellationToken: ct);

        /// <summary>Revert to the built-in.</summary>
        public Task<EmailTemplateResult> DeleteAsync(string name, CancellationToken ct = default)
            => Req<EmailTemplateResult>(HttpVerb.Delete, $"/v1/email_templates/{Enc(name)}", cancellationToken: ct);
    }
}
