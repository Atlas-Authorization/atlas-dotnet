using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class SendNotificationBody
    {
        /// <summary>The end-user to notify.</summary>
        public string? UserId { get; set; }
        /// <summary>A tenant-defined template name, or a built-in (new_device, unattended_access, password_changed, mfa_changed).</summary>
        public string? Template { get; set; }
        /// <summary>Variables interpolated into the template (e.g. device, location, when, url).</summary>
        public Dictionary<string, string?>? Data { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public SendNotificationBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class CreateNotificationTemplateBody
    {
        /// <summary>The template name a send passes; unique per instance.</summary>
        public string? Name { get; set; }
        public string? Subject { get; set; }
        /// <summary>Email body with `{{variables}}`.</summary>
        public string? Body { get; set; }
        /// <summary>A built-in or tenant-defined notification category key.</summary>
        public string? Category { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateNotificationTemplateBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class UpdateNotificationTemplateBody
    {
        public string? Subject { get; set; }
        /// <summary>Email body with `{{variables}}`.</summary>
        public string? Body { get; set; }
        /// <summary>A built-in category (new_device, unattended_access, credential_change) or a tenant-defined one (POST /v1/notification_categories), so the send honours the opt-out.</summary>
        public string? Category { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public UpdateNotificationTemplateBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class NotificationSent
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Status { get; init; } = "";
    }

    public sealed class NotificationCategory
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        /// <summary>The category key templates and preferences reference.</summary>
        public string Key { get; init; } = "";
        /// <summary>The user-facing label on the preference toggle.</summary>
        public string Label { get; init; } = "";
        /// <summary>Always true for a tenant-defined category.</summary>
        public bool Optional { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class DeletedNotificationCategory
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Key { get; init; }
        public bool? Deleted { get; init; }
    }

    public sealed class NotificationTemplate
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        /// <summary>The template name — the `template` a send passes.</summary>
        public string Name { get; init; } = "";
        public string Subject { get; init; } = "";
        /// <summary>The body with `{{variables}}` interpolated at send.</summary>
        public string Body { get; init; } = "";
        /// <summary>The end-user notification category whose preference gates this template.</summary>
        public string Category { get; init; } = "";
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class DeletedNotificationTemplate
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Name { get; init; }
        public bool? Deleted { get; init; }
    }

    /// <summary>Notification categories (<c>/v1/notification_categories</c>).</summary>
    public sealed class NotificationsCategoriesResource : ResourceBase
    {
        public NotificationsCategoriesResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>POST /v1/notification_categories</c> — Create a tenant-defined notification category: a key (unique per instance, cannot collide with a built-in) and the user-facing label shown on the end-user preference toggle.</summary>
        public Task<NotificationCategory> CreateAsync(string? key, string? label, CancellationToken ct = default)
            => Req<NotificationCategory>(HttpVerb.Post, "/v1/notification_categories", Body(("key", key), ("label", label)), cancellationToken: ct);

        /// <summary><c>GET /v1/notification_categories</c> — List this instance's tenant-defined notification categories.</summary>
        public Task<DataList<NotificationCategory>> ListAsync(CancellationToken ct = default)
            => Req<DataList<NotificationCategory>>(HttpVerb.Get, "/v1/notification_categories", cancellationToken: ct);

        /// <summary><c>PUT /v1/notification_categories/{key}</c> — Update a tenant-defined notification category's user-facing label.</summary>
        public Task<NotificationCategory> UpdateAsync(string key, string? label, CancellationToken ct = default)
            => Req<NotificationCategory>(HttpVerb.Put, $"/v1/notification_categories/{Enc(key)}", Body(("label", label)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/notification_categories/{key}</c> — Delete a tenant-defined notification category.</summary>
        public Task<DeletedNotificationCategory> DeleteAsync(string key, CancellationToken ct = default)
            => Req<DeletedNotificationCategory>(HttpVerb.Delete, $"/v1/notification_categories/{Enc(key)}", cancellationToken: ct);
    }

    /// <summary>Notification templates (<c>/v1/notification_templates</c>).</summary>
    public sealed class NotificationsTemplatesResource : ResourceBase
    {
        public NotificationsTemplatesResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>POST /v1/notification_templates</c> — Create a tenant-defined notification template: a name (unique per instance), a subject and body with `{{variables}}`, and the category whose per-user preference gates it.</summary>
        public Task<NotificationTemplate> CreateAsync(CreateNotificationTemplateBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<NotificationTemplate>(HttpVerb.Post, "/v1/notification_templates", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/notification_templates</c> — List this instance's tenant-defined notification templates.</summary>
        public Task<DataList<NotificationTemplate>> ListAsync(CancellationToken ct = default)
            => Req<DataList<NotificationTemplate>>(HttpVerb.Get, "/v1/notification_templates", cancellationToken: ct);

        /// <summary><c>GET /v1/notification_templates/{name}</c> — Retrieve one tenant-defined notification template by name.</summary>
        public Task<NotificationTemplate> GetAsync(string name, CancellationToken ct = default)
            => Req<NotificationTemplate>(HttpVerb.Get, $"/v1/notification_templates/{Enc(name)}", cancellationToken: ct);

        /// <summary><c>PUT /v1/notification_templates/{name}</c> — Update a tenant-defined notification template's subject, body and category by name.</summary>
        public Task<NotificationTemplate> UpdateAsync(string name, UpdateNotificationTemplateBody body, CancellationToken ct = default)
            => Req<NotificationTemplate>(HttpVerb.Put, $"/v1/notification_templates/{Enc(name)}", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/notification_templates/{name}</c> — Delete a tenant-defined notification template by name.</summary>
        public Task<DeletedNotificationTemplate> DeleteAsync(string name, CancellationToken ct = default)
            => Req<DeletedNotificationTemplate>(HttpVerb.Delete, $"/v1/notification_templates/{Enc(name)}", cancellationToken: ct);
    }

    /// <summary>Tenant-defined notifications: send one, and manage categories and templates.</summary>
    public sealed class NotificationsResource : ResourceBase
    {
        public NotificationsResource(AtlasTransport transport) : base(transport)
        {
            Categories = new NotificationsCategoriesResource(transport);
            Templates = new NotificationsTemplatesResource(transport);
        }

        /// <summary>Notification categories (<c>/v1/notification_categories</c>).</summary>
        public NotificationsCategoriesResource Categories { get; }

        /// <summary>Notification templates (<c>/v1/notification_templates</c>).</summary>
        public NotificationsTemplatesResource Templates { get; }

        /// <summary><c>POST /v1/notifications</c> — Send a templated, instance-branded security-notification email to one of your end-users, honouring their per-category notification preferences.</summary>
        public Task<NotificationSent> SendAsync(SendNotificationBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<NotificationSent>(HttpVerb.Post, "/v1/notifications", body, idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
