using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Credential config, with secrets reduced to <c>has_*</c> markers.</summary>
    public sealed class ManagedWafCredential
    {
        /// <summary><c>keys</c> (BYO IAM key/secret) | <c>role</c> (cross-account STS AssumeRole).</summary>
        public string Mode { get; init; } = "";
        public string? AccessKeyId { get; init; }
        public bool HasSecretAccessKey { get; init; }
        public string? RoleArn { get; init; }
        public bool HasExternalId { get; init; }
    }

    public sealed class ManagedWaf
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "managed_waf";
        public bool Configured { get; init; }
        public bool Enabled { get; init; }
        public string Status { get; init; } = "";
        public string? Scope { get; init; }
        public string? Region { get; init; }
        public string? WebAclName { get; init; }
        public string? EdgeResourceArn { get; init; }
        public string? CloudfrontDistributionId { get; init; }
        public List<string>? TokenDomains { get; init; }
        public List<string>? GatedPaths { get; init; }
        public int? ImmunitySeconds { get; init; }
        public ManagedWafCredential? Credential { get; init; }
        public string? WebAclId { get; init; }
        public string? WebAclArn { get; init; }
        public string? LastError { get; init; }
        public long? LastProvisionedAt { get; init; }
        public long? UpdatedAt { get; init; }
        /// <summary>Present on provision/deprovision — the fail-safe outcome flag.</summary>
        public bool? Ok { get; init; }
        /// <summary>Present on provision/deprovision when the AWS call failed.</summary>
        public string? Error { get; init; }
    }

    /// <summary>Just the provisioning state — what <c>GET /v1/managed_waf/status</c> returns.</summary>
    public sealed class ManagedWafStatus
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "managed_waf_status";
        public bool Enabled { get; init; }
        public string Status { get; init; } = "";
        public string? WebAclId { get; init; }
        public string? WebAclArn { get; init; }
        public string? LastError { get; init; }
        public long? LastProvisionedAt { get; init; }
    }

    public sealed class ManagedWafCredentialInput
    {
        /// <summary><c>keys</c> | <c>role</c>.</summary>
        public string? Mode { get; set; }
        public string? AccessKeyId { get; set; }
        /// <summary>Write-only: accepted, encrypted at rest, never returned.</summary>
        public string? SecretAccessKey { get; set; }
        public string? RoleArn { get; set; }
        /// <summary>Write-only: accepted, encrypted at rest, never returned.</summary>
        public string? ExternalId { get; set; }
    }

    public sealed class UpdateManagedWafBody
    {
        public bool? Enabled { get; set; }
        /// <summary><c>REGIONAL</c> | <c>CLOUDFRONT</c>.</summary>
        public string? Scope { get; set; }
        public string? Region { get; set; }
        public string? WebAclName { get; set; }
        public string? EdgeResourceArn { get; set; }
        public string? CloudfrontDistributionId { get; set; }
        public List<string>? TokenDomains { get; set; }
        public List<string>? GatedPaths { get; set; }
        /// <summary>60..86400 seconds.</summary>
        public int? ImmunitySeconds { get; set; }
        public ManagedWafCredentialInput? Credential { get; set; }
    }

    /// <summary>The managed AWS WAF namespace (<c>/v1/managed_waf</c>).</summary>
    public sealed class ManagedWafResource : ResourceBase
    {
        public ManagedWafResource(AtlasTransport transport) : base(transport) { }

        /// <summary>Read config + provisioning state, with secrets reduced to <c>has_*</c> markers.</summary>
        public Task<ManagedWaf> GetAsync(CancellationToken ct = default)
            => Req<ManagedWaf>(HttpVerb.Get, "/v1/managed_waf", cancellationToken: ct);

        /// <summary>Read just the provisioning state.</summary>
        public Task<ManagedWafStatus> StatusAsync(CancellationToken ct = default)
            => Req<ManagedWafStatus>(HttpVerb.Get, "/v1/managed_waf/status", cancellationToken: ct);

        /// <summary>Set config and write-only credentials; omitted fields are left unchanged.</summary>
        public Task<ManagedWaf> UpdateAsync(UpdateManagedWafBody body, CancellationToken ct = default)
            => Req<ManagedWaf>(HttpVerb.Put, "/v1/managed_waf", body, cancellationToken: ct);

        /// <summary>Apply now — idempotent create/update + associate the WebACL.</summary>
        public Task<ManagedWaf> ProvisionAsync(CancellationToken ct = default)
            => Req<ManagedWaf>(HttpVerb.Post, "/v1/managed_waf/provision", cancellationToken: ct);

        /// <summary>Disassociate and delete the WebACL.</summary>
        public Task<ManagedWaf> DeprovisionAsync(CancellationToken ct = default)
            => Req<ManagedWaf>(HttpVerb.Post, "/v1/managed_waf/deprovision", cancellationToken: ct);
    }
}
