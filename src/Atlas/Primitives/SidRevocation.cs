using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Verification;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.Primitives
{
    public enum SidRevocationSource { Feed, StatusList }

    /// <summary>What to do when the list cannot be refreshed for longer than <see cref="SidRevocationOptions.StaleAfter"/>.</summary>
    public enum SidRevocationOnStale { Allow, Reject }

    public sealed class SidRevocationOptions
    {
        /// <summary>A secret key with <c>sessions:read</c>. Server-side only.</summary>
        public string SecretKey { get; set; } = "";
        public string ApiUrl { get; set; } = AtlasClientOptions.DefaultApiUrl;
        /// <summary>How often to poll. Default 15 s. This plus the token lifetime is the worst-case revocation window.</summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(15);
        /// <summary>Your longest-lived session token (Atlas's default is 60 s). A listed sid is remembered this long. Default 1 hour.</summary>
        public TimeSpan MaxTokenLifetime { get; set; } = TimeSpan.FromHours(1);
        /// <summary><c>Allow</c> (default) keeps trusting the last list through an Atlas outage; <c>Reject</c> fails closed.</summary>
        public SidRevocationOnStale OnStale { get; set; } = SidRevocationOnStale.Allow;
        public TimeSpan StaleAfter { get; set; } = TimeSpan.FromMinutes(2);
        /// <summary>With <c>Reject</c>: keep answering from the last list this much longer past <see cref="StaleAfter"/> before failing closed. Default none.</summary>
        public TimeSpan ServeStaleFor { get; set; } = TimeSpan.Zero;
        public Action<TimeSpan>? OnServeStale { get; set; }
        /// <summary><c>Feed</c> (default) pages <c>GET /v1/sessions/revoked</c>; <c>StatusList</c> fetches the signed list at <c>GET /v1/sessions/revocations/status-list</c>, the smaller answer for a busy verifier.</summary>
        public SidRevocationSource Source { get; set; } = SidRevocationSource.Feed;
        /// <summary><c>StatusList</c> source: the instance JWKS URL the signature is checked against.</summary>
        public string? JwksUrl { get; set; }
        /// <summary><c>StatusList</c> source: the issuer the signed list must carry.</summary>
        public string? Issuer { get; set; }
        public Action<Exception>? OnError { get; set; }
        public HttpClient? HttpClient { get; set; }
        /// <summary>Epoch ms, for tests.</summary>
        public Func<long>? Now { get; set; }
    }

    /// <summary>The verdict of a status-list lookup, or why none could be given.</summary>
    public sealed class StatusListException : Exception
    {
        public StatusListException(string reason) : base("Invalid session status list: " + reason) => Reason = reason;
        /// <summary><c>malformed</c>, <c>unsupported</c>, <c>no_key</c>, <c>bad_signature</c>, <c>bad_claims</c> or <c>expired</c>.</summary>
        public string Reason { get; }
    }

    /// <summary>A verified signed session status list (<c>GET /v1/sessions/revocations/status-list</c>).</summary>
    public sealed class VerifiedStatusList
    {
        private readonly byte[] _list;
        private readonly int _hashBytes;

        internal VerifiedStatusList(byte[] list, int hashBytes, int count, bool truncated, long issuedAt, long expiresAt, long since)
        { _list = list; _hashBytes = hashBytes; Count = count; Truncated = truncated; IssuedAt = issuedAt; ExpiresAt = expiresAt; Since = since; }

        public int Count { get; }
        /// <summary>More sessions were revoked than fit: a miss proves nothing, use the feed.</summary>
        public bool Truncated { get; }
        public long IssuedAt { get; }
        public long ExpiresAt { get; }
        public long Since { get; }

        /// <summary>True when the session id's hash is in the list.</summary>
        public bool Has(string sid)
        {
            using var sha = SHA256.Create();
            var needle = sha.ComputeHash(Encoding.UTF8.GetBytes(sid)).Take(_hashBytes).ToArray();
            int lo = 0, hi = Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                var c = 0;
                for (var i = 0; i < _hashBytes && c == 0; i++) c = _list[mid * _hashBytes + i] - needle[i];
                if (c == 0) return true;
                if (c < 0) lo = mid + 1; else hi = mid - 1;
            }
            return false;
        }

        /// <summary>Verify the signed list against <paramref name="jwksJson"/> and return a lookup. Throws <see cref="StatusListException"/>; never returns a half-trusted list.</summary>
        public static async Task<VerifiedStatusList> VerifyAsync(string token, string jwksJson, string? issuer, long nowMs, int skewSeconds = 30)
        {
            var parts = token.Split('.');
            if (parts.Length != 3 || token.Length > 4_000_000) throw new StatusListException("malformed");
            JsonElement Obj(string seg)
            {
                try { using var d = JsonDocument.Parse(JwksCache.Base64UrlDecode(seg)); if (d.RootElement.ValueKind == JsonValueKind.Object) return d.RootElement.Clone(); }
                catch (Exception e) when (e is JsonException || e is FormatException) { }
                throw new StatusListException("malformed");
            }
            var header = Obj(parts[0]);
            if (!header.TryGetProperty("typ", out var typ) || typ.GetString() != "statuslist+jwt") throw new StatusListException("unsupported");
            var alg = header.TryGetProperty("alg", out var a) ? a.GetString() : null;
            if (alg == null || !SigningAlgs.All.Contains(alg)) throw new StatusListException("unsupported");
            if (new JsonWebKeySet(jwksJson).Keys.Count == 0) throw new StatusListException("no_key");
            var result = await Jws.ValidateAsync(token, jwksJson, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = false, RequireSignedTokens = true,
            }).ConfigureAwait(false);
            if (!result.IsValid) throw new StatusListException("bad_signature");

            var c = Obj(parts[1]);
            var nowSec = nowMs / 1000.0;
            double Num(string n)
            {
                if (!c.TryGetProperty(n, out var v) || v.ValueKind != JsonValueKind.Number) throw new StatusListException("bad_claims");
                return v.GetDouble();
            }
            string? S(string n) => c.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            if (S("aud") != "atlas:session-status-list" || S("token_use") != "session_status_list") throw new StatusListException("bad_claims");
            if (issuer != null && S("iss") != issuer) throw new StatusListException("bad_claims");
            var exp = Num("exp"); var iat = Num("iat");
            if (exp + skewSeconds < nowSec) throw new StatusListException("expired");
            if (iat - skewSeconds > nowSec) throw new StatusListException("bad_claims");
            var hashBytes = (int)Num("hash_bytes");
            if (hashBytes < 4 || hashBytes > 32) throw new StatusListException("bad_claims");
            var count = (int)Num("count");
            var h = S("h");
            byte[]? list = null;
            try { list = h == null ? null : JwksCache.Base64UrlDecode(h); } catch (FormatException) { }
            if (list == null || list.Length != count * hashBytes) throw new StatusListException("bad_claims");
            return new VerifiedStatusList(list, hashBytes, count, c.TryGetProperty("truncated", out var t) && t.ValueKind == JsonValueKind.True,
                (long)iat * 1000, (long)exp * 1000, (long)Num("since"));
        }
    }

    /// <summary>
    /// One poller: an in-memory set of revoked session ids, each kept until its token cannot be live any more. Use it next to
    /// local session-token verification: <c>if (await list.IsRevokedAsync(claims.Sid)) reject</c>, and with
    /// <c>OnStale = Reject</c> also <c>list.Unavailable()</c>.
    /// </summary>
    public sealed class SidRevocationList
    {
        private readonly SidRevocationOptions _o;
        private readonly HttpClient _http;
        private readonly Dictionary<string, long> _revoked = new Dictionary<string, long>();
        private readonly SemaphoreSlim _poll = new SemaphoreSlim(1, 1);
        private readonly long _createdAt;
        private long? _since;
        private long _nextPollAt;
        private long? _lastSuccessAt;
        private VerifiedStatusList? _status;
        private long _statusAt;
        private string? _jwks;
        private long _jwksAt;

        public SidRevocationList(SidRevocationOptions options)
        {
            _o = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.SecretKey)) throw new ArgumentException("SecretKey (sessions:read) is required.", nameof(options));
            if (options.Source == SidRevocationSource.StatusList && string.IsNullOrEmpty(options.JwksUrl))
                throw new ArgumentException("Source StatusList needs JwksUrl.", nameof(options));
            _http = options.HttpClient ?? new HttpClient();
            _createdAt = Now();
        }

        private long Now() => _o.Now != null ? _o.Now() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>True when the list could not be refreshed for <c>StaleAfter</c> and <c>OnStale</c> is <c>Reject</c>. Call after <see cref="IsRevokedAsync"/>.</summary>
        public bool Unavailable()
        {
            if (_o.OnStale != SidRevocationOnStale.Reject) return false;
            var stale = Now() - (_lastSuccessAt ?? _createdAt) - (long)_o.StaleAfter.TotalMilliseconds;
            if (stale <= 0) return false;
            // A list that was fetched at least once may be served a little longer; one never fetched may not.
            if (_lastSuccessAt != null && stale <= (long)_o.ServeStaleFor.TotalMilliseconds)
            {
                _o.OnServeStale?.Invoke(TimeSpan.FromMilliseconds(stale));
                return false;
            }
            return true;
        }

        public async Task<bool> IsRevokedAsync(string sid, CancellationToken ct = default)
        {
            if (_o.Source == SidRevocationSource.StatusList)
            {
                var verdict = await StatusVerdictAsync(sid, ct).ConfigureAwait(false);
                if (verdict != null) return verdict.Value;
            }
            var now = Now();
            if (now >= _nextPollAt) await PollAsync(ct).ConfigureAwait(false);
            lock (_revoked) return _revoked.TryGetValue(sid, out var expiry) && expiry > now;
        }

        /// <summary>The signed list answers; null means "ask the feed" (never fetched, or it was cut short).</summary>
        private async Task<bool?> StatusVerdictAsync(string sid, CancellationToken ct)
        {
            var now = Now();
            if (_status == null || now - _statusAt >= (long)_o.Interval.TotalMilliseconds)
            {
                try
                {
                    if (_jwks == null || now - _jwksAt > 600_000) { _jwks = await GetAsync(_o.JwksUrl!, false, ct).ConfigureAwait(false); _jwksAt = now; }
                    var window = Math.Min(86_400, Math.Max(60, (int)Math.Ceiling(_o.MaxTokenLifetime.TotalSeconds)));
                    var body = await GetAsync(_o.ApiUrl.TrimEnd('/') + "/v1/sessions/revocations/status-list?window_seconds=" + window, true, ct).ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(body);
                    var token = doc.RootElement.TryGetProperty("token", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
                    if (token == null) throw new InvalidOperationException("status list answer had no token");
                    _status = await VerifiedStatusList.VerifyAsync(token, _jwks, _o.Issuer, now).ConfigureAwait(false);
                    _statusAt = now;
                    _lastSuccessAt = now;
                }
                catch (Exception e) when (!ct.IsCancellationRequested) { _o.OnError?.Invoke(e); }
            }
            if (_status == null) return null;
            if (_status.Has(sid)) return true;
            return _status.Truncated ? (bool?)null : false;
        }

        private async Task<string> GetAsync(string url, bool auth, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (auth) request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _o.SecretKey);
            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"GET {new Uri(url).AbsolutePath} answered {(int)response.StatusCode}");
#if NET8_0_OR_GREATER
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }

        private async Task PollAsync(CancellationToken ct)
        {
            await _poll.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (Now() < _nextPollAt) return; // another caller just polled
                var started = Now();
                _nextPollAt = started + (long)_o.Interval.TotalMilliseconds;
                var lifetimeMs = (long)_o.MaxTokenLifetime.TotalMilliseconds;
                try
                {
                    string? cursor = null;
                    var serverTime = started;
                    do
                    {
                        var url = _o.ApiUrl.TrimEnd('/') + "/v1/sessions/revoked?since=" + (_since ?? started - lifetimeMs) + "&limit=5000"
                            + (cursor != null ? "&cursor=" + Uri.EscapeDataString(cursor) : "");
                        using var doc = JsonDocument.Parse(await GetAsync(url, true, ct).ConfigureAwait(false));
                        var root = doc.RootElement;
                        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                            lock (_revoked)
                                foreach (var r in data.EnumerateArray())
                                    _revoked[r.GetProperty("sid").GetString()!] = r.GetProperty("revoked_at").GetInt64() + lifetimeMs;
                        if (root.TryGetProperty("server_time", out var st) && st.ValueKind == JsonValueKind.Number) serverTime = st.GetInt64();
                        cursor = root.TryGetProperty("has_more", out var hm) && hm.ValueKind == JsonValueKind.True
                            && root.TryGetProperty("next_cursor", out var nc) && nc.ValueKind == JsonValueKind.String ? nc.GetString() : null;
                    } while (cursor != null);
                    // Re-ask from a little before the answer's own clock, so a row committed late is not missed.
                    _since = serverTime - 10_000;
                    _lastSuccessAt = started;
                    lock (_revoked) foreach (var sid in _revoked.Where(kv => kv.Value <= started).Select(kv => kv.Key).ToList()) _revoked.Remove(sid);
                }
                catch (Exception e) when (!ct.IsCancellationRequested) { _o.OnError?.Invoke(e); } // a failed poll never rejects or accepts more than before
            }
            finally { _poll.Release(); }
        }
    }
}
