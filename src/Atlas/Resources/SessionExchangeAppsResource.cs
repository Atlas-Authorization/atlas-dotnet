using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class CreateSessionExchangeAppBody
    {
        /// <summary>A human label for the downstream app.</summary>
        public string? Name { get; set; }
        /// <summary>Public https endpoint Atlas POSTs the signed logout_token to (application/x-www-form-urlencoded: logout_token=&lt;jwt&gt;) when a session ends.</summary>
        public string? BackchannelLogoutUri { get; set; }
        /// <summary>Optional override for the logout_token `aud` claim. Defaults to the app id when omitted.</summary>
        public string? Audience { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateSessionExchangeAppBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class UpdateSessionExchangeAppBody
    {
        public string? Name { get; set; }
        /// <summary>Public https endpoint.</summary>
        public string? BackchannelLogoutUri { get; set; }
        public string? Audience { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public UpdateSessionExchangeAppBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class SessionExchangeApp
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? BackchannelLogoutUri { get; init; }
        /// <summary>The aud claim the logout_token carries.</summary>
        public string? Audience { get; init; }
        public long? CreatedAt { get; init; }
        public long? UpdatedAt { get; init; }
    }

    /// <summary>Session-exchange apps (<c>/v1/session_exchange_apps</c>).</summary>
    public sealed class SessionExchangeAppsResource : ResourceBase
    {
        public SessionExchangeAppsResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/session_exchange_apps</c> — List registered session-exchange apps (downstream apps that exchange an Atlas session for their own) and their back-channel logout endpoints.</summary>
        public Task<ListPage<SessionExchangeApp>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<SessionExchangeApp>>(HttpVerb.Get, "/v1/session_exchange_apps", cancellationToken: ct);

        /// <summary><c>POST /v1/session_exchange_apps</c> — Register a session-exchange app and its backchannel_logout_uri.</summary>
        public Task<SessionExchangeApp> CreateAsync(CreateSessionExchangeAppBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<SessionExchangeApp>(HttpVerb.Post, "/v1/session_exchange_apps", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/session_exchange_apps/{id}</c> — Fetch one session-exchange app.</summary>
        public Task<SessionExchangeApp> GetAsync(string id, CancellationToken ct = default)
            => Req<SessionExchangeApp>(HttpVerb.Get, $"/v1/session_exchange_apps/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/session_exchange_apps/{id}</c> — Update a session-exchange app's name, backchannel_logout_uri or audience.</summary>
        public Task<SessionExchangeApp> UpdateAsync(string id, UpdateSessionExchangeAppBody body, CancellationToken ct = default)
            => Req<SessionExchangeApp>(HttpVerb.Patch, $"/v1/session_exchange_apps/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/session_exchange_apps/{id}</c> — Delete a session-exchange app; its queued back-channel logout deliveries cascade with it.</summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/session_exchange_apps/{Enc(id)}", cancellationToken: ct);
    }
}
