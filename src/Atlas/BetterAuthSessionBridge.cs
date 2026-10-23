using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Resources;

namespace Atlas
{
    // Moving from Better Auth to Atlas without signing anyone out.
    //
    // During the migration window a returning user still carries their Better Auth session cookie
    // (better-auth.session_token, __Secure- prefixed over https). It is HttpOnly, so only your server can read it.
    // The bridge reads it, exchanges it ONCE with Atlas through the Backend API (secret key, forwarding the end
    // user's IP and user agent), sets the Atlas session as your own first-party cookies and clears the Better Auth
    // cookies. Atlas finds the session in your import (or asks your old app, with the live fallback on) and runs
    // the normal sign-in path on it — MFA policy included: a user who still owes a second factor is sent to finish it.

    /// <summary>The refusal outcomes of an exchange (<c>meta.bridge_outcome</c>, plus <c>rate_limited</c> and <c>unavailable</c>).</summary>
    public static class BetterAuthOutcome
    {
        public const string Closed = "closed";
        public const string Invalid = "invalid";
        public const string SignatureInvalid = "signature_invalid";
        public const string Expired = "expired";
        public const string Reused = "reused";
        public const string UserUnavailable = "user_unavailable";
        public const string RateLimited = "rate_limited";
        /// <summary>Not a verdict on the Better Auth session (a 5xx, a guard refusal, a transport failure): keep it and retry later.</summary>
        public const string Unavailable = "unavailable";

        internal static readonly string[] Known = { Closed, Invalid, SignatureInvalid, Expired, Reused, UserUnavailable, RateLimited };

        /// <summary>Refusals that say the Better Auth session will never exchange: its cookies are cleared.</summary>
        internal static readonly string[] Final = { Invalid, SignatureInvalid, Expired, Reused, UserUnavailable };
    }

    public enum BetterAuthExchangeKind { Session, NeedsMfa, Refused }

    /// <summary>
    /// The classified outcome of an exchange: <see cref="BetterAuthExchangeKind.Session"/> (<see cref="Jwt"/>,
    /// <see cref="RefreshToken"/>, ...), <see cref="BetterAuthExchangeKind.NeedsMfa"/> (<see cref="AttemptId"/> and
    /// <see cref="Status"/>: the Better Auth session is spent, the user finishes the second factor) or
    /// <see cref="BetterAuthExchangeKind.Refused"/> (<see cref="Outcome"/>; <see cref="HttpStatus"/> 0 for a transport failure).
    /// </summary>
    public sealed class BetterAuthExchangeResult
    {
        public BetterAuthExchangeKind Kind { get; init; }
        public string Jwt { get; init; } = "";
        public string RefreshToken { get; init; } = "";
        public string SessionId { get; init; } = "";
        public string UserId { get; init; } = "";
        public long ExpiresIn { get; init; }
        public string AttemptId { get; init; } = "";
        public string Status { get; init; } = "";
        public string? Outcome { get; init; }
        public int HttpStatus { get; init; } = 200;
    }

    /// <summary>The classified secret-key exchange.</summary>
    public static class BetterAuthExchange
    {
        /// <summary>
        /// Exchange a Better Auth session (the raw cookie value or the bare token) for an Atlas session over the Backend API.
        /// Never throws for an Atlas or transport failure: that is <c>Refused</c>/<c>unavailable</c>, so the caller keeps the
        /// Better Auth cookie and tries again on a later request. Pass the END USER's <paramref name="clientIp"/> and
        /// <paramref name="userAgent"/>.
        /// </summary>
        public static async Task<BetterAuthExchangeResult> ExchangeAsync(
            AtlasClient client, string token, string? clientIp = null, string? userAgent = null, bool? remember = null, CancellationToken ct = default)
        {
            try
            {
                var data = await client.BetterAuthBridge.ExchangeAsync(token, clientIp, userAgent, remember, ct).ConfigureAwait(false);
                return ClassifySuccess(data);
            }
            catch (AtlasException e)
            {
                return ClassifyError(e);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e) when (e is HttpRequestException || e is JsonException || e is OperationCanceledException)
            {
                return Refused(BetterAuthOutcome.Unavailable, 0);
            }
        }

        internal static BetterAuthExchangeResult ClassifySuccess(BetterAuthExchangeResponse? data)
        {
            if (data != null)
            {
                if (data.ObjectType == "session_tokens" && !string.IsNullOrEmpty(data.Jwt) && !string.IsNullOrEmpty(data.RefreshToken))
                {
                    return new BetterAuthExchangeResult
                    {
                        Kind = BetterAuthExchangeKind.Session,
                        Jwt = data.Jwt!,
                        RefreshToken = data.RefreshToken!,
                        SessionId = data.SessionId ?? "",
                        UserId = data.UserId ?? "",
                        ExpiresIn = data.ExpiresIn ?? 0,
                    };
                }
                if (data.ObjectType == "sign_in_attempt" && !string.IsNullOrEmpty(data.Id)
                    && (data.Status == "needs_second_factor" || data.Status == "needs_mfa_enrollment"))
                {
                    return new BetterAuthExchangeResult { Kind = BetterAuthExchangeKind.NeedsMfa, AttemptId = data.Id!, Status = data.Status! };
                }
            }
            return Refused(BetterAuthOutcome.Unavailable, 200);
        }

        internal static BetterAuthExchangeResult ClassifyError(AtlasException err)
        {
            if (err.Status == 429) return Refused(BetterAuthOutcome.RateLimited, 429);
            string? outcome = null;
            var meta = err.Errors.Count > 0 ? err.Errors[0].Meta : null;
            if (meta != null && meta.TryGetValue("bridge_outcome", out var v))
            {
                outcome = v is JsonElement el ? (el.ValueKind == JsonValueKind.String ? el.GetString() : null) : v as string;
            }
            if (outcome != null && Array.IndexOf(BetterAuthOutcome.Known, outcome) >= 0) return Refused(outcome, err.Status);
            return Refused(BetterAuthOutcome.Unavailable, err.Status);
        }

        private static BetterAuthExchangeResult Refused(string outcome, int status)
            => new BetterAuthExchangeResult { Kind = BetterAuthExchangeKind.Refused, Outcome = outcome, HttpStatus = status };
    }

    /// <summary>A cookie to set (or, with <c>MaxAge</c> 0, clear) on the response.</summary>
    public sealed class BridgeCookie
    {
        public string Name { get; init; } = "";
        public string Value { get; init; } = "";
        public string Path { get; init; } = "/";
        public string? Domain { get; init; }
        public bool HttpOnly { get; init; }
        /// <summary><c>lax</c>, <c>strict</c> or <c>none</c>.</summary>
        public string? SameSite { get; init; }
        public bool Secure { get; init; }
        public int? MaxAge { get; init; }

        /// <summary>This cookie as a <c>Set-Cookie</c> header value.</summary>
        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append(Name).Append('=').Append(Value).Append("; Path=").Append(string.IsNullOrEmpty(Path) ? "/" : Path);
            if (!string.IsNullOrEmpty(Domain)) sb.Append("; Domain=").Append(Domain);
            if (MaxAge.HasValue)
            {
                sb.Append("; Max-Age=").Append(MaxAge.Value);
                if (MaxAge.Value == 0) sb.Append("; Expires=Thu, 01 Jan 1970 00:00:00 GMT");
            }
            if (HttpOnly) sb.Append("; HttpOnly");
            if (Secure) sb.Append("; Secure");
            if (!string.IsNullOrEmpty(SameSite)) sb.Append("; SameSite=").Append(char.ToUpperInvariant(SameSite![0])).Append(SameSite.Substring(1));
            return sb.ToString();
        }
    }

    public enum BridgeAction { None, SignedIn, NeedsMfa, Refused }

    /// <summary>
    /// What the bridge decided for one request. <c>None</c>: no Better Auth cookie, or an Atlas session is already present.
    /// <c>SignedIn</c>: set <see cref="Cookies"/> and re-enter (redirect to the same URL). <c>NeedsMfa</c>: send the browser to
    /// your sign-in page carrying the attempt (<see cref="ContinuationUrl"/>); <see cref="Cookies"/> clears the spent Better Auth
    /// cookies. <c>Refused</c>: carry on unauthenticated; <see cref="Cookies"/> clears the Better Auth cookies only when the
    /// refusal is final (invalid, expired, reused, ...).
    /// </summary>
    public sealed class BridgeDecision
    {
        public BridgeAction Action { get; init; }
        public IReadOnlyList<BridgeCookie> Cookies { get; init; } = Array.Empty<BridgeCookie>();
        public BetterAuthExchangeResult? Session { get; init; }
        public string AttemptId { get; init; } = "";
        public string Status { get; init; } = "";
        public string? Outcome { get; init; }

        /// <summary><paramref name="url"/> with <c>__atlas_attempt</c> / <c>__atlas_status</c> set (<c>NeedsMfa</c>).</summary>
        public string ContinuationUrl(string url)
        {
            var hash = "";
            var h = url.IndexOf('#');
            if (h >= 0) { hash = url.Substring(h); url = url.Substring(0, h); }
            var q = url.IndexOf('?');
            var path = q >= 0 ? url.Substring(0, q) : url;
            var kept = new List<string>();
            if (q >= 0)
            {
                foreach (var pair in url.Substring(q + 1).Split('&'))
                {
                    if (pair.Length == 0) continue;
                    var name = pair.Split('=')[0];
                    if (name == "__atlas_attempt" || name == "__atlas_status") continue;
                    kept.Add(pair);
                }
            }
            kept.Add("__atlas_attempt=" + Uri.EscapeDataString(AttemptId));
            kept.Add("__atlas_status=" + Uri.EscapeDataString(Status));
            return path + "?" + string.Join("&", kept) + hash;
        }
    }

    /// <summary>Settings of a <see cref="BetterAuthSessionBridge"/>.</summary>
    public sealed class BetterAuthBridgeOptions
    {
        /// <summary>Better Auth's <c>advanced.cookiePrefix</c> (default <c>better-auth</c>).</summary>
        public string CookiePrefix { get; set; } = BetterAuthCookies.DefaultPrefix;
        /// <summary>The session cookie's full name, when the old app overrode <c>advanced.cookies.session_token.name</c>.</summary>
        public string? CookieName { get; set; }
        /// <summary>The <c>domain</c> the Better Auth cookies were set on (its <c>crossSubDomainCookies.domain</c>), so they clear.</summary>
        public string? BetterAuthCookieDomain { get; set; }
        /// <summary>Mark the Atlas cookies <c>Secure</c>. Set false only for plain-http local development.</summary>
        public bool Secure { get; set; } = true;
        /// <summary>Where a user who owes a second factor is sent (carrying <c>__atlas_attempt</c> / <c>__atlas_status</c>). Defaults to the current URL.</summary>
        public string? SignInUrl { get; set; }
    }

    /// <summary>Reading and naming the Better Auth cookies.</summary>
    public static class BetterAuthCookies
    {
        /// <summary>Better Auth's default cookie prefix.</summary>
        public const string DefaultPrefix = "better-auth";

        /// <summary>The Better Auth cookies the bridge reads (<c>SessionToken</c>, the <c>__Secure-</c> name first) and clears (<c>All</c>).</summary>
        public static (string[] SessionToken, string[] All) Names(string prefix = DefaultPrefix)
        {
            var b = new[] { "session_token", "session_data", "account_data", "dont_remember" }.Select(n => prefix + "." + n).ToArray();
            return (
                new[] { "__Secure-" + prefix + ".session_token", prefix + ".session_token" },
                b.Select(n => "__Secure-" + n).Concat(b).ToArray());
        }

        /// <summary>Parse a <c>Cookie</c> header (the first occurrence of a name wins).</summary>
        public static Dictionary<string, string> ParseHeader(string? header)
        {
            var jar = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(header)) return jar;
            foreach (var part in header!.Split(';'))
            {
                var p = part.Trim();
                var eq = p.IndexOf('=');
                if (eq <= 0) continue;
                var key = p.Substring(0, eq);
                if (!jar.ContainsKey(key)) jar[key] = p.Substring(eq + 1);
            }
            return jar;
        }

        /// <summary>
        /// The raw Better Auth session cookie value from a parsed cookie jar, the <c>__Secure-</c> name first; null when absent.
        /// </summary>
        public static string? ReadSessionCookie(IReadOnlyDictionary<string, string> cookies, string prefix = DefaultPrefix, string? cookieName = null)
        {
            var names = !string.IsNullOrEmpty(cookieName) ? new[] { "__Secure-" + cookieName, cookieName! } : Names(prefix).SessionToken;
            foreach (var name in names)
            {
                if (cookies.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v)) return v;
            }
            return null;
        }

        /// <summary>The same, from a <c>Cookie</c> header.</summary>
        public static string? ReadSessionCookie(string? cookieHeader, string prefix = DefaultPrefix, string? cookieName = null)
            => ReadSessionCookie(ParseHeader(cookieHeader), prefix, cookieName);
    }

    /// <summary>
    /// The bridge as a function of a request: call it from middleware before your normal auth check. It does nothing (no
    /// network) for a request without a Better Auth cookie or with an Atlas session (<c>__session</c> / <c>__atlas_rt</c>)
    /// present. Use <c>UseAtlasBetterAuthBridge</c> in Atlas.AspNetCore for the ready-made middleware.
    /// </summary>
    public sealed class BetterAuthSessionBridge
    {
        private readonly Func<string, string?, string?, CancellationToken, Task<BetterAuthExchangeResult>> _exchange;

        /// <param name="client">An <see cref="AtlasClient"/> on the instance secret key.</param>
        /// <param name="options">Cookie names, domain and the Atlas cookies' attributes.</param>
        public BetterAuthSessionBridge(AtlasClient client, BetterAuthBridgeOptions? options = null)
            : this(Exchanger(client), options)
        {
        }

        /// <summary>A bridge over any exchange function (tests, custom transports).</summary>
        public BetterAuthSessionBridge(
            Func<string, string?, string?, CancellationToken, Task<BetterAuthExchangeResult>> exchange, BetterAuthBridgeOptions? options = null)
        {
            _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
            Options = options ?? new BetterAuthBridgeOptions();
        }

        private static Func<string, string?, string?, CancellationToken, Task<BetterAuthExchangeResult>> Exchanger(AtlasClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            return (token, ip, ua, ct) => BetterAuthExchange.ExchangeAsync(client, token, ip, ua, null, ct);
        }

        public BetterAuthBridgeOptions Options { get; }

        /// <summary>
        /// Decide for one request. <paramref name="cookies"/> is the parsed cookie jar; <paramref name="clientIp"/> is the end
        /// user's IP as your framework resolved it (falls back to the first <c>X-Forwarded-For</c> hop of <paramref name="forwardedFor"/>).
        /// </summary>
        public async Task<BridgeDecision> DecideAsync(
            IReadOnlyDictionary<string, string> cookies, string? clientIp = null, string? userAgent = null, string? forwardedFor = null, CancellationToken ct = default)
        {
            var token = BetterAuthCookies.ReadSessionCookie(cookies, Options.CookiePrefix, Options.CookieName);
            // Already on Atlas: leave the old cookie for the old app's own expiry.
            if (string.IsNullOrEmpty(token) || cookies.ContainsKey("__session") || cookies.ContainsKey("__atlas_rt"))
                return new BridgeDecision { Action = BridgeAction.None };

            var ip = !string.IsNullOrEmpty(clientIp) ? clientIp : FirstHop(forwardedFor);
            var result = await _exchange(token!, ip, userAgent, ct).ConfigureAwait(false);
            return Decide(result);
        }

        /// <summary>The same, from a <c>Cookie</c> header.</summary>
        public Task<BridgeDecision> DecideAsync(string? cookieHeader, string? clientIp = null, string? userAgent = null, string? forwardedFor = null, CancellationToken ct = default)
            => DecideAsync(BetterAuthCookies.ParseHeader(cookieHeader), clientIp, userAgent, forwardedFor, ct);

        private static string? FirstHop(string? forwardedFor)
        {
            if (string.IsNullOrEmpty(forwardedFor)) return null;
            var hop = forwardedFor!.Split(',')[0].Trim();
            return hop.Length > 0 ? hop : null;
        }

        private IReadOnlyList<BridgeCookie> Clear()
        {
            var names = BetterAuthCookies.Names(Options.CookiePrefix).All.ToList();
            if (!string.IsNullOrEmpty(Options.CookieName))
            {
                names.Add(Options.CookieName!);
                names.Add("__Secure-" + Options.CookieName);
            }
            return names.Select(n => new BridgeCookie
            {
                Name = n,
                Value = "",
                MaxAge = 0,
                Domain = Options.BetterAuthCookieDomain,
                // A `__Secure-` cookie can only be overwritten by a Secure Set-Cookie.
                Secure = n.StartsWith("__Secure-", StringComparison.Ordinal),
            }).ToList();
        }

        private BridgeDecision Decide(BetterAuthExchangeResult result)
        {
            if (result.Kind == BetterAuthExchangeKind.Session)
            {
                var cookies = new List<BridgeCookie>
                {
                    new BridgeCookie { Name = "__session", Value = result.Jwt, SameSite = "lax", Secure = Options.Secure },
                    new BridgeCookie { Name = "__atlas_rt", Value = result.RefreshToken, HttpOnly = true, SameSite = "lax", Secure = Options.Secure },
                };
                cookies.AddRange(Clear());
                return new BridgeDecision { Action = BridgeAction.SignedIn, Session = result, Cookies = cookies };
            }
            if (result.Kind == BetterAuthExchangeKind.NeedsMfa)
            {
                return new BridgeDecision { Action = BridgeAction.NeedsMfa, AttemptId = result.AttemptId, Status = result.Status, Cookies = Clear() };
            }
            var outcome = result.Outcome ?? BetterAuthOutcome.Unavailable;
            return new BridgeDecision
            {
                Action = BridgeAction.Refused,
                Outcome = outcome,
                Cookies = Array.IndexOf(BetterAuthOutcome.Final, outcome) >= 0 ? Clear() : Array.Empty<BridgeCookie>(),
            };
        }
    }
}
