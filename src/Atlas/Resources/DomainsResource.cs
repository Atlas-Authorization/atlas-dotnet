using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class CustomDomain
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "custom_domain";
        public string Id { get; init; } = "";
        public string Role { get; init; } = "";
        public string Host { get; init; } = "";
        public string Status { get; init; } = "";
        public string Action { get; init; } = "";
        public bool Live { get; init; }
        public string CnameTarget { get; init; } = "";
        public long? LastCheckedAt { get; init; }
        public string? LastObservedTarget { get; init; }
        public string? FailureReason { get; init; }
        public long? CertificateExpiresAt { get; init; }
        public string? CookieDomain { get; init; }
        /// <summary>Present on create; the DNS records to add. Absent on list rows.</summary>
        public Dictionary<string, object?>? Instructions { get; init; }
        /// <summary>Present on <see cref="DomainsResource.VerifyAsync"/>.</summary>
        public bool? Verified { get; init; }
    }

    public sealed class CookieCheck
    {
        public string Origin { get; init; } = "";
        public bool FirstParty { get; init; }
    }

    public sealed class ListDomainsResponse
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<CustomDomain> Data { get; init; } = new List<CustomDomain>();
        public string CnameTarget { get; init; } = "";
        public Dictionary<string, object?> Instructions { get; init; } = new Dictionary<string, object?>();
        public List<CookieCheck> CookieChecks { get; init; } = new List<CookieCheck>();
        public bool Required { get; init; }
        public string? Environment { get; init; }
    }

    public sealed class CreateDomainBody
    {
        public string Host { get; set; } = "";
        public string? Role { get; set; }
    }

    /// <summary>The custom-domains namespace (<c>/v1/domains</c>).</summary>
    public sealed class DomainsResource : ResourceBase
    {
        public DomainsResource(AtlasTransport transport) : base(transport) { }

        public Task<ListDomainsResponse> ListAsync(CancellationToken ct = default)
            => Req<ListDomainsResponse>(HttpVerb.Get, "/v1/domains", cancellationToken: ct);

        public Task<CustomDomain> CreateAsync(CreateDomainBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<CustomDomain>(HttpVerb.Post, "/v1/domains", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<CustomDomain> VerifyAsync(string id, CancellationToken ct = default)
            => Req<CustomDomain>(HttpVerb.Post, $"/v1/domains/{Enc(id)}/verify", cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/domains/{Enc(id)}", cancellationToken: ct);
    }
}
