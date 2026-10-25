using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Resources;

namespace Atlas
{
    // Operator sign-in (`atlas login`) and token exchange, without a long-lived key, for CLIs written in C#.
    //
    //  1. StartLoginAsync runs the RFC 8628 device flow against Atlas's own sign-in with the public atlas-cli client:
    //     show the person VerificationUriComplete / UserCode, then
    //  2. PollLoginAsync until it returns the operator access token, then
    //  3. ExchangeTokenAsync swaps it (RFC 8693) for a ONE-HOUR Atlas token bound to one instance (ins_…, Backend API)
    //     or app/account (app_… / acct_…, Platform API), with only the scopes you ask for.
    //
    // Root actions (create an app or instance, mint a key) need a multi-factor sign-in within 5 minutes; otherwise the
    // API answers 403 STEP_UP_REQUIRED.

    /// <summary>Tunes the operator helpers.</summary>
    public sealed class OperatorOptions
    {
        /// <summary>The Atlas API origin (default <see cref="AtlasClientOptions.DefaultApiUrl"/>).</summary>
        public string ApiUrl { get; set; } = AtlasClientOptions.DefaultApiUrl;

        public HttpClient? HttpClient { get; set; }

        /// <summary>Replaces the wait between polls (tests).</summary>
        public Func<TimeSpan, CancellationToken, Task>? Delay { get; set; }

        /// <summary>Replaces the clock (tests).</summary>
        public Func<DateTimeOffset>? Now { get; set; }
    }

    /// <summary><c>GET /v1/operator</c>: where the device flow and token exchange live.</summary>
    public sealed class OperatorConfiguration
    {
        public string Issuer { get; init; } = "";
        public string DeviceAuthorizationEndpoint { get; init; } = "";
        public string TokenEndpoint { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string Scope { get; init; } = "";
        public string TokenExchangeEndpoint { get; init; } = "";
        public string TokenExchangeAudience { get; init; } = "";
        public int RootActionMaxMfaAgeSeconds { get; init; }
        /// <summary>Other products that accept Atlas operators (pass one as the audience of a login).</summary>
        public List<string>? Audiences { get; init; }
        /// <summary>The claims an operator access token carries.</summary>
        public List<string>? AccessTokenClaims { get; init; }
    }

    /// <summary>The RFC 8628 device authorization response.</summary>
    public sealed class OperatorDeviceAuthorization
    {
        public string DeviceCode { get; init; } = "";
        public string UserCode { get; init; } = "";
        public string VerificationUri { get; init; } = "";
        public string VerificationUriComplete { get; init; } = "";
        public int ExpiresIn { get; init; }
        public int Interval { get; init; }

        /// <summary>The operator configuration the flow runs against.</summary>
        [JsonIgnore] public OperatorConfiguration Config { get; internal set; } = new OperatorConfiguration();
    }

    /// <summary>A token endpoint answer.</summary>
    public sealed class OAuthTokenResponse
    {
        public string AccessToken { get; init; } = "";
        public string TokenType { get; init; } = "";
        public int ExpiresIn { get; init; }
        public string? Scope { get; init; }
        public string? RefreshToken { get; init; }
        public string? IdToken { get; init; }
        public string? IssuedTokenType { get; init; }
        /// <summary>The resource a token exchange bound the token to.</summary>
        public string? Resource { get; init; }
    }

    /// <summary>
    /// A failed device-flow, token-exchange or token request. <see cref="Error"/> is the OAuth error code
    /// (<c>access_denied</c>, <c>expired_token</c>, <c>invalid_grant</c>, <c>STEP_UP_REQUIRED</c>, …).
    /// </summary>
    public sealed class OAuthFlowException : Exception
    {
        public OAuthFlowException(string error, string? description, int status)
            : base(string.IsNullOrEmpty(description) ? $"atlas: {error} (HTTP {status})" : $"atlas: {error}: {description} (HTTP {status})")
        {
            Error = error;
            Description = description;
            Status = status;
        }

        public string Error { get; }
        public string? Description { get; }
        public int Status { get; }
    }

    /// <summary>One outcome of <see cref="OperatorClient.RevokeLeakedKeysAsync"/>.</summary>
    public sealed class LeakedKeyResult
    {
        public int Index { get; init; }
        public string Kind { get; init; } = "";
        /// <summary><c>revoked</c>, <c>already_revoked</c> or <c>not_found</c>.</summary>
        public string Outcome { get; init; } = "";
    }

    /// <summary>The operator device flow, RFC 8693 token exchange, partner client_credentials and leaked-key revocation.</summary>
    public static class OperatorClient
    {
        private static readonly HttpClient SharedHttp = new HttpClient();

        private static string ApiUrl(OperatorOptions? o) =>
            (o?.ApiUrl is { Length: > 0 } u ? u : AtlasClientOptions.DefaultApiUrl).TrimEnd('/');

        private static HttpClient Http(OperatorOptions? o) => o?.HttpClient ?? SharedHttp;

        private static async Task<(JsonElement Body, int Status)> PostFormAsync(OperatorOptions? o, string endpoint, IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new FormUrlEncodedContent(form) };
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var resp = await Http(o).SendAsync(req, ct).ConfigureAwait(false);
            var text = await ReadAsync(resp, ct).ConfigureAwait(false);
            return (ParseObject(text), (int)resp.StatusCode);
        }

        private static async Task<string> ReadAsync(HttpResponseMessage resp, CancellationToken ct)
        {
#if NET8_0_OR_GREATER
            return await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            ct.ThrowIfCancellationRequested();
            return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }

        private static JsonElement ParseObject(string text)
        {
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
                return doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                using var doc = JsonDocument.Parse("{}");
                return doc.RootElement.Clone();
            }
        }

        private static string? Str(JsonElement body, string name) =>
            body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        /// <summary>Build an error from a token-endpoint answer, accepting both the OAuth shape and the Atlas error envelope.</summary>
        private static OAuthFlowException FlowError(JsonElement body, int status, string fallback)
        {
            var code = Str(body, "error");
            var desc = Str(body, "error_description");
            if (string.IsNullOrEmpty(code) && body.ValueKind == JsonValueKind.Object
                && body.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0)
            {
                var first = errs[0];
                code = Str(first, "code");
                if (string.IsNullOrEmpty(desc)) desc = Str(first, "message");
            }
            return new OAuthFlowException(string.IsNullOrEmpty(code) ? "error" : code!, string.IsNullOrEmpty(desc) ? fallback : desc, status);
        }

        private static T Decode<T>(JsonElement body) => AtlasJson.Deserialize<T>(body.GetRawText())!;

        /// <summary><c>GET /v1/operator</c> (no key needed).</summary>
        public static async Task<OperatorConfiguration> GetConfigurationAsync(OperatorOptions? options = null, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, ApiUrl(options) + "/v1/operator");
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var resp = await Http(options).SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new OAuthFlowException("not_configured", "The operator issuer is not configured on this deployment.", (int)resp.StatusCode);
            return AtlasJson.Deserialize<OperatorConfiguration>(await ReadAsync(resp, ct).ConfigureAwait(false))!;
        }

        /// <summary>Begin the device flow. <paramref name="config"/> may be null to read it from <c>GET /v1/operator</c>.</summary>
        public static async Task<OperatorDeviceAuthorization> StartLoginAsync(OperatorConfiguration? config = null, OperatorOptions? options = null, CancellationToken ct = default)
        {
            config ??= await GetConfigurationAsync(options, ct).ConfigureAwait(false);
            var (body, status) = await PostFormAsync(options, config.DeviceAuthorizationEndpoint, new[]
            {
                new KeyValuePair<string, string>("client_id", config.ClientId),
                new KeyValuePair<string, string>("scope", config.Scope),
            }, ct).ConfigureAwait(false);
            if (status != 200) throw FlowError(body, status, "Device authorization failed.");
            var start = Decode<OperatorDeviceAuthorization>(body);
            start.Config = config;
            return start;
        }

        /// <summary>
        /// Poll until the person approves (returning the operator token), denies or the code expires
        /// (an <see cref="OAuthFlowException"/>). Honours the interval, <c>slow_down</c> and the token.
        /// </summary>
        public static async Task<OAuthTokenResponse> PollLoginAsync(OperatorDeviceAuthorization start, OperatorOptions? options = null, CancellationToken ct = default)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            var interval = start.Interval < 1 ? 1 : start.Interval;
            var wait = TimeSpan.FromSeconds(interval);
            Func<DateTimeOffset> now = options?.Now ?? (() => DateTimeOffset.UtcNow);
            var deadline = now().AddSeconds(start.ExpiresIn);
            while (now() < deadline)
            {
                if (options?.Delay != null) await options.Delay(wait, ct).ConfigureAwait(false);
                else await Task.Delay(wait, ct).ConfigureAwait(false);
                var (body, status) = await PostFormAsync(options, start.Config.TokenEndpoint, new[]
                {
                    new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                    new KeyValuePair<string, string>("device_code", start.DeviceCode),
                    new KeyValuePair<string, string>("client_id", start.Config.ClientId),
                }, ct).ConfigureAwait(false);
                if (status == 200) return Decode<OAuthTokenResponse>(body);
                var code = Str(body, "error");
                if (code == "authorization_pending") continue;
                if (code == "slow_down") { wait += TimeSpan.FromSeconds(5); continue; }
                throw FlowError(body, status, "Sign-in failed.");
            }
            throw new OAuthFlowException("expired_token", "The sign-in code expired. Run the login again.", 400);
        }

        /// <summary>
        /// Swap an operator token (RFC 8693) for a one-hour, resource-bound Atlas token. <paramref name="resource"/> is an
        /// <c>ins_…</c> / <c>app_…</c> / <c>acct_…</c> id; <paramref name="scopes"/> are the scopes to keep.
        /// </summary>
        public static async Task<OAuthTokenResponse> ExchangeTokenAsync(string subjectToken, string resource, IEnumerable<string> scopes, OperatorOptions? options = null, CancellationToken ct = default)
        {
            var (body, status) = await PostFormAsync(options, ApiUrl(options) + "/v1/oauth/token", new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange"),
                new KeyValuePair<string, string>("subject_token", subjectToken),
                new KeyValuePair<string, string>("subject_token_type", "urn:ietf:params:oauth:token-type:access_token"),
                new KeyValuePair<string, string>("audience", "atlas"),
                new KeyValuePair<string, string>("resource", resource),
                new KeyValuePair<string, string>("scope", string.Join(" ", scopes)),
            }, ct).ConfigureAwait(false);
            if (status != 200) throw FlowError(body, status, "Token exchange failed.");
            return Decode<OAuthTokenResponse>(body);
        }

        /// <summary><c>client_credentials</c> with a partner link's credentials (a partner service calling an instance). <paramref name="scope"/> may be null.</summary>
        public static async Task<OAuthTokenResponse> PartnerLinkTokenAsync(string clientId, string clientSecret, string? scope = null, OperatorOptions? options = null, CancellationToken ct = default)
        {
            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
            };
            if (!string.IsNullOrEmpty(scope)) form.Add(new KeyValuePair<string, string>("scope", scope!));
            var (body, status) = await PostFormAsync(options, ApiUrl(options) + "/v1/oauth/token", form, ct).ConfigureAwait(false);
            if (status != 200) throw FlowError(body, status, "Token request failed.");
            return Decode<OAuthTokenResponse>(body);
        }

        /// <summary>Revoke leaked Atlas credentials (<c>POST /v1/keys/revoke_leaked</c>, no key needed). <paramref name="sourceUrl"/> optionally says where the secret was found.</summary>
        public static async Task<List<LeakedKeyResult>> RevokeLeakedKeysAsync(IEnumerable<string> secrets, string? sourceUrl = null, OperatorOptions? options = null, CancellationToken ct = default)
        {
            var payload = new Dictionary<string, object?> { ["secrets"] = secrets };
            if (!string.IsNullOrEmpty(sourceUrl)) payload["url"] = sourceUrl;
            using var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl(options) + "/v1/keys/revoke_leaked")
            {
                Content = new StringContent(AtlasJson.Serialize(payload), Encoding.UTF8, "application/json"),
            };
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var resp = await Http(options).SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new OAuthFlowException("request_failed", $"revoke_leaked failed ({(int)resp.StatusCode}).", (int)resp.StatusCode);
            var parsed = AtlasJson.Deserialize<LeakedKeyEnvelope>(await ReadAsync(resp, ct).ConfigureAwait(false));
            return parsed?.Results ?? new List<LeakedKeyResult>();
        }

        private sealed class LeakedKeyEnvelope
        {
            public List<LeakedKeyResult>? Results { get; init; }
        }

        /// <summary>
        /// Build a client signed with a one-hour Atlas access token from <see cref="ExchangeTokenAsync"/>. An <c>ins_…</c> token
        /// drives the Backend API (<c>Users</c>, …); an <c>app_…</c> / <c>acct_…</c> token drives <c>Platform</c>.
        /// </summary>
        public static AtlasClient NewClientFromAccessToken(string token, string? apiUrl = null, HttpClient? httpClient = null)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentException("atlas: empty access token", nameof(token));
            var o = new AtlasClientOptions { SecretKey = token, PlatformKey = token, HttpClient = httpClient };
            if (!string.IsNullOrEmpty(apiUrl)) o.ApiUrl = apiUrl!;
            return new AtlasClient(o);
        }
    }

    /// <summary>
    /// The partner side of the partner link request protocol (no Atlas credential): any product asks an instance for a link
    /// with <c>POST /v1/partner_link_requests</c>; the credentials arrive at its callback signed with the request secret
    /// (verify them with <see cref="Atlas.Verification.WebhookVerifier"/> using that secret).
    /// </summary>
    public static class PartnerLinkProtocol
    {
        private static readonly HttpClient SharedHttp = new HttpClient();

        /// <summary>Ask an instance for a link. The answer carries <c>RequestSecret</c> once.</summary>
        public static async Task<PartnerLinkRequest> CreateRequestAsync(CreatePartnerLinkRequestBody body, string? apiUrl = null, HttpClient? http = null, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, (apiUrl ?? AtlasClientOptions.DefaultApiUrl).TrimEnd('/') + "/v1/partner_link_requests")
            {
                Content = new StringContent(AtlasJson.Serialize(body), Encoding.UTF8, "application/json"),
            };
            return await SendAsync(req, http, ct).ConfigureAwait(false);
        }

        /// <summary>Read a request as the partner, presenting the request secret as the bearer credential.</summary>
        public static async Task<PartnerLinkRequest> GetRequestAsync(string id, string requestSecret, string? apiUrl = null, HttpClient? http = null, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, (apiUrl ?? AtlasClientOptions.DefaultApiUrl).TrimEnd('/') + "/v1/partner_link_requests/" + Uri.EscapeDataString(id));
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + requestSecret);
            return await SendAsync(req, http, ct).ConfigureAwait(false);
        }

        private static async Task<PartnerLinkRequest> SendAsync(HttpRequestMessage req, HttpClient? http, CancellationToken ct)
        {
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var resp = await (http ?? SharedHttp).SendAsync(req, ct).ConfigureAwait(false);
            string text;
#if NET8_0_OR_GREATER
            text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
            if (!resp.IsSuccessStatusCode)
            {
                var envelope = string.IsNullOrEmpty(text) ? null : AtlasJson.Deserialize<AtlasErrorEnvelope>(text);
                var errors = envelope?.Errors ?? new List<AtlasErrorItem> { new AtlasErrorItem { Code = "UNKNOWN", Message = $"Atlas API request failed with status {(int)resp.StatusCode}" } };
                throw AtlasException.FromStatus((int)resp.StatusCode, errors);
            }
            return AtlasJson.Deserialize<PartnerLinkRequest>(text)!;
        }
    }
}
