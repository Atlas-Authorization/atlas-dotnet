using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Atlas.AspNetCore
{
    /// <summary>
    /// Moving from Better Auth without signing anyone out. Add this BEFORE your authentication middleware: a GET/HEAD that carries
    /// a Better Auth session cookie and no Atlas session is exchanged ONCE with Atlas through the Backend API (instance secret
    /// key, forwarding the user's IP and user agent), the Atlas session is set as your own first-party cookies
    /// (<c>__session</c>, <c>__atlas_rt</c>), the Better Auth cookies are cleared and the browser is redirected to the same URL.
    /// A user who still owes a second factor is sent to <see cref="BetterAuthBridgeOptions.SignInUrl"/> carrying
    /// <c>__atlas_attempt</c> / <c>__atlas_status</c>. A final refusal (invalid, expired, already used, ...) clears the Better Auth
    /// cookies and the request carries on unauthenticated; a closed bridge, a rate limit or an outage keeps them so a later request retries.
    /// <code>
    /// app.UseAtlasBetterAuthBridge(new AtlasClient(secretKey), o => o.SignInUrl = "/sign-in");
    /// app.UseAuthentication();
    /// </code>
    /// The end user's IP is <c>HttpContext.Connection.RemoteIpAddress</c>: behind a proxy, enable ASP.NET Core's forwarded-headers
    /// middleware (<c>UseForwardedHeaders</c>) so it is the visitor and not the proxy; without an address the first
    /// <c>X-Forwarded-For</c> hop is sent.
    /// </summary>
    public static class AtlasBetterAuthBridgeExtensions
    {
        /// <summary>Add the bridge using an <see cref="AtlasClient"/> on the instance secret key.</summary>
        public static IApplicationBuilder UseAtlasBetterAuthBridge(this IApplicationBuilder app, AtlasClient client, Action<BetterAuthBridgeOptions>? configure = null)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            var options = new BetterAuthBridgeOptions();
            configure?.Invoke(options);
            return app.UseAtlasBetterAuthBridge(new BetterAuthSessionBridge(client, options));
        }

        /// <summary>Add the bridge using a ready <see cref="BetterAuthSessionBridge"/>.</summary>
        public static IApplicationBuilder UseAtlasBetterAuthBridge(this IApplicationBuilder app, BetterAuthSessionBridge bridge)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (bridge == null) throw new ArgumentNullException(nameof(bridge));
            return app.Use(async (context, next) =>
            {
                var req = context.Request;
                if (!HttpMethods.IsGet(req.Method) && !HttpMethods.IsHead(req.Method))
                {
                    await next().ConfigureAwait(false);
                    return;
                }

                var decision = await bridge.DecideAsync(
                    string.Join("; ", req.Headers.Cookie.ToArray()),
                    context.Connection.RemoteIpAddress?.ToString(),
                    req.Headers.UserAgent.ToString(),
                    req.Headers["X-Forwarded-For"].ToString(),
                    context.RequestAborted).ConfigureAwait(false);

                switch (decision.Action)
                {
                    case BridgeAction.SignedIn:
                    case BridgeAction.NeedsMfa:
                        foreach (var cookie in decision.Cookies) context.Response.Headers.Append("Set-Cookie", cookie.Serialize());
                        context.Response.Headers.CacheControl = "no-store";
                        var same = req.PathBase.ToString() + req.Path.ToString() + req.QueryString.ToString();
                        context.Response.Redirect(decision.Action == BridgeAction.SignedIn
                            ? same
                            : decision.ContinuationUrl(bridge.Options.SignInUrl ?? req.Scheme + "://" + req.Host + same));
                        return;
                    case BridgeAction.Refused when decision.Cookies.Count > 0:
                        var clear = decision.Cookies;
                        context.Response.OnStarting(() =>
                        {
                            foreach (var cookie in clear) context.Response.Headers.Append("Set-Cookie", cookie.Serialize());
                            return Task.CompletedTask;
                        });
                        break;
                }
                await next().ConfigureAwait(false);
            });
        }
    }
}
