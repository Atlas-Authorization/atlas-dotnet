using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Resources;

namespace Atlas.Primitives
{
    /// <summary>What the mirror keeps per end-user API key. Never the secret: only its SHA-256 (lowercase hex).</summary>
    public sealed record MirroredApiKey
    {
        /// <summary>The Atlas key id (<c>eak_...</c>).</summary>
        public string Id { get; init; } = "";
        public string Hash { get; init; } = "";
        /// <summary>The public head, for listings.</summary>
        public string Prefix { get; init; } = "";
        public string SubjectType { get; init; } = "";
        public string SubjectId { get; init; } = "";
        public string? Name { get; init; }
        public IReadOnlyList<string>? Scopes { get; init; }
        /// <summary>CIDR allowlist; null = any caller.</summary>
        public IReadOnlyList<string>? AllowedIps { get; init; }
        /// <summary>Epoch ms; null = never.</summary>
        public long? ExpiresAt { get; init; }
        /// <summary>Epoch ms; null = live.</summary>
        public long? RevokedAt { get; init; }
        /// <summary>The organization a user key is bound to; null = unbound.</summary>
        public string? OrganizationId { get; init; }
        public string? RevokedReason { get; init; }
    }

    /// <summary>The smallest store that works. <see cref="InMemoryKeyMirrorStore"/> is for tests and single-process apps; back it with a table in production.</summary>
    public interface IKeyMirrorStore
    {
        Task PutAsync(MirroredApiKey key);
        Task<MirroredApiKey?> GetAsync(string id);
        Task<MirroredApiKey?> GetByHashAsync(string hash);
        /// <summary>Stamp a key revoked. A no-op for an id the mirror does not hold.</summary>
        Task MarkRevokedAsync(string id, long revokedAt, string? reason = null);
        /// <summary>Stamp every live key of <paramref name="userId"/> bound to <paramref name="organizationId"/> revoked (reason <c>membership_ended</c>).</summary>
        Task MarkMembershipEndedAsync(string userId, string organizationId, long revokedAt);
        Task<string?> GetCursorAsync();
        Task SetCursorAsync(string cursor);
        Task<long?> GetLastSyncAtAsync();
        Task SetLastSyncAtAsync(long at);
    }

    public sealed class InMemoryKeyMirrorStore : IKeyMirrorStore
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, MirroredApiKey> _keys = new Dictionary<string, MirroredApiKey>();
        private string? _cursor;
        private long? _syncAt;

        public IReadOnlyCollection<MirroredApiKey> Keys { get { lock (_gate) return _keys.Values.ToList(); } }

        public Task PutAsync(MirroredApiKey key) { lock (_gate) _keys[key.Id] = key; return Task.CompletedTask; }
        public Task<MirroredApiKey?> GetAsync(string id) { lock (_gate) return Task.FromResult(_keys.TryGetValue(id, out var k) ? k : null); }
        public Task<MirroredApiKey?> GetByHashAsync(string hash) { lock (_gate) return Task.FromResult(_keys.Values.FirstOrDefault(k => k.Hash == hash)); }

        public Task MarkRevokedAsync(string id, long revokedAt, string? reason = null)
        {
            lock (_gate)
                if (_keys.TryGetValue(id, out var k) && k.RevokedAt == null)
                    _keys[id] = k with { RevokedAt = revokedAt, RevokedReason = reason ?? k.RevokedReason };
            return Task.CompletedTask;
        }

        public Task MarkMembershipEndedAsync(string userId, string organizationId, long revokedAt)
        {
            lock (_gate)
                foreach (var k in _keys.Values.Where(k => k.RevokedAt == null && k.SubjectType == "user" && k.SubjectId == userId && k.OrganizationId == organizationId).ToList())
                    _keys[k.Id] = k with { RevokedAt = revokedAt, RevokedReason = "membership_ended" };
            return Task.CompletedTask;
        }

        public Task<string?> GetCursorAsync() { lock (_gate) return Task.FromResult(_cursor); }
        public Task SetCursorAsync(string cursor) { lock (_gate) _cursor = cursor; return Task.CompletedTask; }
        public Task<long?> GetLastSyncAtAsync() { lock (_gate) return Task.FromResult(_syncAt); }
        public Task SetLastSyncAtAsync(long at) { lock (_gate) _syncAt = at; return Task.CompletedTask; }
    }

    /// <summary>The Atlas calls the mirror needs. <see cref="KeyMirrorApi.From"/> adapts an <see cref="AtlasClient"/>.</summary>
    public interface IKeyMirrorApi
    {
        Task<ApiKeyWithSecret> CreateAsync(CreateApiKeyBody body, string? idempotencyKey, CancellationToken ct);
        Task DeleteAsync(string id, CancellationToken ct);
        /// <summary>One page of the revocation feed: (id, revokedAt) pairs, whether there are more, and the next cursor.</summary>
        Task<(IReadOnlyList<(string Id, long? RevokedAt, string? Reason)> Data, bool HasMore, string? NextCursor)> ListRevokedAsync(string? since, CancellationToken ct);
        /// <summary>Optional: ask Atlas to verify a secret (for keys imported with a bcrypt / argon2id hash, and for keys bound to an organization).</summary>
        Task<ApiKeyVerification?> VerifyAsync(string secret, string? clientIp, CancellationToken ct);
        Task<ApiKey?> GetAsync(string id, CancellationToken ct);
    }

    public static class KeyMirrorApi
    {
        public static IKeyMirrorApi From(AtlasClient client) => new ClientApi(client.ApiKeys);

        private sealed class ClientApi : IKeyMirrorApi
        {
            private readonly ApiKeysResource _keys;
            public ClientApi(ApiKeysResource keys) => _keys = keys;
            public Task<ApiKeyWithSecret> CreateAsync(CreateApiKeyBody body, string? idempotencyKey, CancellationToken ct) => _keys.CreateAsync(body, idempotencyKey, ct);
            public Task DeleteAsync(string id, CancellationToken ct) => _keys.DeleteAsync(id, ct);
            public async Task<(IReadOnlyList<(string Id, long? RevokedAt, string? Reason)> Data, bool HasMore, string? NextCursor)> ListRevokedAsync(string? since, CancellationToken ct)
            {
                var page = await _keys.ListRevokedPageAsync(since, 100, null, ct).ConfigureAwait(false);
                return (page.Data.Select(r => (r.Id, r.RevokedAt, r.RevokedReason)).ToList(), page.HasMore, page.NextCursor);
            }
            public async Task<ApiKeyVerification?> VerifyAsync(string secret, string? clientIp, CancellationToken ct) => await _keys.VerifyAsync(secret, ct).ConfigureAwait(false);
            public async Task<ApiKey?> GetAsync(string id, CancellationToken ct) => await _keys.GetAsync(id, ct).ConfigureAwait(false);
        }
    }

    public sealed class KeyMirrorOptions
    {
        public IKeyMirrorStore Store { get; set; } = new InMemoryKeyMirrorStore();
        public IKeyMirrorApi Api { get; set; } = null!;
        /// <summary>The instance scope implications (<c>scopeImplications</c>), so a local required-scope check matches Atlas verify. <c>*</c> = every scope in <see cref="AllowedScopes"/> plus both sides of the map.</summary>
        public IDictionary<string, string[]> ScopeImplications { get; set; } = new Dictionary<string, string[]>();
        public IList<string> AllowedScopes { get; set; } = new List<string>();
        /// <summary>On a local miss, ask Atlas to verify the secret. Required for keys imported with a bcrypt / argon2id hash; once Atlas accepts one, the mirror stores its SHA-256 and verifies locally from then on.</summary>
        public bool VerifyAtAtlas { get; set; }
        /// <summary>
        /// Freshness bound for the revocation feed: when set, <see cref="KeyMirror.VerifyAsync"/> answers <c>stale</c> if the mirror has never
        /// completed a <c>SyncAsync</c> or the last one finished longer ago than this (a key revoked at Atlas may not have reached it).
        /// Leave null for webhook-only mirrors.
        /// </summary>
        public TimeSpan? MaxStaleness { get; set; }
        /// <summary>Keep answering from the last synced data for this much longer after <see cref="MaxStaleness"/>; such an answer carries <c>Stale = true</c>.</summary>
        public TimeSpan ServeStaleFor { get; set; } = TimeSpan.Zero;
        public Action<TimeSpan>? OnServeStale { get; set; }
        /// <summary>For a key bound to an organization: the user's CURRENT membership permissions from your own data (null when no longer a member).</summary>
        public Func<string, string, Task<IReadOnlyList<string>?>>? Membership { get; set; }
        public Func<long>? Now { get; set; }
    }

    public enum KeyMirrorCode { None, InsufficientScope, IpNotAllowed, ClientIpRequired, MembershipUnresolved, Stale }

    public sealed class MirrorVerification
    {
        public bool Valid { get; init; }
        public KeyMirrorCode Code { get; init; }
        public MirroredApiKey? Key { get; init; }
        public IReadOnlyList<string> EffectiveScopes { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> MissingScopes { get; init; } = Array.Empty<string>();
        /// <summary>True when the mirror was past <c>MaxStaleness</c> and this answer came from its last synced data.</summary>
        public bool Stale { get; init; }
        public long? LastSyncAt { get; init; }
        public IReadOnlyList<string>? OrganizationPermissions { get; init; }
    }

    /// <summary>
    /// A local, authoritative mirror of end-user API keys (<c>ak_</c>) with Atlas as the minting and revocation backstop. A presented key is
    /// verified against the LOCAL row (SHA-256 hash, scopes, expiry, revocation, caller address) with no network call; Atlas mints and imports
    /// keys, and a revoke made anywhere else reaches the mirror through <see cref="SyncAsync"/> (the <c>/v1/api_keys/revoked</c> feed) or
    /// <see cref="ApplyWebhookAsync"/> (the <c>api_key.*</c> webhooks). Revoking locally happens FIRST, so a failed Atlas call never leaves
    /// a key usable here.
    /// </summary>
    public sealed class KeyMirror
    {
        private readonly KeyMirrorOptions _o;
        private long? _memorySyncAt;

        public KeyMirror(KeyMirrorOptions options)
        {
            _o = options ?? throw new ArgumentNullException(nameof(options));
            if (options.Api == null) throw new ArgumentException("Api is required (KeyMirrorApi.From(client)).", nameof(options));
        }

        private long Now() => _o.Now != null ? _o.Now() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>SHA-256 hex: the hash the mirror stores and Atlas imports as <c>sha256_hex</c>.</summary>
        public static string HashSecret(string secret)
        {
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(secret)).Select(b => b.ToString("x2")));
        }

        private static MirroredApiKey ToMirrored(ApiKey k, string hash) => new MirroredApiKey
        {
            Id = k.Id, Hash = hash, Prefix = k.Prefix, SubjectType = k.SubjectType, SubjectId = k.SubjectId, Name = k.Name,
            Scopes = k.Scopes, AllowedIps = k.AllowedIps, ExpiresAt = k.ExpiresAt, RevokedAt = k.RevokedAt,
            OrganizationId = k.OrganizationId, RevokedReason = k.RevokedReason,
        };

        private static MirroredApiKey ToMirrored(ApiKeyWithSecret k, string hash) => new MirroredApiKey
        {
            Id = k.Id, Hash = hash, Prefix = k.Prefix, SubjectType = k.SubjectType, SubjectId = k.SubjectId, Name = k.Name,
            ExpiresAt = k.ExpiresAt, RevokedAt = k.RevokedAt,
        };

        /// <summary>Mint at Atlas, then mirror it. Resolves with the one-time secret.</summary>
        public async Task<ApiKeyWithSecret> MintAsync(CreateApiKeyBody body, string? idempotencyKey = null, CancellationToken ct = default)
        {
            var created = await _o.Api.CreateAsync(body, idempotencyKey, ct).ConfigureAwait(false);
            var m = ToMirrored(created, HashSecret(created.Secret));
            await _o.Store.PutAsync(m with { Scopes = body.Scopes ?? m.Scopes }).ConfigureAwait(false);
            return created;
        }

        /// <summary>Import an existing key by its SHA-256 (hex) so customers keep their secret: Atlas stores the hash, the mirror stores the same hash. The mirror row is written here; call Atlas's import with the same hash.</summary>
        public Task RememberImportedAsync(ApiKey imported, string secretOrHash, bool isHash = false)
        {
            var hash = isHash ? secretOrHash.ToLowerInvariant() : HashSecret(secretOrHash);
            if (hash.Length != 64 || hash.Any(c => !Uri.IsHexDigit(c) || char.IsUpper(c))) throw new ArgumentException("A SHA-256 hash is 64 lowercase hex characters.", nameof(secretOrHash));
            return _o.Store.PutAsync(ToMirrored(imported, hash));
        }

        /// <summary>
        /// Revoke LOCALLY first (the key stops working here at once), then at Atlas. A key Atlas already revoked or no longer knows (404/409)
        /// counts as revoked; any other Atlas error is thrown after the local revoke, so retry it.
        /// </summary>
        public async Task RevokeAsync(string id, CancellationToken ct = default)
        {
            await _o.Store.MarkRevokedAsync(id, Now()).ConfigureAwait(false);
            try { await _o.Api.DeleteAsync(id, ct).ConfigureAwait(false); }
            catch (AtlasException e) when (e.Status == 404 || e.Status == 409) { }
        }

        public async Task<long?> LastSyncAtAsync() => _o.Store != null ? await _o.Store.GetLastSyncAtAsync().ConfigureAwait(false) ?? _memorySyncAt : _memorySyncAt;

        /// <summary>
        /// Verify a presented secret against the mirror: no network call. Unknown, revoked and expired keys all answer invalid; a mirror that
        /// is not fresh (see <see cref="KeyMirrorOptions.MaxStaleness"/>) answers <see cref="KeyMirrorCode.Stale"/> instead.
        /// </summary>
        public async Task<MirrorVerification> VerifyAsync(string secret, IEnumerable<string>? requiredScopes = null, string? clientIp = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(secret)) return new MirrorVerification();
            var servedStale = false;
            if (_o.MaxStaleness != null)
            {
                var last = await LastSyncAtAsync().ConfigureAwait(false);
                var behind = last == null ? long.MaxValue : Now() - last.Value - (long)_o.MaxStaleness.Value.TotalMilliseconds;
                if (behind > 0)
                {
                    if (last == null || behind > (long)_o.ServeStaleFor.TotalMilliseconds) return new MirrorVerification { Code = KeyMirrorCode.Stale, LastSyncAt = last };
                    servedStale = true;
                    _o.OnServeStale?.Invoke(TimeSpan.FromMilliseconds(behind));
                }
            }
            var hash = HashSecret(secret);
            var key = await _o.Store.GetByHashAsync(hash).ConfigureAwait(false);
            if (key == null && _o.VerifyAtAtlas)
            {
                var remote = await _o.Api.VerifyAsync(secret, clientIp, ct).ConfigureAwait(false);
                if (remote is { Valid: true, Id: { } rid })
                {
                    var full = await _o.Api.GetAsync(rid, ct).ConfigureAwait(false);
                    if (full != null) { key = ToMirrored(full, hash); await _o.Store.PutAsync(key).ConfigureAwait(false); }
                }
            }
            if (key == null || key.RevokedAt != null) return new MirrorVerification();
            if (key.ExpiresAt != null && key.ExpiresAt.Value <= Now()) return new MirrorVerification();
            // A key with allowed_ips needs the end caller's address; no address fails closed.
            if (key.AllowedIps is { Count: > 0 })
            {
                if (string.IsNullOrEmpty(clientIp)) return new MirrorVerification { Code = KeyMirrorCode.ClientIpRequired };
                if (!IpInAllowlist(clientIp!, key.AllowedIps)) return new MirrorVerification { Code = KeyMirrorCode.IpNotAllowed };
            }
            var effective = Expand(key.Scopes ?? Array.Empty<string>());
            IReadOnlyList<string>? orgPerms = null;
            if (key.SubjectType == "user" && key.OrganizationId != null)
            {
                // A bound key stands for the user's CURRENT membership; never answer from the key's own scopes.
                if (_o.Membership != null)
                {
                    orgPerms = await _o.Membership(key.SubjectId, key.OrganizationId).ConfigureAwait(false);
                    if (orgPerms == null) return new MirrorVerification();
                    effective = IntersectScopesWithPermissions(effective, orgPerms);
                }
                else
                {
                    var remote = await _o.Api.VerifyAsync(secret, clientIp, ct).ConfigureAwait(false);
                    if (remote == null) return new MirrorVerification { Code = KeyMirrorCode.MembershipUnresolved };
                    if (!remote.Valid) return new MirrorVerification();
                    effective = remote.EffectiveScopes ?? new List<string>();
                }
            }
            var missing = (requiredScopes ?? Array.Empty<string>()).Where(s => !effective.Contains(s)).ToList();
            if (missing.Count > 0) return new MirrorVerification { Code = KeyMirrorCode.InsufficientScope, MissingScopes = missing };
            return new MirrorVerification { Valid = true, Key = key, EffectiveScopes = effective, Stale = servedStale, OrganizationPermissions = orgPerms };
        }

        /// <summary>
        /// Consume the revocation feed from the stored cursor, revoking each listed key locally. Call it on a timer (every few seconds to a
        /// minute): a key revoked at Atlas is rejected here within one tick. Returns how many ids it applied.
        /// </summary>
        public async Task<int> SyncAsync(CancellationToken ct = default)
        {
            var since = await _o.Store.GetCursorAsync().ConfigureAwait(false);
            var revoked = 0;
            while (true)
            {
                var page = await _o.Api.ListRevokedAsync(since, ct).ConfigureAwait(false);
                foreach (var r in page.Data)
                {
                    await _o.Store.MarkRevokedAsync(r.Id, r.RevokedAt ?? Now(), r.Reason).ConfigureAwait(false);
                    revoked++;
                }
                if (page.NextCursor != null)
                {
                    since = page.NextCursor;
                    await _o.Store.SetCursorAsync(since).ConfigureAwait(false);
                }
                if (!page.HasMore || page.NextCursor == null)
                {
                    _memorySyncAt = Now();
                    await _o.Store.SetLastSyncAtAsync(_memorySyncAt.Value).ConfigureAwait(false);
                    return revoked;
                }
            }
        }

        /// <summary>
        /// Apply a VERIFIED <c>api_key.*</c> webhook event (<c>type</c> and <c>data</c>; check the signature first). <c>api_key.revoked</c>
        /// revokes locally; <c>api_key.restored</c> lifts a revocation a ban caused; <c>api_key.updated</c> refreshes name, scopes and expiry;
        /// <c>organization_membership.deleted</c> drops the keys bound to that membership. A key the mirror does not hold is ignored.
        /// Returns whether the event changed the mirror.
        /// </summary>
        public async Task<bool> ApplyWebhookAsync(string type, JsonElement data)
        {
            string? S(string n) => data.ValueKind == JsonValueKind.Object && data.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            long? L(string n) => data.ValueKind == JsonValueKind.Object && data.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : (long?)null;
            if (type == "organization_membership.deleted" || type == "organizationMembership.deleted")
            {
                if (S("user_id") is { } u && S("organization_id") is { } org) { await _o.Store.MarkMembershipEndedAsync(u, org, Now()).ConfigureAwait(false); return true; }
                return false;
            }
            if (!type.StartsWith("api_key.") || data.ValueKind != JsonValueKind.Object) return false;
            if (S("id") is not { } id) return false;
            var held = await _o.Store.GetAsync(id).ConfigureAwait(false);
            if (held == null) return false;
            if (type == "api_key.restored")
            {
                if (held.RevokedAt == null) return false;
                await _o.Store.PutAsync(held with { RevokedAt = null, RevokedReason = null, ExpiresAt = data.TryGetProperty("expires_at", out _) ? L("expires_at") : held.ExpiresAt }).ConfigureAwait(false);
                return true;
            }
            if (type == "api_key.revoked" || (L("revoked_at") != null && held.RevokedAt == null))
            {
                await _o.Store.MarkRevokedAsync(held.Id, L("revoked_at") ?? Now(), S("revoked_reason")).ConfigureAwait(false);
                return true;
            }
            if (type == "api_key.updated" || type == "api_key.created")
            {
                await _o.Store.PutAsync(held with
                {
                    Name = data.TryGetProperty("name", out _) ? S("name") : held.Name,
                    Scopes = data.TryGetProperty("scopes", out var sc) && sc.ValueKind == JsonValueKind.Array
                        ? sc.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList() : held.Scopes,
                    AllowedIps = data.TryGetProperty("allowed_ips", out var ips) && ips.ValueKind == JsonValueKind.Array
                        ? ips.EnumerateArray().Select(x => x.GetString()!).ToList() : held.AllowedIps,
                    ExpiresAt = data.TryGetProperty("expires_at", out _) ? L("expires_at") : held.ExpiresAt,
                }).ConfigureAwait(false);
                return true;
            }
            return false;
        }

        private IReadOnlyList<string> Expand(IReadOnlyList<string> held)
        {
            var vocab = new HashSet<string>(_o.AllowedScopes);
            foreach (var kv in _o.ScopeImplications)
            {
                vocab.Add(kv.Key);
                foreach (var t in kv.Value) if (t != "*") vocab.Add(t);
            }
            var output = new HashSet<string>(held);
            var queue = new Stack<string>(held);
            while (queue.Count > 0)
            {
                if (!_o.ScopeImplications.TryGetValue(queue.Pop(), out var tos)) continue;
                foreach (var to in tos)
                    foreach (var t in to == "*" ? vocab.ToArray() : new[] { to })
                        if (output.Add(t)) queue.Push(t);
            }
            return output.OrderBy(s => s, StringComparer.Ordinal).ToList();
        }

        /// <summary>Whether a permission covers a key scope: same name, <c>*</c>, or <c>prefix:*</c>. Mirrors Atlas verify.</summary>
        public static bool PermissionCoversScope(string permission, string scope)
            => permission == scope || permission == "*" || (permission.EndsWith(":*") && scope.StartsWith(permission.Substring(0, permission.Length - 1)));

        /// <summary>The key scopes a membership's permissions cover: what Atlas returns as <c>effective_scopes</c> for an organization-bound key. Sorted.</summary>
        public static IReadOnlyList<string> IntersectScopesWithPermissions(IEnumerable<string> scopes, IEnumerable<string> permissions)
        {
            var perms = permissions.ToList();
            return scopes.Where(s => perms.Any(p => PermissionCoversScope(p, s))).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        }

        /// <summary>Whether <paramref name="ip"/> is inside one of the addresses / CIDR ranges. Unparseable input is never inside.</summary>
        public static bool IpInAllowlist(string ip, IEnumerable<string> cidrs)
        {
            if (!IPAddress.TryParse(ip.Trim(), out var addr)) return false;
            var a = Normalize(addr).GetAddressBytes();
            foreach (var entry in cidrs)
            {
                var parts = entry.Trim().Split('/');
                if (!IPAddress.TryParse(parts[0], out var baseIp)) continue;
                var net = Normalize(baseIp).GetAddressBytes();
                if (net.Length != a.Length) continue;
                var bits = parts.Length > 1 ? (int.TryParse(parts[1], out var b) ? b : -1) : net.Length * 8;
                if (bits < 0 || bits > net.Length * 8) continue;
                var ok = true;
                for (var i = 0; i < net.Length && ok; i++)
                {
                    var n = Math.Min(8, Math.Max(0, bits - i * 8));
                    var mask = (0xff << (8 - n)) & 0xff;
                    ok = (a[i] & mask) == (net[i] & mask);
                }
                if (ok) return true;
            }
            return false;
        }

        private static IPAddress Normalize(IPAddress ip) => ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
    }
}
