using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Verification
{
    /// <summary>Configures RFC 7662 token introspection at Atlas (see <see cref="Introspector"/>).</summary>
    public sealed class IntrospectionOptions
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        /// <summary>Default <c>{Issuer}/oauth2/introspect</c>.</summary>
        public string? Endpoint { get; set; }
        /// <summary>How long an active answer is reused (default 60 s; never past the token's <c>exp</c>).</summary>
        public TimeSpan CacheTtl { get; set; } = TimeSpan.FromSeconds(60);
        /// <summary>How long an inactive answer is reused (default 10 s; zero disables).</summary>
        public TimeSpan NegativeCacheTtl { get; set; } = TimeSpan.FromSeconds(10);
        /// <summary>Most answers kept (default 10 000).</summary>
        public int CacheMax { get; set; } = 10_000;
        /// <summary>
        /// Also introspect JWT access tokens, so a revoked grant or a banned user is refused before the token expires
        /// (one cached call per token). Off by default: JWTs are verified locally.
        /// </summary>
        public bool IntrospectJwts { get; set; }
    }

    /// <summary>Atlas's introspection endpoint could not be reached, or answered with a non-2xx status.</summary>
    public sealed class IntrospectionException : Exception
    {
        public IntrospectionException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// RFC 7662 token introspection at Atlas, for opaque (reference) access tokens. An API, client or instance set to
    /// the <c>opaque</c> access-token format gets <c>oat_…</c> tokens only Atlas can read. The introspector asks
    /// Atlas with your API's own client credentials (<c>client_secret_basic</c>) and caches the answer by the token's
    /// SHA-256 (the token itself is never kept). The client must be a confidential client the API lists in
    /// <c>introspection_client_ids</c> (or the client the tokens are issued to).
    /// </summary>
    public sealed class Introspector
    {
        private readonly IntrospectionOptions _options;
        private readonly string _endpoint;
        private readonly string _basic;
        private readonly HttpClient _http;
        private readonly Func<long> _now;
        private readonly object _lock = new object();
        private readonly Dictionary<string, (JsonElement? Claims, long UntilMs)> _cache = new Dictionary<string, (JsonElement?, long)>();
        private readonly LinkedList<string> _order = new LinkedList<string>();

        public Introspector(string issuer, IntrospectionOptions options, HttpClient? httpClient = null, Func<long>? now = null)
        {
            if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret))
                throw new ArgumentException("Introspection needs a ClientId and ClientSecret.", nameof(options));
            _options = options;
            _endpoint = options.Endpoint ?? issuer.TrimEnd('/') + "/oauth2/introspect";
            var creds = Uri.EscapeDataString(options.ClientId) + ":" + Uri.EscapeDataString(options.ClientSecret);
            _basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(creds));
            _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _now = now ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        /// <summary>Whether JWT access tokens are introspected too.</summary>
        public bool IntrospectJwts => _options.IntrospectJwts;

        /// <summary>How many answers are cached.</summary>
        public int CacheSize { get { lock (_lock) return _cache.Count; } }

        internal static string CacheKey(string token)
        {
            using var sha = SHA256.Create();
            var d = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(d).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>The claims of an active access token, or null for an inactive one (also for a refresh token).</summary>
        public async Task<JsonElement?> IntrospectAsync(string token, CancellationToken ct = default)
        {
            var key = CacheKey(token);
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var hit))
                {
                    if (hit.UntilMs > _now()) return hit.Claims;
                    _cache.Remove(key);
                    _order.Remove(key);
                }
            }
            using var req = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", token),
                    new KeyValuePair<string, string>("token_type_hint", "access_token"),
                }),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", _basic);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage res;
            try
            {
                res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            }
            catch (Exception e) when (!(e is OperationCanceledException && ct.IsCancellationRequested))
            {
                throw new IntrospectionException("Introspection request failed.", e);
            }
            using (res)
            {
                var status = (int)res.StatusCode;
                if (status < 200 || status > 299) throw new IntrospectionException($"Introspection failed with HTTP {status}.");
                JsonElement body;
                try
                {
                    var text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(text);
                    body = doc.RootElement.Clone();
                }
                catch (Exception e)
                {
                    throw new IntrospectionException("Introspection answered with invalid JSON.", e);
                }
                JsonElement? claims = null;
                if (body.ValueKind == JsonValueKind.Object
                    && body.TryGetProperty("active", out var a) && a.ValueKind == JsonValueKind.True
                    && !(body.TryGetProperty("token_use", out var tu) && tu.ValueKind == JsonValueKind.String && tu.GetString() == "refresh_token"))
                {
                    claims = body;
                }
                var now = _now();
                long until = now + (long)(claims != null ? _options.CacheTtl : _options.NegativeCacheTtl).TotalMilliseconds;
                if (claims != null && claims.Value.TryGetProperty("exp", out var exp) && exp.ValueKind == JsonValueKind.Number)
                    until = Math.Min(until, (long)(exp.GetDouble() * 1000));
                if (until > now)
                {
                    lock (_lock)
                    {
                        if (_cache.ContainsKey(key)) _order.Remove(key);
                        _cache[key] = (claims, until);
                        _order.AddLast(key);
                        if (_cache.Count > _options.CacheMax)
                        {
                            var node = _order.First;
                            while (node != null)
                            {
                                var next = node.Next;
                                if (_cache[node.Value].UntilMs <= now) { _cache.Remove(node.Value); _order.Remove(node); }
                                node = next;
                            }
                            while (_cache.Count > _options.CacheMax && _order.First != null)
                            {
                                _cache.Remove(_order.First.Value);
                                _order.RemoveFirst();
                            }
                        }
                    }
                }
                return claims;
            }
        }
    }
}
