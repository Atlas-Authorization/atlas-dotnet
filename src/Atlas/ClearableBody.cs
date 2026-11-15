using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Atlas
{
    /// <summary>
    /// Base of every generated request record. A property left unset is omitted from the body (the default); to send an explicit
    /// <c>null</c>, which the API reads as "clear this field", call the record's <c>Clear&lt;Property&gt;()</c> method:
    /// <code>
    /// var body = new UpdateThingBody { Name = "x" }.ClearDescription();   // {"name":"x","description":null}
    /// </code>
    /// Works at any depth (nested records and lists of records).
    /// </summary>
    public abstract record ClearableBody
    {
        /// <summary>Wire names of the fields to send as <c>null</c>.</summary>
        [JsonIgnore] public IReadOnlyList<string> Cleared { get; init; } = Array.Empty<string>();

        /// <summary>The cleared list with <paramref name="wireName"/> added (for the generated <c>Clear...</c> methods).</summary>
        protected IReadOnlyList<string> Plus(string wireName) => Cleared.Contains(wireName) ? Cleared : Cleared.Concat(new[] { wireName }).ToList();

        internal static bool AnyCleared(object? value, int depth = 0)
        {
            if (value == null || depth > 16) return false;
            if (value is ClearableBody cb && cb.Cleared.Count > 0) return true;
            if (value is string) return false;
            if (value is ClearableBody)
                return value.GetType().GetProperties().Any(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null && p.GetIndexParameters().Length == 0 && AnyCleared(p.GetValue(value), depth + 1));
            if (value is IEnumerable e && !(value is IDictionary))
                foreach (var item in e) if (item is ClearableBody && AnyCleared(item, depth + 1)) return true;
            return false;
        }

        /// <summary>Serialize <paramref name="value"/> with every cleared field written as <c>null</c>.</summary>
        internal static JsonNode? ToNode(object? value, int depth = 0)
        {
            if (value == null) return null;
            var node = JsonSerializer.SerializeToNode(value, value.GetType(), AtlasJson.Options);
            if (!(value is ClearableBody cb) || !(node is JsonObject obj) || depth > 16) return node;
            foreach (var p in value.GetType().GetProperties())
            {
                if (p.GetCustomAttribute<JsonIgnoreAttribute>() != null || p.GetIndexParameters().Length > 0) continue;
                var child = p.GetValue(value);
                if (child == null) continue;
                var wire = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? SnakeCaseNamingPolicy.Instance.ConvertName(p.Name);
                if (!obj.ContainsKey(wire)) continue;
                if (child is ClearableBody) obj[wire] = ToNode(child, depth + 1);
                else if (child is IList list && !(child is string) && obj[wire] is JsonArray arr && arr.Count == list.Count)
                    for (var i = 0; i < list.Count; i++) if (list[i] is ClearableBody) arr[i] = ToNode(list[i], depth + 1);
            }
            foreach (var wire in cb.Cleared) obj[wire] = null;
            return node;
        }
    }
}
