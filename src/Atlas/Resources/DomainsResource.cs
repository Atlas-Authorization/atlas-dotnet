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
        /// <summary><c>cname</c>, or <c>path</c>: a path mount on your own host, proven by an https file (no DNS).</summary>
        public string? Method { get; init; }
        /// <summary>Null for a path mount (no CNAME).</summary>
        public string? CnameTarget { get; init; }
        public long? LastCheckedAt { get; init; }
        public string? LastObservedTarget { get; init; }
        public string? FailureReason { get; init; }
        public long? CertificateExpiresAt { get; init; }
        public string? CookieDomain { get; init; }
        /// <summary>Present on create; the DNS records to add (a verified path mount: nothing to add, or the reverse-proxy rule). Absent on list rows.</summary>
        public Dictionary<string, object?>? Instructions { get; init; }
        /// <summary>Present on <see cref="DomainsResource.VerifyAsync"/>.</summary>
        public bool? Verified { get; init; }
        /// <summary>On <see cref="DomainsResource.VerifyAsync"/>: the instance's issuer once a verified host serves it.</summary>
        public string? Issuer { get; init; }
        public string? Note { get; init; }
        /// <summary>Path mount: the prefix the Frontend API is served under.</summary>
        public string? BasePath { get; init; }
        /// <summary>Path mount, once verified: <c>atlas_ingress</c> (Atlas routes it) or <c>customer_proxy</c> (your reverse proxy does).</summary>
        public string? Routing { get; init; }
        /// <summary>Path mount: <c>https://&lt;host&gt;&lt;base_path&gt;</c>, the Frontend API and (once verified) the issuer.</summary>
        public string? FrontendApiUrl { get; init; }
        /// <summary>Path mount: publish <c>Content</c> at <c>Url</c> (status 200, no redirect), then verify.</summary>
        public CustomDomainVerification? Verification { get; init; }
        /// <summary>DNS automation (the API token is never returned), or null.</summary>
        public CustomDomainDns? Dns { get; init; }
    }

    public sealed class CustomDomainVerification
    {
        /// <summary><c>https_file</c>.</summary>
        public string Method { get; init; } = "";
        public string Url { get; init; } = "";
        public string Token { get; init; } = "";
        public string Content { get; init; } = "";
        public string Instructions { get; init; } = "";
    }

    public sealed class CustomDomainDns
    {
        public string Provider { get; init; } = "";
        public bool TokenSet { get; init; }
        /// <summary>Managed email: <c>pending</c>, <c>verifying</c>, <c>verified</c> or <c>failed</c> (Atlas writes the records and verifies in the background); null on older setups.</summary>
        public string? Status { get; init; }
        public string? LastError { get; init; }
        public int? Attempts { get; init; }
        /// <summary>Epoch ms of the next background check.</summary>
        public long? NextCheckAt { get; init; }
        /// <summary>Epoch ms.</summary>
        public long? StartedAt { get; init; }
    }

    /// <summary>Let Atlas write the domain's DNS record. The token is write-only.</summary>
    public sealed class CustomDomainDnsInput
    {
        /// <summary><c>cloudflare</c>.</summary>
        public string Provider { get; set; } = "cloudflare";
        public string ApiToken { get; set; } = "";
        /// <summary>Replace a conflicting existing record.</summary>
        public bool? Overwrite { get; set; }
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
        public string? Host { get; set; }
        /// <summary>Alias of <see cref="Host"/>.</summary>
        public string? Hostname { get; set; }
        /// <summary><c>cname</c> (the default) or <c>path</c>: the Frontend API under <see cref="BasePath"/> on a host you already run.</summary>
        public string? Method { get; set; }
        /// <summary>Path mount: default <c>/__atlas</c>.</summary>
        public string? BasePath { get; set; }
        public string? Role { get; set; }
        /// <summary>Optional: have Atlas write the DNS record.</summary>
        public CustomDomainDnsInput? Dns { get; set; }
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
