using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sodium;

namespace Atlas.Primitives
{
    /// <summary>What a machine keeps between runs. The private key never leaves it.</summary>
    public sealed class MachineIdentity
    {
        [JsonPropertyName("issuer")] public string Issuer { get; set; } = "";
        [JsonPropertyName("machine_id")] public string MachineId { get; set; } = "";
        /// <summary>PKCS#8 PEM of the Ed25519 private key.</summary>
        [JsonPropertyName("private_key_pem")] public string PrivateKeyPem { get; set; } = "";
        /// <summary>SPKI PEM of the matching public key (what enrolment registered).</summary>
        [JsonPropertyName("public_key_pem")] public string PublicKeyPem { get; set; } = "";
        [JsonPropertyName("enrolled_at")] public long EnrolledAt { get; set; }
    }

    /// <summary>Where the identity lives; implement your own to use a keychain or secret manager.</summary>
    public interface IMachineStore
    {
        Task<MachineIdentity?> LoadAsync();
        Task SaveAsync(MachineIdentity identity);
        Task ClearAsync();
    }

    /// <summary>A store backed by one file (written atomically; mode 0600 on Unix when the runtime supports it).</summary>
    public sealed class FileMachineStore : IMachineStore
    {
        private readonly string _path;
        public FileMachineStore(string path) => _path = path ?? throw new ArgumentNullException(nameof(path));

        public async Task<MachineIdentity?> LoadAsync()
        {
            try { return JsonSerializer.Deserialize<MachineIdentity>(await ReadAllAsync(_path).ConfigureAwait(false)); }
            catch (Exception e) when (e is IOException || e is JsonException || e is UnauthorizedAccessException) { return null; }
        }

        public Task SaveAsync(MachineIdentity identity)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(_path))!;
            Directory.CreateDirectory(dir);
            var tmp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(identity));
#if NET7_0_OR_GREATER
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(tmp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
#endif
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(tmp, _path);
            return Task.CompletedTask;
        }

        public Task ClearAsync() { if (File.Exists(_path)) File.Delete(_path); return Task.CompletedTask; }

        private static Task<string> ReadAllAsync(string path) => Task.FromResult(File.ReadAllText(path));
    }

    /// <summary>A store that forgets on exit (tests, short-lived jobs that enrol every run).</summary>
    public sealed class MemoryMachineStore : IMachineStore
    {
        private MachineIdentity? _value;
        public MemoryMachineStore(MachineIdentity? initial = null) => _value = initial;
        public Task<MachineIdentity?> LoadAsync() => Task.FromResult(_value);
        public Task SaveAsync(MachineIdentity identity) { _value = identity; return Task.CompletedTask; }
        public Task ClearAsync() { _value = null; return Task.CompletedTask; }
    }

    /// <summary>Atlas refused a request. <see cref="Code"/> is the machine-readable error code when the response had one.</summary>
    public sealed class MachineClientException : Exception
    {
        public MachineClientException(int status, string? code, string message) : base(message) { Status = status; Code = code; }
        public int Status { get; }
        public string? Code { get; }
    }

    public sealed class MachineClientOptions
    {
        /// <summary>Your instance's issuer URL: the <c>iss</c> of its tokens (for example <c>https://clerk.example.com</c>).</summary>
        public string Issuer { get; set; } = "";
        public IMachineStore Store { get; set; } = new MemoryMachineStore();
        public HttpClient? HttpClient { get; set; }
        /// <summary>Epoch ms.</summary>
        public Func<long>? Now { get; set; }
        /// <summary>Fetch a new token this long before the cached one expires. Default 60 s.</summary>
        public TimeSpan RefreshSkew { get; set; } = TimeSpan.FromSeconds(60);
        /// <summary>
        /// Bind every token to a DPoP key (RFC 9449): a P-256 key as PKCS#8 PEM, or <see cref="GenerateDpopKeyPem"/>'s output. Needed when the machine's
        /// token_binding is <c>dpop</c>. Send <see cref="MachineClient.DpopProof"/> with each call to your API.
        /// </summary>
        public string? DpopKeyPem { get; set; }
    }

    public sealed class MachineEnrollOptions
    {
        /// <summary>A <c>met_</c> enrolment token, or a short user code (<c>XXXX-XXXX</c>).</summary>
        public string Credential { get; set; } = "";
        public string Name { get; set; } = "";
        public IDictionary<string, object?>? Metadata { get; set; }
        /// <summary>A stable device identifier, so re-enrolling replaces this device's earlier machine.</summary>
        public string? DeviceKey { get; set; }
    }

    public sealed class MachineTokenRequest
    {
        /// <summary>The resource server (API) the token is for; becomes the token's <c>aud</c>.</summary>
        public string? Audience { get; set; }
        /// <summary>Scopes within the grant. Needs <see cref="Audience"/>.</summary>
        public IReadOnlyList<string>? Scopes { get; set; }
        public IReadOnlyList<string>? ScopePresets { get; set; }
        /// <summary>The organization context; defaults to the machine's own organization.</summary>
        public string? OrganizationId { get; set; }
    }

    public sealed class MachineToken
    {
        public string Token { get; init; } = "";
        /// <summary><c>DPoP</c> when bound to this client's key (send <c>Authorization: DPoP &lt;token&gt;</c> with a proof), else <c>Bearer</c>.</summary>
        public string TokenType { get; init; } = "Bearer";
        /// <summary>Epoch ms when the token stops being valid.</summary>
        public long ExpiresAt { get; init; }
        public string? Audience { get; init; }
        public string? Scope { get; init; }
        /// <summary>The grant's <c>constraints</c>, as the token carries them. Your API enforces them.</summary>
        public JsonElement? Constraints { get; init; }
        public string? OrganizationId { get; init; }
    }

    public sealed class MachineEnrollResult
    {
        public string MachineId { get; init; } = "";
        public string Status { get; init; } = "";
        /// <summary>True when an administrator must approve the machine before it can get tokens.</summary>
        public bool ApprovalRequired { get; init; }
    }

    /// <summary>
    /// The device side of machine enrolment, for a service or device of your own: generate an Ed25519 key, enrol with a token or a short code,
    /// then trade a signed challenge for short-lived machine tokens, cached until shortly before they expire (concurrent callers asking for the
    /// same audience, scopes and organization share one mint). The three calls are public: possession of the private key is the credential.
    /// </summary>
    public sealed class MachineClient
    {
        private static readonly byte[] Pkcs8Prefix = { 0x30, 0x2e, 0x02, 0x01, 0x00, 0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70, 0x04, 0x22, 0x04, 0x20 };
        private static readonly byte[] SpkiPrefix = { 0x30, 0x2a, 0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70, 0x03, 0x21, 0x00 };

        private readonly MachineClientOptions _o;
        private readonly string _issuer;
        private readonly HttpClient _http;
        private readonly Dictionary<string, MachineToken> _cache = new Dictionary<string, MachineToken>();
        private readonly Dictionary<string, Task<MachineToken>> _inflight = new Dictionary<string, Task<MachineToken>>();
        private readonly object _gate = new object();
        private readonly ECDsa? _dpop;

        public MachineClient(MachineClientOptions options)
        {
            _o = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.Issuer)) throw new ArgumentException("Issuer is required.", nameof(options));
            _issuer = options.Issuer.TrimEnd('/');
            _http = options.HttpClient ?? new HttpClient();
            if (options.DpopKeyPem != null)
            {
                _dpop = ECDsa.Create();
                _dpop.ImportPkcs8PrivateKey(PemBytes(options.DpopKeyPem), out _);
            }
        }

        private long Now() => _o.Now != null ? _o.Now() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // ── keys ────────────────────────────────────────────────────────────────

        /// <summary><c>base64url(sign(privateKey, nonce))</c>: pure Ed25519 over the nonce's UTF-8 bytes (no pre-hash, no context).</summary>
        public static string SignChallenge(string privateKeyPem, string nonce)
        {
            var der = PemBytes(privateKeyPem);
            if (der.Length != 48 || !der.Take(16).SequenceEqual(Pkcs8Prefix)) throw new ArgumentException("An Ed25519 PKCS#8 private key is required.", nameof(privateKeyPem));
            var seed = der.Skip(16).ToArray();
            var secret = PublicKeyAuth.GenerateKeyPair(seed).PrivateKey;
            return B64U(PublicKeyAuth.SignDetached(Encoding.UTF8.GetBytes(nonce), secret));
        }

        /// <summary>A fresh Ed25519 pair as (PKCS#8 private PEM, SPKI public PEM).</summary>
        public static (string PrivatePem, string PublicPem) GenerateKeyPair()
        {
            var seed = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(seed);
            var pair = PublicKeyAuth.GenerateKeyPair(seed);
            return (Pem("PRIVATE KEY", Pkcs8Prefix.Concat(seed).ToArray()), Pem("PUBLIC KEY", SpkiPrefix.Concat(pair.PublicKey).ToArray()));
        }

        /// <summary>A P-256 key (PKCS#8 PEM) for <see cref="MachineClientOptions.DpopKeyPem"/>.</summary>
        public static string GenerateDpopKeyPem()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return Pem("PRIVATE KEY", key.ExportPkcs8PrivateKey());
        }

        private static string Pem(string label, byte[] der)
            => "-----BEGIN " + label + "-----\n" + string.Join("\n", Chunks(Convert.ToBase64String(der), 64)) + "\n-----END " + label + "-----\n";

        private static IEnumerable<string> Chunks(string s, int n) { for (var i = 0; i < s.Length; i += n) yield return s.Substring(i, Math.Min(n, s.Length - i)); }

        private static byte[] PemBytes(string pem)
        {
            var body = string.Concat(pem.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("-----")));
            return Convert.FromBase64String(body);
        }

        private static string B64U(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // ── wire ────────────────────────────────────────────────────────────────

        private async Task<JsonElement> PostAsync(string path, Dictionary<string, object?> body, bool proof, CancellationToken ct)
        {
            var url = _issuer + path;
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            if (proof && _dpop != null) request.Headers.TryAddWithoutValidation("DPoP", Proof("POST", url, null, null));
            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
#if NET8_0_OR_GREATER
            var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
            JsonElement json = default;
            try { json = JsonDocument.Parse(text).RootElement.Clone(); } catch (JsonException) { /* a non-JSON error page: fall through to the status */ }
            if (!response.IsSuccessStatusCode)
            {
                string? code = null, message = null;
                if (json.ValueKind == JsonValueKind.Object)
                {
                    if (json.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0)
                    {
                        code = errs[0].TryGetProperty("code", out var c) ? c.GetString() : null;
                        message = errs[0].TryGetProperty("message", out var m) ? m.GetString() : null;
                    }
                    code ??= json.TryGetProperty("code", out var c2) && c2.ValueKind == JsonValueKind.String ? c2.GetString() : null;
                    message ??= json.TryGetProperty("message", out var m2) && m2.ValueKind == JsonValueKind.String ? m2.GetString() : null;
                }
                throw new MachineClientException((int)response.StatusCode, code, message ?? $"Request failed with status {(int)response.StatusCode}.");
            }
            return json;
        }

        private async Task<MachineIdentity?> IdentityAsync()
        {
            var i = await _o.Store.LoadAsync().ConfigureAwait(false);
            return i != null && i.Issuer == _issuer ? i : null;
        }

        /// <summary>Generate a key, enrol with a token or short code, and store the identity. Only the public half is sent.</summary>
        public async Task<MachineEnrollResult> EnrollAsync(MachineEnrollOptions options, CancellationToken ct = default)
        {
            var (priv, pub) = GenerateKeyPair();
            var body = new Dictionary<string, object?>
            {
                ["enrolment_token"] = options.Credential.Trim(), ["name"] = options.Name, ["public_key_pem"] = pub,
            };
            if (options.Metadata != null) body["metadata"] = options.Metadata;
            if (options.DeviceKey != null) body["device_key"] = options.DeviceKey;
            var res = await PostAsync("/v1/machines/enroll", body, false, ct).ConfigureAwait(false);
            string S(string n) => res.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
            await _o.Store.SaveAsync(new MachineIdentity
            {
                Issuer = _issuer, MachineId = S("id"), PrivateKeyPem = priv, PublicKeyPem = pub,
                EnrolledAt = res.TryGetProperty("enrolled_at", out var ea) && ea.ValueKind == JsonValueKind.Number ? ea.GetInt64() : Now(),
            }).ConfigureAwait(false);
            lock (_gate) _cache.Clear();
            return new MachineEnrollResult
            {
                MachineId = S("id"), Status = S("status"),
                ApprovalRequired = res.TryGetProperty("approval_required", out var ar) && ar.ValueKind == JsonValueKind.True,
            };
        }

        public async Task<bool> IsEnrolledAsync() => await IdentityAsync().ConfigureAwait(false) != null;

        public async Task<string?> MachineIdAsync() => (await IdentityAsync().ConfigureAwait(false))?.MachineId;

        /// <summary>A machine token for this request, from the cache while it has more than the refresh skew left.</summary>
        public Task<MachineToken> GetTokenAsync(MachineTokenRequest? request = null, CancellationToken ct = default)
        {
            request ??= new MachineTokenRequest();
            var key = JsonSerializer.Serialize(new object?[]
            {
                request.Audience ?? "", (request.Scopes ?? Array.Empty<string>()).OrderBy(s => s, StringComparer.Ordinal).ToArray(),
                (request.ScopePresets ?? Array.Empty<string>()).OrderBy(s => s, StringComparer.Ordinal).ToArray(), request.OrganizationId ?? "",
            });
            lock (_gate)
            {
                if (_cache.TryGetValue(key, out var hit) && hit.ExpiresAt - (long)_o.RefreshSkew.TotalMilliseconds > Now()) return Task.FromResult(hit);
                if (_inflight.TryGetValue(key, out var pending)) return pending;
                var run = MintAsync(request, key, ct);
                _inflight[key] = run;
                return run;
            }
        }

        private async Task<MachineToken> MintAsync(MachineTokenRequest req, string cacheKey, CancellationToken ct)
        {
            try
            {
                await Task.Yield(); // let GetTokenAsync release its lock before the first await runs on
                var id = await IdentityAsync().ConfigureAwait(false)
                    ?? throw new MachineClientException(0, "not_enrolled", "This machine is not enrolled. Call EnrollAsync() first.");
                var challenge = await PostAsync("/v1/machines/challenge", new Dictionary<string, object?> { ["machine_id"] = id.MachineId }, false, ct).ConfigureAwait(false);
                var nonce = challenge.GetProperty("nonce").GetString()!;
                var body = new Dictionary<string, object?>
                {
                    ["machine_id"] = id.MachineId, ["nonce"] = nonce, ["signature"] = SignChallenge(id.PrivateKeyPem, nonce),
                };
                if (req.Audience != null) body["audience"] = req.Audience;
                if (req.Scopes != null) body["scopes"] = req.Scopes;
                if (req.ScopePresets != null) body["scope_presets"] = req.ScopePresets;
                if (req.OrganizationId != null) body["organization_id"] = req.OrganizationId;
                var res = await PostAsync("/v1/machines/token", body, true, ct).ConfigureAwait(false);
                string? S(string n) => res.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                var token = new MachineToken
                {
                    Token = S("token") ?? "", TokenType = S("token_type") == "DPoP" ? "DPoP" : "Bearer",
                    ExpiresAt = Now() + (res.TryGetProperty("expires_in", out var ei) ? ei.GetInt64() : 0) * 1000,
                    Audience = S("audience"), Scope = S("scope"), OrganizationId = S("organization_id"),
                    Constraints = res.TryGetProperty("constraints", out var c) && c.ValueKind == JsonValueKind.Object ? c.Clone() : (JsonElement?)null,
                };
                lock (_gate) _cache[cacheKey] = token;
                return token;
            }
            finally { lock (_gate) _inflight.Remove(cacheKey); }
        }

        // ── DPoP ────────────────────────────────────────────────────────────────

        /// <summary>
        /// A DPoP proof for one call to your API with a token this client minted: <c>Authorization: DPoP &lt;token&gt;</c> plus <c>DPoP: &lt;proof&gt;</c>.
        /// Pass <paramref name="nonce"/> when the API answered <c>use_dpop_nonce</c>. Needs <see cref="MachineClientOptions.DpopKeyPem"/>.
        /// </summary>
        public string DpopProof(string token, string method, string url, string? nonce = null)
        {
            if (_dpop == null) throw new MachineClientException(0, "dpop_not_configured", "Create the client with DpopKeyPem to sign DPoP proofs.");
            return Proof(method, url, token, nonce);
        }

        /// <summary>The thumbprint (<c>cnf.jkt</c>) tokens minted by this client carry, or null without a DPoP key.</summary>
        public string? DpopThumbprint()
        {
            if (_dpop == null) return null;
            var p = _dpop.ExportParameters(false);
            var canonical = "{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\"" + B64U(p.Q.X!) + "\",\"y\":\"" + B64U(p.Q.Y!) + "\"}";
            using var sha = SHA256.Create();
            return B64U(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical)));
        }

        private string Proof(string method, string url, string? accessToken, string? nonce)
        {
            var p = _dpop!.ExportParameters(false);
            var u = new Uri(url);
            var htu = u.GetLeftPart(UriPartial.Path);
            var header = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["typ"] = "dpop+jwt", ["alg"] = "ES256",
                ["jwk"] = new Dictionary<string, object?> { ["kty"] = "EC", ["crv"] = "P-256", ["x"] = B64U(p.Q.X!), ["y"] = B64U(p.Q.Y!) },
            });
            var claims = new Dictionary<string, object?>
            {
                ["jti"] = Guid.NewGuid().ToString("N"), ["htm"] = method.ToUpperInvariant(), ["htu"] = htu, ["iat"] = Now() / 1000,
            };
            if (accessToken != null)
            {
                using var sha = SHA256.Create();
                claims["ath"] = B64U(sha.ComputeHash(Encoding.ASCII.GetBytes(accessToken)));
            }
            if (nonce != null) claims["nonce"] = nonce;
            var signingInput = B64U(Encoding.UTF8.GetBytes(header)) + "." + B64U(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(claims)));
            return signingInput + "." + B64U(_dpop.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256));
        }

        /// <summary>Forget cached tokens (the stored identity stays).</summary>
        public void ClearTokens() { lock (_gate) _cache.Clear(); }

        /// <summary>Forget the identity too: the machine must enrol again.</summary>
        public async Task ResetAsync()
        {
            ClearTokens();
            await _o.Store.ClearAsync().ConfigureAwait(false);
        }
    }
}
