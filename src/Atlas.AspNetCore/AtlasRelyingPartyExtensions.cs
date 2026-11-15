using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Atlas.Rp;
using Atlas.Verification;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.AspNetCore
{
    /// <summary>Claim types the relying party puts on the signed-in principal.</summary>
    public static class AtlasRpClaims
    {
        public const string Sid = "atlas_sid";
        public const string SessionId = "atlas_app_session";
        public const string AuthTime = "auth_time";
        public const string EmailVerified = "email_verified";
        public const string OrgId = "org_id";
        public const string OrgRole = "org_role";
        public const string ExternalId = "external_id";
    }

    /// <summary>How <see cref="AtlasRelyingPartyExtensions.MapAtlasRelyingParty"/> routes the flow and keeps your session.</summary>
    public sealed class AtlasRelyingPartyEndpointOptions
    {
        public string SignInPath { get; set; } = "/auth/sign-in";
        /// <summary>Default: the path of the configured <c>RedirectUri</c>.</summary>
        public string? CallbackPath { get; set; }
        public string SignOutPath { get; set; } = "/auth/sign-out";
        /// <summary>When set, <c>POST</c> here receives Atlas's back-channel logout and ends the matching app sessions.</summary>
        public string? BackchannelLogoutPath { get; set; }
        /// <summary>The scheme that holds the app session (a cookie scheme you registered). Default <c>Cookies</c>.</summary>
        public string AuthenticationScheme { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;
        public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(8);
        /// <summary>Keep the ID, access and refresh tokens in the session (<c>GetTokenAsync</c>); the ID token also lets sign-out skip the confirmation page.</summary>
        public bool SaveTokens { get; set; } = true;
        /// <summary>Called after the principal is signed in, before the redirect: add claims (<c>return the principal to sign in</c>) or do your own provisioning.</summary>
        public Func<HttpContext, RpCallbackResult, ClaimsPrincipal, Task<ClaimsPrincipal>>? OnSignedIn { get; set; }
        /// <summary>Called on a failed callback. Default: a plain 400.</summary>
        public Func<HttpContext, AtlasOidcException, Task>? OnSignInError { get; set; }
        /// <summary>Called by back-channel logout with the app session ids to end; the default records them so <c>UseAtlasSession</c> rejects those sessions.</summary>
        public Func<HttpContext, IReadOnlyList<string>, LogoutTokenClaims, Task>? OnBackchannelLogout { get; set; }
    }

    /// <summary>App sessions ended by a back-channel logout, remembered until their cookie would have expired.</summary>
    public sealed class AtlasEndedSessions
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _ended = new ConcurrentDictionary<string, DateTimeOffset>();

        public void End(string appSessionId, DateTimeOffset until)
        {
            _ended[appSessionId] = until;
            if (_ended.Count > 10_000) foreach (var kv in _ended.Where(k => k.Value <= DateTimeOffset.UtcNow).ToList()) _ended.TryRemove(kv.Key, out _);
        }

        public bool IsEnded(string appSessionId) => _ended.TryGetValue(appSessionId, out var until) && until > DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// ASP.NET Core wiring for the hosted-redirect relying party (<see cref="RelyingParty"/>): minimal-API endpoints for sign-in, callback,
    /// sign-out and back-channel logout over your cookie scheme, plus a middleware that keeps the Atlas session alive and drops app
    /// sessions Atlas has ended.
    /// <code>
    /// builder.Services.AddAuthentication("Cookies").AddCookie();
    /// builder.Services.AddAtlasRelyingParty(o =&gt; { o.Issuer = ...; o.ClientId = ...; o.ClientSecret = ...; o.RedirectUri = "https://app.example/auth/callback"; o.Secret = ...; });
    /// var app = builder.Build();
    /// app.UseAuthentication();
    /// app.UseAtlasSession();
    /// app.MapAtlasRelyingParty();
    /// app.MapGet("/me", (ClaimsPrincipal user) =&gt; user.FindFirstValue("email")).RequireAuthorization();
    /// </code>
    /// </summary>
    public static class AtlasRelyingPartyExtensions
    {
        /// <summary>Register the <see cref="RelyingParty"/> (singleton), an in-memory <see cref="ISidStore"/> unless you set one, and <see cref="AtlasEndedSessions"/>.</summary>
        public static IServiceCollection AddAtlasRelyingParty(this IServiceCollection services, Action<RelyingPartyOptions> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var options = new RelyingPartyOptions();
            configure(options);
            options.SidStore ??= new MemorySidStore();
            services.AddSingleton(new RelyingParty(options));
            services.AddSingleton(options);
            services.AddSingleton<AtlasEndedSessions>();
            return services;
        }

        /// <summary>Map the relying-party endpoints. Returns the builder for chaining.</summary>
        public static IEndpointRouteBuilder MapAtlasRelyingParty(this IEndpointRouteBuilder endpoints, Action<AtlasRelyingPartyEndpointOptions>? configure = null)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
            var o = new AtlasRelyingPartyEndpointOptions();
            configure?.Invoke(o);
            var rpOptions = endpoints.ServiceProvider.GetRequiredService<RelyingPartyOptions>();
            var callbackPath = o.CallbackPath ?? new Uri(rpOptions.RedirectUri).AbsolutePath;

            endpoints.MapGet(o.SignInPath, async (HttpContext http, RelyingParty rp) =>
            {
                var redirect = await rp.StartAsync(QueryOf(http), null, http.RequestAborted).ConfigureAwait(false);
                Send(http, redirect);
            });

            endpoints.MapGet(callbackPath, async (HttpContext http, RelyingParty rp, AtlasEndedSessions ended) =>
            {
                RpCallbackResult result;
                try
                {
                    result = await rp.CallbackAsync(QueryOf(http), http.Request.Headers["Cookie"].ToString(), http.RequestAborted).ConfigureAwait(false);
                }
                catch (AtlasOidcException e)
                {
                    if (e.ClearCookie != null) http.Response.Headers.Append("Set-Cookie", e.ClearCookie);
                    http.Response.Headers["Cache-Control"] = "no-store";
                    if (o.OnSignInError != null) await o.OnSignInError(http, e).ConfigureAwait(false);
                    else
                    {
                        http.Response.StatusCode = StatusCodes.Status400BadRequest;
                        http.Response.ContentType = "text/plain; charset=utf-8";
                        await http.Response.WriteAsync("Sign-in failed. Please try again.").ConfigureAwait(false);
                    }
                    return;
                }
                var principal = Principal(result, o.AuthenticationScheme);
                if (o.OnSignedIn != null) principal = await o.OnSignedIn(http, result, principal).ConfigureAwait(false);
                var expires = DateTimeOffset.UtcNow.Add(o.SessionLifetime);
                var props = new AuthenticationProperties { ExpiresUtc = expires, IsPersistent = true };
                if (o.SaveTokens)
                    props.StoreTokens(new[]
                    {
                        new AuthenticationToken { Name = "id_token", Value = result.Tokens.IdToken },
                        new AuthenticationToken { Name = "access_token", Value = result.Tokens.AccessToken },
                    }.Concat(result.Tokens.RefreshToken != null ? new[] { new AuthenticationToken { Name = "refresh_token", Value = result.Tokens.RefreshToken } } : Array.Empty<AuthenticationToken>()));
                await http.SignInAsync(o.AuthenticationScheme, principal, props).ConfigureAwait(false);
                var appSession = principal.FindFirstValue(AtlasRpClaims.SessionId);
                if (result.User.Sid != null && appSession != null) await rp.BindSessionAsync(result.User.Sid, appSession, expires).ConfigureAwait(false);
                http.Response.Headers.Append("Set-Cookie", result.ClearCookie);
                http.Response.Headers["Cache-Control"] = "no-store";
                http.Response.Redirect(result.ReturnTo);
            });

            endpoints.MapMethods(o.SignOutPath, new[] { "GET", "POST" }, async (HttpContext http, RelyingParty rp) =>
            {
                var auth = await http.AuthenticateAsync(o.AuthenticationScheme).ConfigureAwait(false);
                var sid = auth.Principal?.FindFirstValue(AtlasRpClaims.Sid);
                var idToken = auth.Properties?.GetTokenValue("id_token");
                await http.SignOutAsync(o.AuthenticationScheme).ConfigureAwait(false);
                Send(http, await rp.SignOutAsync(new SignOutOptions { Sid = sid, IdToken = idToken }, http.RequestAborted).ConfigureAwait(false));
            });

            if (o.BackchannelLogoutPath != null)
            {
                endpoints.MapPost(o.BackchannelLogoutPath, async (HttpContext http, RelyingParty rp, AtlasEndedSessions ended) =>
                {
                    http.Response.Headers["Cache-Control"] = "no-store";
                    string body;
                    using (var reader = new StreamReader(http.Request.Body, Encoding.UTF8)) body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var answer = await rp.HandleBackchannelLogoutAsync(body, async (ids, claims) =>
                    {
                        if (o.OnBackchannelLogout != null) await o.OnBackchannelLogout(http, ids, claims).ConfigureAwait(false);
                        else foreach (var id in ids) ended.End(id, DateTimeOffset.UtcNow.Add(o.SessionLifetime));
                    }, http.RequestAborted).ConfigureAwait(false);
                    if (answer.RetryAfterSeconds != null) http.Response.Headers["Retry-After"] = answer.RetryAfterSeconds.Value.ToString();
                    http.Response.StatusCode = answer.Status;
                });
            }
            return endpoints;
        }

        /// <summary>
        /// Middleware (after <c>UseAuthentication</c>): for a signed-in request it drops a session Atlas ended by back-channel logout and
        /// keeps the Atlas session alive (once per <c>TouchInterval</c> per session, tracked in this process); a session that has
        /// ended at Atlas is signed out here and the request continues as anonymous.
        /// </summary>
        public static IApplicationBuilder UseAtlasSession(this IApplicationBuilder app, string authenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme)
        {
            var touched = new ConcurrentDictionary<string, long>();
            return app.Use(async (http, next) =>
            {
                var user = http.User;
                var sid = user.FindFirstValue(AtlasRpClaims.Sid);
                if (user.Identity?.IsAuthenticated == true && sid != null)
                {
                    var ended = http.RequestServices.GetService<AtlasEndedSessions>();
                    var appSession = user.FindFirstValue(AtlasRpClaims.SessionId);
                    var gone = appSession != null && ended != null && ended.IsEnded(appSession);
                    if (!gone)
                    {
                        var rp = http.RequestServices.GetRequiredService<RelyingParty>();
                        touched.TryGetValue(sid, out var last);
                        try
                        {
                            var r = await rp.TouchIfDueAsync(sid, last == 0 ? (long?)null : last, http.RequestAborted).ConfigureAwait(false);
                            if (r.Status == TouchStatus.Touched) touched[sid] = r.LastTouchedAt;
                            gone = r.Status == TouchStatus.Ended;
                        }
                        catch (Exception) when (!http.RequestAborted.IsCancellationRequested) { /* a key or availability problem is not "signed out": keep the session */ }
                    }
                    if (gone)
                    {
                        touched.TryRemove(sid, out _);
                        await http.SignOutAsync(authenticationScheme).ConfigureAwait(false);
                        http.User = new ClaimsPrincipal(new ClaimsIdentity());
                    }
                }
                await next().ConfigureAwait(false);
            });
        }

        private static ClaimsPrincipal Principal(RpCallbackResult r, string scheme)
        {
            var u = r.User;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, u.Sub),
                new Claim("sub", u.Sub),
                new Claim(AtlasRpClaims.SessionId, Guid.NewGuid().ToString("N")),
                new Claim(AtlasRpClaims.EmailVerified, u.EmailVerified ? "true" : "false"),
            };
            void Add(string type, string? value) { if (!string.IsNullOrEmpty(value)) claims.Add(new Claim(type, value!)); }
            Add(AtlasRpClaims.Sid, u.Sid);
            Add(ClaimTypes.Email, u.Email);
            Add("email", u.Email);
            Add(ClaimTypes.Name, u.Name);
            Add(AtlasRpClaims.ExternalId, u.ExternalId);
            Add(AtlasRpClaims.OrgId, u.OrgId);
            Add(AtlasRpClaims.OrgRole, u.OrgRole);
            if (u.AuthTime != null) claims.Add(new Claim(AtlasRpClaims.AuthTime, u.AuthTime.Value.ToString()));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
        }

        private static IReadOnlyDictionary<string, string> QueryOf(HttpContext http)
            => http.Request.Query.ToDictionary(k => k.Key, k => k.Value.ToString());

        private static void Send(HttpContext http, RpRedirect redirect)
        {
            foreach (var c in redirect.SetCookies) http.Response.Headers.Append("Set-Cookie", c);
            http.Response.Headers["Cache-Control"] = "no-store";
            http.Response.Redirect(redirect.Location);
        }
    }
}
