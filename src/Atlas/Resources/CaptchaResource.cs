using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class VerifyCaptchaBody
    {
        public string? Token { get; set; }
        public string? Action { get; set; }
        public string? RemoteIp { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public VerifyCaptchaBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class CaptchaVerification
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public bool Success { get; init; }
        public string? Action { get; init; }
        public string Provider { get; init; } = "";
        public double? Score { get; init; }
        public string? Verdict { get; init; }
        public string? ChallengeTs { get; init; }
        public Dictionary<string, object?>? Meta { get; init; }
    }

    /// <summary>Server-side captcha verification (<c>/v1/captcha/verify</c>).</summary>
    public sealed class CaptchaResource : ResourceBase
    {
        public CaptchaResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>POST /v1/captcha/verify</c> — §5 "captcha anywhere": verify a CAPTCHA token server-side against the instance's centrally-configured provider.</summary>
        public Task<CaptchaVerification> VerifyAsync(VerifyCaptchaBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<CaptchaVerification>(HttpVerb.Post, "/v1/captcha/verify", body, idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
