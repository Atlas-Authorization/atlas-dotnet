using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The last call Atlas made to the <c>before_user_create</c> hook.</summary>
    public sealed class BeforeUserCreateHookLastCall
    {
        /// <summary>Epoch ms.</summary>
        public long At { get; init; }
        /// <summary>ok, timeout, error or invalid_response.</summary>
        public string? Status { get; init; }
        public string? Error { get; init; }
    }

    /// <summary><c>GET /v1/instance/hooks/before_user_create</c>. The signing secret is never returned.</summary>
    public sealed class BeforeUserCreateHook
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "before_user_create_hook";
        public bool Enabled { get; init; }
        public string? Url { get; init; }
        public bool SigningSecretSet { get; init; }
        /// <summary>500-5000, default 3000.</summary>
        public int TimeoutMs { get; init; }
        public BeforeUserCreateHookLastCall? LastCall { get; init; }
        public long? UpdatedAt { get; init; }
    }

    /// <summary>
    /// Body of <c>PATCH /v1/instance/hooks/before_user_create</c>: only the fields set change. Turning it on needs
    /// <see cref="Url"/> and <see cref="SigningSecret"/>. Clear a field with <see cref="Clear"/> (sent as <c>null</c>).
    /// </summary>
    public sealed class UpdateBeforeUserCreateHookBody
    {
        public bool? Enabled { get; set; }
        /// <summary>https (http only for localhost on a development instance).</summary>
        public string? Url { get; set; }
        /// <summary>Write-only, 16-512 characters.</summary>
        public string? SigningSecret { get; set; }
        /// <summary>500-5000.</summary>
        public int? TimeoutMs { get; set; }

        /// <summary>Fields set with <see cref="Clear"/>, written as-is.</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Clear a field by its wire name (<c>url</c>, <c>signing_secret</c>): it is sent as <c>null</c>.</summary>
        public UpdateBeforeUserCreateHookBody Clear(string key)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = null;
            return this;
        }
    }

    /// <summary>What the hook answered to a test.</summary>
    public sealed class BeforeUserCreateHookAnswer
    {
        public string? ExternalId { get; init; }
        public Dictionary<string, object?>? PublicMetadata { get; init; }
        public bool PrivateMetadataSet { get; init; }
        public Dictionary<string, object?>? UnsafeMetadata { get; init; }
    }

    /// <summary><c>POST /v1/instance/hooks/before_user_create/test</c>: a sample draft sent to the hook; nothing is stored.</summary>
    public sealed class BeforeUserCreateHookTest
    {
        public bool Ok { get; init; }
        public int LatencyMs { get; init; }
        public BeforeUserCreateHookAnswer? Answer { get; init; }
        /// <summary><c>Code</c>: timeout, error, invalid_response or not_configured.</summary>
        public ReferralTestError? Error { get; init; }
    }

    /// <summary>
    /// The <c>before_user_create</c> hook (<c>/v1/instance/hooks</c>). Just before Atlas creates a user from a
    /// self-service sign-up, it sends your endpoint a signed <c>POST</c> (the webhook headers; verify it like a webhook)
    /// of type <c>user.before_create</c>, with <c>data</c> <c>{user, strategy, referral}</c>, and stamps the
    /// <c>external_id</c> and metadata you answer. A slow or failing hook never blocks a sign-up.
    /// </summary>
    public sealed class UserHooksResource : ResourceBase
    {
        private const string Path = "/v1/instance/hooks/before_user_create";

        public UserHooksResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>GET /v1/instance/hooks/before_user_create</c> — the hook settings and its last call.</summary>
        public Task<BeforeUserCreateHook> GetBeforeUserCreateAsync(CancellationToken ct = default)
            => Req<BeforeUserCreateHook>(HttpVerb.Get, Path, cancellationToken: ct);

        /// <summary><c>PATCH /v1/instance/hooks/before_user_create</c> — partial update; the secret is write-only.</summary>
        public Task<BeforeUserCreateHook> UpdateBeforeUserCreateAsync(UpdateBeforeUserCreateHookBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<BeforeUserCreateHook>(HttpVerb.Patch, Path, body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/instance/hooks/before_user_create/test</c> — send the hook a sample draft; nothing is stored.</summary>
        public Task<BeforeUserCreateHookTest> TestBeforeUserCreateAsync(CancellationToken ct = default)
            => Req<BeforeUserCreateHookTest>(HttpVerb.Post, Path + "/test", cancellationToken: ct);
    }
}
