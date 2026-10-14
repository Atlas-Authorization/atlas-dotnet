using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class TestUsersPurged
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        public long Purged { get; init; }
        public bool HasMore { get; init; }
    }

    public sealed class TestInstance
    {
        [JsonPropertyName("object")] public string? ObjectType { get; init; }
        public string? InstanceId { get; init; }
        public string? Environment { get; init; }
        public string? PublishableKey { get; init; }
        public bool? Created { get; init; }
        /// <summary>The sk_test — returned ONLY on create/rotate. Store it now.</summary>
        public string? SecretKey { get; init; }
    }

    public sealed class TestingToken
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "";
        /// <summary>The signed testing token.</summary>
        public string Jwt { get; init; } = "";
        public string TokenUse { get; init; } = "";
        /// <summary>Lifetime in seconds.</summary>
        public long ExpiresIn { get; init; }
    }

    /// <summary>Test helpers for development instances.</summary>
    public sealed class TestingResource : ResourceBase
    {
        public TestingResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>POST /v1/test_users/purge</c> — Dispose test users in bulk (development instances only): soft-delete every user whose email carries a reserved test token (+clerk_test / +atlas_test).</summary>
        public Task<TestUsersPurged> PurgeTestUsersAsync(long? limit = null, CancellationToken ct = default)
            => Req<TestUsersPurged>(HttpVerb.Post, "/v1/test_users/purge", Body(("limit", limit)), cancellationToken: ct);

        /// <summary><c>POST /v1/test_instance</c> — Get (creating on first call) your production instance's paired test-mode (development) sibling — an isolated instance with its own pk_test/sk_test, user pool and JWKS.</summary>
        public Task<TestInstance> CreateTestInstanceAsync(bool? rotateSecret = null, CancellationToken ct = default)
            => Req<TestInstance>(HttpVerb.Post, "/v1/test_instance", Body(("rotate_secret", rotateSecret)), cancellationToken: ct);

        /// <summary><c>POST /v1/testing_tokens</c> — Mint a short-lived signed testing token (development instances only) that @atlasauth/testing presents to the sign-in gate to skip the human-only CAPTCHA.</summary>
        public Task<TestingToken> CreateTestingTokenAsync(CancellationToken ct = default)
            => Req<TestingToken>(HttpVerb.Post, "/v1/testing_tokens", cancellationToken: ct);
    }
}
