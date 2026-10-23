using System.Threading.Tasks;
using Atlas.Verification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Atlas.AspNetCore
{
    /// <summary>
    /// Protects an API with an Atlas-issued OAuth access token (an <c>Authorization: Bearer</c> or <c>DPoP</c>
    /// header, never a cookie). A DPoP-bound token must come with a valid <c>DPoP</c> proof for this request
    /// (RFC 9449). A refusal answers with the verifier's status and RFC 6750 / RFC 9449 / RFC 9728
    /// <c>WWW-Authenticate</c> challenge; on success the <see cref="AccessTokenResult"/> is in
    /// <c>HttpContext.Items[ItemKey]</c>. The verifier's <c>DPoP-Nonce</c>, when it issues nonces, is set on every response.
    /// </summary>
    public static class AtlasAccessTokenExtensions
    {
        /// <summary>The <c>HttpContext.Items</c> key holding the verified <see cref="AccessTokenResult"/>.</summary>
        public const string ItemKey = "atlas.access_token";

        /// <summary>Add the access-token gate to the pipeline (use <c>app.Map</c> / <c>UseWhen</c> to scope it).</summary>
        public static IApplicationBuilder UseAtlasAccessToken(this IApplicationBuilder app, AccessTokenVerifier verifier, params string[] requiredScopes)
            => app.Use(async (context, next) =>
            {
                var req = context.Request;
                var path = req.PathBase.ToString() + req.Path.ToString() + req.QueryString.ToString();
                var proofs = req.Headers["DPoP"];
                var result = await verifier.VerifyRequestAsync(new AccessTokenRequest
                {
                    Method = req.Method,
                    Url = verifier.HasPublicOrigin ? path : req.Scheme + "://" + req.Host.ToString() + path,
                    Authorization = req.Headers["Authorization"].ToString(),
                    Dpop = proofs.Count == 1 ? proofs[0] : null, // several DPoP headers are no proof at all
                }, requiredScopes, context.RequestAborted);
                if (result.DpopNonce != null) context.Response.Headers["DPoP-Nonce"] = result.DpopNonce;
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
