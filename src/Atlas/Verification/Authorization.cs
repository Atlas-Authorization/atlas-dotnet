using System;
using System.Collections.Generic;

namespace Atlas.Verification
{
    /// <summary>
    /// A single authorization condition — the .NET peer of <c>@atlas/authz</c>'s
    /// <c>ProtectCondition</c>. An empty condition means "signed in". When several
    /// fields are set, ALL must hold (they are ANDed), matching the shared engine
    /// that React <c>&lt;Protect&gt;</c> and the Next.js middleware evaluate.
    /// </summary>
    public sealed class ProtectCondition
    {
        /// <summary>Require this exact organization role (matched against <c>org_role</c>).</summary>
        public string? Role { get; set; }

        /// <summary>Require this permission (matched against <c>org_permissions</c>).</summary>
        public string? Permission { get; set; }

        /// <summary>Require AT LEAST ONE of these permissions.</summary>
        public IReadOnlyList<string>? AnyPermission { get; set; }

        /// <summary>Require ALL of these permissions.</summary>
        public IReadOnlyList<string>? AllPermissions { get; set; }
    }

    /// <summary>The outcome of evaluating a <see cref="ProtectCondition"/> against claims.</summary>
    public sealed class ProtectOutcome
    {
        public bool Allowed { get; }
        /// <summary>A coarse reason when denied: <c>unauthenticated</c> | <c>forbidden</c>.</summary>
        public string Reason { get; }

        private ProtectOutcome(bool allowed, string reason)
        {
            Allowed = allowed;
            Reason = reason;
        }

        public static readonly ProtectOutcome Allow = new ProtectOutcome(true, "");
        public static ProtectOutcome Deny(string reason) => new ProtectOutcome(false, reason);
    }

    /// <summary>
    /// Thrown by <c>protect()</c> when a condition is unmet. An app maps this to
    /// a 401 (<see cref="Reason"/> == <c>unauthenticated</c>) or 403 (<c>forbidden</c>).
    /// </summary>
    public sealed class ForbiddenError : Exception
    {
        public string Reason { get; }
        public ProtectCondition Condition { get; }

        public ForbiddenError(string reason, ProtectCondition condition)
            : base($"Forbidden: {reason}")
        {
            Reason = reason;
            Condition = condition;
        }
    }

    /// <summary>
    /// The shared evaluation primitive over a bare claims object. Kept
    /// standalone so code that already holds claims (e.g. from a JWT it decoded
    /// itself) can check a condition the same way a verified session does.
    /// </summary>
    public static class Authz
    {
        /// <summary>True when the claims satisfy the condition (empty condition = signed in).</summary>
        public static bool Has(SessionClaims claims, ProtectCondition? condition = null)
            => Evaluate(claims, condition).Allowed;

        /// <summary>
        /// Evaluate a condition, returning an allow/deny outcome with a coarse
        /// reason. A present-but-unauthenticated claims object still counts as
        /// signed in for the empty condition — the caller already verified it.
        /// </summary>
        public static ProtectOutcome Evaluate(SessionClaims claims, ProtectCondition? condition = null)
        {
            if (claims == null) return ProtectOutcome.Deny("unauthenticated");
            if (condition == null) return ProtectOutcome.Allow;

            var permissions = claims.OrgPermissions ?? new List<string>();

            if (condition.Role != null && claims.OrgRole != condition.Role)
                return ProtectOutcome.Deny("forbidden");

            if (condition.Permission != null && !permissions.Contains(condition.Permission))
                return ProtectOutcome.Deny("forbidden");

            if (condition.AnyPermission != null && condition.AnyPermission.Count > 0)
            {
                var any = false;
                foreach (var p in condition.AnyPermission)
                {
                    if (permissions.Contains(p)) { any = true; break; }
                }
                if (!any) return ProtectOutcome.Deny("forbidden");
            }

            if (condition.AllPermissions != null)
            {
                foreach (var p in condition.AllPermissions)
                {
                    if (!permissions.Contains(p)) return ProtectOutcome.Deny("forbidden");
                }
            }

            return ProtectOutcome.Allow;
        }
    }
}
