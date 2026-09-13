using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas
{
    /// <summary>The HTTP verb of a BAPI request.</summary>
    public enum HttpVerb { Get, Post, Patch, Put, Delete }

    /// <summary>A single BAPI request, already interpolated (no <c>:params</c> in the path).</summary>
    public sealed class RequestOptions
    {
        public HttpVerb Method { get; set; }

        /// <summary>Path beginning with <c>/v1/...</c>.</summary>
        public string Path { get; set; } = "";

        /// <summary>Query key/value pairs; null values and array items are dropped/spread.</summary>
        public IEnumerable<KeyValuePair<string, object?>>? Query { get; set; }

        /// <summary>JSON body; omitted entirely when null.</summary>
        public object? Body { get; set; }

        /// <summary>Idempotency-Key header (§9.1), when a caller supplies one.</summary>
        public string? IdempotencyKey { get; set; }

        /// <summary>When true, the raw text body is returned instead of parsed JSON (SAML XML).</summary>
        public bool Raw { get; set; }
    }

    /// <summary>
    /// The shared HTTP core every resource namespace calls. One place decides how
    /// a BAPI request is authenticated, serialized, and how a failure becomes an
    /// <see cref="AtlasException"/> — so a namespace method is a one-liner naming
    /// a method, a path, and its shapes.
    ///
    /// The secret key is only ever sent as <c>Authorization: Bearer &lt;key&gt;</c>;
    /// it is never logged and never placed in a URL.
    /// </summary>
    public sealed class AtlasTransport
    {
        private readonly string _secretKey;
        private readonly string _baseUrl;
        private readonly HttpClient _http;

        public AtlasTransport(string secretKey, string baseUrl, HttpClient http)
        {
            _secretKey = secretKey;
            _baseUrl = baseUrl.TrimEnd('/');
            _http = http;
        }

        public async Task<T?> SendAsync<T>(RequestOptions options, CancellationToken cancellationToken = default)
        {
            var url = _baseUrl + (options.Path.StartsWith("/") ? options.Path : "/" + options.Path)
                      + QuerySerializer.Serialize(options.Query);

            using var request = new HttpRequestMessage(ToHttpMethod(options.Method), url);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _secretKey);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            if (!string.IsNullOrEmpty(options.IdempotencyKey))
            {
                request.Headers.TryAddWithoutValidation("Idempotency-Key", options.IdempotencyKey);
            }
            if (options.Body != null)
            {
                var json = AtlasJson.Serialize(options.Body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

            var text =
#if NET8_0_OR_GREATER
                await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

            if (!response.IsSuccessStatusCode)
            {
                throw ToException((int)response.StatusCode, text);
            }

            // 204 and other empty bodies: nothing to parse.
            if ((int)response.StatusCode == 204 || string.IsNullOrEmpty(text))
            {
                return default;
            }

            if (options.Raw && typeof(T) == typeof(string))
            {
                return (T)(object)text;
            }

            return AtlasJson.Deserialize<T>(text);
        }

        private static AtlasException ToException(int status, string text)
        {
            List<AtlasErrorItem>? errors = null;
            string? message = null;

            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    var envelope = AtlasJson.Deserialize<AtlasErrorEnvelope>(text);
                    if (envelope?.Errors != null && envelope.Errors.Count > 0)
                    {
                        errors = envelope.Errors;
                    }
                    else
                    {
                        message = Truncate(text);
                    }
                }
                catch
                {
                    // Non-JSON error body (e.g. an upstream proxy): keep it as the message.
                    message = Truncate(text);
                }
            }

            if (errors == null || errors.Count == 0)
            {
                errors = new List<AtlasErrorItem>
                {
                    new AtlasErrorItem
                    {
                        Code = "UNKNOWN",
                        Message = message ?? $"Atlas API request failed with status {status}",
                    },
                };
            }

            return AtlasException.FromStatus(status, errors);
        }

        private static string Truncate(string s) => s.Length <= 500 ? s : s.Substring(0, 500);

        private static HttpMethod ToHttpMethod(HttpVerb verb)
        {
            switch (verb)
            {
                case HttpVerb.Get: return HttpMethod.Get;
                case HttpVerb.Post: return HttpMethod.Post;
                case HttpVerb.Put: return HttpMethod.Put;
                case HttpVerb.Delete: return HttpMethod.Delete;
                case HttpVerb.Patch:
#if NET8_0_OR_GREATER
                    return HttpMethod.Patch;
#else
                    return new HttpMethod("PATCH");
#endif
                default: return HttpMethod.Get;
            }
        }
    }

    /// <summary>Serialize a query enumerable to a string, dropping null values and spreading arrays.</summary>
    internal static class QuerySerializer
    {
        public static string Serialize(IEnumerable<KeyValuePair<string, object?>>? query)
        {
            if (query == null) return "";
            var parts = new List<string>();
            foreach (var pair in query)
            {
                if (pair.Value == null) continue;
                if (pair.Value is string s)
                {
                    parts.Add(Encode(pair.Key, s));
                }
                else if (pair.Value is IEnumerable enumerable && !(pair.Value is string))
                {
                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;
                        parts.Add(Encode(pair.Key, Stringify(item)));
                    }
                }
                else
                {
                    parts.Add(Encode(pair.Key, Stringify(pair.Value)));
                }
            }
            return parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        }

        private static string Stringify(object value)
        {
            if (value is bool b) return b ? "true" : "false";
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        }

        private static string Encode(string key, string value) =>
            Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
    }
}
