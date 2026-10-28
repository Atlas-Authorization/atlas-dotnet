using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Atlas.Blazor.WebAssembly
{
    /// <summary>Settings for the Blazor WebAssembly integration.</summary>
    public sealed class AtlasWasmOptions
    {
        /// <summary>The session cookie the Atlas frontend keeps fresh. Default <c>__session</c>.</summary>
        public string CookieName { get; set; } = "__session";

        /// <summary>
        /// Expected <c>iss</c>. Strongly recommended: a token for another issuer is shown as signed out.
        /// (This is a UI convenience; your API is what verifies the signature.)
        /// </summary>
        public string? Issuer { get; set; }

        /// <summary>Optional <c>azp</c> allowlist, same meaning as on the server.</summary>
        public IList<string> AuthorizedParties { get; } = new List<string>();

        /// <summary>
        /// Base URLs the session token may be sent to (for example your API origin). A request to any
        /// other URL goes out without the token, so it cannot leak to a third party.
        /// </summary>
        public IList<string> AuthorizedUrls { get; } = new List<string>();

        /// <summary>How often the cookie is re-read to pick up a refreshed or ended session. Default 15 seconds; zero disables.</summary>
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(15);
    }

    /// <summary>Where the session token comes from. The default reads the Atlas cookie through JS interop.</summary>
    public interface IAtlasSessionSource
    {
        ValueTask<string?> GetTokenAsync();
    }

    internal sealed class CookieSessionSource : IAtlasSessionSource
    {
        private readonly IJSRuntime _js;
        private readonly string _cookie;
        private Task<IJSObjectReference>? _module;

        public CookieSessionSource(IJSRuntime js, IOptions<AtlasWasmOptions> options)
        {
            _js = js;
            _cookie = options.Value.CookieName;
        }

        public async ValueTask<string?> GetTokenAsync()
        {
            try
            {
                _module ??= _js.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/Atlas.Blazor.WebAssembly/atlas-session.js").AsTask();
                var module = await _module.ConfigureAwait(false);
                return await module.InvokeAsync<string?>("readCookie", _cookie).ConfigureAwait(false);
            }
            catch (Exception)
            {
                _module = null; // retry the import next time (prerendering has no JS)
                return null;
            }
        }
    }

    /// <summary>A decoded, UNVERIFIED session token. For showing UI state only; never authorize on it.</summary>
    public static class AtlasSessionToken
    {
        public const int ClockSkewSeconds = 5;

        /// <summary>
        /// Decode a session JWT's payload and apply the cheap checks the browser can make: three
        /// segments, <c>alg</c> not <c>none</c>, issuer, <c>exp</c>/<c>nbf</c> with 5 seconds of
        /// skew, and the <c>azp</c> allowlist. Returns null for anything else. The signature is NOT
        /// checked (a browser has no business holding the verification policy); the API verifies it.
        /// </summary>
        public static JsonElement? TryDecode(string? token, AtlasWasmOptions options, DateTimeOffset now)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var parts = token!.Split('.');
            if (parts.Length != 3) return null;
            try
            {
                using var header = JsonDocument.Parse(Base64UrlDecode(parts[0]));
                if (!header.RootElement.TryGetProperty("alg", out var alg) || alg.ValueKind != JsonValueKind.String
                    || string.Equals(alg.GetString(), "none", StringComparison.OrdinalIgnoreCase)) return null;
                var payload = JsonDocument.Parse(Base64UrlDecode(parts[1])).RootElement.Clone();
                if (payload.ValueKind != JsonValueKind.Object) return null;
                var t = now.ToUnixTimeSeconds();
                if (!payload.TryGetProperty("exp", out var exp) || exp.ValueKind != JsonValueKind.Number || exp.GetInt64() + ClockSkewSeconds < t) return null;
                if (payload.TryGetProperty("nbf", out var nbf) && nbf.ValueKind == JsonValueKind.Number && nbf.GetInt64() - ClockSkewSeconds > t) return null;
                if (!string.IsNullOrEmpty(options.Issuer)
                    && !(payload.TryGetProperty("iss", out var iss) && iss.ValueKind == JsonValueKind.String && iss.GetString() == options.Issuer)) return null;
                if (options.AuthorizedParties.Count > 0
                    && !(payload.TryGetProperty("azp", out var azp) && azp.ValueKind == JsonValueKind.String && options.AuthorizedParties.Contains(azp.GetString()!))) return null;
                if (!payload.TryGetProperty("sub", out var sub) || sub.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(sub.GetString())) return null;
                return payload;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static byte[] Base64UrlDecode(string s)
        {
            var b = s.Replace('-', '+').Replace('_', '/');
            switch (b.Length % 4)
            {
                case 2: b += "=="; break;
                case 3: b += "="; break;
                case 1: throw new FormatException("Invalid base64url.");
            }
            return Convert.FromBase64String(b);
        }

        internal static ClaimsPrincipal ToPrincipal(JsonElement payload)
        {
            var identity = new ClaimsIdentity("Atlas", ClaimTypes.NameIdentifier, "org_role");
            void Add(string claim, string type)
            {
                if (payload.TryGetProperty(claim, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(v.GetString()))
                    identity.AddClaim(new Claim(type, v.GetString()!));
            }
            Add("sub", ClaimTypes.NameIdentifier);
            Add("sid", "sid");
            Add("org_id", "org_id");
            Add("org_slug", "org_slug");
            Add("org_role", "org_role");
            Add("azp", "azp");
            foreach (var name in new[] { "org_permissions", "app_roles", "app_permissions" })
            {
                if (payload.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    foreach (var item in arr.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(item.GetString()))
                            identity.AddClaim(new Claim(name, item.GetString()!));
            }
            return new ClaimsPrincipal(identity);
        }
    }

    /// <summary>
    /// Authentication state from the Atlas session cookie. The state is a UI convenience for
    /// <c>&lt;AuthorizeView&gt;</c> and routing; every call to your API is authorized by the API itself.
    /// </summary>
    public sealed class AtlasAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
    {
        private readonly IAtlasSessionSource _source;
        private readonly AtlasWasmOptions _options;
        private readonly Func<DateTimeOffset> _now;
        private Timer? _timer;
        private string? _lastToken;
        private bool _started;

        public AtlasAuthenticationStateProvider(IAtlasSessionSource source, IOptions<AtlasWasmOptions> options, Func<DateTimeOffset>? now = null)
        {
            _source = source;
            _options = options.Value;
            _now = now ?? (() => DateTimeOffset.UtcNow);
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _source.GetTokenAsync().ConfigureAwait(false);
            _lastToken = token;
            StartPolling();
            var payload = AtlasSessionToken.TryDecode(token, _options, _now());
            return new AuthenticationState(payload is { } p ? AtlasSessionToken.ToPrincipal(p) : new ClaimsPrincipal(new ClaimsIdentity()));
        }

        private void StartPolling()
        {
            if (_started || _options.PollInterval <= TimeSpan.Zero) return;
            _started = true;
            _timer = new Timer(async _ => await PollAsync().ConfigureAwait(false), null, _options.PollInterval, _options.PollInterval);
        }

        internal async Task PollAsync()
        {
            var token = await _source.GetTokenAsync().ConfigureAwait(false);
            if (token != _lastToken)
            {
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            }
        }

        public void Dispose() => _timer?.Dispose();
    }

    /// <summary>
    /// Adds <c>Authorization: Bearer &lt;session token&gt;</c> to requests whose URL starts with one of
    /// <see cref="AtlasWasmOptions.AuthorizedUrls"/>. Nothing is added for any other URL, and nothing
    /// when there is no usable session.
    /// </summary>
    public sealed class AtlasBearerHandler : DelegatingHandler
    {
        private readonly IAtlasSessionSource _source;
        private readonly AtlasWasmOptions _options;
        private readonly Func<DateTimeOffset> _now;

        public AtlasBearerHandler(IAtlasSessionSource source, IOptions<AtlasWasmOptions> options, Func<DateTimeOffset>? now = null)
        {
            _source = source;
            _options = options.Value;
            _now = now ?? (() => DateTimeOffset.UtcNow);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is { IsAbsoluteUri: true } uri && IsAuthorized(uri) && request.Headers.Authorization == null)
            {
                var token = await _source.GetTokenAsync().ConfigureAwait(false);
                if (AtlasSessionToken.TryDecode(token, _options, _now()) != null)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private bool IsAuthorized(Uri uri)
        {
            foreach (var allowed in _options.AuthorizedUrls)
            {
                if (!Uri.TryCreate(allowed, UriKind.Absolute, out var baseUri)) continue;
                if (!string.Equals(baseUri.GetLeftPart(UriPartial.Authority), uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) continue;
                var basePath = baseUri.AbsolutePath.TrimEnd('/');
                if (basePath.Length == 0 || uri.AbsolutePath == basePath || uri.AbsolutePath.StartsWith(basePath + "/", StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }

    public static class AtlasBlazorWebAssemblyExtensions
    {
        /// <summary>
        /// Register the authentication state provider, <c>AddCascadingAuthenticationState</c> and
        /// the <see cref="AtlasBearerHandler"/>. Attach the handler to the HttpClient that calls your
        /// API with <c>.AddHttpMessageHandler&lt;AtlasBearerHandler&gt;()</c>.
        /// </summary>
        public static IServiceCollection AddAtlasWebAssembly(this IServiceCollection services, Action<AtlasWasmOptions> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            services.AddOptions<AtlasWasmOptions>().Configure(configure);
            services.AddAuthorizationCore();
            services.AddCascadingAuthenticationState();
            services.AddScoped<IAtlasSessionSource, CookieSessionSource>();
            services.AddScoped<AuthenticationStateProvider, AtlasAuthenticationStateProvider>();
            services.AddTransient<AtlasBearerHandler>();
            return services;
        }
    }
}
