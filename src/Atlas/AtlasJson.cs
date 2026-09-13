using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atlas
{
    /// <summary>
    /// The single <see cref="JsonSerializerOptions"/> instance the whole SDK
    /// serializes and deserializes with.
    ///
    /// The BAPI wire is snake_case JSON, so the default property-naming policy
    /// converts C# PascalCase members to snake_case (<c>FirstName</c> →
    /// <c>first_name</c>). A handful of fields are camelCase on the wire
    /// (branding, <c>roleKey</c>, the org policy patch, risk weights); those
    /// carry an explicit <see cref="JsonPropertyNameAttribute"/> that overrides
    /// the policy. Null members are dropped on write so an optional body field
    /// left unset is omitted, matching the TypeScript SDK's <c>undefined</c>
    /// behaviour. Dictionary keys (metadata bags) are written verbatim.
    /// </summary>
    public static class AtlasJson
    {
        public static readonly JsonSerializerOptions Options = Build();

        private static JsonSerializerOptions Build()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                // Dictionary keys are arbitrary metadata and must survive intact.
                DictionaryKeyPolicy = null,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        public static string Serialize(object value) => JsonSerializer.Serialize(value, Options);

        public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
    }

    /// <summary>
    /// Converts a PascalCase / camelCase .NET member name to lower_snake_case.
    /// (.NET 8 ships <c>JsonNamingPolicy.SnakeCaseLower</c>, but netstandard2.1
    /// does not, so the SDK carries its own for a single behaviour on every
    /// target framework.)
    /// </summary>
    public sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public static readonly SnakeCaseNamingPolicy Instance = new SnakeCaseNamingPolicy();

        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var sb = new StringBuilder(name.Length + 8);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    bool prevIsLowerOrDigit = i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));
                    bool nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                    bool prevIsUpper = i > 0 && char.IsUpper(name[i - 1]);
                    // Insert a break before an uppercase that starts a new word:
                    // after a lowercase/digit, or when it begins a word inside an
                    // acronym run (e.g. "OAuthClient" -> "o_auth_client").
                    if (i > 0 && (prevIsLowerOrDigit || (prevIsUpper && nextIsLower)))
                    {
                        sb.Append('_');
                    }
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
