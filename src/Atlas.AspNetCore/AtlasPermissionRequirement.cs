using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Atlas.Verification;
using Microsoft.AspNetCore.Authorization;

namespace Atlas.AspNetCore
{
    /// <summary>
    /// An authorization requirement that defers to Atlas's own authorization
    /// engine. Rather than re-deciding "does this principal have permission X"
    /// here, it reconstructs the Atlas claim shape from the principal and asks
    /// <see cref="Authz"/> — so an ASP.NET policy, the React <c>&lt;Protect&gt;</c>
    /// component, and the Next.js middleware all reach the same verdict from the
    /// one shared rule set.
    /// </summary>
    public sealed class AtlasPermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>The condition the principal must satisfy (ANDed fields, per <see cref="ProtectCondition"/>).</summary>
        public ProtectCondition Condition { get; }

        public AtlasPermissionRequirement(ProtectCondition condition)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }
    }

    /// <summary>
    /// Evaluates an <see cref="AtlasPermissionRequirement"/> by rebuilding the
    /// <see cref="SessionClaims"/> the handler projected onto the principal and
    /// running them through <see cref="Authz.Has(SessionClaims, ProtectCondition)"/>.
    /// </summary>
    public sealed class AtlasPermissionAuthorizationHandler
        : AuthorizationHandler<AtlasPermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, AtlasPermissionRequirement requirement)
        {
            var user = context.User;

            // An unauthenticated principal can satisfy nothing. Leaving the
            // requirement unmet (not failing it outright) keeps a parallel policy
            // branch able to succeed, matching ASP.NET's own convention.
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
                return Task.CompletedTask;

            // Rebuild only the fields the authz engine inspects. The permission and
            // role claims the handler emitted are the single source of truth here —
            // we are not re-verifying the token, only re-reading its verdict.
            var claims = new SessionClaims
            {
                OrgRole = user.FindFirst(AtlasClaimTypes.OrgRole)?.Value,
                OrgPermissions = user.FindAll(AtlasClaimTypes.OrgPermissions)
                    .Select(c => c.Value).ToList(),
            };

            if (Authz.Has(claims, requirement.Condition))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
