using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/passkey_api/registration/init</c>. Only the fields set are sent.</summary>
    public sealed class PasskeyApiRegistrationInitBody
    {
        /// <summary>Required. Your own user id. Atlas stores it with the passkey and never needs a matching Atlas user.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        /// <summary>Shown by the authenticator. Defaults to user_id.</summary>
        [JsonPropertyName("username")] public string? Username { get; set; }
        [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
    }

    /// <summary>The body of <c>POST /v1/passkey_api/registration/finalize</c>. Only the fields set are sent.</summary>
    public sealed class PasskeyApiRegistrationFinalizeBody
    {
        /// <summary>Required. Your own user id. Atlas stores it with the passkey and never needs a matching Atlas user.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("challenge")] public string? Challenge { get; set; }
        /// <summary>Required. base64url.</summary>
        [JsonPropertyName("attestation_object")] public string? AttestationObject { get; set; }
        /// <summary>Required. base64url.</summary>
        [JsonPropertyName("client_data_json")] public string? ClientDataJson { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("transports")] public List<string>? Transports { get; set; }
    }

    /// <summary>The body of <c>POST /v1/passkey_api/login/init</c>. Only the fields set are sent.</summary>
    public sealed class PasskeyApiLoginInitBody
    {
        /// <summary>Your own user id. Atlas stores it with the passkey and never needs a matching Atlas user.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
    }

    /// <summary>The body of <c>POST /v1/passkey_api/login/finalize</c>. Only the fields set are sent.</summary>
    public sealed class PasskeyApiLoginFinalizeBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("handle")] public string? Handle { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("challenge")] public string? Challenge { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("credential_id")] public string? CredentialId { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("authenticator_data")] public string? AuthenticatorData { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("client_data_json")] public string? ClientDataJson { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("signature")] public string? Signature { get; set; }
        /// <summary>Your own user id. Atlas stores it with the passkey and never needs a matching Atlas user.</summary>
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
    }

    /// <summary>Operations of <c>passkeyApi</c>.</summary>
    public sealed partial class PasskeyApiResource : ResourceBase
    {
        public PasskeyApiResource(AtlasTransport t) : base(t) { }

        /// <summary><c>POST /v1/passkey_api/registration/init</c> - Start registering a passkey for one of your users. Returns WebAuthn creation options for the browser.</summary>
        /// <remarks>Needs the <c>passkey_api:write</c> scope.</remarks>
        public Task<JsonElement> RegistrationInitAsync(PasskeyApiRegistrationInitBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/passkey_api/registration/init", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/passkey_api/registration/finalize</c> - Verify the browser’s attestation and store the passkey for the user. 409 if the credential is already registered; 403 if the authenticator is blocked by the passkey policy.</summary>
        /// <remarks>Needs the <c>passkey_api:write</c> scope.</remarks>
        public Task<JsonElement> RegistrationFinalizeAsync(PasskeyApiRegistrationFinalizeBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/passkey_api/registration/finalize", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/passkey_api/login/init</c> - Start a passkey login. With user_id the user’s credentials are listed; without it the login is discoverable and the credential names the user.</summary>
        /// <remarks>Needs the <c>passkey_api:write</c> scope.</remarks>
        public Task<JsonElement> LoginInitAsync(PasskeyApiLoginInitBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/passkey_api/login/init", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/passkey_api/login/finalize</c> - Verify the browser’s assertion. Returns the user_id the passkey belongs to; your backend then starts its own session. Pass user_id to require that exact user. Challenges are single use.</summary>
        /// <remarks>Needs the <c>passkey_api:write</c> scope.</remarks>
        public Task<JsonElement> LoginFinalizeAsync(PasskeyApiLoginFinalizeBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/passkey_api/login/finalize", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/passkey_api/users/{userId}/passkeys</c> - List the passkeys registered for one of your users.</summary>
        /// <remarks>Needs the <c>passkey_api:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string userId, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/passkey_api/users/{Enc(userId)}/passkeys", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/passkey_api/users/{userId}/passkeys/{id}</c> - Delete one of your user’s passkeys.</summary>
        /// <remarks>Needs the <c>passkey_api:write</c> scope.</remarks>
        public Task<JsonElement> DeleteAsync(string userId, string id, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Delete, $"/v1/passkey_api/users/{Enc(userId)}/passkeys/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/passkey_api/usage</c> - Passkeys stored through the Passkey API on this instance (the metered quantity).</summary>
        /// <remarks>Needs the <c>passkey_api:read</c> scope.</remarks>
        public Task<JsonElement> UsageAsync(CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/passkey_api/usage", cancellationToken: ct);
    }
}
