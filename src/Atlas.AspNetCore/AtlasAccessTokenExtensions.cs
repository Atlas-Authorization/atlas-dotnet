using System.Threading.Tasks;
using Atlas.Verification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Atlas.AspNetCore
{
    /// <summary>
    /// Protects an API with an Atlas-issued OAuth access token (an <c>Authorization: Bearer</c> header, never a
    /// cookie). A refusal answers with the verifier's status and RFC 6750 / RFC 9728 <c>WWW-Authenticate</c>
    /// challenge; on success the <see cref="AccessTokenResult"/> is in <c>HttpContext.Items[ItemKey]</c>.
    /// </summary>
    public static class AtlasAccessTokenExtensions
    {
        /// <summary>The <c>HttpContext.Items</c> key holding the verified <see cref="AccessTokenResult"/>.</summary>
        public const string ItemKey = "atlas.access_token";

        /// <summary>Add the access-token gate to the pipeline (use <c>app.Map</c> / <c>UseWhen</c> to scope it).</summary>
        public static IApplicationBuilder UseAtlasAccessToken(this IApplicationBuilder app, AccessTokenVerifier verifier, params string[] requiredScopes)
            => app.Use(async (context, next) =>
            {
                var result = await verifier.VerifyAsync(context.Request.Headers["Authorization"].ToString(), requiredScopes, context.RequestAborted);
                if (!result.Ok)
                {
                    context.Response.StatusCode = result.Status;
                    context.Response.Headers["WWW-Authenticate"] = result.WwwAuthenticate;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"" + (result.Error ?? "unauthorized") + "\"}");
                    return;
                }
                context.Items[ItemKey] = result;
                await next();
            });

        /// <summary>The verified access token for this request, or null.</summary>
        public static AccessTokenResult? GetAtlasAccessToken(this HttpContext context)
            => context.Items.TryGetValue(ItemKey, out var v) ? v as AccessTokenResult : null;
    }
}
