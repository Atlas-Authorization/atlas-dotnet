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
            if (request.Content != null)
            {
                body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            Requests.Add(new RecordedRequest
            {
                Method = request.Method.Method,
                Url = request.RequestUri!,
                Body = body,
                Authorization = request.Headers.TryGetValues("Authorization", out var auth)
                    ? string.Join(",", auth) : null,
                IdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var idem)
                    ? string.Join(",", idem) : null,
            });

            var (status, responseBody) = _responder(request, body);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    public sealed class RecordedRequest
    {
        public string Method { get; set; } = "";
        public Uri Url { get; set; } = null!;
        public string Body { get; set; } = "";
        public string? Authorization { get; set; }
        public string? IdempotencyKey { get; set; }

        public string Path => Url.AbsolutePath;
        public string Query => Url.Query;
    }
}
