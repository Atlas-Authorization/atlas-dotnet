using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Native
{
    /// <summary>A live Atlas session held outside a cookie.</summary>
    public sealed class NativeSession
    {
        /// <summary>The short-lived session JWT; send it as <c>Authorization: Bearer</c>.</summary>
        public string SessionToken { get; init; } = "";
        /// <summary>Mints the next session token; it ROTATES on every refresh, so always store the new one.</summary>
        public string RefreshToken { get; init; } = "";
        public string SessionId { get; init; } = "";
        /// <summary>When <see cref="SessionToken"/> expires (from the server's <c>expires_in</c>); null when unknown, which refreshes on first use.</summary>
        public DateTimeOffset? ExpiresAt { get; init; }
    }

    /// <summary>Where the session is kept between launches. Use the platform's secure storage.</summary>
    public interface ISessionStore
    {
        Task<NativeSession?> LoadAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(NativeSession session, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
    }

    public sealed class MemorySessionStore : ISessionStore
    {
        private NativeSession? _session;
        public Task<NativeSession?> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_session);
        public Task SaveAsync(NativeSession session, CancellationToken cancellationToken = default) { _session = session; return Task.CompletedTask; }
        public Task ClearAsync(CancellationToken cancellationToken = default) { _session = null; return Task.CompletedTask; }
    }

    /// <summary>Opens the system browser on the authorize URL and returns the URL it was redirected back to.</summary>
    public interface IAuthBrowser
    {
        /// <exception cref="OperationCanceledException">When the person closes the browser without finishing.</exception>
        Task<Uri> AuthenticateAsync(Uri authorizeUrl, Uri callbackUrl, CancellationToken cancellationToken = default);
    }

    public sealed class AtlasNativeOptions
    {
        /// <summary>The instance's frontend API origin (the <c>iss</c> of its tokens), e.g. <c>https://auth.example.com</c>. Required.</summary>
        public string Issuer { get; set; } = "";
        /// <summary>The instance publishable key (<c>pk_...</c>), sent when the session is refreshed. Required.</summary>
        public string PublishableKey { get; set; } = "";
        /// <summary>Your first-party OAuth client (public, PKCE). Required.</summary>
        public string ClientId { get; set; } = "";
        /// <summary>The redirect URI registered for the client, e.g. <c>myapp://callback</c>. Required.</summary>
        public string RedirectUri { get; set; } = "";
        public string Scope { get; set; } = "openid profile email";
        /// <summary>Rotate the session token once it is within this long of expiry. Default 10 seconds.</summary>
        public TimeSpan RefreshLead { get; set; } = TimeSpan.FromSeconds(10);
        public HttpClient? HttpClient { get; set; }
        public Func<DateTimeOffset>? Now { get; set; }
    }

    public sealed class AtlasNativeException : Exception
    {
        public AtlasNativeException(string message) : base(message) { }
    }

    /// <summary>PKCE (RFC 7636) S256 helpers.</summary>
    public static class Pkce
    {
        public static (string Verifier, string Challenge) Create()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            var verifier = Base64Url(bytes);
            return (verifier, ChallengeFor(verifier));
        }

        public static string ChallengeFor(string verifier)
            => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        internal static string Base64Url(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Signs a person in through Atlas's hosted pages (authorization code + PKCE in the system browser),
    /// trades the resulting OAuth access token for an Atlas session, and keeps that session fresh.
    /// Sessions are rotated lazily, single-flight, and persisted to the <see cref="ISessionStore"/> each time.
    /// </summary>
    public sealed class AtlasNativeClient : IDisposable
    {
        private const string TokenExchangeGrant = "urn:ietf:params:oauth:grant-type:token-exchange";
        private const string AccessTokenType = "urn:ietf:params:oauth:token-type:access_token";
        private const string SessionTokenType = "urn:atlas:token-type:session";

        private readonly AtlasNativeOptions _options;
        private readonly IAuthBrowser _browser;
        private readonly ISessionStore _store;
        private readonly HttpClient _http;
        private readonly bool _ownsHttp;
        private readonly Func<DateTimeOffset> _now;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Uri _issuer;
        private NativeSession? _session;
        private bool _loaded;

        public AtlasNativeClient(AtlasNativeOptions options, IAuthBrowser browser, ISessionStore store)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            foreach (var (name, value) in new[] { ("Issuer", options.Issuer), ("PublishableKey", options.PublishableKey), ("ClientId", options.ClientId), ("RedirectUri", options.RedirectUri) })
                if (string.IsNullOrEmpty(value)) throw new ArgumentException($"AtlasNativeOptions.{name} is required.");
            _issuer = new Uri(options.Issuer.TrimEnd('/') + "/");
            if (_issuer.Scheme != Uri.UriSchemeHttps && !(_issuer.IsLoopback && _issuer.Scheme == Uri.UriSchemeHttp))
                throw new ArgumentException("AtlasNativeOptions.Issuer must be an https URL.");
            _ownsHttp = options.HttpClient == null;
            _http = options.HttpClient ?? new HttpClient();
            _now = options.Now ?? (() => DateTimeOffset.UtcNow);
        }

        /// <summary>True when a session is held (it may need a refresh before use).</summary>
        public async Task<bool> IsSignedInAsync(CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return _session != null;
        }

        /// <summary>Raised after sign-in, sign-out, and when a failed refresh ends the session.</summary>
        public event Action<bool>? SignedInChanged;

        /// <summary>Run the hosted sign-in and store the session. Throws <see cref="AtlasNativeException"/> on any refusal.</summary>
        public async Task SignInAsync(CancellationToken cancellationToken = default)
        {
            var (verifier, challenge) = Pkce.Create();
            var state = Pkce.Base64Url(RandomNumberGenerator.GetBytes(16));
            var (authorizeEndpoint, tokenEndpoint) = await DiscoverAsync(cancellationToken).ConfigureAwait(false);

            var query = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = _options.ClientId,
                ["redirect_uri"] = _options.RedirectUri,
                ["scope"] = _options.Scope,
                ["state"] = state,
                ["code_challenge"] = challenge,
                ["code_challenge_method"] = "S256",
            };
            var authorizeUrl = new UriBuilder(authorizeEndpoint) { Query = Form(query) }.Uri;
            var callback = await _browser.AuthenticateAsync(authorizeUrl, new Uri(_options.RedirectUri), cancellationToken).ConfigureAwait(false);

            var returned = ParseQuery(callback.Query);
            if (returned.TryGetValue("error", out var error)) throw new AtlasNativeException($"Sign-in was refused: {error}.");
            // The state ties this callback to the request we started; compare without leaking timing.
            if (!returned.TryGetValue("state", out var returnedState)
                || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(returnedState), Encoding.ASCII.GetBytes(state)))
                throw new AtlasNativeException("The sign-in callback did not match the request.");
            if (!returned.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
                throw new AtlasNativeException("The sign-in callback carried no code.");

            var tokens = await PostFormAsync(tokenEndpoint, new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _options.RedirectUri,
                ["client_id"] = _options.ClientId,
                ["code_verifier"] = verifier,
            }, cancellationToken).ConfigureAwait(false);
            var accessToken = Str(tokens, "access_token") ?? throw new AtlasNativeException("The token endpoint returned no access token.");

            var exchanged = await PostFormAsync(new Uri(_issuer, "oauth2/token"), new Dictionary<string, string>
            {
                ["grant_type"] = TokenExchangeGrant,
                ["client_id"] = _options.ClientId,
                ["subject_token"] = accessToken,
                ["subject_token_type"] = AccessTokenType,
                ["requested_token_type"] = SessionTokenType,
            }, cancellationToken).ConfigureAwait(false);
            var session = ToSession(exchanged, Str(exchanged, "access_token"), Str(exchanged, "refresh_token"), Str(exchanged, "session_id"))
                ?? throw new AtlasNativeException("The session exchange returned no session.");

            await SetAsync(session, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// A usable session token, rotated first when it is within <see cref="AtlasNativeOptions.RefreshLead"/> of expiry.
        /// Null when signed out. A failed refresh keeps the existing token (the API's 401 is the cue to sign in again);
        /// a refresh the server REFUSES (the refresh token is dead) signs the person out.
        /// </summary>
        public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            var current = _session;
            if (current == null) return null;
            if (!NeedsRefresh(current)) return current.SessionToken;

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                current = _session;
                if (current == null) return null;
                if (!NeedsRefresh(current)) return current.SessionToken; // another caller rotated it
                if (string.IsNullOrEmpty(current.RefreshToken)) return current.SessionToken;

                HttpResponseMessage response;
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_issuer, $"v1/client/sessions/{Uri.EscapeDataString(current.SessionId)}/tokens"))
                    {
                        Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, string> { ["refresh_token"] = current.RefreshToken }), Encoding.UTF8, "application/json"),
                    };
                    request.Headers.Add("x-publishable-key", _options.PublishableKey);
                    response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException)
                {
                    return current.SessionToken;
                }

                using (response)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        await ClearInternalAsync(cancellationToken).ConfigureAwait(false);
                        return null;
                    }
                    if (!response.IsSuccessStatusCode) return current.SessionToken;
                    var body = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
                    var rotated = body is not { } b ? null : ToSession(b, Str(b, "jwt"), Str(b, "refresh_token") ?? current.RefreshToken, Str(b, "session_id") ?? current.SessionId);
                    if (rotated == null) return current.SessionToken;
                    await SetAsync(rotated, cancellationToken).ConfigureAwait(false);
                    return rotated.SessionToken;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Forget the session on this device.</summary>
        public async Task SignOutAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try { await ClearInternalAsync(cancellationToken).ConfigureAwait(false); }
            finally { _gate.Release(); }
        }

        /// <summary>A message handler that adds the session token to requests for the given API base URLs, and to nothing else.</summary>
        public DelegatingHandler CreateHandler(IEnumerable<string> authorizedUrls, HttpMessageHandler? inner = null)
            => new AtlasNativeBearerHandler(this, authorizedUrls) { InnerHandler = inner ?? new HttpClientHandler() };

        private bool NeedsRefresh(NativeSession s) => s.ExpiresAt == null || _now() >= s.ExpiresAt.Value - _options.RefreshLead;

        private async Task EnsureLoadedAsync(CancellationToken ct)
        {
            if (_loaded) return;
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_loaded) return;
                try { _session = await _store.LoadAsync(ct).ConfigureAwait(false); }
                catch (Exception) { _session = null; } // unreadable storage means signed out
                _loaded = true;
            }
            finally { _gate.Release(); }
        }

        private async Task SetAsync(NativeSession session, CancellationToken ct)
        {
            _session = session;
            _loaded = true;
            await _store.SaveAsync(session, ct).ConfigureAwait(false);
            SignedInChanged?.Invoke(true);
        }

        private async Task ClearInternalAsync(CancellationToken ct)
        {
            _session = null;
            _loaded = true;
            try { await _store.ClearAsync(ct).ConfigureAwait(false); } catch (Exception) { }
            SignedInChanged?.Invoke(false);
        }

        private NativeSession? ToSession(JsonElement body, string? token, string? refresh, string? sessionId)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(sessionId)) return null;
            DateTimeOffset? expires = body.TryGetProperty("expires_in", out var e) && e.ValueKind == JsonValueKind.Number && e.GetInt32() > 0
                ? _now().AddSeconds(e.GetInt32())
                : null;
            return new NativeSession { SessionToken = token!, RefreshToken = refresh ?? "", SessionId = sessionId!, ExpiresAt = expires };
        }

        private async Task<(Uri Authorize, Uri Token)> DiscoverAsync(CancellationToken ct)
        {
            using var response = await _http.GetAsync(new Uri(_issuer, ".well-known/openid-configuration"), ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new AtlasNativeException("Could not load the sign-in configuration.");
            var doc = await ReadJsonAsync(response, ct).ConfigureAwait(false) ?? throw new AtlasNativeException("The sign-in configuration was not JSON.");
            if (Str(doc, "issuer")?.TrimEnd('/') != _options.Issuer.TrimEnd('/'))
                throw new AtlasNativeException("The sign-in configuration is for a different issuer.");
            Uri Endpoint(string name)
            {
                if (!Uri.TryCreate(Str(doc, name), UriKind.Absolute, out var u)) throw new AtlasNativeException($"The sign-in configuration has no {name}.");
                // Never send a code or token to another origin than the issuer.
                if (!string.Equals(u.GetLeftPart(UriPartial.Authority), _issuer.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                    throw new AtlasNativeException($"The {name} is not on the issuer's origin.");
                return u;
            }
            return (Endpoint("authorization_endpoint"), Endpoint("token_endpoint"));
        }

        private async Task<JsonElement> PostFormAsync(Uri url, Dictionary<string, string> form, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new AtlasNativeException($"{url.AbsolutePath} refused the request ({(int)response.StatusCode}).");
            return await ReadJsonAsync(response, ct).ConfigureAwait(false) ?? throw new AtlasNativeException($"{url.AbsolutePath} did not answer JSON.");
        }

        private static async Task<JsonElement?> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(text);
                return doc.RootElement.ValueKind == JsonValueKind.Object ? doc.RootElement.Clone() : null;
            }
            catch (JsonException) { return null; }
        }

        private static string? Str(JsonElement o, string name)
            => o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static string Form(Dictionary<string, string> values)
        {
            var sb = new StringBuilder();
            foreach (var kv in values)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
            }
            return sb.ToString();
        }

        internal static Dictionary<string, string> ParseQuery(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var i = pair.IndexOf('=');
                var key = Uri.UnescapeDataString((i < 0 ? pair : pair.Substring(0, i)).Replace('+', ' '));
                var value = i < 0 ? "" : Uri.UnescapeDataString(pair.Substring(i + 1).Replace('+', ' '));
                result.TryAdd(key, value);
            }
            return result;
        }

        public void Dispose()
        {
            if (_ownsHttp) _http.Dispose();
            _gate.Dispose();
        }
    }

    internal sealed class AtlasNativeBearerHandler : DelegatingHandler
    {
        private readonly AtlasNativeClient _client;
        private readonly List<Uri> _allowed = new List<Uri>();

        public AtlasNativeBearerHandler(AtlasNativeClient client, IEnumerable<string> authorizedUrls)
        {
            _client = client;
            foreach (var u in authorizedUrls)
                if (Uri.TryCreate(u, UriKind.Absolute, out var parsed)) _allowed.Add(parsed);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is { IsAbsoluteUri: true } uri && request.Headers.Authorization == null && IsAllowed(uri))
            {
                var token = await _client.GetTokenAsync(cancellationToken).ConfigureAwait(false);
                if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private bool IsAllowed(Uri uri)
        {
            foreach (var b in _allowed)
            {
                if (!string.Equals(b.GetLeftPart(UriPartial.Authority), uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) continue;
                var basePath = b.AbsolutePath.TrimEnd('/');
                if (basePath.Length == 0 || uri.AbsolutePath == basePath || uri.AbsolutePath.StartsWith(basePath + "/", StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
