using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Describes a deprecated webhook event spelling.</summary>
    public sealed class WebhookEventAlias
    {
        public WebhookEventAlias(string canonical, string kind)
        {
            Canonical = canonical;
            Kind = kind;
        }

        /// <summary>The event to subscribe to instead.</summary>
        public string Canonical { get; }

        /// <summary><c>rename</c> (sent whenever the canonical event is) or <c>companion</c> (only for some occurrences).</summary>
        public string Kind { get; }
    }

    /// <summary>
    /// Webhook event names: the canonical spellings, the deprecated aliases, and the events added with endpoint health, secret keys and the
    /// test event.
    /// </summary>
    public static class WebhookEvents
    {
        // Events added with endpoint health, secret keys and the test event.

        /// <summary>An endpoint's health changed (also sent as email to the alert contacts).</summary>
        public const string WebhookEndpointFailing = "webhook_endpoint.failing";
        public const string WebhookEndpointDisabled = "webhook_endpoint.disabled";
        public const string WebhookEndpointRecovered = "webhook_endpoint.recovered";
        /// <summary>The same for any outbound target (referral connection, back-channel receiver, integration).</summary>
        public const string DeliveryTargetFailing = "delivery_target.failing";
        public const string DeliveryTargetDisabled = "delivery_target.disabled";
        public const string DeliveryTargetRecovered = "delivery_target.recovered";
        /// <summary>The synthetic test event.</summary>
        public const string WebhookTest = "webhook.test";
        /// <summary>A secret key is about to expire (<c>data</c> carries <c>days_left</c> and <c>threshold_days</c>); audience <c>security</c>.</summary>
        public const string SecretKeyExpiring = "secret_key.expiring";
        /// <summary>A secret key was revoked; audience <c>security</c>.</summary>
        public const string SecretKeyRevoked = "secret_key.revoked";

        /// <summary>Lets a key reveal webhook signing secrets.</summary>
        public const string ScopeWebhooksReveal = "webhooks:reveal";

        /// <summary>
        /// Every deprecated event spelling mapped to its canonical event. An endpoint that lists one by name still receives it (with the same
        /// <c>atlas-id</c> as the canonical event) until the sunset date.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, WebhookEventAlias> Aliases = new Dictionary<string, WebhookEventAlias>
        {
            ["session.ended"] = new WebhookEventAlias("session.revoked", "companion"),
            ["session.removed"] = new WebhookEventAlias("session.revoked", "companion"),
            ["organizationMembership.created"] = new WebhookEventAlias("organization_membership.created", "rename"),
            ["organizationMembership.updated"] = new WebhookEventAlias("organization_membership.updated", "rename"),
            ["organizationMembership.deleted"] = new WebhookEventAlias("organization_membership.deleted", "rename"),
            ["organization.membership.deleted"] = new WebhookEventAlias("organization_membership.deleted", "rename"),
            ["organizationInvitation.created"] = new WebhookEventAlias("organization_invitation.created", "rename"),
            ["organizationInvitation.accepted"] = new WebhookEventAlias("organization_invitation.accepted", "rename"),
            ["organizationInvitation.revoked"] = new WebhookEventAlias("organization_invitation.revoked", "rename"),
            ["organizationInvitation.declined"] = new WebhookEventAlias("organization_invitation.declined", "rename"),
            ["organizationInvitation.resent"] = new WebhookEventAlias("organization_invitation.resent", "rename"),
            ["teamMembership.created"] = new WebhookEventAlias("team_membership.created", "rename"),
            ["teamMembership.deleted"] = new WebhookEventAlias("team_membership.deleted", "rename"),
            ["appRole.created"] = new WebhookEventAlias("app_role.created", "rename"),
            ["appRole.updated"] = new WebhookEventAlias("app_role.updated", "rename"),
            ["appRole.deleted"] = new WebhookEventAlias("app_role.deleted", "rename"),
            ["organizationDomain.created"] = new WebhookEventAlias("organization_domain.created", "rename"),
            ["organizationDomain.updated"] = new WebhookEventAlias("organization_domain.updated", "rename"),
            ["organizationDomain.deleted"] = new WebhookEventAlias("organization_domain.deleted", "rename"),
            ["organization.settings.updated"] = new WebhookEventAlias("organization_settings.updated", "rename"),
            ["waitlistEntry.created"] = new WebhookEventAlias("waitlist_entry.created", "rename"),
            ["waitlistEntry.updated"] = new WebhookEventAlias("waitlist_entry.updated", "rename"),
            ["billing.subscription.created"] = new WebhookEventAlias("billing_subscription.created", "rename"),
            ["billing.subscription.updated"] = new WebhookEventAlias("billing_subscription.updated", "rename"),
            ["billing.subscription.canceled"] = new WebhookEventAlias("billing_subscription.canceled", "rename"),
            ["billing.invoice.payment_failed"] = new WebhookEventAlias("billing_invoice.payment_failed", "rename"),
            ["billing.trial.started"] = new WebhookEventAlias("billing_trial.started", "rename"),
            ["billing.trial.will_end"] = new WebhookEventAlias("billing_trial.will_end", "rename"),
            ["billing.trial.ended"] = new WebhookEventAlias("billing_trial.ended", "rename"),
            ["entitlement.override.created"] = new WebhookEventAlias("entitlement_override.created", "rename"),
            ["entitlement.override.deleted"] = new WebhookEventAlias("entitlement_override.deleted", "rename"),
            ["entitlement.override.expired"] = new WebhookEventAlias("entitlement_override.expired", "rename"),
            ["scimToken.created"] = new WebhookEventAlias("scim_token.created", "rename"),
            ["scimToken.revoked"] = new WebhookEventAlias("scim_token.revoked", "rename"),
            ["ssoConnection.created"] = new WebhookEventAlias("sso_connection.created", "rename"),
            ["ssoConnection.updated"] = new WebhookEventAlias("sso_connection.updated", "rename"),
            ["ssoConnection.deleted"] = new WebhookEventAlias("sso_connection.deleted", "rename"),
            ["user.data_export.requested"] = new WebhookEventAlias("user.data_export_requested", "rename"),
            ["user.deletion_cancelled"] = new WebhookEventAlias("user.deletion_canceled", "companion"),
        };

        /// <summary>True when <paramref name="name"/> is a deprecated spelling.</summary>
        public static bool IsDeprecated(string name) => Aliases.ContainsKey(name);

        /// <summary>The canonical name for any spelling (an unknown name is returned unchanged).</summary>
        public static string Canonical(string name) => Aliases.TryGetValue(name, out var a) ? a.Canonical : name;

        /// <summary>Every name an event is known by: the canonical one, then its deprecated aliases (sorted).</summary>
        public static IReadOnlyList<string> Spellings(string name)
        {
            var canonical = Canonical(name);
            var aliases = Aliases.Where(kv => kv.Value.Canonical == canonical).Select(kv => kv.Key).ToList();
            aliases.Sort(StringComparer.Ordinal);
            var result = new List<string> { canonical };
            result.AddRange(aliases);
            return result;
        }
    }

    /// <summary>Delivery target kinds for <see cref="DeliveryResource"/>.</summary>
    public static class DeliveryTargetKinds
    {
        public const string WebhookEndpoint = "webhook_endpoint";
        public const string ReferralConnection = "referral_connection";
        public const string BackchannelReceiver = "backchannel_receiver";
        public const string Integration = "integration";
    }

    /// <summary>The addresses deliveries leave from.</summary>
    public sealed class EgressIps
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>False when this deployment does not send from fixed addresses.</summary>
        public bool Static { get; init; }
        [JsonPropertyName("ipv4")] public List<string> Ipv4 { get; init; } = new List<string>();
        [JsonPropertyName("ipv6")] public List<string> Ipv6 { get; init; } = new List<string>();
        public List<string>? AppliesTo { get; init; }
        public string? Note { get; init; }
    }

    /// <summary>The delivery health of one outbound target.</summary>
    public sealed class DeliveryHealth
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Kind { get; init; } = "";
        public string TargetId { get; init; } = "";
        public string? Url { get; init; }
        public string? Label { get; init; }
        /// <summary><c>healthy</c>, <c>failing</c> or <c>disabled</c>.</summary>
        public string State { get; init; } = "";
        public long? FailingSince { get; init; }
        public long? DisabledAt { get; init; }
        public long? LastSuccessAt { get; init; }
        public WebhookLastFailure? LastFailure { get; init; }
        public int ConsecutiveFailures { get; init; }
        [JsonPropertyName("success_rate_24h")] public double? SuccessRate24h { get; init; }
        [JsonPropertyName("attempts_24h")] public WebhookAttempts24h? Attempts24h { get; init; }
        public List<string>? AlertContacts { get; init; }
        public WebhookAlertsSent? Alerts { get; init; }
    }

    /// <summary>The delivery health list.</summary>
    public sealed class DeliveryHealthList
    {
        public List<DeliveryHealth> Data { get; init; } = new List<DeliveryHealth>();
        public bool HasMore { get; init; }
        public int UnhealthyCount { get; init; }
    }

    /// <summary><c>/v1/delivery_health</c> and <c>/v1/meta/egress_ips</c>.</summary>
    public sealed class DeliveryResource : ResourceBase
    {
        public DeliveryResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/meta/egress_ips</c> — the addresses deliveries leave from (public).</summary>
        public Task<EgressIps> EgressIpsAsync(CancellationToken ct = default)
            => Req<EgressIps>(HttpVerb.Get, "/v1/meta/egress_ips", cancellationToken: ct);

        /// <summary>
        /// <c>GET /v1/delivery_health</c> — the delivery health of every outbound target (webhooks, the referral connection, back-channel
        /// receivers, integrations). <paramref name="kind"/> is a <see cref="DeliveryTargetKinds"/> value; <paramref name="state"/> is <c>unhealthy</c> or <c>all</c>.
        /// </summary>
        public Task<DeliveryHealthList> HealthAsync(string? kind = null, string? state = null, CancellationToken ct = default)
            => Req<DeliveryHealthList>(HttpVerb.Get, "/v1/delivery_health", query: Q(("kind", kind), ("state", state)), cancellationToken: ct);

        /// <summary><c>PUT /v1/delivery_health/{kind}/{targetId}/alert_contacts</c> — set who is emailed when a target fails; null = the account's owners.</summary>
        public Task<DeliveryHealth> SetAlertContactsAsync(string kind, string targetId, IEnumerable<string>? alertContacts, CancellationToken ct = default)
            => Req<DeliveryHealth>(HttpVerb.Put, $"/v1/delivery_health/{Enc(kind)}/{Enc(targetId)}/alert_contacts",
                new Dictionary<string, object?> { ["alert_contacts"] = alertContacts }, cancellationToken: ct);
    }
}
