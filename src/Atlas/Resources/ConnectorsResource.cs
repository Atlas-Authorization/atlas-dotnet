using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class ConnectorBody
    {
        public string? Slug { get; set; }
        public string? DisplayName { get; set; }
        public string? LogoUrl { get; set; }
        public string? Tagline { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public List<string>? Tags { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? DocsUrl { get; set; }
        public string? SupportUrl { get; set; }
        public List<object?>? Screenshots { get; set; }
        public string? SetupGuide { get; set; }
        public string? Protocol { get; set; }
        public string? AuthorizationEndpoint { get; set; }
        public string? TokenEndpoint { get; set; }
        public string? UserinfoEndpoint { get; set; }
        public string? Issuer { get; set; }
        public List<string>? DefaultScopes { get; set; }
        public List<object?>? ApiScopes { get; set; }
        public List<object?>? CredentialFields { get; set; }
        public Dictionary<string, object?>? AuthorizationParams { get; set; }
        public Dictionary<string, object?>? Quirks { get; set; }
        public List<string>? ShareModes { get; set; }
        public string? IdpInstanceId { get; set; }
        public string? IdpIssuer { get; set; }
        public bool? IdpAutoRegister { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public ConnectorBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class ConnectorSharedCredentialsBody
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public bool? Revoke { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public ConnectorSharedCredentialsBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class InstallConnectorBody
    {
        public string? ShareMode { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public List<string>? Scopes { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public InstallConnectorBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class Connector
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Slug { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string? LogoUrl { get; init; }
        public string? Tagline { get; init; }
        public string? Description { get; init; }
        public string Category { get; init; } = "";
        public List<JsonElement?> Tags { get; init; } = new List<JsonElement?>();
        public string? WebsiteUrl { get; init; }
        public string? DocsUrl { get; init; }
        public string? SupportUrl { get; init; }
        public string? SetupGuide { get; init; }
        public string Protocol { get; init; } = "";
        public string? AuthorizationEndpoint { get; init; }
        public string? TokenEndpoint { get; init; }
        public string? UserinfoEndpoint { get; init; }
        public string? Issuer { get; init; }
        public List<string> DefaultScopes { get; init; } = new List<string>();
        public List<JsonElement?>? ApiScopes { get; init; }
        public List<JsonElement?>? CredentialFields { get; init; }
        public List<string> ShareModes { get; init; } = new List<string>();
        public string? SharedClientId { get; init; }
        public bool SharedRevoked { get; init; }
        public string? IdpIssuer { get; init; }
        public bool IdpAutoRegister { get; init; }
        public string Status { get; init; } = "";
        public bool Verified { get; init; }
        public bool Featured { get; init; }
        public long InstallCount { get; init; }
        public long ViewCount { get; init; }
        public double RatingAvg { get; init; }
        public long RatingCount { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
        /// <summary>Last updated (epoch ms).</summary>
        public long UpdatedAt { get; init; }
    }

    public sealed class ConnectorInstall
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Connector { get; init; } = "";
        public string ShareMode { get; init; } = "";
        public string? ProviderKey { get; init; }
        public bool? NeedsCredentials { get; init; }
    }

    public sealed class ConnectorReview
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Connector { get; init; } = "";
        public long Rating { get; init; }
    }

    /// <summary>A connector catalog entry, emitted verbatim from core with camelCase keys.</summary>
    public sealed class DirectoryEntry
    {
        public string Source { get; init; } = "";
        public string Key { get; init; } = "";
        public string Slug { get; init; } = "";
        [JsonPropertyName("displayName")] public string DisplayName { get; init; } = "";
        public string Category { get; init; } = "";
        public string? Tagline { get; init; }
        public string? Description { get; init; }
        [JsonPropertyName("logoUrl")] public string? LogoUrl { get; init; }
        [JsonPropertyName("websiteUrl")] public string? WebsiteUrl { get; init; }
        [JsonPropertyName("docsUrl")] public string? DocsUrl { get; init; }
        public bool? Verified { get; init; }
        public bool? Featured { get; init; }
        [JsonPropertyName("installCount")] public long? InstallCount { get; init; }
        [JsonPropertyName("ratingAvg")] public double? RatingAvg { get; init; }
        [JsonPropertyName("ratingCount")] public long? RatingCount { get; init; }
        [JsonPropertyName("rankScore")] public double? RankScore { get; init; }
        [JsonPropertyName("defaultScopes")] public List<string>? DefaultScopes { get; init; }
        [JsonPropertyName("apiScopes")] public List<JsonElement?>? ApiScopes { get; init; }
        [JsonPropertyName("credentialFields")] public List<JsonElement?>? CredentialFields { get; init; }
        [JsonPropertyName("shareModes")] public List<string>? ShareModes { get; init; }
    }

    /// <summary>Community connector profiles (<c>/v1/connectors</c>) and the connector directory.</summary>
    public sealed class ConnectorsResource : ResourceBase
    {
        public ConnectorsResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/connectors</c> — List the caller's own community connector profiles (draft + published), with adoption counts.</summary>
        public Task<ListPage<Connector>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<Connector>>(HttpVerb.Get, "/v1/connectors", cancellationToken: ct);

        /// <summary><c>POST /v1/connectors</c> — Create a community connector profile (SEO profile + OAuth config + share modes).</summary>
        public Task<Connector> CreateAsync(ConnectorBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Connector>(HttpVerb.Post, "/v1/connectors", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/connectors/{id}</c> — Fetch one of the caller's own connector profiles.</summary>
        public Task<Connector> GetAsync(string id, CancellationToken ct = default)
            => Req<Connector>(HttpVerb.Get, $"/v1/connectors/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/connectors/{id}</c> — Edit a connector profile — SEO fields, OAuth config, and share modes.</summary>
        public Task<Connector> UpdateAsync(string id, ConnectorBody body, CancellationToken ct = default)
            => Req<Connector>(HttpVerb.Patch, $"/v1/connectors/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/connectors/{id}</c> — Delete one of the caller's own connector profiles and cascade its installs, reviews, and reports.</summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/connectors/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/connectors/{id}/publish</c> — Publish a connector to the public directory.</summary>
        public Task<Connector> PublishAsync(string id, CancellationToken ct = default)
            => Req<Connector>(HttpVerb.Post, $"/v1/connectors/{Enc(id)}/publish", cancellationToken: ct);

        /// <summary><c>POST /v1/connectors/{id}/unlist</c> — Remove a published connector from the directory (back to unlisted) without deleting it.</summary>
        public Task<Connector> UnlistAsync(string id, CancellationToken ct = default)
            => Req<Connector>(HttpVerb.Post, $"/v1/connectors/{Enc(id)}/unlist", cancellationToken: ct);

        /// <summary><c>POST /v1/connectors/{id}/shared_credentials</c> — Set, rotate, or revoke the full_shared OAuth credentials.</summary>
        public Task<Connector> SetSharedCredentialsAsync(string id, ConnectorSharedCredentialsBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Connector>(HttpVerb.Post, $"/v1/connectors/{Enc(id)}/shared_credentials", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/connectors/{slug}/install</c> — Enable a published connector on the caller's instance (prefill, full_shared, or full_idp), snapshotting its OAuth config into the connection.</summary>
        public Task<ConnectorInstall> InstallAsync(string slug, InstallConnectorBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<ConnectorInstall>(HttpVerb.Post, $"/v1/connectors/{Enc(slug)}/install", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/connectors/{slug}/reviews</c> — Rate a connector the caller installed (1–5, optional text).</summary>
        public Task<ConnectorReview> ReviewAsync(string slug, double? rating, string? body = null, CancellationToken ct = default)
            => Req<ConnectorReview>(HttpVerb.Post, $"/v1/connectors/{Enc(slug)}/reviews", Body(("rating", rating), ("body", body)), cancellationToken: ct);

        /// <summary><c>GET /v1/directory</c> — Browse the unified connector directory — built-in providers plus published community connectors, ranked popular-first.</summary>
        public Task<DataList<DirectoryEntry>> DirectoryAsync(DirectoryParams? @params = null, CancellationToken ct = default)
            => Req<DataList<DirectoryEntry>>(HttpVerb.Get, "/v1/directory", query: (@params ?? new DirectoryParams()).ToQuery(), cancellationToken: ct);
    }
}
