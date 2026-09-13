using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>
    /// A per-locale override the render paths resolve at send/render time. Always
    /// an OVERRIDE: the base renders whenever no row matches. Timestamps are ISO
    /// strings, not epoch numbers.
    /// </summary>
    public sealed class Localization
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "localization";
        public string Id { get; init; } = "";
        public string ResourceType { get; init; } = "";
        public string ResourceName { get; init; } = "";
        public string Locale { get; init; } = "";
        public Dictionary<string, object?> Content { get; init; } = new Dictionary<string, object?>();
        public bool Enabled { get; init; }
        public string CreatedAt { get; init; } = "";
        public string UpdatedAt { get; init; } = "";
    }

    public sealed class ListLocalizationsParams
    {
        public string? ResourceType { get; set; }
        public string? ResourceName { get; set; }
        public string? Locale { get; set; }

        public IEnumerable<KeyValuePair<string, object?>> ToQuery()
        {
            yield return new KeyValuePair<string, object?>("resource_type", ResourceType);
            yield return new KeyValuePair<string, object?>("resource_name", ResourceName);
            yield return new KeyValuePair<string, object?>("locale", Locale);
        }
    }

    public sealed class CreateLocalizationBody
    {
        public string ResourceType { get; set; } = "";
        public string ResourceName { get; set; } = "";
        public string Locale { get; set; } = "";
        public Dictionary<string, object?> Content { get; set; } = new Dictionary<string, object?>();
        public bool? Enabled { get; set; }
    }

    public sealed class UpdateLocalizationBody
    {
        public Dictionary<string, object?>? Content { get; set; }
        public bool? Enabled { get; set; }
    }

    /// <summary>The localizations namespace (<c>/v1/localizations</c>).</summary>
    public sealed class LocalizationsResource : ResourceBase
    {
        public LocalizationsResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<Localization>> ListAsync(ListLocalizationsParams? @params = null, CancellationToken ct = default)
            => Req<ListPage<Localization>>(HttpVerb.Get, "/v1/localizations", query: (@params ?? new ListLocalizationsParams()).ToQuery(), cancellationToken: ct);

        public Task<Localization> CreateAsync(CreateLocalizationBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Localization>(HttpVerb.Post, "/v1/localizations", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<Localization> GetAsync(string id, CancellationToken ct = default)
            => Req<Localization>(HttpVerb.Get, $"/v1/localizations/{Enc(id)}", cancellationToken: ct);

        public Task<Localization> UpdateAsync(string id, UpdateLocalizationBody body, CancellationToken ct = default)
            => Req<Localization>(HttpVerb.Patch, $"/v1/localizations/{Enc(id)}", body, cancellationToken: ct);

        public Task DeleteAsync(string id, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/localizations/{Enc(id)}", cancellationToken: ct);
    }
}
