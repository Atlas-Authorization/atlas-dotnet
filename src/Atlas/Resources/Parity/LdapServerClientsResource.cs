using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/ldap_server_clients</c>. Only the fields set are sent.</summary>
    public sealed class LdapServerClientsCreateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>Required. e.g. dc=acme,dc=com</summary>
        [JsonPropertyName("base_dn")] public string? BaseDn { get; set; }
        /// <summary>Required. The service account cn under base_dn.</summary>
        [JsonPropertyName("bind_cn")] public string? BindCn { get; set; }
        /// <summary>Source networks allowed to bind (empty = anywhere).</summary>
        [JsonPropertyName("allowed_cidrs")] public List<string>? AllowedCidrs { get; set; }
        [JsonPropertyName("allow_user_bind")] public bool? AllowUserBind { get; set; }
        [JsonPropertyName("size_limit")] public long? SizeLimit { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/ldap_server_clients/{id}</c>. Only the fields set are sent.</summary>
    public sealed class LdapServerClientsUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        /// <summary>e.g. dc=acme,dc=com</summary>
        [JsonPropertyName("base_dn")] public string? BaseDn { get; set; }
        /// <summary>The service account cn under base_dn.</summary>
        [JsonPropertyName("bind_cn")] public string? BindCn { get; set; }
        /// <summary>Source networks allowed to bind (empty = anywhere).</summary>
        [JsonPropertyName("allowed_cidrs")] public List<string>? AllowedCidrs { get; set; }
        [JsonPropertyName("allow_user_bind")] public bool? AllowUserBind { get; set; }
        [JsonPropertyName("size_limit")] public long? SizeLimit { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    /// <summary>Operations of <c>ldapServerClients</c>.</summary>
    public sealed partial class LdapServerClientsResource : ResourceBase
    {
        public LdapServerClientsResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/ldap_server_clients</c> - List the LDAP interface service accounts with their base DN, bind DN and allowed source networks. Secrets are never returned.</summary>
        /// <remarks>Needs the <c>ldap_server:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/ldap_server_clients", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/ldap_server_clients</c> - Create a service account for the LDAPS interface: base_dn (dc=/o=/ou= parts, unique across Atlas), bind_cn, allowed_cidrs (empty = nobody may connect), allow_user_bind and size_limit. The secret is returned once as secret.</summary>
        /// <remarks>Needs the <c>ldap_server:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(LdapServerClientsCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/ldap_server_clients", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/ldap_server_clients/{id}</c> - Read one LDAP interface service account.</summary>
        /// <remarks>Needs the <c>ldap_server:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/ldap_server_clients/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/ldap_server_clients/{id}</c> - Change the name, allowed networks, user-bind switch, size limit or status (active or disabled).</summary>
        /// <remarks>Needs the <c>ldap_server:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, LdapServerClientsUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/ldap_server_clients/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/ldap_server_clients/{id}</c> - Delete a service account; its connections can no longer bind.</summary>
        /// <remarks>Needs the <c>ldap_server:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/ldap_server_clients/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>POST /v1/ldap_server_clients/{id}/rotate_secret</c> - Issue a new service-account secret (returned once); the old one stops working immediately.</summary>
        /// <remarks>Needs the <c>ldap_server:write</c> scope.</remarks>
        public Task<JsonElement> RotateSecretAsync(string id, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/ldap_server_clients/{Enc(id)}/rotate_secret", query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
