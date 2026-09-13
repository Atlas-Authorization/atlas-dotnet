using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class Role
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "role";
        public string Id { get; init; } = "";
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public bool IsSystem { get; init; }
        public List<string> Permissions { get; init; } = new List<string>();
        public int MemberCount { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class Permission
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "permission";
        public string Id { get; init; } = "";
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public bool IsSystem { get; init; }
    }

    public sealed class CreateRoleBody
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public List<string>? Permissions { get; set; }
    }

    public sealed class UpdateRoleBody
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        /// <summary>May be supplied but must equal the current key — the key is immutable.</summary>
        public string? Key { get; set; }
    }

    /// <summary><c>PUT /v1/roles/:id/permissions</c> returns this reduced shape, not a full Role.</summary>
    public sealed class RolePermissionsResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "role";
        public string Id { get; init; } = "";
        public string Key { get; init; } = "";
        public List<string> Permissions { get; init; } = new List<string>();
        public List<string> Ignored { get; init; } = new List<string>();
    }

    public sealed class DeleteRoleResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "role";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
        public int MembersReassigned { get; init; }
    }

    public sealed class CreatePermissionBody
    {
        public string Key { get; set; } = "";
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public sealed class UpdatePermissionBody
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        /// <summary>May be supplied but must equal the current key — the key is immutable.</summary>
        public string? Key { get; set; }
    }

    public sealed class DeletedPermission
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "permission";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
    }

    /// <summary>The instance-roles namespace (<c>/v1/roles</c>).</summary>
    public sealed class RolesResource : ResourceBase
    {
        public RolesResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<Role>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<Role>>(HttpVerb.Get, "/v1/roles", cancellationToken: ct);

        public Task<Role> CreateAsync(CreateRoleBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Role>(HttpVerb.Post, "/v1/roles", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<Role> UpdateAsync(string id, UpdateRoleBody body, CancellationToken ct = default)
            => Req<Role>(HttpVerb.Patch, $"/v1/roles/{Enc(id)}", body, cancellationToken: ct);

        /// <summary>Replace a role's permission set wholesale.</summary>
        public Task<RolePermissionsResult> SetPermissionsAsync(string id, IEnumerable<string> permissions, CancellationToken ct = default)
            => Req<RolePermissionsResult>(HttpVerb.Put, $"/v1/roles/{Enc(id)}/permissions",
                body: Body(("permissions", new List<string>(permissions))), cancellationToken: ct);

        /// <summary>
        /// Delete a role. Pass <paramref name="reassignTo"/> to move every member
        /// onto another role first (atomic, before the delete) so an in-use role
        /// can be retired; without it, deleting a role people still hold fails with
        /// <c>role_in_use</c>.
        /// </summary>
        public Task<DeleteRoleResult> DeleteAsync(string id, string? reassignTo = null, CancellationToken ct = default)
            => Req<DeleteRoleResult>(HttpVerb.Delete, $"/v1/roles/{Enc(id)}",
                query: reassignTo != null ? Q(("reassign_to", reassignTo)) : null, cancellationToken: ct);
    }

    /// <summary>The instance-permissions namespace (<c>/v1/permissions</c>).</summary>
    public sealed class PermissionsResource : ResourceBase
    {
        public PermissionsResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<Permission>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<Permission>>(HttpVerb.Get, "/v1/permissions", cancellationToken: ct);

        public Task<Permission> CreateAsync(CreatePermissionBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Permission>(HttpVerb.Post, "/v1/permissions", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Relabel a custom permission (name/description only — the key is immutable).</summary>
        public Task<Permission> UpdateAsync(string id, UpdatePermissionBody body, CancellationToken ct = default)
            => Req<Permission>(HttpVerb.Patch, $"/v1/permissions/{Enc(id)}", body, cancellationToken: ct);

        /// <summary>Delete a custom permission. Fails with <c>role_in_use</c> if a role still grants it.</summary>
        public Task<DeletedPermission> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedPermission>(HttpVerb.Delete, $"/v1/permissions/{Enc(id)}", cancellationToken: ct);
    }
}
