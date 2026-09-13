using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atlas.Verification
{
    /// <summary>
    /// The verified claims of an Atlas session JWT. The named members are the
    /// first-party session claims; any further claims (custom JWT-template
    /// claims, and the <c>token_use</c> / <c>aud</c> markers the confusion guard
    /// inspects) land in <see cref="Extra"/>.
    /// </summary>
    public sealed class SessionClaims
    {
        [JsonPropertyName("iss")] public string Iss { get; init; } = "";
        [JsonPropertyName("sub")] public string Sub { get; init; } = "";
        [JsonPropertyName("sid")] public string Sid { get; init; } = "";
        [JsonPropertyName("exp")] public long Exp { get; init; }
        [JsonPropertyName("nbf")] public long? Nbf { get; init; }
        [JsonPropertyName("iat")] public long? Iat { get; init; }
        [JsonPropertyName("azp")] public string? Azp { get; init; }
        [JsonPropertyName("sv")] public int? Sv { get; init; }
        [JsonPropertyName("mfa")] public bool? Mfa { get; init; }
        [JsonPropertyName("org_id")] public string? OrgId { get; init; }
        [JsonPropertyName("org_slug")] public string? OrgSlug { get; init; }
        [JsonPropertyName("org_role")] public string? OrgRole { get; init; }
        [JsonPropertyName("org_permissions")] public List<string>? OrgPermissions { get; init; }

        /// <summary>Every claim without a named member above (incl. <c>token_use</c>, <c>aud</c>).</summary>
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; init; } = new Dictionary<string, JsonElement>();
    }

    /// <summary>
    /// A verified session, carrying the ergonomic authorization helpers bound to
    /// its claims — the server-side equivalent of Clerk's <c>auth()</c>.
    /// <see cref="Has"/> answers a condition; <see cref="Protect"/> asserts one,
    /// throwing <see cref="ForbiddenError"/> (which the app maps to 401/403).
    /// </summary>
    public sealed class AuthorizedSession
    {
        public SessionClaims Claims { get; }

        public AuthorizedSession(SessionClaims claims)
        {
            Claims = claims;
        }

        /// <summary>True when the claims satisfy the condition (empty condition = "signed in").</summary>
        public bool Has(ProtectCondition? condition = null) => Authz.Has(Claims, condition);

        /// <summary>Assert the condition; returns the claims on success, throws otherwise.</summary>
        public SessionClaims Protect(ProtectCondition? condition = null)
        {
            var effective = condition ?? new ProtectCondition();
            var outcome = Authz.Evaluate(Claims, effective);
            if (!outcome.Allowed) throw new ForbiddenError(outcome.Reason, effective);
            return Claims;
        }
    }

    /// <summary>Why a verification failed. Mirrors the TypeScript SDK's reason union.</summary>
    public enum VerifyFailureReason
    {
        /// <summary>Not a well-formed three-segment JWT, or no token was present.</summary>
        Malformed,
        /// <summary>Signature, issuer, lifetime, or the token-confusion guard rejected it.</summary>
        Invalid,
        /// <summary>No usable key material (JWKS empty or unreachable, unknown <c>kid</c>).</summary>
        NoKeys,
        /// <summary>An <c>azp</c> outside the configured <c>authorizedParties</c> allowlist.</summary>
        UnauthorizedParty,
    }

    /// <summary>
    /// The result of verifying a token: either an <see cref="AuthorizedSession"/>
    /// (<see cref="Ok"/> true) or a failure with a coarse <see cref="Reason"/>.
    /// </summary>
    public sealed class VerifyResult
    {
        public bool Ok { get; }
        /// <summary>The verified session — non-null exactly when <see cref="Ok"/> is true.</summary>
        public AuthorizedSession? Session { get; }
        /// <summary>Why verification failed — meaningful only when <see cref="Ok"/> is false.</summary>
        public VerifyFailureReason Reason { get; }

        private VerifyResult(bool ok, AuthorizedSession? session, VerifyFailureReason reason)
        {
            Ok = ok;
            Session = session;
            Reason = reason;
        }

        public static VerifyResult Success(AuthorizedSession session) => new VerifyResult(true, session, default);
        public static VerifyResult Failure(VerifyFailureReason reason) => new VerifyResult(false, null, reason);

        /// <summary>The verified claims, or null on failure.</summary>
        public SessionClaims? Claims => Session?.Claims;
    }
}
