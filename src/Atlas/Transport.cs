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

        /// <summary>When true, the raw text body is returned instead of parsed JSON (SAML XML, exports).</summary>
        public bool Raw { get; set; }

        /// <summary>Raw bytes to send instead of a JSON body (an image upload). Requires <see cref="ContentType"/>.</summary>
        public byte[]? BinaryBody { get; set; }

        /// <summary>The <c>Content-Type</c> of <see cref="BinaryBody"/>, e.g. <c>image/png</c>.</summary>
        public string? ContentType { get; set; }

        /// <summary>Extra request headers (e.g. the end user's forwarded IP). <c>Authorization</c> is never replaced.</summary>
        public IReadOnlyDictionary<string, string>? Headers { get; set; }
    }

    /// <summary>
    /// Reported when a write succeeded but the server ignored request-body fields it
    /// does not accept (the <c>Atlas-Unknown-Fields</c> response header). Nested
    /// fields are named <c>parent.child</c>. Turn on
    /// <see cref="AtlasClientOptions.Strict"/> to get a <c>422 UNKNOWN_FIELD</c> instead.
    /// </summary>
    public sealed class UnknownFieldsWarning
    {
        public UnknownFieldsWarning(string method, string path, IReadOnlyList<string> fields)
        {
            Method = method;
            Path = path;
            Fields = fields;
        }

        /// <summary>The HTTP method of the request.</summary>
        public string Method { get; }

        /// <summary>The request path (no query string).</summary>
        public string Path { get; }

        /// <summary>The ignored field names, as the server listed them.</summary>
        public IReadOnlyList<string> Fields { get; }
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
        /// <summary>The request header that turns unknown body fields into a <c>422 UNKNOWN_FIELD</c>.</summary>
        public const string StrictHeader = "Atlas-Strict";

        /// <summary>The response header listing body fields the server ignored.</summary>
        public const string UnknownFieldsHeader = "Atlas-Unknown-Fields";

        private readonly string _secretKey;
        private readonly string _baseUrl;
        private readonly HttpClient _http;
        private readonly bool? _strict;
        private readonly Action<UnknownFieldsWarning>? _onUnknownFields;
        private readonly string? _missingKeyMessage;

        public AtlasTransport(string secretKey, string baseUrl, HttpClient http)
            : this(secretKey, baseUrl, http, null, null, null) { }

        /// <param name="secretKey">The bearer key (<c>sk_</c> or <c>psk_</c>).</param>
        /// <param name="baseUrl">The API origin.</param>
        /// <param name="http">The shared <see cref="HttpClient"/>.</param>
        /// <param name="strict"><c>true</c>/<c>false</c> sends <c>Atlas-Strict</c> on every write; <c>null</c> sends nothing.</param>
        /// <param name="onUnknownFields">Called when a response carries <c>Atlas-Unknown-Fields</c>.</param>
        /// <param name="missingKeyMessage">The error raised when <paramref name="secretKey"/> is empty.</param>
        public AtlasTransport(string secretKey, string baseUrl, HttpClient http, bool? strict,
            Action<UnknownFieldsWarning>? onUnknownFields, string? missingKeyMessage)
        {
            _secretKey = secretKey;
            _baseUrl = baseUrl.TrimEnd('/');
            _http = http;
            _strict = strict;
            _onUnknownFields = onUnknownFields;
            _missingKeyMessage = missingKeyMessage;
        }

        /// <summary>The strict-body setting this transport sends (<c>null</c> = header not sent).</summary>
        public bool? Strict => _strict;

        /// <summary>A copy of this transport that sends <c>Atlas-Strict: &lt;strict&gt;</c> on writes (<c>null</c> = never).</summary>
        public AtlasTransport WithStrict(bool? strict) =>
            new AtlasTransport(_secretKey, _baseUrl, _http, strict, _onUnknownFields, _missingKeyMessage);

        public async Task<T?> SendAsync<T>(RequestOptions options, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new InvalidOperationException(_missingKeyMessage ?? "No API key is configured for this call.");
            }

            var url = _baseUrl + (options.Path.StartsWith("/") ? options.Path : "/" + options.Path)
                      + QuerySerializer.Serialize(options.Query);

            using var request = new HttpRequestMessage(ToHttpMethod(options.Method), url);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _secretKey);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            if (!string.IsNullOrEmpty(options.IdempotencyKey))
            {
                request.Headers.TryAddWithoutValidation("Idempotency-Key", options.IdempotencyKey);
            }
            if (options.Headers != null)
            {
                foreach (var h in options.Headers)
                {
                    if (string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)) continue;
                    request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }
            if (_strict.HasValue && options.Method != HttpVerb.Get)
            {
                request.Headers.TryAddWithoutValidation(StrictHeader, _strict.Value ? "true" : "false");
            }
            if (options.BinaryBody != null)
            {
                if (string.IsNullOrEmpty(options.ContentType))
                    throw new ArgumentException("ContentType is required for a binary body (e.g. image/png).");
                var content = new ByteArrayContent(options.BinaryBody);
                content.Headers.TryAddWithoutValidation("Content-Type", options.ContentType);
                request.Content = content;
            }
            else if (options.Body != null)
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

            if (response.Headers.TryGetValues(UnknownFieldsHeader, out var unknownValues))
            {
                ReportUnknown(options, string.Join(",", unknownValues));
            }

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

        private void ReportUnknown(RequestOptions options, string header)
        {
            var fields = new List<string>();
            foreach (var f in header.Split(','))
            {
                var trimmed = f.Trim();
                if (trimmed.Length > 0) fields.Add(trimmed);
            }
            if (fields.Count == 0) return;
            var warning = new UnknownFieldsWarning(options.Method.ToString().ToUpperInvariant(), options.Path, fields);
            if (_onUnknownFields != null)
            {
                _onUnknownFields(warning);
            }
            else
            {
                System.Diagnostics.Trace.TraceWarning(
                    "Atlas ignored unknown body field(s) on {0} {1}: {2}", warning.Method, warning.Path, string.Join(", ", fields));
            }
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
