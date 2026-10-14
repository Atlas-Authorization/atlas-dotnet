using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class AuditEventActor
    {
        public string? Type { get; set; }
        public string? Id { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public AuditEventActor Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class ConfigChangelogEntry
    {
        public string Id { get; init; } = "";
        /// <summary>Occurred (epoch ms).</summary>
        public long At { get; init; }
        public AuditEventActor? Actor { get; init; }
        public string Action { get; init; } = "";
        public string? Ip { get; init; }
        public List<ConfigChangelogEntryChanges>? Changes { get; init; }
        public List<string>? Fields { get; init; }
    }

    public sealed class ConfigChangelogEntryChanges
    {
        public string? Section { get; init; }
        public JsonElement? Before { get; init; }
        public JsonElement? After { get; init; }
    }

    /// <summary>Config change history (<c>/v1/config_changelog</c>).</summary>
    public sealed class ConfigChangelogResource : ResourceBase
    {
        public ConfigChangelogResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/config_changelog</c> — Config change history for this instance — who changed which config, when, with the per-section before/after diff where the source recorded one (else the changed field names).</summary>
        public Task<CursorPage<ConfigChangelogEntry>> ListAsync(ListConfigChangelogParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<ConfigChangelogEntry>>(HttpVerb.Get, "/v1/config_changelog", query: (@params ?? new ListConfigChangelogParams()).ToQuery(), cancellationToken: ct);
    }
}
