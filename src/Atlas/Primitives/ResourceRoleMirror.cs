using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Resources;

namespace Atlas.Primitives
{
    public sealed record MirrorSubject(string Type, string Id)
    {
        public static MirrorSubject User(string id) => new MirrorSubject("user", id);
        public static MirrorSubject Group(string id) => new MirrorSubject("group", id);
        public static MirrorSubject Org(string id) => new MirrorSubject("org", id);
        internal string Key => Type + ":" + Id;
    }

    /// <summary>A resource reference: the Atlas <c>Id</c>, or your own <c>Type</c> + <c>ExternalId</c>.</summary>
    public sealed record MirrorResourceRef(string? Id = null, string? Type = null, string? ExternalId = null)
    {
        public static MirrorResourceRef ById(string id) => new MirrorResourceRef(Id: id);
        public static MirrorResourceRef ByExternal(string type, string externalId) => new MirrorResourceRef(Type: type, ExternalId: externalId);
    }

    public sealed record MirroredRole(string Key, IReadOnlyList<string> Permissions, IReadOnlyList<string> Includes);
    public sealed record MirroredResourceType(string Key, bool Inherit, IReadOnlyList<MirroredRole> Roles);
    public sealed record MirroredResource(string Id, string Type, string ExternalId, string? ParentId);
    /// <summary>One role assignment. <see cref="Subject"/> is <c>user:usr_1</c>, <c>group:eng</c> or <c>org:org_1</c>.</summary>
    public sealed record MirroredAssignment(string Id, string ResourceId, string Subject, string Role, bool Inherit);
    public sealed record MirroredAssignmentChange(bool Deleted, MirroredAssignment Assignment);

    public interface IResourceRoleMirrorStore
    {
        Task<string?> GetCursorAsync();
        Task SetCursorAsync(string cursor);
        Task<MirroredResourceType?> GetTypeAsync(string key);
        Task<MirroredResource?> GetResourceAsync(MirrorResourceRef reference);
        /// <summary>Every assignment sitting on any of these resources.</summary>
        Task<IReadOnlyList<MirroredAssignment>> AssignmentsOnAsync(IReadOnlyCollection<string> resourceIds);
        Task ReplaceTypesAsync(IReadOnlyList<MirroredResourceType> types);
        Task ReplaceResourcesAsync(IReadOnlyList<MirroredResource> resources);
        /// <summary><c>created</c> upserts by assignment id; <c>deleted</c> removes it.</summary>
        Task ApplyChangesAsync(IReadOnlyList<MirroredAssignmentChange> changes);
        Task<long?> GetLastSyncAtAsync();
        Task SetLastSyncAtAsync(long at);
    }

    public sealed class InMemoryResourceRoleMirrorStore : IResourceRoleMirrorStore
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, MirroredAssignment> _assignments = new Dictionary<string, MirroredAssignment>();
        private Dictionary<string, MirroredResource> _resources = new Dictionary<string, MirroredResource>();
        private Dictionary<string, MirroredResourceType> _types = new Dictionary<string, MirroredResourceType>();
        private string? _cursor;
        private long? _syncAt;

        public Task<string?> GetCursorAsync() { lock (_gate) return Task.FromResult(_cursor); }
        public Task SetCursorAsync(string cursor) { lock (_gate) _cursor = cursor; return Task.CompletedTask; }
        public Task<MirroredResourceType?> GetTypeAsync(string key) { lock (_gate) return Task.FromResult(_types.TryGetValue(key, out var t) ? t : null); }

        public Task<MirroredResource?> GetResourceAsync(MirrorResourceRef r)
        {
            lock (_gate)
            {
                if (r.Id != null) return Task.FromResult(_resources.TryGetValue(r.Id, out var x) ? x : null);
                return Task.FromResult(_resources.Values.FirstOrDefault(v => v.Type == r.Type && v.ExternalId == r.ExternalId));
            }
        }

        public Task<IReadOnlyList<MirroredAssignment>> AssignmentsOnAsync(IReadOnlyCollection<string> ids)
        {
            lock (_gate) return Task.FromResult<IReadOnlyList<MirroredAssignment>>(_assignments.Values.Where(a => ids.Contains(a.ResourceId)).ToList());
        }

        public Task ReplaceTypesAsync(IReadOnlyList<MirroredResourceType> types) { lock (_gate) _types = types.ToDictionary(t => t.Key); return Task.CompletedTask; }
        public Task ReplaceResourcesAsync(IReadOnlyList<MirroredResource> resources) { lock (_gate) _resources = resources.ToDictionary(r => r.Id); return Task.CompletedTask; }

        public Task ApplyChangesAsync(IReadOnlyList<MirroredAssignmentChange> changes)
        {
            lock (_gate)
                foreach (var c in changes)
                    if (c.Deleted) _assignments.Remove(c.Assignment.Id); else _assignments[c.Assignment.Id] = c.Assignment;
            return Task.CompletedTask;
        }

        public Task<long?> GetLastSyncAtAsync() { lock (_gate) return Task.FromResult(_syncAt); }
        public Task SetLastSyncAtAsync(long at) { lock (_gate) _syncAt = at; return Task.CompletedTask; }
    }

    /// <summary>The Atlas calls the mirror needs, as raw JSON pages (<c>data</c>, <c>has_more</c>, <c>next_cursor</c>). <see cref="ResourceRoleMirrorApi.From"/> adapts an <see cref="AtlasClient"/>.</summary>
    public interface IResourceRoleMirrorApi
    {
        Task<JsonElement> ListTypesAsync(CancellationToken ct);
        Task<JsonElement> ListResourcesAsync(string? startingAfter, CancellationToken ct);
        Task<JsonElement> ChangesAsync(string? after, CancellationToken ct);
    }

    public static class ResourceRoleMirrorApi
    {
        public static IResourceRoleMirrorApi From(AtlasClient client) => new ClientApi(client);

        private sealed class ClientApi : IResourceRoleMirrorApi
        {
            private readonly AtlasClient _c;
            public ClientApi(AtlasClient c) => _c = c;
            public Task<JsonElement> ListTypesAsync(CancellationToken ct) => _c.Resources.Types.ListAsync(null, null, ct);
            public Task<JsonElement> ListResourcesAsync(string? startingAfter, CancellationToken ct) => _c.Resources.ListAsync(null, null, 1000, startingAfter, ct);
            public async Task<JsonElement> ChangesAsync(string? after, CancellationToken ct)
            {
                var page = await _c.Catalog.ResourceRoleAssignments.ListChangesAsync(
                    new Generated.ResourceRoleAssignmentsListChangesQuery { After = after, Limit = 500 }, ct).ConfigureAwait(false);
                return JsonSerializer.SerializeToElement(page, AtlasJson.Options);
            }
        }
    }

    public sealed class ResourceRoleMirrorOptions
    {
        public IResourceRoleMirrorStore Store { get; set; } = new InMemoryResourceRoleMirrorStore();
        public IResourceRoleMirrorApi Api { get; set; } = null!;
        /// <summary>When set, <c>Check</c> / <c>Can</c> answer "not allowed" (<c>Stale</c>) if the mirror has never completed a sync or the last one finished longer ago than this. Null turns the check off (webhook-only mirrors).</summary>
        public TimeSpan? MaxStaleness { get; set; }
        public TimeSpan ServeStaleFor { get; set; } = TimeSpan.Zero;
        public Action<TimeSpan>? OnServeStale { get; set; }
        /// <summary>Re-pull the types and the resource tree on a sync only when this long has passed since the last pull (default every sync).</summary>
        public TimeSpan TreeRefresh { get; set; } = TimeSpan.Zero;
        public Func<long>? Now { get; set; }
    }

    public sealed class ResourceRoleCheckOptions
    {
        /// <summary>Organization ids the user belongs to (the server adds these itself).</summary>
        public IReadOnlyList<string> Orgs { get; set; } = Array.Empty<string>();
        /// <summary>Group ids / slugs the subject belongs to.</summary>
        public IReadOnlyList<string> Groups { get; set; } = Array.Empty<string>();
    }

    public sealed record ResourceGrant(string ResourceId, string Role, string Subject, bool Inherited);

    public sealed class ResourceRoleCheck
    {
        public bool Allowed { get; init; }
        public string? ResourceId { get; init; }
        public IReadOnlyList<ResourceGrant> Grants { get; init; } = Array.Empty<ResourceGrant>();
        /// <summary><c>unknown_resource</c> (the mirror does not hold that resource) or <c>stale</c> (see <c>MaxStaleness</c>).</summary>
        public string? Code { get; init; }
        public bool Stale { get; init; }
    }

    /// <summary>
    /// A local mirror of resource-scoped role assignments, so <c>Can(user, permission, resource)</c> is answered with no network call and the
    /// SAME inheritance rules as <c>POST /v1/resource_checks</c>. <see cref="SyncAsync"/> pulls the types and the tree, then consumes the
    /// assignment change feed from the stored cursor; <see cref="ApplyWebhookAsync"/> applies the <c>resource_role_assignment.created</c> /
    /// <c>.deleted</c> events between syncs. One difference from the server: Atlas adds the user's organizations and groups to the subject
    /// itself; offline you pass them in <see cref="ResourceRoleCheckOptions"/>.
    /// </summary>
    public sealed class ResourceRoleMirror
    {
        private const int MaxDepth = 16; // the server's ancestor walk cap
        private readonly ResourceRoleMirrorOptions _o;
        private long? _memorySyncAt;
        private long? _treeAt;

        public ResourceRoleMirror(ResourceRoleMirrorOptions options)
        {
            _o = options ?? throw new ArgumentNullException(nameof(options));
            if (options.Api == null) throw new ArgumentException("Api is required (ResourceRoleMirrorApi.From(client)).", nameof(options));
        }

        private long Now() => _o.Now != null ? _o.Now() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public async Task<long?> LastSyncAtAsync() => await _o.Store.GetLastSyncAtAsync().ConfigureAwait(false) ?? _memorySyncAt;

        /// <summary>May <paramref name="subject"/> hold <paramref name="permission"/> on <paramref name="resource"/>? Answered locally.</summary>
        public async Task<bool> CanAsync(MirrorSubject subject, string permission, MirrorResourceRef resource, ResourceRoleCheckOptions? options = null)
            => (await CheckAsync(subject, permission, resource, options).ConfigureAwait(false)).Allowed;

        /// <summary>The verdict and the grants behind it, in the shape of <c>POST /v1/resource_checks</c>.</summary>
        public async Task<ResourceRoleCheck> CheckAsync(MirrorSubject subject, string permission, MirrorResourceRef resource, ResourceRoleCheckOptions? options = null)
        {
            var servedStale = false;
            if (_o.MaxStaleness != null)
            {
                var at = await LastSyncAtAsync().ConfigureAwait(false);
                var behind = at == null ? long.MaxValue : Now() - at.Value - (long)_o.MaxStaleness.Value.TotalMilliseconds;
                if (behind > 0)
                {
                    if (at == null || behind > (long)_o.ServeStaleFor.TotalMilliseconds) return new ResourceRoleCheck { Code = "stale" };
                    servedStale = true;
                    _o.OnServeStale?.Invoke(TimeSpan.FromMilliseconds(behind));
                }
            }
            var row = await _o.Store.GetResourceAsync(resource).ConfigureAwait(false);
            if (row == null || string.IsNullOrEmpty(permission)) return new ResourceRoleCheck { ResourceId = row?.Id, Code = row == null ? "unknown_resource" : null };
            var subjects = new HashSet<string> { subject.Key };
            foreach (var g in options?.Groups ?? Array.Empty<string>()) subjects.Add("group:" + g);
            foreach (var o in options?.Orgs ?? Array.Empty<string>()) subjects.Add("org:" + o);
            var grants = ResolveGrants(await ChainAsync(row).ConfigureAwait(false), subjects, permission);
            return new ResourceRoleCheck { Allowed = grants.Count > 0, ResourceId = row.Id, Grants = grants, Stale = servedStale };
        }

        private async Task<List<(string ResourceId, MirroredResourceType Type, List<MirroredAssignment> Assignments)>> ChainAsync(MirroredResource resource)
        {
            // The resource, then its ancestors (cycle-safe, depth-capped), exactly like the server's ancestorsOf: ancestors dedupe among
            // themselves only, so a cycle yields the start resource once more.
            var ancestors = new List<MirroredResource>();
            var id = resource.ParentId;
            for (var i = 0; i < MaxDepth && id != null; i++)
            {
                var row = await _o.Store.GetResourceAsync(MirrorResourceRef.ById(id)).ConfigureAwait(false);
                if (row == null || ancestors.Any(r => r.Id == row.Id)) break;
                ancestors.Add(row);
                id = row.ParentId;
            }
            var rows = new[] { resource }.Concat(ancestors).ToList();
            var assignments = await _o.Store.AssignmentsOnAsync(rows.Select(r => r.Id).ToList()).ConfigureAwait(false);
            var chain = new List<(string, MirroredResourceType, List<MirroredAssignment>)>();
            foreach (var r in rows)
            {
                var t = await _o.Store.GetTypeAsync(r.Type).ConfigureAwait(false);
                chain.Add((r.Id, t ?? new MirroredResourceType(r.Type, true, Array.Empty<MirroredRole>()), assignments.Where(a => a.ResourceId == r.Id).ToList()));
            }
            return chain;
        }

        /// <summary>All permissions a role grants, following <c>includes</c> (cycle-safe). <c>*</c> means every permission.</summary>
        internal static HashSet<string> RolePermissions(MirroredResourceType type, string roleKey, HashSet<string>? seen = null)
        {
            var output = new HashSet<string>();
            seen ??= new HashSet<string>();
            if (!seen.Add(roleKey)) return output;
            var role = type.Roles.FirstOrDefault(r => r.Key == roleKey);
            if (role == null) return output;
            output.UnionWith(role.Permissions);
            foreach (var inc in role.Includes) output.UnionWith(RolePermissions(type, inc, seen));
            return output;
        }

        /// <summary>The server's <c>resolveResourceGrants</c> over a chain of (resource, type, assignments), resource first.</summary>
        internal static List<ResourceGrant> ResolveGrants(IReadOnlyList<(string ResourceId, MirroredResourceType Type, List<MirroredAssignment> Assignments)> chain,
            ISet<string> subjects, string permission)
        {
            var grants = new List<ResourceGrant>();
            for (var i = 0; i < chain.Count; i++)
            {
                var link = chain[i];
                foreach (var a in link.Assignments)
                {
                    if (!subjects.Contains(a.Subject) || (i > 0 && !a.Inherit)) continue;
                    var perms = RolePermissions(link.Type, a.Role);
                    if (perms.Contains(permission) || perms.Contains("*")) grants.Add(new ResourceGrant(link.ResourceId, a.Role, a.Subject, i > 0));
                }
                if (!link.Type.Inherit) break; // the next link up is only reachable when this resource's type inherits
            }
            return grants;
        }

        /// <summary>
        /// Pull the types and the resource tree (when <c>TreeRefresh</c> has passed), then consume the assignment change feed from the stored
        /// cursor. Call it on a timer (every few seconds to a minute) and at startup. Returns how many changes it applied.
        /// </summary>
        public async Task<int> SyncAsync(CancellationToken ct = default)
        {
            if (_treeAt == null || Now() - _treeAt.Value >= (long)_o.TreeRefresh.TotalMilliseconds)
            {
                // Tree first: an assignment the feed then delivers never names a resource this mirror lacks.
                var types = Items(await _o.Api.ListTypesAsync(ct).ConfigureAwait(false)).Select(t => new MirroredResourceType(
                    Str(t, "key"), !t.TryGetProperty("inherit", out var inh) || inh.ValueKind != JsonValueKind.False,
                    t.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array
                        ? roles.EnumerateArray().Select(r => new MirroredRole(Str(r, "key"), Strs(r, "permissions"), Strs(r, "includes"))).ToList()
                        : new List<MirroredRole>())).ToList();
                var tree = new List<MirroredResource>();
                string? cursor = null;
                while (true)
                {
                    var page = await _o.Api.ListResourcesAsync(cursor, ct).ConfigureAwait(false);
                    foreach (var r in Items(page))
                        tree.Add(new MirroredResource(Str(r, "id"), Str(r, "type"), Str(r, "external_id"), r.TryGetProperty("parent_id", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null));
                    var next = NextCursor(page);
                    if (!HasMore(page) || next == null) break;
                    cursor = next;
                }
                await _o.Store.ReplaceTypesAsync(types).ConfigureAwait(false);
                await _o.Store.ReplaceResourcesAsync(tree).ConfigureAwait(false);
                _treeAt = Now();
            }
            var after = await _o.Store.GetCursorAsync().ConfigureAwait(false);
            var applied = 0;
            while (true)
            {
                var page = await _o.Api.ChangesAsync(after, ct).ConfigureAwait(false);
                var changes = Items(page).Select(c => new MirroredAssignmentChange(Str(c, "change") == "deleted", new MirroredAssignment(
                    Str(c, "assignment_id"), Str(c, "resource_id"), Str(c.GetProperty("subject"), "type") + ":" + Str(c.GetProperty("subject"), "id"),
                    Str(c, "role"), !c.TryGetProperty("inherit", out var inh) || inh.ValueKind != JsonValueKind.False))).ToList();
                await _o.Store.ApplyChangesAsync(changes).ConfigureAwait(false);
                applied += changes.Count;
                var next = NextCursor(page);
                if (next != null) { after = next; await _o.Store.SetCursorAsync(next).ConfigureAwait(false); }
                if (!HasMore(page))
                {
                    _memorySyncAt = Now();
                    await _o.Store.SetLastSyncAtAsync(_memorySyncAt.Value).ConfigureAwait(false);
                    return applied;
                }
            }
        }

        /// <summary>
        /// Apply a VERIFIED <c>resource_role_assignment.created</c> / <c>.deleted</c> webhook (check the signature first). Returns whether the event
        /// changed the mirror.
        /// </summary>
        public async Task<bool> ApplyWebhookAsync(string type, JsonElement data)
        {
            if (type != "resource_role_assignment.created" && type != "resource_role_assignment.deleted") return false;
            if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("subject", out var subject) || subject.ValueKind != JsonValueKind.Object) return false;
            string? S(JsonElement e, string n) => e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            var id = S(data, "id"); var rid = S(data, "resource_id"); var role = S(data, "role"); var st = S(subject, "type"); var sid = S(subject, "id");
            if (id == null || rid == null || role == null || st == null || sid == null) return false;
            var inherit = !data.TryGetProperty("inherit", out var inh) || inh.ValueKind != JsonValueKind.False;
            await _o.Store.ApplyChangesAsync(new[] { new MirroredAssignmentChange(type.EndsWith(".deleted"), new MirroredAssignment(id, rid, st + ":" + sid, role, inherit)) }).ConfigureAwait(false);
            return true;
        }

        private static IEnumerable<JsonElement> Items(JsonElement page)
            => page.ValueKind == JsonValueKind.Object && page.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array ? d.EnumerateArray() : Enumerable.Empty<JsonElement>();
        private static bool HasMore(JsonElement page) => page.ValueKind == JsonValueKind.Object && page.TryGetProperty("has_more", out var h) && h.ValueKind == JsonValueKind.True;
        private static string? NextCursor(JsonElement page) => page.ValueKind == JsonValueKind.Object && page.TryGetProperty("next_cursor", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
        private static string Str(JsonElement e, string n) => e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
        private static IReadOnlyList<string> Strs(JsonElement e, string n)
            => e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList() : new List<string>();
    }
}
