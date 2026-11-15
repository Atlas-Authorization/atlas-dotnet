using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Verification;

namespace Atlas.Primitives
{
    /// <summary>
    /// Where seen delivery ids live. <see cref="SetIfAbsentAsync"/> must be atomic: it stores <c>key</c> for <c>ttl</c> and answers
    /// true when the key was NOT there, false when it already was (Redis: <c>SET key 1 EX ttl NX</c>). <see cref="DeleteAsync"/> lets a
    /// failed handler release the id so Atlas's retry is processed.
    /// </summary>
    public interface IWebhookDedupeStore
    {
        Task<bool> SetIfAbsentAsync(string key, TimeSpan ttl, CancellationToken ct = default);
        Task DeleteAsync(string key, CancellationToken ct = default);
    }

    /// <summary>A single-process store. Use a shared one (Redis, <see cref="DbWebhookDedupeStore"/>) when you run more than one instance.</summary>
    public sealed class MemoryWebhookDedupeStore : IWebhookDedupeStore
    {
        private readonly Dictionary<string, long> _seen = new Dictionary<string, long>();
        private readonly Func<long> _now;

        public MemoryWebhookDedupeStore(Func<long>? nowMs = null) => _now = nowMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        public Task<bool> SetIfAbsentAsync(string key, TimeSpan ttl, CancellationToken ct = default)
        {
            lock (_seen)
            {
                var t = _now();
                foreach (var k in _seen.Where(kv => kv.Value <= t).Select(kv => kv.Key).ToList()) _seen.Remove(k);
                if (_seen.ContainsKey(key)) return Task.FromResult(false);
                _seen[key] = t + (long)ttl.TotalMilliseconds;
                return Task.FromResult(true);
            }
        }

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            lock (_seen) _seen.Remove(key);
            return Task.CompletedTask;
        }
    }

    public enum WebhookDedupeDialect { Postgres, Sqlite, MySql }

    /// <summary>
    /// A <see cref="IWebhookDedupeStore"/> on SQL through any ADO.NET provider (Npgsql, Microsoft.Data.Sqlite, MySqlConnector). Create the
    /// table once (<c>expires_at</c> is epoch milliseconds):
    /// <code>
    /// create table atlas_webhook_seen (key text primary key, expires_at bigint not null);              -- postgres, sqlite
    /// create table atlas_webhook_seen (`key` varchar(255) primary key, expires_at bigint not null);    -- mysql
    /// </code>
    /// A key whose row has expired counts as unseen and is claimed again.
    /// </summary>
    public sealed class DbWebhookDedupeStore : IWebhookDedupeStore
    {
        private readonly Func<DbConnection> _open;
        private readonly WebhookDedupeDialect _dialect;
        private readonly Func<long> _now;

        /// <param name="open">Returns a NEW connection (the store opens and disposes it per call).</param>
        public DbWebhookDedupeStore(Func<DbConnection> open, WebhookDedupeDialect dialect = WebhookDedupeDialect.Postgres, Func<long>? nowMs = null)
        {
            _open = open ?? throw new ArgumentNullException(nameof(open));
            _dialect = dialect;
            _now = nowMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        public async Task<bool> SetIfAbsentAsync(string key, TimeSpan ttl, CancellationToken ct = default)
        {
            var now = _now();
            var exp = now + (long)ttl.TotalMilliseconds;
            using var conn = _open();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct).ConfigureAwait(false);
            if (_dialect == WebhookDedupeDialect.MySql)
            {
                await Run(conn, "delete from atlas_webhook_seen where `key` = @key and expires_at <= @now", key, exp, now, ct).ConfigureAwait(false);
                return await Run(conn, "insert ignore into atlas_webhook_seen (`key`, expires_at) values (@key, @exp)", key, exp, now, ct).ConfigureAwait(false) > 0;
            }
            // Postgres and SQLite share this upsert: a live row blocks it, an expired one is taken over.
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "insert into atlas_webhook_seen (key, expires_at) values (@key, @exp) " +
                "on conflict (key) do update set expires_at = excluded.expires_at where atlas_webhook_seen.expires_at <= @now returning key";
            Add(cmd, "@key", key); Add(cmd, "@exp", exp); Add(cmd, "@now", now);
            return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) != null;
        }

        public async Task DeleteAsync(string key, CancellationToken ct = default)
        {
            using var conn = _open();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct).ConfigureAwait(false);
            await Run(conn, _dialect == WebhookDedupeDialect.MySql ? "delete from atlas_webhook_seen where `key` = @key" : "delete from atlas_webhook_seen where key = @key", key, 0, 0, ct).ConfigureAwait(false);
        }

        private static async Task<int> Run(DbConnection conn, string sql, string key, long exp, long now, CancellationToken ct)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            Add(cmd, "@key", key);
            if (sql.Contains("@exp")) Add(cmd, "@exp", exp);
            if (sql.Contains("@now")) Add(cmd, "@now", now);
            return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        private static void Add(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }

    /// <summary>A delivery that does not verify (answer 400/401). An unverified delivery never touches the store.</summary>
    public sealed class WebhookVerificationException : Exception
    {
        public WebhookVerificationException(string reason) : base("Atlas webhook verification failed: " + reason) => Reason = reason;
        public string Reason { get; }
    }

    public sealed class DedupedWebhook
    {
        /// <summary>The parsed event envelope <c>{ id, type, timestamp, instance_id, data }</c>.</summary>
        public JsonElement Event { get; init; }
        /// <summary>True when this delivery id was already seen: acknowledge with 2xx and do nothing.</summary>
        public bool Duplicate { get; init; }
        /// <summary>The signed delivery id, identical on every retry of one event.</summary>
        public string DeliveryId { get; init; } = "";
        /// <summary>Forget this delivery so Atlas's retry is processed, for example after your handler failed.</summary>
        public Func<Task> Release { get; init; } = () => Task.CompletedTask;
    }

    /// <summary>
    /// Verify an Atlas webhook (signature and timestamp, exactly as <see cref="WebhookVerifier"/>) and drop an event whose delivery id was
    /// already seen.
    /// <code>
    /// var r = await WebhookDedupe.VerifyAndDedupeAsync(verifier, rawBody, headers, store);
    /// if (!r.Duplicate) { try { await Handle(r.Event); } catch { await r.Release(); throw; } }
    /// return Results.Ok();
    /// </code>
    /// </summary>
    public static class WebhookDedupe
    {
        public static Task<DedupedWebhook> VerifyAndDedupeAsync(WebhookVerifier verifier, byte[] rawBody, IEnumerable<KeyValuePair<string, string>> headers,
            IWebhookDedupeStore store, TimeSpan? ttl = null, string keyPrefix = "atlas-webhook:", CancellationToken ct = default)
            => DedupeAsync(verifier.Verify(rawBody, headers), store, ttl, keyPrefix, ct);

        public static Task<DedupedWebhook> VerifyAndDedupeAsync(StandardWebhookVerifier verifier, byte[] rawBody, IEnumerable<KeyValuePair<string, string>> headers,
            IWebhookDedupeStore store, TimeSpan? ttl = null, string keyPrefix = "atlas-webhook:", CancellationToken ct = default)
            => DedupeAsync(verifier.Verify(rawBody, headers), store, ttl, keyPrefix, ct);

        /// <summary>Dedupe an already verified delivery. <paramref name="ttl"/> defaults to 24 hours: keep it longer than your endpoint's retry window.</summary>
        public static async Task<DedupedWebhook> DedupeAsync(WebhookVerification verified, IWebhookDedupeStore store, TimeSpan? ttl = null,
            string keyPrefix = "atlas-webhook:", CancellationToken ct = default)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (!verified.Verified || string.IsNullOrEmpty(verified.DeliveryId)) throw new WebhookVerificationException(verified.Reason ?? "signature_mismatch");
            if (verified.Event == null) throw new WebhookVerificationException("invalid_body");
            var key = keyPrefix + verified.DeliveryId;
            var fresh = await store.SetIfAbsentAsync(key, ttl ?? TimeSpan.FromHours(24), ct).ConfigureAwait(false);
            return new DedupedWebhook
            {
                Event = verified.Event.Value, Duplicate = !fresh, DeliveryId = verified.DeliveryId!,
                Release = () => store.DeleteAsync(key, CancellationToken.None),
            };
        }
    }
}
