using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>A logout fan-out endpoint: gets the same signed logout_token as your OIDC apps.</summary>
    public sealed class LogoutNotifyEndpoint
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "logout_notify_endpoint";
        public string Id { get; init; } = "";
        public string Url { get; init; } = "";
        /// <summary>The client_id it follows; null = instance-wide.</summary>
        public string? OauthClientId { get; init; }
        /// <summary>The aud its logout_tokens carry.</summary>
        public string Audience { get; init; } = "";
        public string? Description { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    /// <summary>The body of creating or updating a logout fan-out endpoint.</summary>
    public sealed class LogoutNotifyEndpointBody
    {
        /// <summary>Public https endpoint (required on create).</summary>
        public string? Url { get; set; }
        /// <summary>A client_id to follow; omit for instance-wide (create only).</summary>
        public string? OauthClientId { get; set; }
        /// <summary>The logout_token aud. Default: the client_id when following a client, else the instance id.</summary>
        public string? Audience { get; set; }
        /// <summary>Up to 200 characters.</summary>
        public string? Description { get; set; }
    }

    /// <summary>One logout_token sent to a fan-out endpoint.</summary>
    public sealed class LogoutNotifyDelivery
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "logout_notify_delivery";
        public string Id { get; init; } = "";
        public string Sub { get; init; } = "";
        /// <summary>Null = a subject-wide logout (ban / deletion).</summary>
        public string? Sid { get; init; }
        /// <summary>pending, success or failed.</summary>
        public string Status { get; init; } = "";
        public int AttemptNumber { get; init; }
        public int? HttpStatus { get; init; }
        public string? ResponseSnippet { get; init; }
        public long? NextRetryAt { get; init; }
        public long? DeliveredAt { get; init; }
        public long CreatedAt { get; init; }
    }

    /// <summary>
    /// Logout fan-out ("also notify") endpoints (<c>/v1/logout_notify_endpoints</c>): downstream services that
    /// receive the same signed <c>logout_token</c> as your OIDC apps whenever an Atlas session ends, plus a
    /// subject-wide one (sub, no sid) on ban, deletion or deprovisioning.
    /// </summary>
    public sealed class LogoutNotifyEndpointsResource : ResourceBase
    {
        public LogoutNotifyEndpointsResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<LogoutNotifyEndpoint>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<LogoutNotifyEndpoint>>(HttpVerb.Get, "/v1/logout_notify_endpoints", cancellationToken: ct);

        /// <summary>Register an endpoint (public https). With OauthClientId it follows one client.</summary>
        public Task<LogoutNotifyEndpoint> CreateAsync(LogoutNotifyEndpointBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<LogoutNotifyEndpoint>(HttpVerb.Post, "/v1/logout_notify_endpoints", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<LogoutNotifyEndpoint> GetAsync(string id, CancellationToken ct = default)
            => Req<LogoutNotifyEndpoint>(HttpVerb.Get, $"/v1/logout_notify_endpoints/{Enc(id)}", cancellationToken: ct);

        /// <summary>Change the url, audience or description.</summary>
        public Task<LogoutNotifyEndpoint> UpdateAsync(string id, LogoutNotifyEndpointBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<LogoutNotifyEndpoint>(HttpVerb.Patch, $"/v1/logout_notify_endpoints/{Enc(id)}", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Delete an endpoint; its queued deliveries go with it.</summary>
        public Task<DeletedObject> DeleteAsync(string id, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/logout_notify_endpoints/{Enc(id)}", idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>The delivery log, newest first (limit 1-100).</summary>
        public Task<ListPage<LogoutNotifyDelivery>> ListDeliveriesAsync(string id, int? limit = null, CancellationToken ct = default)
            => Req<ListPage<LogoutNotifyDelivery>>(HttpVerb.Get, $"/v1/logout_notify_endpoints/{Enc(id)}/deliveries", query: Q(("limit", limit)), cancellationToken: ct);
    }
}
