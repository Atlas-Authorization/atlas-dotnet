using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Frameworks.Tests
{
    internal sealed class FuncHandler : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = new List<(HttpRequestMessage, string)>();
        private readonly Func<HttpRequestMessage, string, (HttpStatusCode, string)> _respond;

        public FuncHandler(Func<HttpRequestMessage, string, (HttpStatusCode, string)> respond) => _respond = respond;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            Calls.Add((request, body));
            var (status, json) = _respond(request, body);
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    internal static class Jwt
    {
        public static string Make(Dictionary<string, object?> payload, string alg = "RS256")
        {
            string B64(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{B64(JsonSerializer.Serialize(new Dictionary<string, object> { ["alg"] = alg, ["typ"] = "JWT" }))}.{B64(JsonSerializer.Serialize(payload))}.c2ln";
        }
    }
}
