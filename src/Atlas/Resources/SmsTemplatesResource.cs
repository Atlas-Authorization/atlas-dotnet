using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class SmsTemplate
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "sms_template";
        public string Name { get; init; } = "";
        public string Default { get; init; } = "";
        public string? Body { get; init; }
        public bool Enabled { get; init; }
        public bool Customised { get; init; }
        public List<string> Variables { get; init; } = new List<string>();
    }

    public sealed class CreateSmsTemplateBody
    {
        public string Name { get; set; } = "";
        public string Body { get; set; } = "";
        public bool? Enabled { get; set; }
    }

    public sealed class UpdateSmsTemplateBody
    {
        public string? Body { get; set; }
        public bool? Enabled { get; set; }
    }

    public sealed class SmsTemplatePreview
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "sms_template_preview";
        public string Name { get; init; } = "";
        public string Body { get; init; } = "";
        public bool Customised { get; init; }
    }

    /// <summary>The SMS-templates namespace (<c>/v1/sms_templates</c>), keyed by template name.</summary>
    public sealed class SmsTemplatesResource : ResourceBase
    {
        public SmsTemplatesResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<SmsTemplate>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<SmsTemplate>>(HttpVerb.Get, "/v1/sms_templates", cancellationToken: ct);

        /// <summary>Upsert an override by name.</summary>
        public Task<SmsTemplate> CreateAsync(CreateSmsTemplateBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<SmsTemplate>(HttpVerb.Post, "/v1/sms_templates", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<SmsTemplate> GetAsync(string name, CancellationToken ct = default)
            => Req<SmsTemplate>(HttpVerb.Get, $"/v1/sms_templates/{Enc(name)}", cancellationToken: ct);

        public Task<SmsTemplate> UpdateAsync(string name, UpdateSmsTemplateBody body, CancellationToken ct = default)
            => Req<SmsTemplate>(HttpVerb.Patch, $"/v1/sms_templates/{Enc(name)}", body, cancellationToken: ct);

        /// <summary>Revert to the built-in.</summary>
        public Task<SmsTemplate> DeleteAsync(string name, CancellationToken ct = default)
            => Req<SmsTemplate>(HttpVerb.Delete, $"/v1/sms_templates/{Enc(name)}", cancellationToken: ct);

        /// <summary>Render an unsaved draft (if a body is given) or the stored/built-in.</summary>
        public Task<SmsTemplatePreview> PreviewAsync(string name, string? body = null, CancellationToken ct = default)
            => Req<SmsTemplatePreview>(HttpVerb.Post, $"/v1/sms_templates/{Enc(name)}/preview",
                body: Body(("body", body)), cancellationToken: ct);
    }
}
