using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Tests
{
    /// <summary>
    /// A test double for <see cref="HttpMessageHandler"/>: it records every request
    /// and answers each with a response produced by a supplied responder function,
    /// so a test can assert on the method, path, headers, and body the SDK emitted
    /// without any network I/O.
    /// </summary>
    public sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, (HttpStatusCode Status, string Body)> _responder;

        /// <summary>Every request the SDK sent, in order.</summary>
        public List<RecordedRequest> Requests { get; } = new List<RecordedRequest>();

        /// <summary>Headers added to every response (e.g. <c>Atlas-Unknown-Fields</c>).</summary>
        public Dictionary<string, string> ResponseHeaders { get; } = new Dictionary<string, string>();

        public MockHttpMessageHandler(Func<HttpRequestMessage, string, (HttpStatusCode Status, string Body)> responder)
        {
            _responder = responder;
        }

        /// <summary>Convenience: always answer 200 with the given JSON.</summary>
        public static MockHttpMessageHandler Json(string body, HttpStatusCode status = HttpStatusCode.OK)
            => new MockHttpMessageHandler((_, __) => (status, body));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = "";
            byte[] bytes = Array.Empty<byte>();
            string? contentType = null;
            if (request.Content != null)
            {
                bytes = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                body = Encoding.UTF8.GetString(bytes);
                contentType = request.Content.Headers.ContentType?.ToString();
            }
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in request.Headers) headers[h.Key] = string.Join(",", h.Value);

            Requests.Add(new RecordedRequest
            {
                Method = request.Method.Method,
                Url = request.RequestUri!,
                Body = body,
                Authorization = request.Headers.TryGetValues("Authorization", out var auth)
                    ? string.Join(",", auth) : null,
                IdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var idem)
                    ? string.Join(",", idem) : null,
                Headers = headers,
                BodyBytes = bytes,
                ContentType = contentType,
            });

            var (status, responseBody) = _responder(request, body);
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
            foreach (var h in ResponseHeaders) response.Headers.TryAddWithoutValidation(h.Key, h.Value);
            return response;
        }
    }

    public sealed class RecordedRequest
    {
        public string Method { get; set; } = "";
        public Uri Url { get; set; } = null!;
        public string Body { get; set; } = "";
        public string? Authorization { get; set; }
        public string? IdempotencyKey { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public byte[] BodyBytes { get; set; } = Array.Empty<byte>();
        public string? ContentType { get; set; }

        public string? Header(string name) => Headers.TryGetValue(name, out var v) ? v : null;

        public string Path => Url.AbsolutePath;
        public string Query => Url.Query;
    }
}
