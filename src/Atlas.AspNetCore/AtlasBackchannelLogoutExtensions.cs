using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Atlas.Verification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Atlas.AspNetCore
{
    /// <summary>
    /// Maps an OIDC Back-Channel Logout endpoint: Atlas POSTs a signed
    /// <c>logout_token</c> here when a user signs out, and your callback ends the
    /// matching local session(s).
    /// </summary>
    public static class AtlasBackchannelLogoutExtensions
    {
        /// <summary>
        /// Map <c>POST <paramref name="pattern"/></c> to verify the form field
        /// <c>logout_token</c> with <paramref name="verifier"/> and then call
        /// <paramref name="onLogout"/>. Responds <c>200</c> on success and <c>400</c> on an
        /// invalid token, always with <c>Cache-Control: no-store</c>.
        /// <code>
        /// var logout = new LogoutTokenVerifier(new LogoutTokenVerifierOptions { JwksUrl = ..., Issuer = ..., ClientId = ... });
        /// app.MapAtlasBackchannelLogout("/logout/backchannel", logout, async (claims, http) =>
        /// {
        ///     if (claims.Sid != null) await sessions.EndBySessionIdAsync(claims.Sid);
        ///     else await sessions.EndAllForUserAsync(claims.Sub!);
        /// });
        /// </code>
        /// </summary>
        public static IEndpointConventionBuilder MapAtlasBackchannelLogout(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            LogoutTokenVerifier verifier,
            Func<LogoutTokenClaims, HttpContext, Task> onLogout)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
            if (verifier == null) throw new ArgumentNullException(nameof(verifier));
            if (onLogout == null) throw new ArgumentNullException(nameof(onLogout));

            return endpoints.MapPost(pattern, async (HttpContext http) =>
            {
                http.Response.Headers.CacheControl = "no-store";
                string body;
                using (var reader = new StreamReader(http.Request.Body, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                LogoutTokenClaims claims;
                try
                {
                    claims = await verifier.VerifyFormBodyAsync(body, http.RequestAborted).ConfigureAwait(false);
                }
                catch (LogoutTokenException)
                {
                    http.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                await onLogout(claims, http).ConfigureAwait(false);
                http.Response.StatusCode = StatusCodes.Status200OK;
            });
        }
    }
}
