using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class LtiPlatform
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "lti_platform";
        public string Id { get; init; } = "";
        public string? OrganizationId { get; init; }
        public string Issuer { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string AuthLoginUrl { get; init; } = "";
        public string JwksUri { get; init; } = "";
        public List<string> DeploymentIds { get; init; } = new List<string>();
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class CreateLtiPlatformBody
    {
        public string Issuer { get; set; } = "";
        public string ClientId { get; set; } = "";
        /// <summary>An absolute https URL.</summary>
        public string AuthLoginUrl { get; set; } = "";
        /// <summary>An absolute https URL.</summary>
        public string JwksUri { get; set; } = "";
        /// <summary>At least one deployment id is required.</summary>
        public List<string> DeploymentIds { get; set; } = new List<string>();
        public string? OrganizationId { get; set; }
    }

    public sealed class UpdateLtiPlatformBody
    {
        public string? AuthLoginUrl { get; set; }
        public string? JwksUri { get; set; }
        public List<string>? DeploymentIds { get; set; }
        /// <summary>Pass <c>null</c> to unbind the platform from an organization.</summary>
        public string? OrganizationId { get; set; }
    }

    /// <summary>The LTI 1.3 platforms namespace (<c>/v1/lti_platforms</c>).</summary>
    public sealed class LtiPlatformsResource : ResourceBase
    {
        public LtiPlatformsResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<LtiPlatform>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<LtiPlatform>>(HttpVerb.Get, "/v1/lti_platforms", cancellationToken: ct);

        public Task<LtiPlatform> GetAsync(string id, CancellationToken ct = default)
            => Req<LtiPlatform>(HttpVerb.Get, $"/v1/lti_platforms/{Enc(id)}", cancellationToken: ct);

        public Task<LtiPlatform> CreateAsync(CreateLtiPlatformBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<LtiPlatform>(HttpVerb.Post, "/v1/lti_platforms", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<LtiPlatform> UpdateAsync(string id, UpdateLtiPlatformBody body, CancellationToken ct = default)
            => Req<LtiPlatform>(HttpVerb.Patch, $"/v1/lti_platforms/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/lti_platforms/{Enc(id)}", cancellationToken: ct);
    }
}
