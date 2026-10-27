using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/connections</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("owner")] public Dictionary<string, object?>? Owner { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("kind")] public string? Kind { get; set; }
        /// <summary>Required. A built-in or custom OAuth provider key for oauth; any short key otherwise.</summary>
        [JsonPropertyName("provider")] public string? Provider { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>Required. Hostnames, or *.example.com wildcards (1 to 20).</summary>
        [JsonPropertyName("allowed_hosts")] public List<string>? AllowedHosts { get; set; }
        /// <summary>api_key: placement (header or query), name, scheme. custom_header: name. hmac_secret: algorithm, encoding, signature_header, prefix, timestamp_header. client_credentials: token_url, client_id, scopes, auth_style, grant_type, account_id, audience, resource, extra_params, request_format, token_header.</summary>
        [JsonPropertyName("config")] public Dictionary<string, object?>? Config { get; set; }
        /// <summary>Relay verbs allowed for this connection (default GET, HEAD, POST).</summary>
        [JsonPropertyName("allowed_methods")] public List<object?>? AllowedMethods { get; set; }
        /// <summary>The provider environment the credential belongs to. sandbox selects the provider's test hosts (Salesforce test.salesforce.com, PayPal and QuickBooks sandboxes, DocuSign demo...).</summary>
        [JsonPropertyName("environment")] public string? Environment { get; set; }
        /// <summary>Values that fill the provider's host templates for this one connection, for example {"subdomain":"acme"} (Zendesk), {"shop":"acme"} (Shopify), {"domain":"acme.my.salesforce.com"} (Salesforce My Domain), {"data_center":"eu"} (Zoho), {"tenant":"contoso.onmicrosoft.com"} (Microsoft), {"host":"git.examp</summary>
        [JsonPropertyName("host_params")] public Dictionary<string, object?>? HostParams { get; set; }
        /// <summary>Only data is accepted. A sign-in link is an external account.</summary>
        [JsonPropertyName("purpose")] public object? Purpose { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/connections/{id}</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("allowed_hosts")] public List<string>? AllowedHosts { get; set; }
        [JsonPropertyName("config")] public Dictionary<string, object?>? Config { get; set; }
        [JsonPropertyName("shared_by_user_id")] public string? SharedByUserId { get; set; }
        /// <summary>Transfer the connection to another organization or user of this instance. Its tokens move with it; nobody reconnects.</summary>
        [JsonPropertyName("owner")] public Dictionary<string, object?>? Owner { get; set; }
        /// <summary>The provider environment the credential belongs to. sandbox selects the provider's test hosts (Salesforce test.salesforce.com, PayPal and QuickBooks sandboxes, DocuSign demo...).</summary>
        [JsonPropertyName("environment")] public string? Environment { get; set; }
        /// <summary>Values that fill the provider's host templates for this one connection, for example {"subdomain":"acme"} (Zendesk), {"shop":"acme"} (Shopify), {"domain":"acme.my.salesforce.com"} (Salesforce My Domain), {"data_center":"eu"} (Zoho), {"tenant":"contoso.onmicrosoft.com"} (Microsoft), {"host":"git.examp</summary>
        [JsonPropertyName("host_params")] public Dictionary<string, object?>? HostParams { get; set; }
        [JsonPropertyName("allowed_methods")] public List<object?>? AllowedMethods { get; set; }
    }

    /// <summary>The body of <c>PUT /v1/connections/{id}/credentials</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsSetCredentialsBody
    {
        /// <summary>api_key.</summary>
        [JsonPropertyName("api_key")] public string? ApiKey { get; set; }
        /// <summary>bearer; or the token of a tba/three-legged oauth1 credential.</summary>
        [JsonPropertyName("token")] public string? Token { get; set; }
        /// <summary>basic (no colon).</summary>
        [JsonPropertyName("username")] public string? Username { get; set; }
        /// <summary>basic.</summary>
        [JsonPropertyName("password")] public string? Password { get; set; }
        /// <summary>custom_header.</summary>
        [JsonPropertyName("value")] public string? Value { get; set; }
        /// <summary>hmac_secret.</summary>
        [JsonPropertyName("secret")] public string? Secret { get; set; }
        /// <summary>client_credentials, or the app client id of a jwt_bearer with Basic client authentication.</summary>
        [JsonPropertyName("client_id")] public string? ClientId { get; set; }
        /// <summary>client_credentials / jwt_bearer client authentication.</summary>
        [JsonPropertyName("client_secret")] public string? ClientSecret { get; set; }
        /// <summary>https token endpoint (client_credentials, jwt_bearer); `{name}` placeholders are filled from host_params.</summary>
        [JsonPropertyName("token_url")] public string? TokenUrl { get; set; }
        [JsonPropertyName("scopes")] public List<string>? Scopes { get; set; }
        /// <summary>jwt_bearer / github_app_installation: a PEM private key (RSA or P-256). Held in the vault; never returned.</summary>
        [JsonPropertyName("private_key")] public string? PrivateKey { get; set; }
        /// <summary>jwt_bearer: have Atlas generate the key pair; the public JWK is returned in config.public_jwk for you to register at the provider.</summary>
        [JsonPropertyName("generate_key")] public bool? GenerateKey { get; set; }
        /// <summary>jwt_bearer: a Google service-account JSON key (a string or the parsed object).</summary>
        [JsonPropertyName("service_account_json")] public object? ServiceAccountJson { get; set; }
        /// <summary>jwt_bearer with a provider-issued JWT credential (RingCentral).</summary>
        [JsonPropertyName("assertion")] public string? Assertion { get; set; }
        /// <summary>oauth1.</summary>
        [JsonPropertyName("consumer_key")] public string? ConsumerKey { get; set; }
        /// <summary>oauth1.</summary>
        [JsonPropertyName("consumer_secret")] public string? ConsumerSecret { get; set; }
        /// <summary>oauth1.</summary>
        [JsonPropertyName("token_secret")] public string? TokenSecret { get; set; }
        /// <summary>Non-secret settings to merge (same keys as the connection config).</summary>
        [JsonPropertyName("config")] public Dictionary<string, object?>? Config { get; set; }
        /// <summary>When this credential expires: epoch milliseconds or an ISO date. Reminders are sent at 30, 7 and 1 days before.</summary>
        [JsonPropertyName("expires_at")] public object? ExpiresAt { get; set; }
    }

    /// <summary>The body of <c>POST /v1/connections/{id}/connect</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsConnectBody
    {
        /// <summary>Required. Where the owner lands after the provider returns. Must be an allowed origin of the instance.</summary>
        [JsonPropertyName("redirect_url")] public string? RedirectUrl { get; set; }
        /// <summary>Values that fill the provider's host templates for this one connection, for example {"subdomain":"acme"} (Zendesk), {"shop":"acme"} (Shopify), {"domain":"acme.my.salesforce.com"} (Salesforce My Domain), {"data_center":"eu"} (Zoho), {"tenant":"contoso.onmicrosoft.com"} (Microsoft), {"host":"git.examp</summary>
        [JsonPropertyName("host_params")] public Dictionary<string, object?>? HostParams { get; set; }
        /// <summary>The provider environment the credential belongs to. sandbox selects the provider's test hosts (Salesforce test.salesforce.com, PayPal and QuickBooks sandboxes, DocuSign demo...).</summary>
        [JsonPropertyName("environment")] public string? Environment { get; set; }
        /// <summary>Scopes to ask for on top of the provider defaults, the owner client's scopes and config.scopes.</summary>
        [JsonPropertyName("additional_scopes")] public List<string>? AdditionalScopes { get; set; }
        [JsonPropertyName("login_hint")] public string? LoginHint { get; set; }
        /// <summary>popup completes through postMessage like a popup sign-in.</summary>
        [JsonPropertyName("display")] public string? Display { get; set; }
        /// <summary>When the provider returns a different account than the one this connection already holds, create a sibling connection for it instead of answering 409.</summary>
        [JsonPropertyName("create_if_new")] public bool? CreateIfNew { get; set; }
        /// <summary>Informational: the user who is connecting (stored as authorized_by_user_id).</summary>
        [JsonPropertyName("connect_user_id")] public string? ConnectUserId { get; set; }
    }

    /// <summary>The body of <c>POST /v1/connections/{id}/revoke</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsRevokeBody
    {
    }

    /// <summary>The body of <c>POST /v1/connections/{id}/oauth1/sign</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsOAuth1SignBody
    {
        /// <summary>Default GET.</summary>
        [JsonPropertyName("method")] public string? Method { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("url")] public string? Url { get; set; }
        /// <summary>The request body, to sign when it is form-encoded.</summary>
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("content_type")] public string? ContentType { get; set; }
    }

    /// <summary>The body of <c>POST /v1/connections/{id}/relay</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsRelayBody
    {
        /// <summary>Must be in the connection's allowed_methods.</summary>
        [JsonPropertyName("method")] public string? Method { get; set; }
        /// <summary>Required. Absolute https URL on an allowed host.</summary>
        [JsonPropertyName("url")] public string? Url { get; set; }
        /// <summary>Extra request headers. Authorization, Cookie and Host cannot be set.</summary>
        [JsonPropertyName("headers")] public Dictionary<string, object?>? Headers { get; set; }
        /// <summary>POST, PUT, PATCH or DELETE: a string, or an object sent as JSON (up to 256 KiB).</summary>
        [JsonPropertyName("body")] public object? Body { get; set; }
        /// <summary>Where the provider returns after authorizing, to receive meta.authorize_url on a 402.</summary>
        [JsonPropertyName("redirect_url")] public string? RedirectUrl { get; set; }
        /// <summary>The user the authorization link is for (organization-owned connections).</summary>
        [JsonPropertyName("connect_user_id")] public string? ConnectUserId { get; set; }
    }

    /// <summary>The body of <c>POST /v1/connections/{id}/token/refresh</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsRefreshTokenBody
    {
        /// <summary>Why you are refreshing; recorded in the audit log.</summary>
        [JsonPropertyName("reason")] public string? Reason { get; set; }
        /// <summary>SHA-256 (hex) of the access token the provider refused.</summary>
        [JsonPropertyName("stale_token_hash")] public string? StaleTokenHash { get; set; }
    }

    /// <summary>The body of <c>POST /v1/connections/tokens/batch</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsTokensBody
    {
        /// <summary>Required. Connection ids (duplicates are read once).</summary>
        [JsonPropertyName("ids")] public List<string>? Ids { get; set; }
        [JsonPropertyName("min_ttl_seconds")] public long? MinTtlSeconds { get; set; }
    }

    /// <summary>The body of <c>POST /v1/connections/import</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsImportConnectionsBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("connections")] public List<object?>? Connections { get; set; }
        /// <summary>Spend each refresh token against the provider and store what it issues.</summary>
        [JsonPropertyName("validate")] public bool? Validate { get; set; }
        [JsonPropertyName("dry_run")] public bool? DryRun { get; set; }
    }

    /// <summary>The body of <c>POST /v1/connections/export</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsExportConnectionsBody
    {
        [JsonPropertyName("owner_type")] public string? OwnerType { get; set; }
        [JsonPropertyName("owner_id")] public string? OwnerId { get; set; }
        [JsonPropertyName("provider")] public string? Provider { get; set; }
        /// <summary>The provider environment the credential belongs to. sandbox selects the provider's test hosts (Salesforce test.salesforce.com, PayPal and QuickBooks sandboxes, DocuSign demo...).</summary>
        [JsonPropertyName("environment")] public string? Environment { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("limit")] public long? Limit { get; set; }
        [JsonPropertyName("starting_after")] public string? StartingAfter { get; set; }
        [JsonPropertyName("include_secrets")] public bool? IncludeSecrets { get; set; }
        /// <summary>Required with include_secrets: a PUBLIC JWK. A key with private parameters is refused.</summary>
        [JsonPropertyName("recipient_public_key")] public Dictionary<string, object?>? RecipientPublicKey { get; set; }
    }

    /// <summary>The body of <c>POST /v1/connections/{id}/credentials/rotate</c>. Only the fields set are sent.</summary>
    public sealed class ConnectionsRotateCredentialsBody
    {
        [JsonPropertyName("overlap_seconds")] public long? OverlapSeconds { get; set; }
        /// <summary>Epoch milliseconds or an ISO date.</summary>
        [JsonPropertyName("expires_at")] public object? ExpiresAt { get; set; }
    }

    /// <summary>Operations of <c>connections</c>.</summary>
    public sealed partial class ConnectionsResource : ResourceBase
    {
        public ConnectionsResource(AtlasTransport t) : base(t) { }

        private ConnectionsOAuthClientsResource? _oAuthClients;
        /// <summary>The <c>connections.oauthClients</c> operations.</summary>
        public ConnectionsOAuthClientsResource OAuthClients => _oAuthClients ??= new ConnectionsOAuthClientsResource(Transport);

        private ConnectionsWebhooksResource? _webhooks;
        /// <summary>The <c>connections.webhooks</c> operations.</summary>
        public ConnectionsWebhooksResource Webhooks => _webhooks ??= new ConnectionsWebhooksResource(Transport);

        /// <summary><c>POST /v1/connections</c> - Define a connection: an owner (an organization or a user) and a third-party API. `kind` is oauth (rides a linked provider account; for an organization, the account of the member who shared it), api_key or client_credentials (a credential the owner enters). `allowed_hosts` lists the only hosts the relay may call. An owner may hold several connections of one provider (one per provider account): create one per account, then call `POST /v1/connections/{id}/connect`. A connection is never a sign-in method.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(ConnectionsCreateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/connections", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/connections</c> - List connections, newest first, with each one's live status. Filter by owner or provider.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(string? ownerType = null, string? ownerId = null, string? provider = null, string? environment = null, string? status = null, string? startingAfter = null, long? limit = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/connections", query: Q(("owner_type", ownerType), ("owner_id", ownerId), ("provider", provider), ("environment", environment), ("status", status), ("starting_after", startingAfter), ("limit", limit)), cancellationToken: ct);

        /// <summary><c>GET /v1/connections/{id}</c> - Read one connection with its current status.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/connections/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/connections/{id}</c> - Rename a connection, change its allowed hosts or settings, transfer it to another owner with `owner` (the stored tokens move with it, no reconnect), change `environment` or `host_params` (an oauth connection that already holds a token becomes needs_reauth, because the token belongs to the old host), or (organization-owned oauth) set `shared_by_user_id` to copy that member's linked account into the connection.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, ConnectionsUpdateBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/connections/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/connections/{id}</c> - Delete a connection and its stored credential (204). With `revoke=true` the token is first revoked at the provider and the answer is 200 with what the provider said (provider_revoked true, false or "unsupported", provider_error, local_cleared); the connection is deleted either way.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, bool? revoke = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/connections/{Enc(id)}", query: Q(("dry_run", dryRun), ("revoke", revoke)), cancellationToken: ct);

        /// <summary><c>PUT /v1/connections/{id}/credentials</c> - Enter the credential for a non-OAuth connection (api_key, bearer, basic, custom_header, hmac_secret, client_credentials, jwt_bearer, oauth1, github_app_installation). Write-only: it is sealed in the vault and never returned. Replaces the earlier credential as a new vault version; use POST .../credentials/rotate to keep the old version valid for an overlap window.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> SetCredentialsAsync(string id, ConnectionsSetCredentialsBody body, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Put, $"/v1/connections/{Enc(id)}/credentials", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/connections/{id}/connect</c> - Start the provider authorization for this connection and return the URL to send the owner to. The tokens are stored on the connection itself, never on a user: no external account is created, no sign-in method is added, no email is proven and no step-up is needed. `host_params` fill the provider's host templates for this connection (validated against a per-provider allowlist; 400 `host_params.&lt;key&gt;` otherwise). Reconnecting the same provider account updates the same connection; a different account answers 409 unless `create_if_new` is true. An owner OAuth client (`PUT /v1/connections/oauth_clients`) overrides the instance client.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> ConnectAsync(string id, ConnectionsConnectBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/connections/{Enc(id)}/connect", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/connections/{id}/revoke</c> - Revoke the stored token at the provider, delete it from the vault and mark the connection revoked (connection.revoked is sent once). The response is the connection plus what the provider said: provider_revoked is true only when it confirmed, "unsupported" when it has no revoke endpoint, false when it refused or was unreachable (provider_error says why). The local token is cleared in every case. Connect again to use it.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> RevokeAsync(string id, ConnectionsRevokeBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/connections/{Enc(id)}/revoke", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/connections/{id}/oauth1/sign</c> - Have Atlas sign a request with an oauth1 connection (NetSuite token-based authentication: HMAC-SHA256 with the realm; X Ads and other three-legged providers: HMAC-SHA1) and return the `Authorization: OAuth ...` header. The consumer and token secrets never leave Atlas. The URL must be https on an allowed host; a form-encoded body is part of the signature. Needs connections:sign.</summary>
        /// <remarks>Needs the <c>connections:sign</c> scope.</remarks>
        public Task<JsonElement> OAuth1SignAsync(string id, ConnectionsOAuth1SignBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/connections/{Enc(id)}/oauth1/sign", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/connections/{id}/relay</c> - Make a provider API call server-side with this connection's credential, so the credential never reaches your code: Atlas injects an api_key (header or query), bearer, basic or custom header, signs with an hmac_secret or OAuth 1.0a, or sends a minted client_credentials / jwt_bearer / GitHub App installation / admin consent token. Only https calls to the connection's allowed_hosts are made, with the verbs in allowed_methods (default GET, HEAD, POST). When the connection is not connected, or the provider rejects the credential, the answer is 402 CONNECTION_REQUIRED with `meta.reason` (not_connected or needs_reauth) and, for a user-owned OAuth connection when you pass `redirect_url`, `meta.authorize_url`. The audit log records the method, host and status only.</summary>
        /// <remarks>Needs the <c>oauth_tokens:read</c> scope.</remarks>
        public Task<JsonElement> RelayAsync(string id, ConnectionsRelayBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/connections/{Enc(id)}/relay", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/connections/{id}/token</c> - The connection's live token for your backend. An oauth connection's token is refreshed first when it is expired or about to be (single-flight across processes, using the client that issued it and the connection's host_params). Built for sync workers: the response carries an `ETag` and `Cache-Control: private, max-age=&lt;seconds until just before expires_at&gt;`, so cache it until then; send `If-None-Match` and an unchanged, still-valid token answers 304 with no body and no vault read; `min_ttl_seconds` refreshes first when less than that much validity remains. A grant connection (client_credentials, jwt_bearer, github_app_installation, admin_consent) has its token minted from the credential Atlas holds instead, answered with `Cache-Control: no-store` (no ETag); pass `subject` to a jwt_bearer connection to act as that user (Google domain-wide delegation). Includes the captured handles (`metadata`, `token_response`) and any further named credentials. 402 CONNECTION_REQUIRED with `meta.reason` not_connected, needs_reauth, revoked or refresh_failed when there is no usable token. When the provider answers 401 to the token, call `POST /v1/connections/{id}/token/refresh` instead of re-reading. Rate limit: 6,000 reads per minute per secret key and 300 per minute per connection (RateLimit-* headers on every response, 429 with Retry-After beyond them). Reads are audited as connection.token_read once per connection per minute with the read count (a read that refreshed is always audited); the token itself is never logged.</summary>
        /// <remarks>Needs the <c>connections:token</c> scope.</remarks>
        public Task<JsonElement> TokenAsync(string id, long? minTtlSeconds = null, string? subject = null, string? refresh = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/connections/{Enc(id)}/token", query: Q(("min_ttl_seconds", minTtlSeconds), ("subject", subject), ("refresh", refresh)), cancellationToken: ct);

        /// <summary><c>POST /v1/connections/{id}/token/refresh</c> - Force a refresh after the provider refused the access token (401). Single-flight: however many callers saw the same 401, the provider is called once. Send `stale_token_hash` (lowercase hex SHA-256 of the access token you were refused with); if the stored token is already a different one, a peer has refreshed and the current token is returned without calling the provider (`refreshed: false`). Same body, ETag and 402 behaviour as the token read; the response is never cached. Counts against the same rate limit as the token read.</summary>
        /// <remarks>Needs the <c>connections:token</c> scope.</remarks>
        public Task<JsonElement> RefreshTokenAsync(string id, ConnectionsRefreshTokenBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/connections/{Enc(id)}/token/refresh", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/connections/tokens/batch</c> - Read the tokens of up to 100 connections in one call (min_ttl_seconds applies to each). Each id gets its own result: status ok with the token, or status error with a code and reason (not_connected, needs_reauth, revoked, refresh_failed, unknown_connection), so one dead connection never fails the batch. The response is never cached. Counts once against the token-read rate limit.</summary>
        /// <remarks>Needs the <c>connections:token</c> scope.</remarks>
        public Task<JsonElement> TokensAsync(ConnectionsTokensBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/connections/tokens/batch", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/connections/import</c> - Bulk-import existing provider refresh tokens as connections, up to 500 rows, so nobody reconnects. Each row: provider, owner, external_id (the provider-side account: how a re-import finds the same connection), refresh_token (or access_token), and optionally scopes, metadata, environment, host_params, name, allowed_hosts. By default (validate true) each refresh token is spent once against the provider with the OAuth client that will own the connection, and the tokens the provider issues are stored, so the imported copy stops being current on a rotating provider. A failing row creates and changes nothing. dry_run checks shapes only and never calls the provider. Idempotent on Idempotency-Key. Tokens go straight to the vault and never appear in the answer or the audit log.</summary>
        /// <remarks>Needs the <c>connections:import</c> scope.</remarks>
        public Task<JsonElement> ImportConnectionsAsync(ConnectionsImportConnectionsBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/connections/import", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/connections/export</c> - Export connections. Without include_secrets: metadata only (provider, owner, account, environment, scopes, status), scope connections:export. With include_secrets: needs scope connections:export_secrets, the header X-Atlas-Confirm: export-secrets and recipient_public_key (a public JWK: RSA of 3072 bits or more, or EC P-256/384/521). The tokens come back as one compact JWE sealed to that key that only its holder can open; Atlas keeps no plaintext. Pages hold up to 200 connections with secrets (500 without); follow next_cursor with starting_after. Every export is audited with its count, never the secrets. Opened, each connection is a valid import row.</summary>
        /// <remarks>Needs the <c>connections:export</c> scope.</remarks>
        public Task<JsonElement> ExportConnectionsAsync(ConnectionsExportConnectionsBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/connections/export", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/connections/{id}/secret</c> - Read the credential an api_key, bearer, basic, custom_header or hmac_secret connection stores, for your backend. Needs the dedicated connections:secret_read scope; every read is audited as connection.secret.read with the caller, connection and version (never the value) and the response is `Cache-Control: no-store`. `version` is current (default), previous (only while a rotation's overlap window is open) or a version number. A signing grant's private key is never readable: 409 `not_readable`. There is no browser or session route that reads a credential.</summary>
        /// <remarks>Needs the <c>connections:secret_read</c> scope.</remarks>
        public Task<JsonElement> SecretAsync(string id, string? version = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/connections/{Enc(id)}/secret", query: Q(("version", version)), cancellationToken: ct);

        /// <summary><c>GET /v1/connections/{id}/credentials/versions</c> - The vault versions of a connection's entered credential (numbers and times only, never values) and the open rotation window.</summary>
        /// <remarks>Needs the <c>organizations:read</c> scope.</remarks>
        public Task<JsonElement> SecretVersionsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/connections/{Enc(id)}/credentials/versions", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/connections/{id}/credentials/rotate</c> - Rotate the credential: the body is the new credential (the same fields as PUT credentials). It becomes the current vault version; the replaced one stays valid for `overlap_seconds` (default 3600, up to 30 days): the relay falls back to it when the provider refuses the new one, and GET secret?version=previous returns it. `expires_at` sets the new credential's expiry. The secret never reaches the audit log.</summary>
        /// <remarks>Needs the <c>organizations:write</c> scope.</remarks>
        public Task<JsonElement> RotateCredentialsAsync(string id, ConnectionsRotateCredentialsBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/connections/{Enc(id)}/credentials/rotate", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
