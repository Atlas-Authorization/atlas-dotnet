using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Verification
{
    /// <summary>
    /// A fresh session minted by redeeming a cross-property handshake nonce. The
    /// members are the raw cookie VALUES a satellite's ASP.NET handler sets as its
    /// own first-party cookies — never in a URL:
    ///
    /// <code>
    /// var s = await Handshake.RedeemHandshakeAsync(fapiOrigin, publishableKey, userId, nonce, codeVerifier);
    /// if (s != null)
    /// {
    ///     // Script-readable, short-lived session JWT.
    ///     Response.Cookies.Append("__session", s.Jwt,
    ///         new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax, Secure = true });
    ///     // The refresh value — HttpOnly, so the browser never exposes it to script.
    ///     Response.Cookies.Append("__atlas_rt", s.RefreshToken,
    ///         new CookieOptions { Path = "/", HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = true });
    /// }
    /// </code>
    /// </summary>
    public sealed record HandshakeSession
    {
        /// <summary>The <c>__session</c> JWT value to set as a cookie (script-readable, short-lived).</summary>
        [JsonPropertyName("jwt")] public string Jwt { get; init; } = "";

        /// <summary>The <c>__atlas_rt</c> refresh value to set as an HttpOnly cookie.</summary>
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; init; } = "";

        /// <summary>The session id.</summary>
        [JsonPropertyName("session_id")] public string SessionId { get; init; } = "";

        /// <summary>Seconds until the <c>__session</c> JWT expires.</summary>
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
    }

    /// <summary>
    /// The handshake parameters carried on a satellite's return URL:
    /// <c>?__atlas_hs=ok&amp;__atlas_hu=&lt;userId&gt;&amp;__atlas_hn=&lt;nonce&gt;</c>.
    /// </summary>
    public sealed record HandshakeParams(string UserId, string Nonce);

    /// <summary>
    /// A PKCE (RFC 7636, S256) verifier/challenge pair for a single handshake.
    ///
    /// The handshake nonce rides back on a return URL that gets written to access
    /// logs, referrers, and browser history, so on its own it is a bearer
    /// credential anyone who can read that URL could replay. PKCE binds the redeem
    /// to a secret that NEVER leaves the satellite's server: only the derived
    /// <see cref="Challenge"/> travels (on the outbound handshake URL), while the
    /// <see cref="Verifier"/> is stashed server-side (an HttpOnly cookie) and
    /// presented at redeem. A stolen nonce is then useless without the matching
    /// verifier the attacker never saw.
    /// </summary>
    /// <param name="Verifier">
    /// The high-entropy secret — base64url (no padding) of 32 random bytes. Stash
    /// it server-side (documented as an HttpOnly cookie <c>__atlas_hv</c>, path
    /// <c>/</c>, ~300s) and pass it to <see cref="Handshake.RedeemHandshakeAsync"/>.
    /// It must NEVER appear in a URL.
    /// </param>
    /// <param name="Challenge">
    /// The public transform — base64url (no padding) of <c>SHA-256</c> over the
    /// ASCII bytes of <see cref="Verifier"/>. Sent to the Atlas server as the
    /// <c>code_challenge</c> query parameter on the handshake bounce.
    /// </param>
    public sealed record PkcePair(string Verifier, string Challenge);

    /// <summary>
    /// The outbound handshake bounce built by <see cref="Handshake.BuildHandshakeUrl"/>:
    /// the <see cref="Url"/> to redirect the browser to, and the
    /// <see cref="CodeVerifier"/> the caller must stash server-side (never in a
    /// URL) and hand back to <see cref="Handshake.RedeemHandshakeAsync"/>.
    /// </summary>
    public sealed record HandshakeBounce(string Url, string CodeVerifier);

    /// <summary>
    /// Cross-property SSO handshake — the satellite property's server-side redeem
    /// step.
    ///
    /// When a satellite (a property on a DIFFERENT registrable domain than the
    /// main app) has no local session, the SDK middleware bounces the browser
    /// through <c>GET {fapiOrigin}/v1/client/handshake</c>, which — if the user has
    /// an Atlas session — redirects back with
    /// <c>?__atlas_hs=ok&amp;__atlas_hu=&lt;userId&gt;&amp;__atlas_hn=&lt;nonce&gt;</c>.
    /// <see cref="RedeemHandshakeAsync"/> exchanges that single-use nonce for a
    /// fresh session, server-to-server, so the tokens come back in the response
    /// BODY (never a URL). The caller sets them as its OWN first-party cookies
    /// (see <see cref="HandshakeSession"/>).
    ///
    /// The nonce alone is NOT a bearer credential: it rides back on a return URL
    /// that ends up in access logs, referrers, and history, so the flow is bound
    /// with PKCE (RFC 7636, S256). Build the bounce with
    /// <see cref="BuildHandshakeUrl"/> (or generate a pair with
    /// <see cref="GeneratePkce"/>): the <c>code_challenge</c> goes out on the URL,
    /// the verifier is stashed server-side (an HttpOnly cookie <c>__atlas_hv</c>),
    /// and the same verifier is passed back to <see cref="RedeemHandshakeAsync"/>.
    /// A stolen nonce is then useless without the verifier the attacker never saw.
    ///
    /// <code>
    /// // 1. Bounce the browser to the Atlas handshake, stashing the verifier.
    /// var bounce = Handshake.BuildHandshakeUrl(fapiOrigin);
    /// Response.Cookies.Append("__atlas_hv", bounce.CodeVerifier,
    ///     new CookieOptions { Path = "/", HttpOnly = true, SameSite = SameSiteMode.Lax,
    ///                         Secure = true, MaxAge = TimeSpan.FromSeconds(300) });
    /// return Redirect(bounce.Url + "&amp;return_url=" + Uri.EscapeDataString(here));
    ///
    /// // 2. On the return URL, redeem the nonce WITH the stashed verifier.
    /// var p = Handshake.ReadHandshakeParams(Request.QueryString.Value);
    /// var verifier = Request.Cookies["__atlas_hv"];
    /// var s = await Handshake.RedeemHandshakeAsync(fapiOrigin, publishableKey, p.UserId, p.Nonce, verifier);
    /// </code>
    ///
    /// (Same-registrable-domain SUBDOMAINS don't need this — the handshake sets a
    /// parent-domain cookie directly; this is only the cross-domain path.)
    /// </summary>
    public static class Handshake
    {
        /// <summary>
        /// Generate a fresh PKCE (RFC 7636, S256) verifier/challenge pair for one
        /// handshake. The <c>verifier</c> is base64url (no padding) over 32
        /// cryptographically-random bytes; the <c>challenge</c> is base64url (no
        /// padding) over <c>SHA-256</c> of the verifier's ASCII bytes.
        ///
        /// WHY this lives in the SDK: the handshake nonce travels on a loggable
        /// return URL, so it must not be replayable on its own. Only the challenge
        /// is exposed (on the outbound URL); the verifier stays server-side (an
        /// HttpOnly cookie) and is presented at redeem, so a leaked nonce can't be
        /// exchanged without the secret the attacker never observed. Encoding and
        /// hash match the Atlas server byte-for-byte so the server-side comparison
        /// (<c>base64url(sha256(verifier)) == code_challenge</c>) succeeds.
        /// </summary>
        public static PkcePair GeneratePkce()
        {
            // 32 bytes ≈ 256 bits of entropy, comfortably inside RFC 7636's
            // 43–128 char verifier bound once base64url-encoded (43 chars).
            var raw = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(raw);
            }
            var verifier = Base64UrlNoPad(raw);

            // The server hashes the ASCII bytes of the verifier string (which is
            // itself already ASCII-safe base64url), NOT the raw entropy bytes.
            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(Encoding.ASCII.GetBytes(verifier));
            }
            var challenge = Base64UrlNoPad(hash);

            return new PkcePair(verifier, challenge);
        }

        /// <summary>
        /// Build the outbound handshake bounce URL with a fresh PKCE challenge, and
        /// return the matching verifier for the caller to stash server-side (an
        /// HttpOnly cookie <c>__atlas_hv</c>, path <c>/</c>, ~300s) — NEVER in a
        /// URL. Redirect the browser to <see cref="HandshakeBounce.Url"/> (append
        /// your own <c>return_url</c> etc.), then pass
        /// <see cref="HandshakeBounce.CodeVerifier"/> to
        /// <see cref="RedeemHandshakeAsync"/> on the return leg.
        /// </summary>
        /// <param name="fapiOrigin">Origin of the Atlas Frontend API, e.g. <c>https://id.atlasauth.net</c>.</param>
        public static HandshakeBounce BuildHandshakeUrl(string fapiOrigin)
        {
            var pkce = GeneratePkce();
            var url = (fapiOrigin ?? "").TrimEnd('/')
                + "/v1/client/handshake?code_challenge=" + pkce.Challenge;
            return new HandshakeBounce(url, pkce.Verifier);
        }

        /// <summary>
        /// Redeem a handshake nonce for a fresh session, server-to-server. POSTs to
        /// <c>{fapiOrigin}/v1/client/handshake/redeem</c> with an
        /// <c>X-Publishable-Key</c> header and a
        /// <c>{"user_id","nonce","code_verifier"}</c> body.
        ///
        /// The <paramref name="codeVerifier"/> is the PKCE (RFC 7636, S256) secret
        /// stashed server-side when the bounce was built (see
        /// <see cref="BuildHandshakeUrl"/> / <see cref="GeneratePkce"/>). The Atlas
        /// server checks <c>base64url(sha256(code_verifier)) == code_challenge</c>
        /// it saw on the outbound handshake, so a nonce leaked from a return URL is
        /// worthless without the matching verifier. The server now REQUIRES it.
        ///
        /// Returns <c>null</c> when the nonce is missing / expired / already used, the
        /// verifier is absent or does not match, or on any transport failure — a bad
        /// nonce is a normal "signed-out" signal, so the caller should fall back to
        /// sign-in rather than treat it as an error. This method NEVER throws on an
        /// auth failure.
        /// </summary>
        /// <param name="fapiOrigin">Origin of the Atlas Frontend API, e.g. <c>https://id.atlasauth.net</c>.</param>
        /// <param name="publishableKey">The instance publishable key (<c>pk_…</c>).</param>
        /// <param name="userId">The <c>__atlas_hu</c> value from the return URL.</param>
        /// <param name="nonce">The single-use <c>__atlas_hn</c> nonce from the return URL.</param>
        /// <param name="codeVerifier">The PKCE verifier stashed server-side (the <c>__atlas_hv</c> cookie) when the handshake bounce was built.</param>
        /// <param name="httpClient">An optional shared <see cref="HttpClient"/>; a transient one is used when null.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        public static async Task<HandshakeSession?> RedeemHandshakeAsync(
            string fapiOrigin,
            string publishableKey,
            string userId,
            string nonce,
            string codeVerifier,
            HttpClient? httpClient = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fapiOrigin) || string.IsNullOrEmpty(publishableKey)
                || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nonce)
                || string.IsNullOrEmpty(codeVerifier))
            {
                return null;
            }

            var http = httpClient ?? new HttpClient();
            try
            {
                var url = fapiOrigin.TrimEnd('/') + "/v1/client/handshake/redeem";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.TryAddWithoutValidation("X-Publishable-Key", publishableKey);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Content = new StringContent(
                    AtlasJson.Serialize(new { user_id = userId, nonce, code_verifier = codeVerifier }),
                    Encoding.UTF8,
                    "application/json");

                using var response = await http
                    .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return null;

                var text =
#if NET8_0_OR_GREATER
                    await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                if (string.IsNullOrEmpty(text))
                    return null;

                var session = AtlasJson.Deserialize<HandshakeSession>(text);
                // A 2xx with no usable tokens is still a signed-out signal.
                if (session == null || string.IsNullOrEmpty(session.Jwt) || string.IsNullOrEmpty(session.RefreshToken))
                    return null;

                return session;
            }
            catch
            {
                // A transport error is a signed-out signal, not a page-breaking
                // exception — mirrors the TypeScript SDK's redeemHandshake.
                return null;
            }
            finally
            {
                if (httpClient == null) http.Dispose();
            }
        }

        /// <summary>
        /// Extract the handshake params from a satellite's return URL (or its bare
        /// query string). Returns a <see cref="HandshakeParams"/> only when
        /// <c>__atlas_hs</c> is <c>ok</c> and both <c>__atlas_hu</c> (userId) and
        /// <c>__atlas_hn</c> (nonce) are present; otherwise <c>null</c>.
        /// </summary>
        public static HandshakeParams? ReadHandshakeParams(string urlOrQuery)
        {
            if (string.IsNullOrEmpty(urlOrQuery))
                return null;

            var query = ExtractQuery(urlOrQuery);
            var pairs = ParseQuery(query);

            if (!pairs.TryGetValue("__atlas_hs", out var status) || status != "ok")
                return null;

            var hasUser = pairs.TryGetValue("__atlas_hu", out var userId) && !string.IsNullOrEmpty(userId);
            var hasNonce = pairs.TryGetValue("__atlas_hn", out var nonce) && !string.IsNullOrEmpty(nonce);

            return hasUser && hasNonce ? new HandshakeParams(userId!, nonce!) : null;
        }

        // ---- helpers ----

        /// <summary>
        /// base64url without padding, per RFC 7636: standard base64, then
        /// <c>+</c>→<c>-</c>, <c>/</c>→<c>_</c>, and the trailing <c>=</c> stripped.
        /// Matches the Atlas server's encoding exactly so PKCE comparison lines up.
        /// </summary>
        private static string Base64UrlNoPad(byte[] bytes)
            => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private static string ExtractQuery(string urlOrQuery)
        {
            // Drop any fragment first, then take everything after the first '?'.
            var hash = urlOrQuery.IndexOf('#');
            var s = hash >= 0 ? urlOrQuery.Substring(0, hash) : urlOrQuery;

            var mark = s.IndexOf('?');
            return mark >= 0 ? s.Substring(mark + 1) : s;
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(query))
                return result;

            foreach (var part in query.Split('&'))
            {
                if (part.Length == 0) continue;
                var eq = part.IndexOf('=');
                string key, value;
                if (eq < 0)
                {
                    key = part;
                    value = "";
                }
                else
                {
                    key = part.Substring(0, eq);
                    value = part.Substring(eq + 1);
                }

                key = Decode(key);
                if (key.Length == 0) continue;
                // First value wins, matching URLSearchParams.get.
                if (!result.ContainsKey(key))
                    result[key] = Decode(value);
            }
            return result;
        }

        private static string Decode(string s)
        {
            if (s.Length == 0) return s;
            try
            {
                // Query encoding uses '+' for space in addition to percent-encoding.
                return Uri.UnescapeDataString(s.Replace('+', ' '));
            }
            catch
            {
                return s;
            }
        }
    }
}
