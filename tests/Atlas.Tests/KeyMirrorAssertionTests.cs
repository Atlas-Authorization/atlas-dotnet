using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Primitives;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>The key mirror verifies key-pair assertions locally with the server's refusal codes; a disabled row is refused.</summary>
    public class KeyMirrorAssertionTests
    {
        private sealed class NoApi : IKeyMirrorApi
        {
            public System.Threading.Tasks.Task<Atlas.Resources.ApiKeyWithSecret> CreateAsync(Atlas.Resources.CreateApiKeyBody b, string? i, System.Threading.CancellationToken ct) => throw new NotSupportedException();
            public Task DeleteAsync(string id, System.Threading.CancellationToken ct) => Task.CompletedTask;
            public Task<(System.Collections.Generic.IReadOnlyList<(string Id, long? RevokedAt, string? Reason)> Data, bool HasMore, string? NextCursor)> ListRevokedAsync(string? since, System.Threading.CancellationToken ct)
                => throw new NotSupportedException();
            public Task<Atlas.Resources.ApiKeyVerification?> VerifyAsync(string s, string? ip, System.Threading.CancellationToken ct) => Task.FromResult<Atlas.Resources.ApiKeyVerification?>(null);
            public Task<Atlas.Resources.ApiKey?> GetAsync(string id, System.Threading.CancellationToken ct) => Task.FromResult<Atlas.Resources.ApiKey?>(null);
        }

        private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256)!;
        private readonly InMemoryKeyMirrorStore _store = new InMemoryKeyMirrorStore();
        private readonly KeyMirror _mirror;
        private long _offsetMs;

        public KeyMirrorAssertionTests()
        {
            _mirror = new KeyMirror(new KeyMirrorOptions { Store = _store, Api = new NoApi(), Now = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _offsetMs });
            _store.PutAsync(new MirroredApiKey
            {
                Id = "key_1", Hash = "key_pair:key_1", SubjectType = "machine", SubjectId = "m1", Scopes = new[] { "read", "write" },
                Credential = "key_pair", Audience = "ins_1",
                PublicKeyPem = "-----BEGIN PUBLIC KEY-----\n" + Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo()) + "\n-----END PUBLIC KEY-----\n",
            }).GetAwaiter().GetResult();
        }

        private string Sign(string audience = "ins_1", int ttl = 300, string? scope = null, string? jti = null) =>
            ApiKeyAssertion.Sign("-----BEGIN PRIVATE KEY-----\n" + Convert.ToBase64String(_key.ExportPkcs8PrivateKey()) + "\n-----END PRIVATE KEY-----\n", "key_1", audience, ttl, scope, jti);

        [Fact]
        public async Task ValidAssertionNarrowsScope()
        {
            var v = await _mirror.VerifyAssertionAsync(Sign(scope: "read", jti: "a"), new[] { "read" });
            Assert.True(v.Valid);
            Assert.Equal("key_pair", v.Credential);
            Assert.Equal("a", v.Jti);
            Assert.Equal(new[] { "read" }, v.EffectiveScopes.ToArray());
            Assert.Equal(KeyMirrorCode.ScopeNotGranted, (await _mirror.VerifyAssertionAsync(Sign(scope: "admin"))).Code);
        }

        [Fact]
        public async Task RefusalCodesAndJtiClaimedLast()
        {
            Assert.Equal(KeyMirrorCode.WrongAudience, (await _mirror.VerifyAssertionAsync(Sign("other"))).Code);
            Assert.Equal(KeyMirrorCode.AssertionExpired, (await _mirror.VerifyAssertionAsync(Sign(ttl: -100))).Code);
            Assert.Equal(KeyMirrorCode.AssertionLifetimeTooLong, (await _mirror.VerifyAssertionAsync(Sign(ttl: 7200))).Code);
            Assert.Equal(KeyMirrorCode.InvalidAssertion, (await _mirror.VerifyAssertionAsync("a.b.c")).Code);
            var token = Sign(jti: "j1");
            Assert.Equal(KeyMirrorCode.InsufficientScope, (await _mirror.VerifyAssertionAsync(token, new[] { "nope" })).Code);
            Assert.True((await _mirror.VerifyAssertionAsync(token)).Valid);
            Assert.Equal(KeyMirrorCode.AssertionReplayed, (await _mirror.VerifyAssertionAsync(token)).Code);
            _offsetMs = -600_000;
            Assert.Equal(KeyMirrorCode.AssertionNotYetValid, (await _mirror.VerifyAssertionAsync(Sign())).Code);
        }

        [Fact]
        public async Task DisabledRowsAreRefusedAndWebhookCopiesEnabled()
        {
            await _store.PutAsync((await _store.GetAsync("key_1"))! with { Enabled = false });
            Assert.Equal(KeyMirrorCode.InvalidAssertion, (await _mirror.VerifyAssertionAsync(Sign())).Code);
            Assert.True(await _mirror.ApplyWebhookAsync("api_key.updated", JsonDocument.Parse("{\"id\":\"key_1\",\"enabled\":true}").RootElement));
            Assert.True((await _mirror.VerifyAssertionAsync(Sign())).Valid);
            await _store.MarkRevokedAsync("key_1", 1);
            Assert.False((await _mirror.VerifyAssertionAsync(Sign())).Valid);

            await _store.PutAsync(new MirroredApiKey { Id = "k2", Hash = KeyMirror.HashSecret("s"), Enabled = false });
            Assert.False((await _mirror.VerifyAsync("s")).Valid);
            await _mirror.ApplyWebhookAsync("api_key.updated", JsonDocument.Parse("{\"id\":\"k2\",\"enabled\":true}").RootElement);
            Assert.True((await _mirror.VerifyAsync("s")).Valid);
        }
    }
}
