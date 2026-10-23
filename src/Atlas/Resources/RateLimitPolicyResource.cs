using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The min/max a single knob is clamped to on write.</summary>
    public sealed class RateLimitBound
    {
        public int Min { get; init; }
        public int Max { get; init; }
    }

    public sealed class RateLimitBounds
    {
        public RateLimitBound FapiCreatePerMin { get; init; } = new RateLimitBound();
        public RateLimitBound BapiPerMin { get; init; } = new RateLimitBound();
    }

    public sealed class RateLimitPolicy
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "rate_limit_policy";
        /// <summary>Sign-in / sign-up CREATE budget, per IP. Sign-in and sign-up share it.</summary>
        public int FapiCreatePerMin { get; init; }
        /// <summary>Backend-API budget, per secret key.</summary>
        public int BapiPerMin { get; init; }
        /// <summary>The floor/ceiling each knob is bounded to when written.</summary>
        public RateLimitBounds Bounds { get; init; } = new RateLimitBounds();
        public int? FapiVerifyPerMin { get; init; }
        public int? LdapPerIpPerMin { get; init; }
        public int? LdapPerUserPerMin { get; init; }
        /// <summary>Per-endpoint rules (one per group) over the built-in limiter.</summary>
        public List<RateLimitRule> Rules { get; init; } = new List<RateLimitRule>();
        /// <summary>The groups a rule may name, with what each covers and its budget.</summary>
        public JsonElement? Groups { get; init; }
    }

    public sealed class UpdateRateLimitPolicyBody
    {
        public int? FapiCreatePerMin { get; set; }
        public int? BapiPerMin { get; set; }
        public int? FapiVerifyPerMin { get; set; }
        public int? LdapPerIpPerMin { get; set; }
        public int? LdapPerUserPerMin { get; set; }
        /// <summary>The FULL rule list (replaces the stored one; an empty list clears it).</summary>
        public List<RateLimitRule>? Rules { get; set; }
    }

    /// <summary>The rate-limit-policy namespace (<c>/v1/rate_limit_policy</c>).</summary>
    public sealed class RateLimitPolicyResource : ResourceBase
    {
        public RateLimitPolicyResource(AtlasTransport transport) : base(transport) { }

        /// <summary>Read the current policy and the bounds each knob is clamped to.</summary>
        public Task<RateLimitPolicy> GetAsync(CancellationToken ct = default)
            => Req<RateLimitPolicy>(HttpVerb.Get, "/v1/rate_limit_policy", cancellationToken: ct);

        /// <summary>Adjust either budget; omitted knobs are left unchanged.</summary>
        public Task<RateLimitPolicy> UpdateAsync(UpdateRateLimitPolicyBody body, CancellationToken ct = default)
            => Req<RateLimitPolicy>(HttpVerb.Patch, "/v1/rate_limit_policy", body, cancellationToken: ct);
    }

    /// <summary>
    /// One per-endpoint rule: at most <see cref="Max"/> requests per <see cref="WindowSeconds"/> for the named
    /// <see cref="Group"/>, counted per <see cref="Key"/> (<c>ip</c>, <c>user</c> or <c>instance</c>). A rule on a
    /// group that owns a built-in budget replaces it; any other rule adds a budget on top.
    /// </summary>
    public sealed class RateLimitRule
    {
        public string Group { get; set; } = "";
        public int WindowSeconds { get; set; }
        public int Max { get; set; }
        public string Key { get; set; } = "ip";
    }
}
