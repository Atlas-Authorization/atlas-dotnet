using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Verification;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.Rp
{
    /// <summary>Stable failure codes of the sign-in flow.</summary>
    public enum AtlasOidcErrorCode
    {
        StateMismatch, IssuerMismatch, InvalidRequest, InvalidToken, TokenExchangeFailed, AccessDenied,
        DiscoveryFailed, ReauthenticationRequired, SubjectMismatch,
    }

    /// <summary>A sign-in that did not complete. Send <see cref="ClearCookie"/> as <c>Set-Cookie</c> on your error response.</summary>
    public sealed class AtlasOidcException : Exception
    {
        public AtlasOidcException(AtlasOidcErrorCode code, string message, string? oauthError = null, string? description = null, Exception? inner = null)
            : base(message, inner) { Code = code; OAuthError = oauthError; Description = description; }

        public AtlasOidcErrorCode Code { get; }
        /// <summary>The OAuth <c>error</c> from the authorization or token endpoint, when there was one.</summary>
        public string? OAuthError { get; }
        public string? Description { get; }
        /// <summary>A <c>Set-Cookie</c> value that removes the flow cookie.</summary>
        public string? ClearCookie { get; internal set; }
    }

    public enum OidcClientAuth { ClientSecretBasic, ClientSecretPost, None }

    public sealed class RelyingPartyOptions
    {
        /// <summary>Your instance's issuer: <c>https://</c> + its frontend API host.</summary>
        public string Issuer { get; set; } = "";
        public string ClientId { get; set; } = "";
        /// <summary>The client secret (a confidential client). Without a <see cref="SecretKey"/>, it also authenticates touch and end-session calls.</summary>
        public string? ClientSecret { get; set; }
        /// <summary>Token-endpoint authentication. Default <c>client_secret_basic</c> with a secret, else none.</summary>
        public OidcClientAuth? ClientAuth { get; set; }
        /// <summary>The callback URL, registered on the client exactly.</summary>
        public string RedirectUri { get; set; } = "";
        /// <summary>At least 32 characters: seals the short-lived sign-in cookie.</summary>
        public string Secret { get; set; } = "";
        public string Scope { get; set; } = "openid profile email";
        /// <summary>Where the callback sends the browser when the sign-in carried no return path. Default <c>/</c>.</summary>
        public string AfterSignInPath { get; set; } = "/";
        /// <summary>Where Atlas sends the browser after sign-out; registered on the client exactly.</summary>
        public string? PostLogoutRedirectUri { get; set; }
        /// <summary>The instance secret key (scope <c>sessions:write</c>): used by touch and server-side revoke.</summary>
        public string? SecretKey { get; set; }
        public string ApiUrl { get; set; } = AtlasClientOptions.DefaultApiUrl;
        public string CookieName { get; set; } = "atlas_oidc";
        /// <summary>Sign out with ONE server call, <c>POST /v1/oauth_clients/{id}/logout</c> (the key also needs <c>oauth_clients:write</c>).</summary>
        public bool ServerSignOut { get; set; }
        public TimeSpan TouchInterval { get; set; } = TimeSpan.FromMinutes(5);
        /// <summary>Extra query parameters passed from your sign-in URL to <c>/oauth2/authorize</c> unchanged (for example <c>ref</c>, <c>utm_source</c>).</summary>
        public IReadOnlyList<string> ForwardParams { get; set; } = Array.Empty<string>();
        public int ClockToleranceSeconds { get; set; } = 60;
        public ISidStore? SidStore { get; set; }
        public HttpClient? HttpClient { get; set; }
        /// <summary>Epoch milliseconds, for tests.</summary>
        public Func<long>? Now { get; set; }
    }

    /// <summary>Per-request overrides for <see cref="RelyingParty.StartAsync"/>.</summary>
    public sealed class StartOverrides
    {
        public string? Prompt { get; set; }
        public string? LoginHint { get; set; }
        public string? UiLocales { get; set; }
        public int? MaxAge { get; set; }
        public string? Organization { get; set; }
        public string? ReturnTo { get; set; }
        public string? ExpectedSub { get; set; }
        public string? Scope { get; set; }
        public IDictionary<string, string>? ExtraParams { get; set; }
    }

    /// <summary>A 302 to <see cref="Location"/> carrying <see cref="SetCookies"/>.</summary>
    public sealed class RpRedirect
    {
        public string Location { get; init; } = "";
        public IReadOnlyList<string> SetCookies { get; init; } = Array.Empty<string>();
    }

    /// <summary>Who signed in: the ID-token claims an app needs, in the token's own names.</summary>
    public sealed class RelyingPartyUser
    {
        public string Sub { get; init; } = "";
        /// <summary>The Atlas session id. Keep it on your session: touch, revoke and back-channel logout all name it.</summary>
        public string? Sid { get; init; }
        public string? Email { get; init; }
        /// <summary>True only when the ID token says so. Never match accounts on an unverified email.</summary>
        public bool EmailVerified { get; init; }
        public string? Name { get; init; }
        public string? ExternalId { get; init; }
        public string? OrgId { get; init; }
        public string? OrgRole { get; init; }
        /// <summary>When the user last authenticated, epoch seconds. Store it for <see cref="RelyingParty.RequireRecentAuthAsync"/>.</summary>
        public long? AuthTime { get; init; }
    }

    public sealed class RelyingPartyTokens
    {
        public string IdToken { get; init; } = "";
        public string AccessToken { get; init; } = "";
        public string? RefreshToken { get; init; }
        /// <summary>When the access token expires, epoch milliseconds.</summary>
        public long ExpiresAt { get; init; }
    }

    public sealed class RpCallbackResult
    {
        public RelyingPartyUser User { get; init; } = new RelyingPartyUser();
        public RelyingPartyTokens Tokens { get; init; } = new RelyingPartyTokens();
        /// <summary>Every verified ID-token claim.</summary>
        public JsonElement Claims { get; init; }
        /// <summary>Where to send the browser: the sign-in's return path, or <see cref="RelyingPartyOptions.AfterSignInPath"/>.</summary>
        public string ReturnTo { get; init; } = "/";
        /// <summary>A <c>Set-Cookie</c> value that removes the flow cookie. Send it on the response.</summary>
        public string ClearCookie { get; init; } = "";
    }

    public enum TouchStatus { Skipped, Touched, Ended }

    public sealed class TouchResult
    {
        public TouchStatus Status { get; init; }
        public long LastTouchedAt { get; init; }
        public string? IdleExpiresAt { get; init; }
        /// <summary>When <see cref="Status"/> is <see cref="TouchStatus.Ended"/>: <c>revoked</c>, <c>idle_expired</c>, <c>expired</c> or <c>not_found</c>.</summary>
        public string? Reason { get; init; }
    }

    public sealed class SignOutOptions
    {
        public string? IdToken { get; set; }
        public string? Sid { get; set; }
        public string? PostLogoutRedirectUri { get; set; }
        public string? State { get; set; }
        /// <summary><c>Set-Cookie</c> values to send on the redirect, for example the one that clears your session cookie.</summary>
        public IReadOnlyList<string>? SetCookies { get; set; }
    }

    /// <summary>An answer for a back-channel logout request.</summary>
    public sealed class BackchannelResponse
    {
        public int Status { get; init; }
        public int? RetryAfterSeconds { get; init; }
    }

    /// <summary>
    /// The server-side relying party for the hosted redirect: <see cref="StartAsync"/> (state, nonce and PKCE S256 in one sealed
    /// short-lived cookie), <see cref="CallbackAsync"/> (ID-token checks: algorithm pinned, <c>iss</c>, <c>aud</c>/<c>azp</c>,
    /// <c>exp</c>/<c>iat</c>, <c>nonce</c>, <c>at_hash</c>), keep-alive (<see cref="TouchIfDueAsync"/>), step-up
    /// (<see cref="RequireRecentAuthAsync"/>), sign-out and back-channel logout. Framework neutral: it returns redirects and
    /// cookies as data; <c>Atlas.AspNetCore</c> wires it to ASP.NET Core. Create once at startup and reuse.
    /// </summary>
    public sealed class RelyingParty
    {
        public const int FlowMaxAgeSeconds = 600;
        /// <summary>How long a <c>RequireRecentAuthAsync(0, ...)</c> confirmation counts, in seconds.</summary>
        public const int ConfirmNowWindowSeconds = 60;

        private static readonly Regex SafePath = new Regex(@"^/(?![/\\])[^\\\u0000-\u001f\u007f]*$", RegexOptions.Compiled);
        private static readonly HashSet<string> NotForwardable = new HashSet<string>
        {
            "response_type", "client_id", "redirect_uri", "state", "nonce", "code_challenge", "code_challenge_method",
            "scope", "return_to", "id_token_hint", "request", "request_uri",
        };

        private readonly RelyingPartyOptions _o;
        private readonly string _issuer;
        private readonly HttpClient _http;
        private readonly byte[] _flowKey;
        private readonly Uri _redirect;
        private Discovery? _discovery;
        private long _discoveryAt;
        private JwksCache? _jwks;
        private LogoutTokenVerifier? _logout;
        private readonly object _gate = new object();

        public RelyingParty(RelyingPartyOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.Issuer)) throw new ArgumentException("Issuer is required.", nameof(options));
            if (string.IsNullOrEmpty(options.ClientId)) throw new ArgumentException("ClientId is required.", nameof(options));
            if (options.Secret == null || options.Secret.Length < 32)
                throw new ArgumentException("Secret (at least 32 characters) is required to seal the sign-in cookie.", nameof(options));
            if (!IsSafeReturnTo(options.AfterSignInPath)) throw new ArgumentException("AfterSignInPath must be a path starting with a single '/'.", nameof(options));
            if (!Uri.TryCreate(options.RedirectUri, UriKind.Absolute, out var redirect) || (redirect.Scheme != "https" && redirect.Scheme != "http"))
                throw new ArgumentException("RedirectUri must be an absolute http(s) URL.", nameof(options));
            foreach (var name in options.ForwardParams)
                if (!Regex.IsMatch(name, "^[A-Za-z0-9_.-]{1,64}$") || NotForwardable.Contains(name))
                    throw new ArgumentException($"ForwardParams cannot forward \"{name}\": use a plain parameter name the helper does not set itself.", nameof(options));
            _o = options;
            _redirect = redirect;
            _issuer = options.Issuer.TrimEnd('/');
            _http = options.HttpClient ?? new HttpClient();
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.Secret));
            _flowKey = hmac.ComputeHash(Encoding.UTF8.GetBytes("atlas-rp-flow-v1"));
        }

        private long NowMs => _o.Now != null ? _o.Now() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>A plain same-site path: one leading <c>/</c>, no scheme or authority, no backslash or control characters.</summary>
        public static bool IsSafeReturnTo(string? value) => value != null && SafePath.IsMatch(value);

        // ── start ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Begin sign-in: a 302 to <c>/oauth2/authorize</c> with the sign-in cookie set. <c>prompt</c>, <c>login_hint</c>,
        /// <c>ui_locales</c>, <c>max_age</c>, <c>organization</c> and <c>return_to</c> on <paramref name="query"/> are passed through, as
        /// are the configured <see cref="RelyingPartyOptions.ForwardParams"/>; <paramref name="overrides"/> win.
        /// </summary>
        public async Task<RpRedirect> StartAsync(IReadOnlyDictionary<string, string>? query = null, StartOverrides? overrides = null, CancellationToken ct = default)
        {
            var merged = new StartOverrides();
            string? Q(string k) => query != null && query.TryGetValue(k, out var v) && !string.IsNullOrEmpty(v) ? v : null;
            merged.Prompt = Q("prompt");
            merged.LoginHint = Q("login_hint");
            merged.UiLocales = Q("ui_locales");
            merged.Organization = Q("organization");
            var rt = Q("return_to");
            if (rt != null && IsSafeReturnTo(rt)) merged.ReturnTo = rt; // a bad return_to from the query is dropped, never an error page
            var ma = Q("max_age");
            if (ma != null && Regex.IsMatch(ma, @"^\d{1,9}$")) merged.MaxAge = int.Parse(ma);
            var extra = new Dictionary<string, string>();
            foreach (var name in _o.ForwardParams)
            {
                var v = Q(name);
                if (v != null && v.Length <= 512) extra[name] = v;
            }
            if (overrides?.ExtraParams != null) foreach (var kv in overrides.ExtraParams) extra[kv.Key] = kv.Value;
            if (overrides != null)
            {
                merged.Prompt = overrides.Prompt ?? merged.Prompt;
                merged.LoginHint = overrides.LoginHint ?? merged.LoginHint;
                merged.UiLocales = overrides.UiLocales ?? merged.UiLocales;
                merged.Organization = overrides.Organization ?? merged.Organization;
                merged.ReturnTo = overrides.ReturnTo ?? merged.ReturnTo;
                merged.MaxAge = overrides.MaxAge ?? merged.MaxAge;
                merged.ExpectedSub = overrides.ExpectedSub;
                merged.Scope = overrides.Scope;
            }
            merged.ExtraParams = extra;
            return await BeginAsync(merged, ct).ConfigureAwait(false);
        }

        private async Task<RpRedirect> BeginAsync(StartOverrides s, CancellationToken ct)
        {
            if (s.ReturnTo != null && !IsSafeReturnTo(s.ReturnTo)) throw new ArgumentException("ReturnTo must be a path on this site, starting with a single '/'.");
            if (s.MaxAge is < 0) throw new ArgumentException("MaxAge must be 0 or more.");
            var d = await DiscoverAsync(ct).ConfigureAwait(false);
            var state = RandomToken();
            var nonce = RandomToken();
            var verifier = RandomToken();
            var challenge = B64U(Sha256(Encoding.ASCII.GetBytes(verifier)));
            var q = new List<KeyValuePair<string, string>>();
            if (s.ExtraParams != null)
                foreach (var kv in s.ExtraParams) if (!NotForwardable.Contains(kv.Key)) q.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
            q.Add(new KeyValuePair<string, string>("response_type", "code"));
            q.Add(new KeyValuePair<string, string>("client_id", _o.ClientId));
            q.Add(new KeyValuePair<string, string>("redirect_uri", _o.RedirectUri));
            q.Add(new KeyValuePair<string, string>("scope", NormaliseScope(s.Scope ?? _o.Scope)));
            q.Add(new KeyValuePair<string, string>("state", state));
            q.Add(new KeyValuePair<string, string>("nonce", nonce));
            q.Add(new KeyValuePair<string, string>("code_challenge", challenge));
            q.Add(new KeyValuePair<string, string>("code_challenge_method", "S256"));
            if (s.Prompt != null) q.Add(new KeyValuePair<string, string>("prompt", s.Prompt));
            if (s.LoginHint != null) q.Add(new KeyValuePair<string, string>("login_hint", s.LoginHint));
            if (s.Organization != null) q.Add(new KeyValuePair<string, string>("organization", s.Organization));
            if (s.UiLocales != null) q.Add(new KeyValuePair<string, string>("ui_locales", s.UiLocales));
            if (s.MaxAge != null) q.Add(new KeyValuePair<string, string>("max_age", s.MaxAge.Value.ToString()));
            var url = d.AuthorizationEndpoint + (d.AuthorizationEndpoint.Contains("?") ? "&" : "?")
                + string.Join("&", q.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));
            var flow = new Flow
            {
                S = state, N = nonce, V = verifier, R = _o.RedirectUri, I = _issuer, C = _o.ClientId,
                T = NowMs / 1000, M = s.MaxAge, U = s.ReturnTo, X = s.ExpectedSub,
            };
            return new RpRedirect { Location = url, SetCookies = new[] { Cookie(_o.CookieName, Seal(flow), FlowMaxAgeSeconds) } };
        }

        // ── callback ────────────────────────────────────────────────────────────

        /// <summary>
        /// Finish sign-in: <paramref name="query"/> is the callback's query string, <paramref name="cookieHeader"/> the request's
        /// <c>Cookie</c> header. Throws <see cref="AtlasOidcException"/> on any failure (send its <c>ClearCookie</c>).
        /// </summary>
        public async Task<RpCallbackResult> CallbackAsync(IReadOnlyDictionary<string, string> query, string? cookieHeader, CancellationToken ct = default)
        {
            var clear = ClearFlowCookie();
            try
            {
                var r = await RunCallbackAsync(query, cookieHeader, ct).ConfigureAwait(false);
                return new RpCallbackResult { User = r.User, Tokens = r.Tokens, Claims = r.Claims, ReturnTo = r.ReturnTo, ClearCookie = clear };
            }
            catch (AtlasOidcException e)
            {
                e.ClearCookie = clear;
                throw;
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                throw new AtlasOidcException(AtlasOidcErrorCode.InvalidRequest, e.Message, inner: e) { ClearCookie = clear };
            }
        }

        private async Task<RpCallbackResult> RunCallbackAsync(IReadOnlyDictionary<string, string> query, string? cookieHeader, CancellationToken ct)
        {
            string? Q(string k) => query.TryGetValue(k, out var v) && v.Length > 0 ? v : null;
            // The flow cookie first: without it nothing in the query can be trusted.
            var raw = ReadCookie(cookieHeader, _o.CookieName);
            if (raw == null) throw new AtlasOidcException(AtlasOidcErrorCode.StateMismatch, "The sign-in flow cookie is missing or expired. Start sign-in again.");
            var flow = Open(raw) ?? throw new AtlasOidcException(AtlasOidcErrorCode.StateMismatch, "The sign-in flow cookie could not be read. Start sign-in again.");
            var state = Q("state");
            if (state == null || !FixedEq(state, flow.S))
                throw new AtlasOidcException(AtlasOidcErrorCode.StateMismatch, "The `state` returned to the callback does not match this browser's sign-in.");
            var nowSec = NowMs / 1000;
            if (nowSec - flow.T > FlowMaxAgeSeconds)
                throw new AtlasOidcException(AtlasOidcErrorCode.StateMismatch, "This sign-in took longer than 10 minutes. Start sign-in again.");
            if (flow.I != _issuer || flow.C != _o.ClientId || flow.R != _o.RedirectUri)
                throw new AtlasOidcException(AtlasOidcErrorCode.StateMismatch, "This sign-in was started for a different issuer, client or redirect URI.");
            var iss = Q("iss"); // RFC 9207
            if (iss != null && iss.TrimEnd('/') != _issuer)
                throw new AtlasOidcException(AtlasOidcErrorCode.IssuerMismatch, $"The callback came from {iss}, not {_issuer}.");
            var oauthError = Q("error");
            if (oauthError != null)
            {
                var desc = Q("error_description");
                throw new AtlasOidcException(AtlasOidcErrorCode.AccessDenied,
                    $"Sign-in did not complete: {oauthError}{(desc != null ? " (" + desc + ")" : "")}.", oauthError, desc);
            }
            var code = Q("code") ?? throw new AtlasOidcException(AtlasOidcErrorCode.InvalidRequest, "The callback has no `code` parameter.");

            var d = await DiscoverAsync(ct).ConfigureAwait(false);
            var tokens = await ExchangeAsync(d, code, flow.V, ct).ConfigureAwait(false);
            var claims = await VerifyIdTokenAsync(tokens.IdToken, d, flow.N, tokens.AccessToken, ct).ConfigureAwait(false);

            string? Str(string n) => claims.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            var sub = Str("sub")!;
            long? authTime = claims.TryGetProperty("auth_time", out var at) && at.ValueKind == JsonValueKind.Number && at.TryGetInt64(out var atv) ? atv : (long?)null;
            if (flow.M != null)
            {
                // OIDC Core 3.1.2.1: with max_age the client must check auth_time itself.
                if (authTime == null || nowSec - authTime.Value > flow.M.Value + _o.ClockToleranceSeconds)
                    throw new AtlasOidcException(AtlasOidcErrorCode.ReauthenticationRequired,
                        $"The sign-in is older than the {flow.M} seconds this request allowed (max_age). Ask the user to sign in again.");
            }
            if (flow.X != null && !FixedEq(sub, flow.X))
                throw new AtlasOidcException(AtlasOidcErrorCode.SubjectMismatch, "A different user signed in than the one asked to confirm it is them.");
            var user = new RelyingPartyUser
            {
                Sub = sub,
                Sid = Str("sid") is { Length: > 0 } sid ? sid : null,
                Email = Str("email"),
                EmailVerified = claims.TryGetProperty("email_verified", out var ev) && ev.ValueKind == JsonValueKind.True,
                Name = Str("name"),
                ExternalId = Str("external_id") is { Length: > 0 } ext ? ext : null,
                OrgId = Str("org_id"),
                OrgRole = Str("org_id") != null ? Str("org_role") ?? "" : null,
                AuthTime = authTime,
            };
            return new RpCallbackResult
            {
                User = user, Tokens = tokens, Claims = claims,
                ReturnTo = flow.U != null && IsSafeReturnTo(flow.U) ? flow.U : _o.AfterSignInPath,
            };
        }

        private async Task<RelyingPartyTokens> ExchangeAsync(Discovery d, string code, string verifier, CancellationToken ct)
        {
            var method = _o.ClientAuth ?? (string.IsNullOrEmpty(_o.ClientSecret) ? OidcClientAuth.None : OidcClientAuth.ClientSecretBasic);
            if (method != OidcClientAuth.None && string.IsNullOrEmpty(_o.ClientSecret))
                throw new AtlasOidcException(AtlasOidcErrorCode.TokenExchangeFailed, $"ClientAuth '{method}' needs ClientSecret.");
            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", _o.RedirectUri),
                new KeyValuePair<string, string>("code_verifier", verifier),
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, d.TokenEndpoint);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            if (method == OidcClientAuth.ClientSecretBasic)
                request.Headers.TryAddWithoutValidation("Authorization", "Basic " + Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(FormEncode(_o.ClientId) + ":" + FormEncode(_o.ClientSecret!))));
            else
            {
                form.Add(new KeyValuePair<string, string>("client_id", _o.ClientId));
                if (method == OidcClientAuth.ClientSecretPost) form.Add(new KeyValuePair<string, string>("client_secret", _o.ClientSecret!));
            }
            request.Content = new FormUrlEncodedContent(form);
            HttpResponseMessage response;
            try { response = await _http.SendAsync(request, ct).ConfigureAwait(false); }
            catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException && !ct.IsCancellationRequested)
            {
                throw new AtlasOidcException(AtlasOidcErrorCode.TokenExchangeFailed, "The token endpoint could not be reached: " + e.Message + ".", inner: e);
            }
            using (response)
            {
                var text = await ReadAsync(response, ct).ConfigureAwait(false);
                JsonElement json = default;
                var parsed = false;
                try { json = JsonDocument.Parse(text).RootElement.Clone(); parsed = json.ValueKind == JsonValueKind.Object; } catch (JsonException) { }
                string? Str(string n) => parsed && json.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                if (!response.IsSuccessStatusCode)
                {
                    var err = Str("error");
                    var desc = Str("error_description");
                    throw new AtlasOidcException(AtlasOidcErrorCode.TokenExchangeFailed,
                        $"The token endpoint answered {(int)response.StatusCode}{(err != null ? " " + err : "")}{(desc != null ? ": " + desc : "")}.", err, desc);
                }
                var idToken = Str("id_token");
                var access = Str("access_token");
                if (idToken == null || access == null)
                    throw new AtlasOidcException(AtlasOidcErrorCode.TokenExchangeFailed, "The token response has no `id_token`. Request the `openid` scope.");
                long expiresMs = 0;
                if (json.TryGetProperty("expires_in", out var ei))
                {
                    double secs = ei.ValueKind == JsonValueKind.Number ? ei.GetDouble() : double.TryParse(ei.GetString(), out var p) ? p : 0;
                    if (secs > 0) expiresMs = NowMs + (long)(secs * 1000);
                }
                if (expiresMs == 0) expiresMs = ExpOf(idToken) * 1000;
                return new RelyingPartyTokens { IdToken = idToken, AccessToken = access, RefreshToken = Str("refresh_token"), ExpiresAt = expiresMs };
            }
        }

        private async Task<JsonElement> VerifyIdTokenAsync(string idToken, Discovery d, string nonce, string? accessToken, CancellationToken ct)
        {
            AtlasOidcException Invalid(string why, Exception? inner = null) => new AtlasOidcException(AtlasOidcErrorCode.InvalidToken, $"The ID token was refused: {why}.", inner: inner);
            var parts = idToken.Split('.');
            if (parts.Length != 3 || parts[0].Length == 0) throw Invalid("it is not a JWT");
            string? alg = null, kid = null;
            try
            {
                using var h = JsonDocument.Parse(JwksCache.Base64UrlDecode(parts[0]));
                alg = h.RootElement.TryGetProperty("alg", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString() : null;
                kid = h.RootElement.TryGetProperty("kid", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
            }
            catch (Exception e) when (e is JsonException || e is FormatException) { throw Invalid("it is not a JWT"); }
            // Pinned before any key lookup, so `none` and HS* never get near a key.
            if (alg == null || !SigningAlgs.All.Contains(alg)) throw Invalid($"its algorithm is {alg ?? "missing"}, not one of RS256, ES256, ES384, PS256 or EdDSA");
            JwksCache cache;
            lock (_gate) cache = _jwks ??= new JwksCache(d.JwksUri, _http, _o.Now);
            var keys = await cache.GetAsync(kid, ct).ConfigureAwait(false);
            if (keys == null || keys.Count == 0)
                throw new AtlasOidcException(AtlasOidcErrorCode.DiscoveryFailed, $"The issuer's signing keys could not be fetched from {d.JwksUri}.");
            var tol = TimeSpan.FromSeconds(_o.ClockToleranceSeconds);
            var now = DateTimeOffset.FromUnixTimeMilliseconds(NowMs).UtcDateTime;
            var parameters = new TokenValidationParameters
            {
                ValidAlgorithms = SigningAlgs.All,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidIssuer = d.Issuer,
                ValidateAudience = true,
                ValidAudience = _o.ClientId,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                LifetimeValidator = (nbf, exp, _, __) => exp.HasValue && now - tol <= exp.Value && (!nbf.HasValue || now + tol >= nbf.Value),
            };
            JwsResult result;
            try { result = await Jws.ValidateAsync(idToken, keys.Raw, parameters).ConfigureAwait(false); }
            catch (Exception e) { throw Invalid("its signature or claims do not verify", e); }
            if (!result.IsValid) throw Invalid(result.Exception is SecurityTokenExpiredException ? "it has expired" : "its signature, issuer, audience or lifetime do not verify", result.Exception);
            JsonElement claims;
            using (var doc = JsonDocument.Parse(Encoding.UTF8.GetString(JwksCache.Base64UrlDecode(parts[1])))) claims = doc.RootElement.Clone();
            if (!claims.TryGetProperty("sub", out var sub) || sub.ValueKind != JsonValueKind.String || sub.GetString()!.Length == 0) throw Invalid("it has no sub");
            if (!claims.TryGetProperty("iat", out var iat) || iat.ValueKind != JsonValueKind.Number) throw Invalid("it has no iat");
            // OIDC Core 3.1.3.7 (4)-(5): several audiences need an azp, and any azp must be us.
            if (claims.TryGetProperty("aud", out var aud) && aud.ValueKind == JsonValueKind.Array && aud.GetArrayLength() > 1 && !claims.TryGetProperty("azp", out _))
                throw Invalid("it has several audiences and no azp");
            if (claims.TryGetProperty("azp", out var azp) && azp.GetString() != _o.ClientId) throw Invalid($"its azp is {azp}, not this client");
            if (!claims.TryGetProperty("nonce", out var n) || n.ValueKind != JsonValueKind.String || !FixedEq(n.GetString()!, nonce))
                throw Invalid("its nonce does not match this sign-in");
            if (iat.GetInt64() > NowMs / 1000 + _o.ClockToleranceSeconds) throw Invalid("it was issued in the future");
            if (!string.IsNullOrEmpty(accessToken) && claims.TryGetProperty("at_hash", out var ath) && ath.ValueKind == JsonValueKind.String)
            {
                var digest = Sha256(Encoding.ASCII.GetBytes(accessToken!));
                if (!FixedEq(B64U(digest.Take(16).ToArray()), ath.GetString()!)) throw Invalid("its at_hash does not match the access token");
            }
            return claims;
        }

        // ── step-up ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Step-up: null when <paramref name="authTime"/> (epoch seconds, stored at sign-in) is within <paramref name="maxAgeSeconds"/>;
        /// otherwise a redirect that asks the same user (<paramref name="sub"/>) to authenticate again. The callback then refuses another
        /// user or an old <c>auth_time</c>; store the new <c>User.AuthTime</c>. <c>0</c> asks unless the user authenticated in the last
        /// <see cref="ConfirmNowWindowSeconds"/> seconds, so the request that returns from the confirmation goes through.
        /// </summary>
        public async Task<RpRedirect?> RequireRecentAuthAsync(int maxAgeSeconds, string sub, long? authTime, string returnTo, CancellationToken ct = default)
        {
            if (maxAgeSeconds < 0) throw new ArgumentException("maxAgeSeconds must be 0 or more.", nameof(maxAgeSeconds));
            if (string.IsNullOrEmpty(sub)) throw new ArgumentException("sub is required.", nameof(sub));
            var allowed = maxAgeSeconds == 0 ? ConfirmNowWindowSeconds : maxAgeSeconds;
            if (authTime != null && NowMs / 1000.0 - authTime.Value <= allowed) return null;
            return await BeginAsync(new StartOverrides
            {
                MaxAge = maxAgeSeconds, ExpectedSub = sub, Prompt = maxAgeSeconds == 0 ? "login" : null,
                ReturnTo = IsSafeReturnTo(returnTo) ? returnTo : null,
            }, ct).ConfigureAwait(false);
        }

        // ── Atlas session calls ─────────────────────────────────────────────────

        /// <summary>
        /// Keep the Atlas session alive while the user is active, at most once per <see cref="RelyingPartyOptions.TouchInterval"/> per
        /// session. With a <c>SecretKey</c> it touches the Atlas session; without one but with a <c>ClientSecret</c> it touches this app's
        /// own sign-in (<c>POST {issuer}/oauth2/session/touch</c>).
        /// </summary>
        public async Task<TouchResult> TouchIfDueAsync(string sid, long? lastTouchedAt, CancellationToken ct = default)
        {
            var now = NowMs;
            if (lastTouchedAt != null && now - lastTouchedAt.Value < (long)_o.TouchInterval.TotalMilliseconds && now >= lastTouchedAt.Value)
                return new TouchResult { Status = TouchStatus.Skipped, LastTouchedAt = lastTouchedAt.Value };
            if (string.IsNullOrEmpty(_o.SecretKey))
            {
                var (status, body) = await AppSessionCallAsync(sid, "touch", ct).ConfigureAwait(false);
                if (status >= 200 && status < 300)
                {
                    string? idle = null;
                    if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("idle_expires_at", out var ie))
                        idle = ie.ValueKind == JsonValueKind.Number ? DateTimeOffset.FromUnixTimeMilliseconds(ie.GetInt64()).UtcDateTime.ToString("o") : ie.ValueKind == JsonValueKind.String ? ie.GetString() : null;
                    return new TouchResult { Status = TouchStatus.Touched, LastTouchedAt = now, IdleExpiresAt = idle };
                }
                if (status == 404 && OAuthError(body) == "session_not_found") return new TouchResult { Status = TouchStatus.Ended, Reason = "not_found" };
                throw OAuthFailure(status, body);
            }
            var (st, resp) = await SessionCallAsync(sid, "touch", ct).ConfigureAwait(false);
            if (st >= 200 && st < 300)
                return new TouchResult
                {
                    Status = TouchStatus.Touched, LastTouchedAt = now,
                    IdleExpiresAt = resp.ValueKind == JsonValueKind.Object && resp.TryGetProperty("idle_expires_at", out var i2) && i2.ValueKind == JsonValueKind.String ? i2.GetString() : null,
                };
            if (st == 404) return new TouchResult { Status = TouchStatus.Ended, Reason = "not_found" };
            if (st == 409)
            {
                var c = ApiCode(resp);
                return new TouchResult { Status = TouchStatus.Ended, Reason = c == "SESSION_REVOKED" ? "revoked" : c == "SESSION_IDLE_EXPIRED" ? "idle_expired" : "expired" };
            }
            throw ApiFailure(st, resp); // a key, scope or availability problem is not "signed out": say so loudly
        }

        /// <summary>End the Atlas session server-side (<c>POST /v1/sessions/{id}/revoke</c>); every app signed in through it gets a back-channel logout. <c>false</c>: this instance has no such session.</summary>
        public async Task<bool> RevokeSessionAsync(string sid, CancellationToken ct = default)
        {
            var (status, body) = await SessionCallAsync(sid, "revoke", ct).ConfigureAwait(false);
            if (status >= 200 && status < 300) return true;
            if (status == 404) return false;
            throw ApiFailure(status, body);
        }

        /// <summary>End THIS app's sign-in under <paramref name="sid"/> with the app's client credentials (<c>POST {issuer}/oauth2/session/end</c>). <c>false</c>: no live sign-in of this app has that sid.</summary>
        public async Task<bool> EndAppSessionAsync(string sid, CancellationToken ct = default)
        {
            var (status, body) = await AppSessionCallAsync(sid, "end", ct).ConfigureAwait(false);
            if (status >= 200 && status < 300) return true;
            if (status == 404 && OAuthError(body) == "session_not_found") return false;
            throw OAuthFailure(status, body);
        }

        /// <summary>Record that your app session <paramref name="appSessionId"/> is backed by the Atlas <paramref name="sid"/> until <paramref name="expiresAt"/>. Needs <see cref="RelyingPartyOptions.SidStore"/>.</summary>
        public Task BindSessionAsync(string sid, string appSessionId, DateTimeOffset expiresAt)
        {
            if (_o.SidStore == null) throw new InvalidOperationException("BindSessionAsync: SidStore is not configured.");
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(appSessionId)) throw new ArgumentException("sid and appSessionId are required.");
            return _o.SidStore.BindAsync(sid, appSessionId, expiresAt);
        }

        /// <summary>
        /// The back-channel logout receiver: verifies the <c>logout_token</c> in the form body, takes the app sessions bound to its
        /// <c>sid</c> from the store and passes them to <paramref name="onLogout"/> (end them; end by <c>claims.Sub</c> when the list is
        /// empty). 200 when handled, 400 for a bad token, 503 with <c>Retry-After</c> when the keys could not be loaded or
        /// <paramref name="onLogout"/> threw (the bindings are put back so Atlas's retry finds them).
        /// </summary>
        public async Task<BackchannelResponse> HandleBackchannelLogoutAsync(string formBody, Func<IReadOnlyList<string>, LogoutTokenClaims, Task> onLogout, CancellationToken ct = default)
        {
            var d = await DiscoverAsync(ct).ConfigureAwait(false);
            LogoutTokenVerifier verifier;
            lock (_gate)
                verifier = _logout ??= new LogoutTokenVerifier(new LogoutTokenVerifierOptions
                    { JwksUrl = d.JwksUri, Issuer = _issuer, ClientId = _o.ClientId, HttpClient = _http, Now = _o.Now });
            LogoutTokenClaims claims;
            try { claims = await verifier.VerifyFormBodyAsync(formBody, ct).ConfigureAwait(false); }
            catch (LogoutTokenException e) when (e.Retryable) { return new BackchannelResponse { Status = 503, RetryAfterSeconds = 30 }; }
            catch (LogoutTokenException) { return new BackchannelResponse { Status = 400 }; }
            IReadOnlyList<string> ids = claims.Sid != null && _o.SidStore != null ? await _o.SidStore.TakeAsync(claims.Sid).ConfigureAwait(false) : Array.Empty<string>();
            try { await onLogout(ids, claims).ConfigureAwait(false); }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                foreach (var id in ids) await _o.SidStore!.BindAsync(claims.Sid!, id, DateTimeOffset.FromUnixTimeMilliseconds(NowMs + 86_400_000)).ConfigureAwait(false);
                return new BackchannelResponse { Status = 503, RetryAfterSeconds = 30 };
            }
            return new BackchannelResponse { Status = 200 };
        }

        /// <summary>
        /// Sign-out: with a <c>Sid</c> and a <c>SecretKey</c>, revokes the Atlas session server-side first, then redirects to
        /// <c>/oauth2/logout</c>. With <see cref="RelyingPartyOptions.ServerSignOut"/> one call ends this app's grants and the
        /// Atlas session and the redirect goes straight to the post-logout URI. End your own session as well.
        /// </summary>
        public async Task<RpRedirect> SignOutAsync(SignOutOptions? options = null, CancellationToken ct = default)
        {
            options ??= new SignOutOptions();
            var cookies = options.SetCookies ?? Array.Empty<string>();
            var post = options.PostLogoutRedirectUri ?? _o.PostLogoutRedirectUri;
            if (_o.ServerSignOut && !string.IsNullOrEmpty(options.Sid) && !string.IsNullOrEmpty(_o.SecretKey))
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _o.ApiUrl.TrimEnd('/') + "/v1/oauth_clients/" + Uri.EscapeDataString(_o.ClientId) + "/logout");
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _o.SecretKey);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Content = new StringContent(JsonSerializer.Serialize(new { sid = options.Sid }), Encoding.UTF8, "application/json");
                using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode && (int)response.StatusCode != 404)
                    throw ApiFailure((int)response.StatusCode, Parse(await ReadAsync(response, ct).ConfigureAwait(false)));
                var target = post ?? "/";
                if (options.State != null) target += (target.Contains("?") ? "&" : "?") + "state=" + Uri.EscapeDataString(options.State);
                return new RpRedirect { Location = target, SetCookies = cookies };
            }
            if (!string.IsNullOrEmpty(options.Sid) && !string.IsNullOrEmpty(_o.SecretKey)) await RevokeSessionAsync(options.Sid!, ct).ConfigureAwait(false);
            var d = await DiscoverAsync(ct).ConfigureAwait(false);
            if (d.EndSessionEndpoint == null) throw new AtlasOidcException(AtlasOidcErrorCode.DiscoveryFailed, "The issuer does not advertise an end_session_endpoint.");
            var q = new List<string> { "client_id=" + Uri.EscapeDataString(_o.ClientId) };
            if (options.IdToken != null) q.Add("id_token_hint=" + Uri.EscapeDataString(options.IdToken));
            if (post != null) q.Add("post_logout_redirect_uri=" + Uri.EscapeDataString(post));
            if (options.State != null) q.Add("state=" + Uri.EscapeDataString(options.State));
            return new RpRedirect { Location = d.EndSessionEndpoint + (d.EndSessionEndpoint.Contains("?") ? "&" : "?") + string.Join("&", q), SetCookies = cookies };
        }

        /// <summary>A <c>Set-Cookie</c> value that removes the flow cookie.</summary>
        public string ClearFlowCookie() => Cookie(_o.CookieName, "", 0);

        // ── plumbing ────────────────────────────────────────────────────────────

        private async Task<(int Status, JsonElement Body)> SessionCallAsync(string sid, string action, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_o.SecretKey)) throw new InvalidOperationException($"{action}: SecretKey is not configured.");
            if (string.IsNullOrEmpty(sid)) throw new ArgumentException("A session id (sid) is required.", nameof(sid));
            using var request = new HttpRequestMessage(HttpMethod.Post, _o.ApiUrl.TrimEnd('/') + "/v1/sessions/" + Uri.EscapeDataString(sid) + "/" + action);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _o.SecretKey);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            return ((int)response.StatusCode, Parse(await ReadAsync(response, ct).ConfigureAwait(false)));
        }

        /// <summary>The app's own sign-in, authenticated like the token endpoint (<c>/oauth2/session/touch</c> or <c>/end</c>).</summary>
        private async Task<(int Status, JsonElement Body)> AppSessionCallAsync(string sid, string action, CancellationToken ct)
        {
            var method = _o.ClientAuth ?? (string.IsNullOrEmpty(_o.ClientSecret) ? OidcClientAuth.None : OidcClientAuth.ClientSecretBasic);
            if (method == OidcClientAuth.None || string.IsNullOrEmpty(_o.ClientSecret))
                throw new InvalidOperationException($"Configure ClientSecret (the app's client credentials){(action == "touch" ? " or SecretKey" : "")}; a public client cannot call it.");
            if (string.IsNullOrEmpty(sid)) throw new ArgumentException("A session id (sid) is required.", nameof(sid));
            var form = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("sid", sid) };
            using var request = new HttpRequestMessage(HttpMethod.Post, _issuer + "/oauth2/session/" + action);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            if (method == OidcClientAuth.ClientSecretBasic)
                request.Headers.TryAddWithoutValidation("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(FormEncode(_o.ClientId) + ":" + FormEncode(_o.ClientSecret!))));
            else
            {
                form.Add(new KeyValuePair<string, string>("client_id", _o.ClientId));
                form.Add(new KeyValuePair<string, string>("client_secret", _o.ClientSecret!));
            }
            request.Content = new FormUrlEncodedContent(form);
            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            return ((int)response.StatusCode, Parse(await ReadAsync(response, ct).ConfigureAwait(false)));
        }

        private static string? OAuthError(JsonElement body) => body.ValueKind == JsonValueKind.Object && body.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;

        private static string? ApiCode(JsonElement body)
            => body.ValueKind == JsonValueKind.Object && body.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0
               && errs[0].TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;

        private static AtlasException ApiFailure(int status, JsonElement body)
        {
            var items = new List<AtlasErrorItem>();
            if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array)
                foreach (var e in errs.EnumerateArray())
                    items.Add(new AtlasErrorItem
                    {
                        Code = e.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString()! : "UNKNOWN",
                        Message = e.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString()! : "",
                    });
            if (items.Count == 0) items.Add(new AtlasErrorItem { Code = "UNKNOWN", Message = $"Atlas answered {status}." });
            return AtlasException.FromStatus(status, items);
        }

        private static AtlasException OAuthFailure(int status, JsonElement body)
        {
            var code = OAuthError(body) ?? "request_failed";
            var message = body.ValueKind == JsonValueKind.Object && body.TryGetProperty("error_description", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString()! : $"The issuer answered {status}.";
            return AtlasException.FromStatus(status, new[] { new AtlasErrorItem { Code = code, Message = message } });
        }

        private static JsonElement Parse(string text)
        {
            try { return JsonDocument.Parse(text).RootElement.Clone(); } catch (JsonException) { return default; }
        }

        private static async Task<string> ReadAsync(HttpResponseMessage r, CancellationToken ct)
        {
#if NET8_0_OR_GREATER
            return await r.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            return await r.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }

        private sealed class Discovery
        {
            public string Issuer = "", AuthorizationEndpoint = "", TokenEndpoint = "", JwksUri = "";
            public string? EndSessionEndpoint;
        }

        private async Task<Discovery> DiscoverAsync(CancellationToken ct)
        {
            if (_discovery != null && NowMs - _discoveryAt < 3_600_000) return _discovery;
            var url = _issuer + "/.well-known/openid-configuration";
            AtlasOidcException Fail(string why, Exception? inner = null) => new AtlasOidcException(AtlasOidcErrorCode.DiscoveryFailed, $"OIDC discovery at {url} failed: {why}.", inner: inner);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) throw Fail($"it answered {(int)response.StatusCode}");
                using var doc = JsonDocument.Parse(await ReadAsync(response, ct).ConfigureAwait(false));
                string? S(string n) => doc.RootElement.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                var issuer = S("issuer");
                if (issuer == null || issuer.TrimEnd('/') != _issuer) throw Fail($"its issuer is {issuer ?? "missing"}, not {_issuer}");
                foreach (var req in new[] { "authorization_endpoint", "token_endpoint", "jwks_uri" })
                    if (S(req) == null) throw Fail($"it has no {req}");
                return _discovery = new Discovery
                {
                    Issuer = issuer, AuthorizationEndpoint = S("authorization_endpoint")!, TokenEndpoint = S("token_endpoint")!,
                    JwksUri = S("jwks_uri")!, EndSessionEndpoint = S("end_session_endpoint"),
                };
            }
            catch (AtlasOidcException) { if (_discovery != null) return _discovery; throw; } // keep serving a document we had through an outage
            catch (Exception e) when (e is HttpRequestException || e is JsonException || (e is TaskCanceledException && !ct.IsCancellationRequested))
            {
                if (_discovery != null) return _discovery;
                throw Fail(e.Message, e);
            }
            finally { _discoveryAt = NowMs; }
        }

        private static string NormaliseScope(string scope)
        {
            var set = scope.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!set.Contains("openid")) set.Insert(0, "openid");
            return string.Join(" ", set.Distinct());
        }

        private static long ExpOf(string jwt)
        {
            try
            {
                using var doc = JsonDocument.Parse(JwksCache.Base64UrlDecode(jwt.Split('.')[1]));
                return doc.RootElement.GetProperty("exp").GetInt64();
            }
            catch (Exception) { return 0; }
        }

        private static string FormEncode(string s) => Uri.EscapeDataString(s).Replace("%20", "+");

        // flow cookie ----------------------------------------------------------------

        private sealed class Flow
        {
            public string S { get; set; } = ""; public string N { get; set; } = ""; public string V { get; set; } = "";
            public string R { get; set; } = ""; public string I { get; set; } = ""; public string C { get; set; } = "";
            public long T { get; set; }
            public int? M { get; set; }
            public string? U { get; set; }
            public string? X { get; set; }
        }

        private string Seal(Flow flow)
        {
            var plain = JsonSerializer.SerializeToUtf8Bytes(flow);
            var nonce = Random(12);
            var cipher = new byte[plain.Length];
            var tag = new byte[16];
            using (var aes = new AesGcm(_flowKey)) aes.Encrypt(nonce, plain, cipher, tag);
            return "s1." + B64U(nonce) + "." + B64U(cipher.Concat(tag).ToArray());
        }

        private Flow? Open(string value)
        {
            try
            {
                if (!value.StartsWith("s1.")) return null; // an unsealed cookie is a downgrade: refuse it
                var parts = value.Substring(3).Split('.');
                if (parts.Length != 2) return null;
                var nonce = JwksCache.Base64UrlDecode(parts[0]);
                var blob = JwksCache.Base64UrlDecode(parts[1]);
                if (nonce.Length != 12 || blob.Length < 16) return null;
                var cipher = blob.Take(blob.Length - 16).ToArray();
                var tag = blob.Skip(blob.Length - 16).ToArray();
                var plain = new byte[cipher.Length];
                using (var aes = new AesGcm(_flowKey)) aes.Decrypt(nonce, cipher, tag, plain);
                var flow = JsonSerializer.Deserialize<Flow>(plain);
                return flow == null || flow.S.Length == 0 || flow.N.Length == 0 || flow.V.Length == 0 ? null : flow;
            }
            catch (Exception e) when (e is CryptographicException || e is JsonException || e is FormatException) { return null; }
        }

        private string Cookie(string name, string value, int maxAge)
        {
            var parts = new List<string> { name + "=" + value, "Path=" + _redirect.AbsolutePath, "Max-Age=" + maxAge, "HttpOnly", "SameSite=Lax" };
            if (maxAge == 0) parts.Add("Expires=Thu, 01 Jan 1970 00:00:00 GMT");
            if (_redirect.Scheme == "https") parts.Add("Secure"); // except on a plain-http local callback, where the browser would drop it
            return string.Join("; ", parts);
        }

        private static string? ReadCookie(string? header, string name)
        {
            if (string.IsNullOrWhiteSpace(header)) return null;
            foreach (var part in header!.Split(';'))
            {
                var eq = part.IndexOf('=');
                if (eq < 0 || part.Substring(0, eq).Trim() != name) continue;
                var v = part.Substring(eq + 1).Trim();
                return v.Length > 0 ? v : null;
            }
            return null;
        }

        private static byte[] Random(int n)
        {
            var b = new byte[n];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(b);
            return b;
        }

        private static string RandomToken() => B64U(Random(32));

        private static byte[] Sha256(byte[] data)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(data);
        }

        private static string B64U(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static bool FixedEq(string a, string b)
        {
            var x = Encoding.UTF8.GetBytes(a);
            var y = Encoding.UTF8.GetBytes(b);
            return x.Length == y.Length && CryptographicOperations.FixedTimeEquals(x, y);
        }
    }
}
