using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Atlas.Verification;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atlas.AspNetCore
{
    /// <summary>
    /// The ASP.NET Core authentication handler for Atlas session tokens. It reads
    /// the bearer token (or the session cookie), hands it to the SDK's
    /// <see cref="AtlasBackend"/> verifier, and — on success — projects the
    /// verified claims into a <see cref="ClaimsPrincipal"/> the rest of the
    /// framework already understands (<c>[Authorize]</c>, <c>User.Identity</c>,
    /// role and permission policies). All the cryptography lives in the SDK; this
    /// type's only job is the translation between an HTTP request and the verifier.
    /// </summary>
    public sealed class AtlasAuthenticationHandler : AuthenticationHandler<AtlasAuthenticationOptions>
    {
        // The net8 base ctor that still takes ISystemClock is obsolete; this is the
        // current three-argument one.
        public AtlasAuthenticationHandler(
            IOptionsMonitor<AtlasAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = ReadToken();

            // No credential at all is NoResult, not Fail: it lets other schemes in a
            // multi-scheme policy have their turn, and it is what turns an anonymous
            // request into a clean 401 challenge rather than a hard failure.
            if (string.IsNullOrEmpty(token))
                return AuthenticateResult.NoResult();

            var backend = Options.ResolveBackend();
            VerifyResult result;
            try
            {
                result = Options.UseOnlineVerification
                    ? await backend.VerifyOnlineAsync(token!, Context.RequestAborted).ConfigureAwait(false)
                    : await backend.VerifyAsync(token!, Context.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // A thrown verifier (e.g. online verify misconfigured) must not leak
                // as an unhandled 500 out of the auth pipeline.
                return AuthenticateResult.Fail(ex);
            }

            if (!result.Ok || result.Session == null)
                return AuthenticateResult.Fail($"Atlas token rejected: {result.Reason}.");

            var principal = BuildPrincipal(result.Session.Claims);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }

        /// <summary>
        /// §7.5: the <c>Authorization</c> header wins over the cookie — a caller that
        /// set a bearer token deliberately must not be silently overridden by a
        /// stale cookie the browser still carries.
        /// </summary>
        private string? ReadToken()
        {
            string? header = Request.Headers.Authorization;
            if (!string.IsNullOrEmpty(header) &&
                header!.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                return header.Substring("Bearer ".Length).Trim();
            }

            if (Request.Cookies.TryGetValue(Options.CookieName, out var cookie) &&
                !string.IsNullOrEmpty(cookie))
            {
                return cookie;
            }

            return null;
        }

        /// <summary>
        /// Map the verified session claims onto a <see cref="ClaimsPrincipal"/>.
        /// <c>sub</c> becomes <see cref="ClaimTypes.NameIdentifier"/> (what the
        /// framework treats as the user id); <c>org_role</c> is wired as the identity's
        /// role-claim type so <c>[Authorize(Roles = ...)]</c> works; each
        /// <c>org_permission</c> is a flat claim so permission policies are a lookup.
        /// </summary>
        private ClaimsPrincipal BuildPrincipal(SessionClaims claims)
        {
            // NameIdentifier for sub so User.FindFirstValue(ClaimTypes.NameIdentifier)
            // and HttpContext.User.Identity map the way every ASP.NET app expects;
            // org_role as the role type so the built-in role checks light up.
            var identity = new ClaimsIdentity(
                authenticationType: AtlasDefaults.AuthenticationScheme,
                nameType: ClaimTypes.NameIdentifier,
                roleType: AtlasClaimTypes.OrgRole);

            var list = new List<Claim>();
            if (!string.IsNullOrEmpty(claims.Sub))
                list.Add(new Claim(ClaimTypes.NameIdentifier, claims.Sub));
            if (!string.IsNullOrEmpty(claims.Sid))
                list.Add(new Claim(AtlasClaimTypes.SessionId, claims.Sid));
            if (!string.IsNullOrEmpty(claims.OrgId))
                list.Add(new Claim(AtlasClaimTypes.OrgId, claims.OrgId!));
            if (!string.IsNullOrEmpty(claims.OrgSlug))
                list.Add(new Claim(AtlasClaimTypes.OrgSlug, claims.OrgSlug!));
            if (!string.IsNullOrEmpty(claims.OrgRole))
                list.Add(new Claim(AtlasClaimTypes.OrgRole, claims.OrgRole!));
            if (!string.IsNullOrEmpty(claims.Azp))
                list.Add(new Claim(AtlasClaimTypes.AuthorizedParty, claims.Azp!));
            if (claims.OrgPermissions != null)
            {
                foreach (var p in claims.OrgPermissions)
                {
                    if (!string.IsNullOrEmpty(p))
                        list.Add(new Claim(AtlasClaimTypes.OrgPermissions, p));
                }
            }

            identity.AddClaims(list);
            return new ClaimsPrincipal(identity);
        }
    }
}
