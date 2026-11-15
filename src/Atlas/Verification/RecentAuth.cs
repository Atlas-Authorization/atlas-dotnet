using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Atlas.Verification
{
    /// <summary>
    /// Step-up checks on a verified Atlas token: <c>auth_time</c> (the first-factor sign-in or the last step-up),
    /// <c>amr</c> and <c>acr</c> (<c>urn:atlas:acr:basic</c>, <c>urn:atlas:acr:mfa</c>, <c>urn:atlas:acr:step_up</c>).
    /// The .NET peers of the TypeScript <c>authenticatedWithin</c>, <c>usedMultiFactor</c>, <c>mfaSatisfied</c> and
    /// <c>mfaOrgSatisfied</c>; every helper works on <see cref="SessionClaims"/> and on an OIDC ID token's claims
    /// (<see cref="JsonElement"/> object).
    /// <code>
    /// if (!claims.AuthenticatedWithin(300)) return Results.Json(new { error = "reauthentication_required", max_age = 300 }, statusCode: 401);
    /// </code>
    /// </summary>
    public static class RecentAuth
    {
        public const string AcrMfa = "urn:atlas:acr:mfa";

        /// <summary>Whether <c>auth_time</c> is within <paramref name="maxAgeSeconds"/> of now. A token without <c>auth_time</c> never is.</summary>
        public static bool AuthenticatedWithin(this SessionClaims claims, double maxAgeSeconds, DateTimeOffset? now = null)
            => Within(Get(claims, "auth_time"), maxAgeSeconds, now);

        public static bool AuthenticatedWithin(this JsonElement claims, double maxAgeSeconds, DateTimeOffset? now = null)
            => Within(Get(claims, "auth_time"), maxAgeSeconds, now);

        /// <summary>Whether the session proved a second factor: <c>acr</c> is the mfa level, or <c>amr</c> includes <c>mfa</c>.</summary>
        public static bool UsedMultiFactor(this SessionClaims claims) => Multi(Get(claims, "acr"), Get(claims, "amr"));

        public static bool UsedMultiFactor(this JsonElement claims) => Multi(Get(claims, "acr"), Get(claims, "amr"));

        /// <summary>
        /// Whether the session satisfies a "requires MFA" rule: <c>mfa_satisfied</c> (the server's own verdict) when the token
        /// carries it, else <c>mfa == true</c>, <see cref="UsedMultiFactor(SessionClaims)"/>, or an enterprise SSO sign-in
        /// (the identity provider owns the second factor).
        /// </summary>
        public static bool MfaSatisfied(this SessionClaims claims)
        {
            if (Get(claims, "mfa_satisfied") is { ValueKind: JsonValueKind.True or JsonValueKind.False } v) return v.ValueKind == JsonValueKind.True;
            return claims.Mfa == true || claims.UsedMultiFactor() || claims.SsoConnectionId != null;
        }

        public static bool MfaSatisfied(this JsonElement claims)
        {
            if (Get(claims, "mfa_satisfied") is { ValueKind: JsonValueKind.True or JsonValueKind.False } v) return v.ValueKind == JsonValueKind.True;
            return Get(claims, "mfa") is { ValueKind: JsonValueKind.True } || claims.UsedMultiFactor()
                || Get(claims, "sso_connection_id") is { ValueKind: JsonValueKind.String };
        }

        /// <summary>
        /// Whether the session meets the active organization's MFA policy: true when the organization does not require MFA or the
        /// token says <c>org_mfa_ok</c>. With <paramref name="orgIdOrExternalId"/>, a token whose active organization is another
        /// one answers false. The claims describe the ACTIVE organization only.
        /// </summary>
        public static bool MfaOrgSatisfied(this SessionClaims claims, string? orgIdOrExternalId = null)
        {
            var required = Get(claims, "org_mfa_required") is { ValueKind: JsonValueKind.True };
            var ok = Get(claims, "org_mfa_ok") is { ValueKind: JsonValueKind.True };
            if (orgIdOrExternalId == null) return !required || ok;
            var matches = claims.OrgId == orgIdOrExternalId || Str(Get(claims, "org_external_id")) == orgIdOrExternalId;
            if (required) return ok && (matches || claims.OrgId == null);
            return matches;
        }

        private static bool Within(JsonElement? authTime, double maxAgeSeconds, DateTimeOffset? now)
        {
            if (authTime is not { ValueKind: JsonValueKind.Number } t || !t.TryGetDouble(out var at)) return false;
            return (now ?? DateTimeOffset.UtcNow).ToUnixTimeMilliseconds() / 1000.0 - at <= maxAgeSeconds;
        }

        private static bool Multi(JsonElement? acr, JsonElement? amr)
        {
            if (Str(acr) == AcrMfa) return true;
            if (amr is { ValueKind: JsonValueKind.Array } a)
                foreach (var m in a.EnumerateArray()) if (m.ValueKind == JsonValueKind.String && m.GetString() == "mfa") return true;
            return false;
        }

        private static string? Str(JsonElement? e) => e is { ValueKind: JsonValueKind.String } s ? s.GetString() : null;

        private static JsonElement? Get(SessionClaims c, string name) => c.Extra.TryGetValue(name, out var v) ? v : (JsonElement?)null;

        private static JsonElement? Get(JsonElement c, string name)
            => c.ValueKind == JsonValueKind.Object && c.TryGetProperty(name, out var v) ? v : (JsonElement?)null;
    }
}
