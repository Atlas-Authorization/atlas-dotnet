using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class CreateMachineBody
    {
        public string? Name { get; set; }
        public string? OwnerUserId { get; set; }
        public Dictionary<string, object?>? Data { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateMachineBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class UpdateMachineMetadataBody
    {
        public Dictionary<string, object?>? Metadata { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public UpdateMachineMetadataBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class CreateMachineEnrolmentTokenBody
    {
        public string? OrganizationId { get; set; }
        public double? MaxUses { get; set; }
        public double? ExpiresInSeconds { get; set; }
        public bool? RequiresApproval { get; set; }
        public Dictionary<string, object?>? Data { get; set; }
        public string? OwnerUserId { get; set; }
        public bool? SingleSession { get; set; }
        public double? MaxMachines { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public CreateMachineEnrolmentTokenBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class UserOrganizationMembership
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? OrganizationId { get; init; }
        public string? OrganizationSlug { get; init; }
        /// <summary>The member's role key in this org.</summary>
        public string? Role { get; init; }
        /// <summary>The org's active entitlement plan slug, or null.</summary>
        public string? Plan { get; init; }
        /// <summary>When the membership was created (epoch ms).</summary>
        public long? JoinedAt { get; init; }
    }

    public sealed class MachineWithSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary>Owning org, when org-scoped.</summary>
        public string? OrganizationId { get; init; }
        /// <summary>Owning user, when a user-owned device.</summary>
        public string? OwnerUserId { get; init; }
        /// <summary>Stored device lifecycle state (unchanged by the owner's state).</summary>
        public string Status { get; init; } = "";
        /// <summary>Whether the owning user is usable (not banned, locked, or deleted). True when the machine has no owner. When false the machine cannot mint or verify tokens even while its stored status stays 'active'.</summary>
        public bool? OwnerUsable { get; init; }
        /// <summary>Derived status: 'active' only when the machine is active AND its owner is usable; 'owner_suspended' when active but the owner is not; otherwise the raw status.</summary>
        public string? EffectiveStatus { get; init; }
        /// <summary>The stable device self-identifier supplied at enrolment (null when none). Lets a fleet manager correlate this record with the device it re-enrolled across tokens.</summary>
        public string? DeviceKey { get; init; }
        /// <summary>RFC 7638 thumbprint of the device public key.</summary>
        public string? PublicKeyJkt { get; init; }
        /// <summary>Device metadata (hostname, os, app_version, last_seen_at).</summary>
        public Dictionary<string, object?>? Metadata { get; init; }
        /// <summary>When the device completed enrolment (epoch ms).</summary>
        public long? EnrolledAt { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
        /// <summary>Updated (epoch ms).</summary>
        public long? UpdatedAt { get; init; }
        /// <summary>Reveal-once mk_ secret (create/rotate-only).</summary>
        public string Secret { get; init; } = "";
    }

    public sealed class Machine
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary>Owning org, when org-scoped.</summary>
        public string? OrganizationId { get; init; }
        /// <summary>Owning user, when a user-owned device.</summary>
        public string? OwnerUserId { get; init; }
        /// <summary>Stored device lifecycle state (unchanged by the owner's state).</summary>
        public string Status { get; init; } = "";
        /// <summary>Whether the owning user is usable (not banned, locked, or deleted). True when the machine has no owner. When false the machine cannot mint or verify tokens even while its stored status stays 'active'.</summary>
        public bool? OwnerUsable { get; init; }
        /// <summary>Derived status: 'active' only when the machine is active AND its owner is usable; 'owner_suspended' when active but the owner is not; otherwise the raw status.</summary>
        public string? EffectiveStatus { get; init; }
        /// <summary>The stable device self-identifier supplied at enrolment (null when none). Lets a fleet manager correlate this record with the device it re-enrolled across tokens.</summary>
        public string? DeviceKey { get; init; }
        /// <summary>RFC 7638 thumbprint of the device public key.</summary>
        public string? PublicKeyJkt { get; init; }
        /// <summary>Device metadata (hostname, os, app_version, last_seen_at).</summary>
        public Dictionary<string, object?>? Metadata { get; init; }
        /// <summary>When the device completed enrolment (epoch ms).</summary>
        public long? EnrolledAt { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
        /// <summary>Updated (epoch ms).</summary>
        public long? UpdatedAt { get; init; }
    }

    public sealed class M2mTokenVerification
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public bool Valid { get; init; }
        public string? MachineId { get; init; }
        public string? Name { get; init; }
    }

    public sealed class MachineEnrolmentTokenWithSecret
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string? OrganizationId { get; init; }
        public long MaxUses { get; init; }
        public long Uses { get; init; }
        /// <summary>Expiry (epoch ms).</summary>
        public long ExpiresAt { get; init; }
        public bool RequiresApproval { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
        /// <summary>Reveal-once met_ enrolment token (create-only).</summary>
        public string Token { get; init; } = "";
    }

    public sealed class MachineEnrolmentToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public string Id { get; init; } = "";
        public string? OrganizationId { get; init; }
        public long MaxUses { get; init; }
        public long Uses { get; init; }
        /// <summary>Expiry (epoch ms).</summary>
        public long ExpiresAt { get; init; }
        public bool RequiresApproval { get; init; }
        /// <summary>Created (epoch ms).</summary>
        public long CreatedAt { get; init; }
    }

    /// <summary>Machine enrolment tokens (<c>/v1/machine_enrolment_tokens</c>).</summary>
    public sealed class MachinesEnrolmentTokensResource : ResourceBase
    {
        public MachinesEnrolmentTokensResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>POST /v1/machine_enrolment_tokens</c> — Mint a machine enrolment token (single/multi-use, expiring, org-bound, approval-gated).</summary>
        public Task<MachineEnrolmentTokenWithSecret> CreateAsync(CreateMachineEnrolmentTokenBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<MachineEnrolmentTokenWithSecret>(HttpVerb.Post, "/v1/machine_enrolment_tokens", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/machine_enrolment_tokens</c> — List machine enrolment tokens (never the secret), newest first.</summary>
        public Task<CursorPage<MachineEnrolmentToken>> ListAsync(ListMachineEnrolmentTokensParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<MachineEnrolmentToken>>(HttpVerb.Get, "/v1/machine_enrolment_tokens", query: (@params ?? new ListMachineEnrolmentTokensParams()).ToQuery(), cancellationToken: ct);

        /// <summary><c>DELETE /v1/machine_enrolment_tokens/{id}</c> — Delete a machine enrolment token; it can no longer enrol new devices.</summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/machine_enrolment_tokens/{Enc(id)}", cancellationToken: ct);
    }

    /// <summary>Machine identities (<c>/v1/machines</c>), machine-token verification and enrolment tokens.</summary>
    public sealed class MachinesResource : ResourceBase
    {
        public MachinesResource(AtlasTransport transport) : base(transport)
        {
            EnrolmentTokens = new MachinesEnrolmentTokensResource(transport);
        }

        /// <summary>Machine enrolment tokens (<c>/v1/machine_enrolment_tokens</c>).</summary>
        public MachinesEnrolmentTokensResource EnrolmentTokens { get; }

        /// <summary><c>POST /v1/machines</c> — Create a named machine (M2M) identity; returns the machine secret (mk_) ONCE.</summary>
        public Task<MachineWithSecret> CreateAsync(CreateMachineBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<MachineWithSecret>(HttpVerb.Post, "/v1/machines", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/machines</c> — List machine identities (device registry), filtered by lifecycle status, reported hostname, owning user, organization, and/or metadata, with keyset pagination.</summary>
        public Task<CursorPage<Machine>> ListAsync(ListMachinesParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<Machine>>(HttpVerb.Get, "/v1/machines", query: (@params ?? new ListMachinesParams()).ToQuery(), cancellationToken: ct);

        /// <summary><c>GET /v1/machines/{id}</c> — Fetch one named machine identity by id.</summary>
        public Task<Machine> GetAsync(string id, CancellationToken ct = default)
            => Req<Machine>(HttpVerb.Get, $"/v1/machines/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/machines/{id}</c> — Rename a machine identity.</summary>
        public Task<Machine> UpdateAsync(string id, string? name = null, CancellationToken ct = default)
            => Req<Machine>(HttpVerb.Patch, $"/v1/machines/{Enc(id)}", Body(("name", name)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/machines/{id}</c> — Delete a machine identity.</summary>
        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/machines/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>GET /v1/machines/{id}/organization_memberships</c> — List the organizations a machine belongs to with the org's resolved plan and the enrolment time — the machine-token counterpart of the user organization_memberships route.</summary>
        public Task<ListPage<UserOrganizationMembership>> ListOrganizationMembershipsAsync(string id, CancellationToken ct = default)
            => Req<ListPage<UserOrganizationMembership>>(HttpVerb.Get, $"/v1/machines/{Enc(id)}/organization_memberships", cancellationToken: ct);

        /// <summary><c>PATCH /v1/machines/{id}/metadata</c> — Merge device metadata (hostname, os, app_version, last_seen_at) into a machine.</summary>
        public Task<Machine> UpdateMetadataAsync(string id, UpdateMachineMetadataBody body, CancellationToken ct = default)
            => Req<Machine>(HttpVerb.Patch, $"/v1/machines/{Enc(id)}/metadata", body, cancellationToken: ct);

        /// <summary><c>POST /v1/machines/{id}/approve</c> — Approve a device that enrolled under an approval-gated token (pending → active).</summary>
        public Task<Machine> ApproveAsync(string id, CancellationToken ct = default)
            => Req<Machine>(HttpVerb.Post, $"/v1/machines/{Enc(id)}/approve", cancellationToken: ct);

        /// <summary><c>POST /v1/machines/{id}/revoke</c> — Revoke a machine device; its key stops minting tokens immediately (record kept).</summary>
        public Task<Machine> RevokeAsync(string id, CancellationToken ct = default)
            => Req<Machine>(HttpVerb.Post, $"/v1/machines/{Enc(id)}/revoke", cancellationToken: ct);

        /// <summary><c>POST /v1/machines/{id}/rotate_secret</c> — Rotate a machine secret; the old one stops working and a new mk_ is shown once.</summary>
        public Task<MachineWithSecret> RotateSecretAsync(string id, CancellationToken ct = default)
            => Req<MachineWithSecret>(HttpVerb.Post, $"/v1/machines/{Enc(id)}/rotate_secret", cancellationToken: ct);

        /// <summary><c>POST /v1/machines/{id}/rotate_key</c> — Rotate a device keypair: register a new public key; the old private key stops verifying.</summary>
        public Task<Machine> RotateKeyAsync(string id, string? publicKeyPem, CancellationToken ct = default)
            => Req<Machine>(HttpVerb.Post, $"/v1/machines/{Enc(id)}/rotate_key", Body(("public_key_pem", publicKeyPem)), cancellationToken: ct);

        /// <summary><c>POST /v1/m2m_tokens/verify</c> — Verify a presented machine secret → { valid, machine_id?</summary>
        public Task<M2mTokenVerification> VerifyTokenAsync(string token, CancellationToken ct = default)
            => Req<M2mTokenVerification>(HttpVerb.Post, "/v1/m2m_tokens/verify", Body(("token", token)), cancellationToken: ct);
    }
}
