using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.Rp
{
    /// <summary>
    /// Maps an Atlas <c>sid</c> (the <c>sid</c> claim of the ID token) to the session ids of YOUR app, so a back-channel logout
    /// that names only the <c>sid</c> can end exactly the app sessions it backs. Bind when you create your app session.
    /// </summary>
    public interface ISidStore
    {
        /// <summary>Remember that <paramref name="appSessionId"/> belongs to <paramref name="sid"/> until <paramref name="expiresAt"/>.</summary>
        Task BindAsync(string sid, string appSessionId, DateTimeOffset expiresAt);

        /// <summary>Return AND remove every unexpired app session bound to <paramref name="sid"/> (empty when none).</summary>
        Task<IReadOnlyList<string>> TakeAsync(string sid);
    }

    /// <summary>An in-process <see cref="ISidStore"/>: fine for one server; back it with a shared store for several.</summary>
    public sealed class MemorySidStore : ISidStore
    {
        private readonly Dictionary<string, Dictionary<string, DateTimeOffset>> _bySid = new Dictionary<string, Dictionary<string, DateTimeOffset>>();
        private readonly Func<DateTimeOffset> _now;

        public MemorySidStore(Func<DateTimeOffset>? now = null) => _now = now ?? (() => DateTimeOffset.UtcNow);

        public Task BindAsync(string sid, string appSessionId, DateTimeOffset expiresAt)
        {
            lock (_bySid)
            {
                Sweep();
                if (!_bySid.TryGetValue(sid, out var apps)) _bySid[sid] = apps = new Dictionary<string, DateTimeOffset>();
                apps[appSessionId] = expiresAt;
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> TakeAsync(string sid)
        {
            lock (_bySid)
            {
                Sweep();
                if (!_bySid.TryGetValue(sid, out var apps)) return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
                _bySid.Remove(sid);
                return Task.FromResult<IReadOnlyList<string>>(apps.Keys.ToList());
            }
        }

        private void Sweep()
        {
            var now = _now();
            foreach (var sid in _bySid.Keys.ToList())
            {
                var apps = _bySid[sid];
                foreach (var id in apps.Where(a => a.Value <= now).Select(a => a.Key).ToList()) apps.Remove(id);
                if (apps.Count == 0) _bySid.Remove(sid);
            }
        }
    }
}
