using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Verification;
using Sodium;

namespace Atlas.Primitives
{
    /// <summary>Why a Web Bot Auth verification failed.</summary>
    public enum WebBotAuthFailure
    {
        None, NoSignature, NoWebBotAuthSignature, Malformed, MissingSignatureAgent, BadSignatureAgent, BadParams, UnsupportedAlgorithm,
        UnsupportedComponent, ComponentMissing, ComponentsNotCovered, BadRequest, NotYetValid, Expired, LifetimeTooLong,
        DirectoryUnavailable, KeyNotFound, BadSignature, Replayed,
    }

    public sealed class WebBotAuthRequest
    {
        public string Method { get; set; } = "GET";
        /// <summary>The absolute request URL as the origin server saw it (scheme, host, path, query).</summary>
        public string Url { get; set; } = "";
        /// <summary>Header values; fields of one name are joined with ", ".</summary>
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class WebBotAuthResult
    {
        /// <summary>True when the request carried a signature header at all.</summary>
        public bool Signed { get; init; }
        public bool Verified { get; init; }
        public WebBotAuthFailure Reason { get; init; }
        /// <summary>The directory the signer claims; trust it only when <see cref="Verified"/>.</summary>
        public string? Directory { get; init; }
        public string? KeyId { get; init; }
        public string? Label { get; init; }
        public long? Created { get; init; }
        public long? Expires { get; init; }
        public string? Nonce { get; init; }
    }

    /// <summary>Allow and block lists entry: a directory URL or bare host.</summary>
    public sealed record WebBotAuthListEntry(string Directory, string? Name = null);

    public sealed class WebBotAuthPolicy
    {
        public IReadOnlyList<WebBotAuthListEntry> Allow { get; set; } = Array.Empty<WebBotAuthListEntry>();
        public IReadOnlyList<WebBotAuthListEntry> Block { get; set; } = Array.Empty<WebBotAuthListEntry>();
    }

    public sealed class WebBotAuthDecision
    {
        public bool Verified { get; init; }
        /// <summary>Set only when verified: an unverified claim proves nothing.</summary>
        public (string Directory, string KeyId, string Name)? Agent { get; init; }
        /// <summary><c>allowed</c>, <c>blocked</c>, <c>verified</c> or <c>unverified</c>.</summary>
        public string Tier { get; init; } = "unverified";
        /// <summary><c>allow</c>, <c>block</c> or <c>unknown</c>.</summary>
        public string Decision { get; init; } = "unknown";
        public WebBotAuthFailure Reason { get; init; }
    }

    /// <summary>
    /// Web Bot Auth: verify who is behind an automated request. A bot signs its requests with an Ed25519 key (RFC 9421 HTTP Message Signatures,
    /// tag <c>web-bot-auth</c>) and names the place its public keys live in <c>Signature-Agent</c>; the signature's <c>keyid</c> is the RFC 7638 JWK
    /// thumbprint of the key found in the directory at <c>{agent}/.well-known/http-message-signatures-directory</c>.
    /// shortcut: covered components are the derived ones (<c>@method @authority @path @target-uri @scheme @query @request-target</c>),
    /// <c>@query-param</c>, and header fields with no parameter or with <c>key</c> / <c>bs</c>; the <c>sf</c> parameter is refused as
    /// <c>UnsupportedComponent</c> (fails closed). Add canonical structured-field serialization before accepting it.
    /// </summary>
    public static class WebBotAuth
    {
        public const string Tag = "web-bot-auth";
        public const string DirectoryPath = "/.well-known/http-message-signatures-directory";
        public const string DirectoryMediaType = "application/http-message-signatures-directory+json";
        public const int DefaultSkewSeconds = 30;
        public const int DefaultMaxLifetimeSeconds = 3600;

        private static readonly Regex KeyRe = new Regex("^[a-z*][a-z0-9_.*-]*$", RegexOptions.Compiled);
        private static readonly HashSet<string> Derived = new HashSet<string> { "@method", "@authority", "@path", "@target-uri", "@scheme", "@query", "@request-target" };

        // ── encoding ────────────────────────────────────────────────────────────

        public static string B64U(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public static byte[]? FromB64U(string s)
        {
            if (!Regex.IsMatch(s, "^[A-Za-z0-9_-]*$")) return null;
            try { return JwksCache.Base64UrlDecode(s); } catch (FormatException) { return null; }
        }

        private static byte[]? FromB64(string s)
        {
            if (!Regex.IsMatch(s, "^[A-Za-z0-9+/]*={0,2}$")) return null;
            try { return Convert.FromBase64String(s.PadRight((s.Length + 3) / 4 * 4, '=')); } catch (FormatException) { return null; }
        }

        /// <summary>RFC 7638 thumbprint (base64url SHA-256) of an Ed25519 public JWK <c>x</c>: the web-bot-auth <c>keyid</c>.</summary>
        public static string Ed25519JwkThumbprint(string x)
        {
            using var sha = SHA256.Create();
            return B64U(sha.ComputeHash(Encoding.UTF8.GetBytes("{\"crv\":\"Ed25519\",\"kty\":\"OKP\",\"x\":\"" + x + "\"}")));
        }

        /// <summary>The usable Ed25519 public keys (as their <c>x</c> values) in a directory document (a JWK Set). At most 20.</summary>
        public static IReadOnlyList<string> ParseDirectory(JsonElement doc)
        {
            var output = new List<string>();
            if (doc.ValueKind != JsonValueKind.Object || !doc.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array) return output;
            foreach (var k in keys.EnumerateArray())
            {
                if (k.ValueKind != JsonValueKind.Object) continue;
                string? S(string n) => k.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                if (S("kty") == "OKP" && S("crv") == "Ed25519" && S("x") is { } x && FromB64U(x)?.Length == 32) output.Add(x);
                if (output.Count == 20) break;
            }
            return output;
        }

        // ── structured fields (the subset RFC 9421 headers use) ─────────────────

        private static List<string> SplitTop(string s, char sep)
        {
            var output = new List<string>();
            int depth = 0, start = 0;
            var quoted = false;
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (quoted) { if (c == '\\') i++; else if (c == '"') quoted = false; }
                else if (c == '"') quoted = true;
                else if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == sep && depth == 0) { output.Add(s.Substring(start, i - start)); start = i + 1; }
            }
            output.Add(s.Substring(start));
            return output.Select(m => m.Trim()).Where(m => m.Length > 0).ToList();
        }

        private static (string Value, int End)? ReadString(string s, int i)
        {
            if (i >= s.Length || s[i] != '"') return null;
            var sb = new StringBuilder();
            for (var j = i + 1; j < s.Length; j++)
            {
                var c = s[j];
                if (c == '\\')
                {
                    if (j + 1 >= s.Length || (s[j + 1] != '"' && s[j + 1] != '\\')) return null;
                    sb.Append(s[j + 1]); j++;
                }
                else if (c == '"') return (sb.ToString(), j + 1);
                else if (c < 0x20 || c > 0x7e) return null;
                else sb.Append(c);
            }
            return null;
        }

        /// <summary>Parse <c>;k=v;k2</c>. Values are string, long, double, bool or byte[]. Null when malformed.</summary>
        private static Dictionary<string, object>? ParseParams(string text)
        {
            var output = new Dictionary<string, object>();
            var i = 0;
            while (i < text.Length)
            {
                if (text[i] != ';') return null;
                i++;
                var key = new StringBuilder();
                while (i < text.Length && Regex.IsMatch(text[i].ToString(), "[a-z0-9_.*-]")) key.Append(text[i++]);
                if (!KeyRe.IsMatch(key.ToString())) return null;
                var k = key.ToString();
                if (i >= text.Length || text[i] != '=') { output[k] = true; continue; }
                i++;
                if (i >= text.Length) return null;
                var c = text[i];
                if (c == '"')
                {
                    var str = ReadString(text, i);
                    if (str == null) return null;
                    output[k] = str.Value.Value; i = str.Value.End;
                }
                else if (c == ':')
                {
                    var close = text.IndexOf(':', i + 1);
                    if (close < 0) return null;
                    var bytes = FromB64(text.Substring(i + 1, close - i - 1));
                    if (bytes == null) return null;
                    output[k] = bytes; i = close + 1;
                }
                else if (c == '?')
                {
                    var b = text.Length >= i + 2 ? text.Substring(i, 2) : "";
                    if (b != "?0" && b != "?1") return null;
                    output[k] = b == "?1"; i += 2;
                }
                else if (c == '-' || char.IsDigit(c))
                {
                    var n = new StringBuilder();
                    while (i < text.Length && (text[i] == '-' || text[i] == '.' || char.IsDigit(text[i]))) n.Append(text[i++]);
                    if (!Regex.IsMatch(n.ToString(), @"^-?\d{1,15}(\.\d{1,3})?$")) return null;
                    output[k] = n.ToString().Contains('.') ? (object)double.Parse(n.ToString(), System.Globalization.CultureInfo.InvariantCulture) : long.Parse(n.ToString());
                }
                else if (char.IsLetter(c) || c == '*')
                {
                    var t = new StringBuilder();
                    while (i < text.Length && Regex.IsMatch(text[i].ToString(), @"[A-Za-z0-9:/!#$%&'*+\-.^_`|~]")) t.Append(text[i++]);
                    output[k] = t.ToString();
                }
                else return null;
            }
            return output;
        }

        private sealed class Component { public string Name = ""; public Dictionary<string, object> Params = new Dictionary<string, object>(); }
        private sealed class SignatureInput { public string Label = ""; public List<Component> Components = new List<Component>(); public Dictionary<string, object> Params = new Dictionary<string, object>(); public string Raw = ""; }

        private static List<SignatureInput>? ParseSignatureInput(string header)
        {
            var output = new List<SignatureInput>();
            foreach (var member in SplitTop(header, ','))
            {
                var eq = member.IndexOf('=');
                if (eq <= 0) return null;
                var label = member.Substring(0, eq).Trim();
                var raw = member.Substring(eq + 1).Trim();
                if (!KeyRe.IsMatch(label) || raw.Length == 0 || raw[0] != '(') return null;
                var close = -1;
                var quoted = false;
                for (var i = 1; i < raw.Length; i++)
                {
                    var c = raw[i];
                    if (quoted) { if (c == '\\') i++; else if (c == '"') quoted = false; }
                    else if (c == '"') quoted = true;
                    else if (c == ')') { close = i; break; }
                }
                if (close < 0) return null;
                var inner = raw.Substring(1, close - 1);
                var comps = new List<Component>();
                var p = 0;
                while (p < inner.Length)
                {
                    if (inner[p] == ' ') { p++; continue; }
                    var str = ReadString(inner, p);
                    if (str == null) return null;
                    p = str.Value.End;
                    var ptext = new StringBuilder();
                    while (p < inner.Length && inner[p] != ' ')
                    {
                        if (inner[p] == '"')
                        {
                            var q = ReadString(inner, p);
                            if (q == null) return null;
                            ptext.Append(inner, p, q.Value.End - p); p = q.Value.End;
                        }
                        else ptext.Append(inner[p++]);
                    }
                    var ps = ParseParams(ptext.ToString());
                    if (ps == null) return null;
                    comps.Add(new Component { Name = str.Value.Value, Params = ps });
                }
                var parms = ParseParams(raw.Substring(close + 1));
                if (parms == null) return null;
                output.Add(new SignatureInput { Label = label, Components = comps, Params = parms, Raw = raw });
            }
            return output.Count > 0 ? output : null;
        }

        private static Dictionary<string, byte[]>? ParseSignatureHeader(string header)
        {
            var output = new Dictionary<string, byte[]>();
            foreach (var member in SplitTop(header, ','))
            {
                var m = Regex.Match(member, "^([a-z*][a-z0-9_.*-]*)=:([A-Za-z0-9+/=]*):$");
                if (!m.Success) return null;
                var bytes = FromB64(m.Groups[2].Value);
                if (bytes == null) return null;
                output[m.Groups[1].Value] = bytes;
            }
            return output.Count > 0 ? output : null;
        }

        /// <summary>
        /// The directory a <c>Signature-Agent</c> value names, normalised (https, no credentials, query or fragment, no trailing slash). Accepts a
        /// quoted string, a bare URL, or the dictionary form (<c>sig1="https://bot.example"</c>; the member for <paramref name="label"/> wins, else the first).
        /// </summary>
        public static string? ParseSignatureAgent(string value, string? label = null)
        {
            var v = value.Trim();
            if (v.Length == 0) return null;
            if (Regex.IsMatch(v, "^[a-z*][a-z0-9_.*-]*="))
            {
                var members = SplitTop(v, ',');
                var pick = members.FirstOrDefault(m => label != null && m.StartsWith(label + "=")) ?? members[0];
                v = pick.Substring(pick.IndexOf('=') + 1).Trim();
            }
            if (v.StartsWith("\""))
            {
                var str = ReadString(v, 0);
                if (str == null || str.Value.End != v.Length) return null;
                v = str.Value.Value;
            }
            return NormalizeDirectory(v);
        }

        /// <summary>Canonical <c>https://host[:port][/path]</c>; a bare host gets <c>https://</c>. Null for anything else.</summary>
        public static string? NormalizeDirectory(string input)
        {
            var s = input.Trim();
            if (s.Length == 0) return null;
            if (!Regex.IsMatch(s, "^[a-z][a-z0-9+.-]*://", RegexOptions.IgnoreCase)) s = "https://" + s;
            if (!Uri.TryCreate(s, UriKind.Absolute, out var u)) return null;
            if (u.Scheme != "https" || !string.IsNullOrEmpty(u.UserInfo) || !string.IsNullOrEmpty(u.Query) || !string.IsNullOrEmpty(u.Fragment) || string.IsNullOrEmpty(u.Host)) return null;
            var origin = u.GetLeftPart(UriPartial.Authority);
            return origin + u.AbsolutePath.TrimEnd('/');
        }

        public static string DirectoryUrlFor(string directory) => directory + DirectoryPath;

        // ── signature base (RFC 9421 section 2.5) ───────────────────────────────

        private static string? HeaderValue(IDictionary<string, string> headers, string name)
        {
            foreach (var kv in headers) if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)) return kv.Value.Trim();
            return null;
        }

        private static string Esc(string v) => "\"" + v.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        private static string ComponentId(Component c)
        {
            var sb = new StringBuilder(Esc(c.Name));
            foreach (var kv in c.Params)
            {
                if (kv.Value is bool b) sb.Append(b ? ";" + kv.Key : ";" + kv.Key + "=?0");
                else if (kv.Value is string s) sb.Append(";" + kv.Key + "=" + Esc(s));
                else if (kv.Value is byte[] bytes) sb.Append(";" + kv.Key + "=:" + Convert.ToBase64String(bytes) + ":");
                else sb.Append(";" + kv.Key + "=" + Convert.ToString(kv.Value, System.Globalization.CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static string PercentEncode(string v)
            => Regex.Replace(Uri.EscapeDataString(v), "[!'()*]", m => "%" + ((int)m.Value[0]).ToString("X2"));

        private static string? ComponentValue(Component c, WebBotAuthRequest req, Uri url)
        {
            var name = c.Name;
            if (name == "@query-param")
            {
                if (c.Params.Count != 1 || !(c.Params.TryGetValue("name", out var pn) && pn is string qn)) return null;
                var found = url.Query.TrimStart('?').Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Split(new[] { '=' }, 2)).Where(p => Uri.UnescapeDataString(p[0].Replace('+', ' ')) == qn)
                    .Select(p => p.Length > 1 ? Uri.UnescapeDataString(p[1].Replace('+', ' ')) : "").ToList();
                return found.Count == 1 ? PercentEncode(found[0]) : null;
            }
            if (name.StartsWith("@"))
            {
                if (c.Params.Count > 0) return null;
                var path = string.IsNullOrEmpty(url.AbsolutePath) ? "/" : url.AbsolutePath;
                switch (name)
                {
                    case "@method": return req.Method.ToUpperInvariant();
                    case "@authority": return url.IsDefaultPort ? url.Host.ToLowerInvariant() : url.Host.ToLowerInvariant() + ":" + url.Port;
                    case "@path": return path;
                    case "@target-uri": return url.AbsoluteUri;
                    case "@scheme": return url.Scheme.ToLowerInvariant();
                    case "@query": return url.Query.Length == 0 ? "?" : url.Query;
                    case "@request-target": return path + url.Query;
                    default: return null;
                }
            }
            var raw = HeaderValue(req.Headers, name);
            if (raw == null) return null;
            if (c.Params.Count == 0) return raw;
            if (c.Params.Count != 1) return null;
            if (c.Params.ContainsKey("sf")) return null; // shortcut: see the class remarks
            if (c.Params.TryGetValue("key", out var kk) && kk is string key)
            {
                foreach (var m in SplitTop(raw, ','))
                    if (m.StartsWith(key + "=")) return m.Substring(key.Length + 1).Trim();
                return null;
            }
            if (c.Params.TryGetValue("bs", out var bs) && bs is true) return ":" + Convert.ToBase64String(Encoding.UTF8.GetBytes(raw)) + ":";
            return null;
        }

        private static (string? Base, WebBotAuthFailure Error) BuildBase(WebBotAuthRequest req, IReadOnlyList<Component> components, string signatureParams)
        {
            if (!Uri.TryCreate(req.Url, UriKind.Absolute, out var url)) return (null, WebBotAuthFailure.BadRequest);
            var seen = new HashSet<string>();
            var lines = new List<string>();
            foreach (var c in components)
            {
                if (c.Name != c.Name.ToLowerInvariant()) return (null, WebBotAuthFailure.UnsupportedComponent);
                if (c.Name.StartsWith("@") && !Derived.Contains(c.Name) && c.Name != "@query-param") return (null, WebBotAuthFailure.UnsupportedComponent);
                var id = ComponentId(c);
                if (!seen.Add(id)) return (null, WebBotAuthFailure.UnsupportedComponent);
                if (!c.Params.Keys.All(k => k == "sf" || k == "key" || k == "bs" || k == "name")) return (null, WebBotAuthFailure.UnsupportedComponent);
                if (c.Name.StartsWith("@") && c.Name != "@query-param" && c.Params.Count > 0) return (null, WebBotAuthFailure.UnsupportedComponent);
                var v = ComponentValue(c, req, url);
                if (v == null || v.IndexOfAny(new[] { '\r', '\n' }) >= 0) return (null, WebBotAuthFailure.ComponentMissing);
                lines.Add(id + ": " + v);
            }
            lines.Add("\"@signature-params\": " + signatureParams);
            return (string.Join("\n", lines), WebBotAuthFailure.None);
        }

        // ── verification ────────────────────────────────────────────────────────

        /// <summary>Verify the web-bot-auth signature on a request. Never throws on bad input.</summary>
        /// <param name="getKeys">Directory keys (<c>x</c> values) for a directory URL; <c>refresh</c> asks for a fresh copy. Null = unavailable.</param>
        /// <param name="claimNonce">Replay guard: true the first time a key is seen. Without it nonces are not tracked.</param>
        public static async Task<WebBotAuthResult> VerifyAsync(WebBotAuthRequest req, Func<string, bool, Task<IReadOnlyList<string>?>> getKeys,
            long? nowMs = null, int skewSeconds = DefaultSkewSeconds, int maxLifetimeSeconds = DefaultMaxLifetimeSeconds,
            Func<string, long, Task<bool>>? claimNonce = null)
        {
            WebBotAuthResult Fail(WebBotAuthFailure why, string? lbl = null, string? kid = null, long? cr = null, long? ex = null, string? dir = null)
                => new WebBotAuthResult { Signed = true, Reason = why, Label = lbl, KeyId = kid, Created = cr, Expires = ex, Directory = dir };
            var inputHeader = HeaderValue(req.Headers, "signature-input");
            var sigHeader = HeaderValue(req.Headers, "signature");
            if (inputHeader == null || sigHeader == null) return new WebBotAuthResult { Reason = WebBotAuthFailure.NoSignature };
            var inputs = ParseSignatureInput(inputHeader);
            var sigs = ParseSignatureHeader(sigHeader);
            if (inputs == null || sigs == null) return Fail(WebBotAuthFailure.Malformed);
            var input = inputs.FirstOrDefault(i => i.Params.TryGetValue("tag", out var t) && t is string ts && ts == Tag && sigs.ContainsKey(i.Label));
            if (input == null) return Fail(WebBotAuthFailure.NoWebBotAuthSignature);
            var signature = sigs[input.Label];
            var p = input.Params;
            p.TryGetValue("created", out var createdO); p.TryGetValue("expires", out var expiresO); p.TryGetValue("keyid", out var keyO); p.TryGetValue("nonce", out var nonceO);
            if (!(createdO is long created) || !(expiresO is long expires) || !(keyO is string keyId) || keyId.Length == 0 || keyId.Length > 128
                || (nonceO != null && (!(nonceO is string ns) || ns.Length > 512)))
                return Fail(WebBotAuthFailure.BadParams, input.Label);
            var nonce = nonceO as string;
            if (p.TryGetValue("alg", out var alg) && !(alg is string a && a == "ed25519")) return Fail(WebBotAuthFailure.UnsupportedAlgorithm, input.Label, keyId, created, expires);
            var nowSec = (nowMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 1000;
            if (expires <= created) return Fail(WebBotAuthFailure.BadParams, input.Label, keyId, created, expires);
            if (expires - created > maxLifetimeSeconds) return Fail(WebBotAuthFailure.LifetimeTooLong, input.Label, keyId, created, expires);
            if (created > nowSec + skewSeconds) return Fail(WebBotAuthFailure.NotYetValid, input.Label, keyId, created, expires);
            if (expires < nowSec - skewSeconds) return Fail(WebBotAuthFailure.Expired, input.Label, keyId, created, expires);

            // Coverage: the signature must bind the host, and the directory claim it relies on.
            var agentHeader = HeaderValue(req.Headers, "signature-agent");
            if (agentHeader == null) return Fail(WebBotAuthFailure.MissingSignatureAgent, input.Label, keyId, created, expires);
            var agentComponent = input.Components.FirstOrDefault(c => c.Name == "signature-agent");
            if (!input.Components.Any(c => c.Name == "@authority") || agentComponent == null) return Fail(WebBotAuthFailure.ComponentsNotCovered, input.Label, keyId, created, expires);
            // Covered as a dictionary member (;key="m"): the directory must come from that member, or an uncovered one could be swapped in.
            var agentKey = agentComponent.Params.TryGetValue("key", out var ak) ? ak as string : null;
            if (agentKey != null && !SplitTop(agentHeader, ',').Any(m => m.StartsWith(agentKey + "="))) return Fail(WebBotAuthFailure.BadSignatureAgent, input.Label, keyId, created, expires);
            var directory = ParseSignatureAgent(agentHeader, agentKey ?? input.Label);
            if (directory == null) return Fail(WebBotAuthFailure.BadSignatureAgent, input.Label, keyId, created, expires);

            var built = BuildBase(req, input.Components, input.Raw);
            if (built.Base == null) return Fail(built.Error, input.Label, keyId, created, expires, directory);

            var dirUrl = DirectoryUrlFor(directory);
            string? Find(IReadOnlyList<string>? keys) => keys?.FirstOrDefault(x => Ed25519JwkThumbprint(x) == keyId);
            var found = await getKeys(dirUrl, false).ConfigureAwait(false);
            var x0 = Find(found);
            if (x0 == null) { found = await getKeys(dirUrl, true).ConfigureAwait(false); x0 = Find(found); }
            if (x0 == null) return Fail(found == null ? WebBotAuthFailure.DirectoryUnavailable : WebBotAuthFailure.KeyNotFound, input.Label, keyId, created, expires, directory);

            var pub = FromB64U(x0);
            var ok = false;
            try { ok = pub != null && signature.Length == 64 && PublicKeyAuth.VerifyDetached(signature, Encoding.UTF8.GetBytes(built.Base), pub); }
            catch (Exception) { ok = false; }
            if (!ok) return Fail(WebBotAuthFailure.BadSignature, input.Label, keyId, created, expires, directory);

            if (claimNonce != null && nonce != null && !await claimNonce(directory + "|" + keyId + "|" + nonce, expires * 1000 + skewSeconds * 1000L).ConfigureAwait(false))
                return Fail(WebBotAuthFailure.Replayed, input.Label, keyId, created, expires, directory);
            return new WebBotAuthResult { Signed = true, Verified = true, Directory = directory, KeyId = keyId, Label = input.Label, Created = created, Expires = expires, Nonce = nonce };
        }

        // ── signing (for bots and tests) ────────────────────────────────────────

        /// <summary>The three headers (<c>signature-agent</c>, <c>signature-input</c>, <c>signature</c>) for a request. <paramref name="privateKeySeed"/> is the 32-byte Ed25519 seed.</summary>
        public static IDictionary<string, string> SignRequest(string method, string url, string signatureAgent, byte[] privateKeySeed, long created, long expires,
            string? nonce = null, string label = "sig1", IDictionary<string, string>? headers = null, IEnumerable<string>? components = null)
        {
            var names = (components ?? new[] { "@authority", "signature-agent" }).ToList();
            var pair = PublicKeyAuth.GenerateKeyPair(privateKeySeed);
            var keyId = Ed25519JwkThumbprint(B64U(pair.PublicKey));
            var agentHeader = "\"" + signatureAgent + "\"";
            var comps = names.Select(n => new Component { Name = n }).ToList();
            var parms = "(" + string.Join(" ", comps.Select(ComponentId)) + ");created=" + created + ";keyid=\"" + keyId + "\";alg=\"ed25519\";expires=" + expires
                + (nonce != null ? ";nonce=\"" + nonce + "\"" : "") + ";tag=\"" + Tag + "\"";
            var h = new Dictionary<string, string>(headers ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase) { ["signature-agent"] = agentHeader };
            var built = BuildBase(new WebBotAuthRequest { Method = method, Url = url, Headers = h }, comps, parms);
            if (built.Base == null) throw new InvalidOperationException("Cannot sign: " + built.Error + ".");
            var sig = PublicKeyAuth.SignDetached(Encoding.UTF8.GetBytes(built.Base), pair.PrivateKey);
            return new Dictionary<string, string>
            {
                ["signature-agent"] = agentHeader, ["signature-input"] = label + "=" + parms, ["signature"] = label + "=:" + Convert.ToBase64String(sig) + ":",
            };
        }

        // ── decision ────────────────────────────────────────────────────────────

        /// <summary>
        /// Turn a verification result into a decision. Only a VERIFIED directory can be allowed or blocked: an unsigned or failed request is
        /// <c>unknown</c>, never trusted and never attributed to the directory it claimed. A block entry beats an allow entry.
        /// </summary>
        public static WebBotAuthDecision Decide(WebBotAuthResult result, WebBotAuthPolicy? policy = null)
        {
            policy ??= new WebBotAuthPolicy();
            if (!result.Verified || result.Directory == null || result.KeyId == null) return new WebBotAuthDecision { Reason = result.Reason };
            WebBotAuthListEntry? Match(IEnumerable<WebBotAuthListEntry> l) => l.FirstOrDefault(e => NormalizeDirectory(e.Directory) == result.Directory);
            var blocked = Match(policy.Block);
            var allowed = blocked == null ? Match(policy.Allow) : null;
            var entry = blocked ?? allowed;
            var name = !string.IsNullOrWhiteSpace(entry?.Name) ? entry!.Name!.Trim() : new Uri(result.Directory).Host;
            return new WebBotAuthDecision
            {
                Verified = true, Agent = (result.Directory, result.KeyId, name),
                Tier = blocked != null ? "blocked" : allowed != null ? "allowed" : "verified",
                Decision = blocked != null ? "block" : allowed != null ? "allow" : "unknown",
            };
        }
    }

    /// <summary>
    /// A verifier that runs inside your own server: no call to Atlas, directories cached in memory (10 min; a failed fetch 1 min; a refresh at most
    /// once a minute so a bot cannot force a fetch per request). Directories are fetched over https only, never to a literal or single-label host,
    /// without following redirects, capped at 64 KB and 5 s. Pass an <see cref="HttpClient"/> with <c>AllowAutoRedirect = false</c> and an egress
    /// policy of your own (it must refuse private addresses itself).
    /// </summary>
    public sealed class WebBotAuthVerifier
    {
        private readonly HttpClient _http;
        private readonly Func<long> _now;
        private readonly WebBotAuthPolicy _policy;
        private readonly bool _rejectReplays;
        private readonly TimeSpan _ttl;
        private readonly TimeSpan _failureTtl = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _cooldown;
        private readonly Dictionary<string, (IReadOnlyList<string>? Keys, long At)> _cache = new Dictionary<string, (IReadOnlyList<string>?, long)>();
        private readonly Dictionary<string, long> _seen = new Dictionary<string, long>();

        public WebBotAuthVerifier(WebBotAuthPolicy? policy = null, HttpClient? http = null, Func<long>? nowMs = null, bool rejectReplays = false,
            TimeSpan? ttl = null, TimeSpan? refreshCooldown = null)
        {
            _policy = policy ?? new WebBotAuthPolicy();
            _http = http ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(5) };
            _now = nowMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _rejectReplays = rejectReplays;
            _ttl = ttl ?? TimeSpan.FromMinutes(10);
            _cooldown = refreshCooldown ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>Verify and decide. Never throws.</summary>
        public async Task<WebBotAuthDecision> VerifyAsync(WebBotAuthRequest request, CancellationToken ct = default)
        {
            var result = await WebBotAuth.VerifyAsync(request, (url, refresh) => KeysAsync(url, refresh, ct), _now(),
                claimNonce: _rejectReplays ? ClaimNonce : (Func<string, long, Task<bool>>?)null).ConfigureAwait(false);
            return WebBotAuth.Decide(result, _policy);
        }

        public void ClearCache() { lock (_cache) _cache.Clear(); }

        private Task<bool> ClaimNonce(string key, long expiresAtMs)
        {
            lock (_seen)
            {
                var t = _now();
                if (_seen.Count > 5000) foreach (var k in _seen.Where(kv => kv.Value <= t).Select(kv => kv.Key).ToList()) _seen.Remove(k);
                if (_seen.Count > 20_000) return Task.FromResult(false); // fail closed rather than grow without bound
                if (_seen.TryGetValue(key, out var prior) && prior > t) return Task.FromResult(false);
                _seen[key] = expiresAtMs;
                return Task.FromResult(true);
            }
        }

        private async Task<IReadOnlyList<string>?> KeysAsync(string url, bool refresh, CancellationToken ct)
        {
            var t = _now();
            (IReadOnlyList<string>? Keys, long At) hit;
            bool have;
            lock (_cache) have = _cache.TryGetValue(url, out hit);
            if (have)
            {
                var ttl = hit.Keys != null ? _ttl : _failureTtl;
                if (t - hit.At < (long)ttl.TotalMilliseconds && !(refresh && t - hit.At >= (long)_cooldown.TotalMilliseconds)) return hit.Keys;
            }
            IReadOnlyList<string>? keys = null;
            try
            {
                var u = new Uri(url);
                var host = u.Host.ToLowerInvariant();
                if (u.Scheme == "https" && !(host == "localhost" || host.EndsWith(".localhost") || host.Contains(":") || Regex.IsMatch(host, @"^\d{1,3}(\.\d{1,3}){3}$") || !host.Contains(".")))
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.TryAddWithoutValidation("Accept", WebBotAuth.DirectoryMediaType);
                    using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode && (response.Content.Headers.ContentLength ?? 0) <= 64 * 1024)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        if (bytes.Length <= 64 * 1024)
                        {
                            using var doc = JsonDocument.Parse(bytes);
                            var parsed = WebBotAuth.ParseDirectory(doc.RootElement);
                            keys = parsed.Count > 0 ? parsed : null;
                        }
                    }
                }
            }
            catch (Exception) when (!ct.IsCancellationRequested) { keys = null; }
            // A failed refresh keeps serving the last good copy rather than dropping a working directory.
            var kept = keys ?? (have ? hit.Keys : null);
            lock (_cache)
            {
                if (_cache.Count >= 1000 && !_cache.ContainsKey(url)) _cache.Remove(_cache.Keys.First());
                _cache[url] = (kept, t);
            }
            return kept;
        }
    }
}
