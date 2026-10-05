namespace Atlas.AspNetCore
{
    /// <summary>
    /// Well-known names for the Atlas authentication integration.
    /// </summary>
    public static class AtlasDefaults
    {
        /// <summary>
        /// The authentication scheme registered by <c>AddAtlas</c>. An app opts a
        /// resource into Atlas with
        /// <c>[Authorize(AuthenticationSchemes = AtlasDefaults.AuthenticationScheme)]</c>,
        /// so the name is a public, stable constant rather than a magic string the
        /// caller has to keep in sync with the registration.
        /// </summary>
        public const string AuthenticationScheme = "Atlas";
    }

    /// <summary>
    /// The claim types the handler emits. They are the raw Atlas claim names so a
    /// consumer reading <c>User.FindFirst("org_id")</c> sees exactly what the JWT
    /// carried — no silent renaming into SOAP/WS-* URIs that would force the caller
    /// to learn a second vocabulary.
    /// </summary>
    public static class AtlasClaimTypes
    {
        /// <summary>The session id (<c>sid</c>). Distinct from the user id so an app can revoke one session.</summary>
        public const string SessionId = "sid";

        /// <summary>The active organization id (<c>org_id</c>), when the session is org-scoped.</summary>
        public const string OrgId = "org_id";

        /// <summary>The active organization slug (<c>org_slug</c>).</summary>
        public const string OrgSlug = "org_slug";

        /// <summary>
        /// The caller's role in the active organization (<c>org_role</c>). Also used
        /// as the identity's role-claim type so <c>[Authorize(Roles = "...")]</c> works.
        /// </summary>
        public const string OrgRole = "org_role";

        /// <summary>
        /// A single organization permission (<c>org_permissions</c>). One claim is
        /// emitted per permission so permission checks are a flat claim lookup.
        /// </summary>
        public const string OrgPermissions = "org_permissions";

        /// <summary>The authorized party (<c>azp</c>) the token was minted for, when present.</summary>
        public const string AuthorizedParty = "azp";
    }
}
