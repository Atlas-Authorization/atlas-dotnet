using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    /// <summary>Directory attribute → Atlas field mapping. `email` is required.</summary>
    public sealed class OrgLdapConnectionBodyAttributes
    {
        public string? Email { get; set; }
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        public string? Username { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OrgLdapConnectionBodyAttributes Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OrgLdapConnectionBody
    {
        /// <summary>The owning organization (required on create).</summary>
        public string? OrganizationId { get; set; }
        public string? Status { get; set; }
        /// <summary>ldap:// or ldaps:// directory URL.</summary>
        public string? Url { get; set; }
        /// <summary>Service-account DN Atlas binds as to search.</summary>
        public string? BindDn { get; set; }
        /// <summary>Subtree searched under.</summary>
        public string? BaseDn { get; set; }
        /// <summary>Search filter with a {{username}} placeholder.</summary>
        public string? UserFilter { get; set; }
        public bool? StartTls { get; set; }
        /// <summary>Directory attribute → Atlas field mapping. `email` is required.</summary>
        public OrgLdapConnectionBodyAttributes? Attributes { get; set; }
        /// <summary>Service-account password. Write-only; never returned.</summary>
        public string? BindPassword { get; set; }
        public string? DefaultRoleId { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OrgLdapConnectionBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OrgLdapConnection
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        public string? OrganizationId { get; init; }
        public string? Status { get; init; }
        public string? Url { get; init; }
        public string? BindDn { get; init; }
        public string? BaseDn { get; init; }
        public string? UserFilter { get; init; }
        public bool? StartTls { get; init; }
        /// <summary>Directory attribute → Atlas field mapping. `email` is required.</summary>
        public OrgLdapConnectionBodyAttributes? Attributes { get; init; }
        /// <summary>Whether a bind password is set — never the password.</summary>
        public bool? HasBindPassword { get; init; }
        public string? DefaultRoleId { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long? CreatedAt { get; init; }
        /// <summary>Updated (epoch ms).</summary>
        public long? UpdatedAt { get; init; }
    }

    public sealed class OrgLdapConnectionTest
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? Id { get; init; }
        /// <summary>Whether the service-account bind succeeded.</summary>
        public bool? Ok { get; init; }
        /// <summary>Present only when the bind failed. Never the password.</summary>
        public string? Error { get; init; }
    }

    /// <summary>Organization-owned LDAP / Active Directory connections (<c>/v1/org_ldap_connections</c>).</summary>
    public sealed class OrgLdapConnectionsResource : ResourceBase
    {
        public OrgLdapConnectionsResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/org_ldap_connections</c> — List the organization-owned LDAP / AD connections.</summary>
        public Task<ListPage<OrgLdapConnection>> ListAsync(string? organizationId = null, CancellationToken ct = default)
            => Req<ListPage<OrgLdapConnection>>(HttpVerb.Get, "/v1/org_ldap_connections", query: Q(("organization_id", organizationId)), cancellationToken: ct);

        /// <summary><c>POST /v1/org_ldap_connections</c> — Create an organization-owned LDAP / AD connection (url, bind_dn, base_dn, user_filter, attributes).</summary>
        public Task<OrgLdapConnection> CreateAsync(OrgLdapConnectionBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OrgLdapConnection>(HttpVerb.Post, "/v1/org_ldap_connections", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/org_ldap_connections/{id}</c> — Fetch one organization-owned LDAP connection.</summary>
        public Task<OrgLdapConnection> GetAsync(string id, CancellationToken ct = default)
            => Req<OrgLdapConnection>(HttpVerb.Get, $"/v1/org_ldap_connections/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/org_ldap_connections/{id}</c> — Update an organization-owned LDAP connection.</summary>
        public Task<OrgLdapConnection> UpdateAsync(string id, OrgLdapConnectionBody body, CancellationToken ct = default)
            => Req<OrgLdapConnection>(HttpVerb.Patch, $"/v1/org_ldap_connections/{Enc(id)}", body, cancellationToken: ct);

        /// <summary><c>DELETE /v1/org_ldap_connections/{id}</c> — Delete an organization-owned LDAP connection.</summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/org_ldap_connections/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>POST /v1/org_ldap_connections/{id}/test</c> — Test an organization-owned LDAP connection: attempt a service-account bind and report ok/failure.</summary>
        public Task<OrgLdapConnectionTest> TestAsync(string id, string? bindPassword = null, CancellationToken ct = default)
            => Req<OrgLdapConnectionTest>(HttpVerb.Post, $"/v1/org_ldap_connections/{Enc(id)}/test", Body(("bind_password", bindPassword)), cancellationToken: ct);
    }
}
