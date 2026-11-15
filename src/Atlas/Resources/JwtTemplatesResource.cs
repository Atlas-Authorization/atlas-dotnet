using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class JwtTemplate
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "jwt_template";
        public string Name { get; init; } = "";
        public Dictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
        /// <summary>Non-secret mint settings; the HS256 secret is never returned.</summary>
        public JwtTemplateSettings? Settings { get; init; }
    }

    /// <summary>A template's mint settings as stored (the HS256 secret is never returned).</summary>
    public sealed class JwtTemplateSettings
    {
        public long? LifetimeSeconds { get; init; }
        public string? Audience { get; init; }
        /// <summary><c>RS256</c> or <c>HS256</c>.</summary>
        public string Algorithm { get; init; } = "RS256";
        public bool HasSigningSecret { get; init; }
    }

    /// <summary>
    /// Per-template mint settings. <c>LifetimeSeconds</c> is 10 to 86400 (default: the instance session-token lifetime, 60 s);
    /// on update, <see cref="ClearLifetime"/> / <see cref="ClearAudience"/> send null to reset a value.
    /// </summary>
    public sealed class JwtTemplateSettingsInput
    {
        public long? LifetimeSeconds { get; set; }
        public string? Audience { get; set; }
        /// <summary><c>RS256</c> or <c>HS256</c>.</summary>
        public string? Algorithm { get; set; }
        /// <summary>HS256 shared secret, at least 16 characters. Write-only.</summary>
        public string? SigningSecret { get; set; }
        public bool ClearLifetime { get; set; }
        public bool ClearAudience { get; set; }

        internal Dictionary<string, object?> ToBody()
        {
            var d = new Dictionary<string, object?>();
            if (ClearLifetime) d["lifetime_seconds"] = null; else if (LifetimeSeconds != null) d["lifetime_seconds"] = LifetimeSeconds;
            if (ClearAudience) d["audience"] = null; else if (Audience != null) d["audience"] = Audience;
            if (Algorithm != null) d["algorithm"] = Algorithm;
            if (SigningSecret != null) d["signing_secret"] = SigningSecret;
            return d;
        }
    }

    public sealed class CreateJwtTemplateBody
    {
        public string Name { get; set; } = "";
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
        public JwtTemplateSettingsInput? Settings { get; set; }
    }

    public sealed class DeletedJwtTemplate
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "jwt_template";
        public string Name { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary>The JWT-templates namespace (<c>/v1/jwt_templates</c>), keyed by template name.</summary>
    public sealed class JwtTemplatesResource : ResourceBase
    {
        public JwtTemplatesResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<JwtTemplate>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<JwtTemplate>>(HttpVerb.Get, "/v1/jwt_templates", cancellationToken: ct);

        /// <summary>Fetched by template name.</summary>
        public Task<JwtTemplate> GetAsync(string name, CancellationToken ct = default)
            => Req<JwtTemplate>(HttpVerb.Get, $"/v1/jwt_templates/{Enc(name)}", cancellationToken: ct);

        public Task<JwtTemplate> CreateAsync(CreateJwtTemplateBody body, CancellationToken ct = default)
            => Req<JwtTemplate>(HttpVerb.Post, "/v1/jwt_templates",
                Body(("name", body.Name), ("claims", body.Claims), ("settings", body.Settings?.ToBody())), cancellationToken: ct);

        /// <summary>Only the claims are updatable; the name is the key.</summary>
        public Task<JwtTemplate> UpdateAsync(string name, Dictionary<string, string> claims, CancellationToken ct = default)
            => Req<JwtTemplate>(HttpVerb.Patch, $"/v1/jwt_templates/{Enc(name)}", body: Body(("claims", claims)), cancellationToken: ct);

        /// <summary>Claims and/or settings (either may be null); the name is the key and cannot change.</summary>
        public Task<JwtTemplate> UpdateAsync(string name, Dictionary<string, string>? claims, JwtTemplateSettingsInput? settings, CancellationToken ct = default)
            => Req<JwtTemplate>(HttpVerb.Patch, $"/v1/jwt_templates/{Enc(name)}",
                body: Body(("claims", claims), ("settings", settings?.ToBody())), cancellationToken: ct);

        public Task<DeletedJwtTemplate> DeleteAsync(string name, CancellationToken ct = default)
            => Req<DeletedJwtTemplate>(HttpVerb.Delete, $"/v1/jwt_templates/{Enc(name)}", cancellationToken: ct);
    }
}
