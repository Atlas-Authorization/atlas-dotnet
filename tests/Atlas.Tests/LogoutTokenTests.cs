using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas.AspNetCore;
using Atlas.Verification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>OIDC Back-Channel Logout 1.0 §2.6 checks, with a real RSA key served as the JWKS.</summary>
    public class LogoutTokenTests
    {
        private const string Issuer = "https://atlas.test";
        private const string ClientId = "atlas_client_1";
        private const string Kid = "logout-key-1";
        private const long NowMs = 1_800_000_000_000L;
        private const long NowSec = NowMs / 1000;

        private readonly RSA _rsa = RSA.Create(2048);
        private readonly RSA _other = RSA.Create(2048);

        private string Jwks()
        {
            var p = _rsa.ExportParameters(false);
            return "{\"keys\":[{\"kty\":\"RSA\",\"kid\":\"" + Kid + "\",\"use\":\"sig\",\"alg\":\"RS256\",\"n\":\""
                   + B64(p.Modulus!) + "\",\"e\":\"" + B64(p.Exponent!) + "\"}]}";
        }

        private LogoutTokenVerifier Verifier() => new LogoutTokenVerifier(new LogoutTokenVerifierOptions
        {
            JwksUrl = "https://jwks.test/keys",
            Issuer = Issuer,
            ClientId = ClientId,
            HttpClient = new HttpClient(MockHttpMessageHandler.Json(Jwks())),
            Now = () => NowMs,
        });

        private static Dictionary<string, object?> ValidClaims() => new Dictionary<string, object?>
        {
            ["iss"] = Issuer,
            ["sub"] = "usr_1",
            ["aud"] = ClientId,
            ["iat"] = NowSec - 1,
            ["exp"] = NowSec + 120,
            ["jti"] = "jti_1",
            ["sid"] = "sess_1",
            ["events"] = new Dictionary<string, object> { [LogoutTokenVerifier.BackchannelLogoutEvent] = new Dictionary<string, object>() },
        };

        private string Sign(Dictionary<string, object?> claims, string? typ = "logout+jwt", RSA? key = null)
        {
            var header = new Dictionary<string, object> { ["alg"] = "RS256", ["kid"] = Kid };
            if (typ != null) header["typ"] = typ;
            var payload = new Dictionary<string, object?>();
            foreach (var kv in claims) if (kv.Value != null) payload[kv.Key] = kv.Value;
            var signingInput = B64(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header))) + "."
                               + B64(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
            var sig = (key ?? _rsa).SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return signingInput + "." + B64(sig);
        }

        private string Token(Action<Dictionary<string, object?>> edit)
        {
            var c = ValidClaims();
            edit(c);
            return Sign(c);
        }

        private async Task AssertReason(LogoutTokenFailure reason, string token)
        {
            var e = await Assert.ThrowsAsync<LogoutTokenException>(() => Verifier().VerifyAsync(token));
            Assert.Equal(reason, e.Reason);
        }

        [Fact]
        public async Task Valid_ReturnsClaims()
        {
            var c = await Verifier().VerifyAsync(Token(_ => { }));
            Assert.Equal("usr_1", c.Sub);
            Assert.Equal("sess_1", c.Sid);
            Assert.Equal("jti_1", c.Jti);
            Assert.Equal(NowSec - 1, c.Iat);
        }

        [Fact]
        public async Task FormBody_IsParsed()
        {
            var body = "a=b&logout_token=" + Uri.EscapeDataString(Token(_ => { }));
            Assert.Equal("sess_1", (await Verifier().VerifyFormBodyAsync(body)).Sid);
            await Assert.ThrowsAsync<LogoutTokenException>(() => Verifier().VerifyFormBodyAsync("a=b"));
        }

        [Fact]
        public async Task SidOrSub_IsEnough_ButOneIsRequired()
        {
            Assert.Null((await Verifier().VerifyAsync(Token(c => c["sub"] = null))).Sub);
            Assert.Null((await Verifier().VerifyAsync(Token(c => c["sid"] = null))).Sid);
            await AssertReason(LogoutTokenFailure.MissingSubject, Token(c => { c["sub"] = null; c["sid"] = null; }));
        }

        [Fact]
        public async Task WrongTyp_IsRefused_ButAbsentTypIsFine()
        {
            await AssertReason(LogoutTokenFailure.WrongType, Sign(ValidClaims(), "JWT"));
            Assert.Equal("jti_1", (await Verifier().VerifyAsync(Sign(ValidClaims(), null))).Jti);
        }

        [Fact]
        public Task Nonce_IsRefused() => AssertReason(LogoutTokenFailure.HasNonce, Token(c => c["nonce"] = "n"));

        [Fact]
        public async Task MissingOrWrongEvents_AreRefused()
        {
            await AssertReason(LogoutTokenFailure.MissingEvent, Token(c => c["events"] = null));
            await AssertReason(LogoutTokenFailure.MissingEvent,
                Token(c => c["events"] = new Dictionary<string, object> { ["other"] = new Dictionary<string, object>() }));
            await AssertReason(LogoutTokenFailure.MissingEvent,
                Token(c => c["events"] = new Dictionary<string, object> { [LogoutTokenVerifier.BackchannelLogoutEvent] = "yes" }));
        }

        [Fact]
        public async Task WrongAudience_IsRefused_AudienceArrayIsAccepted()
        {
            await AssertReason(LogoutTokenFailure.WrongAudience, Token(c => c["aud"] = "someone_else"));
            Assert.Equal("jti_1", (await Verifier().VerifyAsync(Token(c => c["aud"] = new[] { "x", ClientId }))).Jti);
        }

        [Fact]
        public Task WrongIssuer_IsRefused() => AssertReason(LogoutTokenFailure.WrongIssuer, Token(c => c["iss"] = "https://evil.test"));

        [Fact]
        public Task Expired_IsRefused() => AssertReason(LogoutTokenFailure.Expired, Token(c => c["exp"] = NowSec - 120));

        [Fact]
        public async Task Iat_Rules()
        {
            await AssertReason(LogoutTokenFailure.MissingIat, Token(c => c["iat"] = null));
            await AssertReason(LogoutTokenFailure.IssuedInFuture, Token(c => c["iat"] = NowSec + 600));
        }

        [Fact]
        public Task MissingJti_IsRefused() => AssertReason(LogoutTokenFailure.MissingJti, Token(c => c["jti"] = null));

        [Fact]
        public Task BadSignature_IsRefused() => AssertReason(LogoutTokenFailure.BadSignature, Sign(ValidClaims(), "logout+jwt", _other));

        [Fact]
        public async Task Garbage_IsMalformed()
        {
            await AssertReason(LogoutTokenFailure.Malformed, "not-a-jwt");
            await AssertReason(LogoutTokenFailure.Malformed, "");
        }

        // ---- ASP.NET Core endpoint helper ----------------------------------------

        private TestServer LogoutServer(List<LogoutTokenClaims> ended, LogoutTokenVerifier? withVerifier = null)
        {
            var verifier = withVerifier ?? Verifier();
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services.AddRouting())
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapAtlasBackchannelLogout("/logout/backchannel", verifier, (claims, _) =>
                    {
                        ended.Add(claims);
                        return Task.CompletedTask;
                    }));
                });
            return new TestServer(builder);
        }

        [Fact]
        public async Task Endpoint_ValidToken_Is200_NoStore_AndEndsTheSession()
        {
            var ended = new List<LogoutTokenClaims>();
            using var server = LogoutServer(ended);
            var res = await server.CreateClient().PostAsync("/logout/backchannel",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["logout_token"] = Token(_ => { }) }));
            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
            Assert.True(res.Headers.CacheControl!.NoStore);
            Assert.Equal("sess_1", Assert.Single(ended).Sid);
        }

        [Fact]
        public async Task Endpoint_InvalidToken_Is400_NoStore()
        {
            var ended = new List<LogoutTokenClaims>();
            using var server = LogoutServer(ended);
            var res = await server.CreateClient().PostAsync("/logout/backchannel",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["logout_token"] = Token(c => c["nonce"] = "n") }));
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, res.StatusCode);
            Assert.True(res.Headers.CacheControl!.NoStore);
            Assert.Empty(ended);
        }

        private LogoutTokenVerifier UnreachableVerifier() => new LogoutTokenVerifier(new LogoutTokenVerifierOptions
        {
            JwksUrl = "https://jwks.test/keys",
            Issuer = Issuer,
            ClientId = ClientId,
            HttpClient = new HttpClient(MockHttpMessageHandler.Json("down", System.Net.HttpStatusCode.ServiceUnavailable)),
            Now = () => NowMs,
        });

        [Fact]
        public async Task UnreachableJwks_IsNoKeys_AndRetryable()
        {
            var e = await Assert.ThrowsAsync<LogoutTokenException>(() => UnreachableVerifier().VerifyAsync(Token(_ => { })));
            Assert.Equal(LogoutTokenFailure.NoKeys, e.Reason);
            Assert.True(e.Retryable);
        }

        [Fact]
        public async Task BadToken_IsNotRetryable()
        {
            var e = await Assert.ThrowsAsync<LogoutTokenException>(() => Verifier().VerifyAsync(Token(c => c["aud"] = "someone-else")));
            Assert.False(e.Retryable);
        }

        [Fact]
        public async Task Endpoint_UnreachableJwks_Is503_WithRetryAfter()
        {
            var ended = new List<LogoutTokenClaims>();
            using var server = LogoutServer(ended, UnreachableVerifier());
            var res = await server.CreateClient().PostAsync("/logout/backchannel",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["logout_token"] = Token(_ => { }) }));
            Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, res.StatusCode);
            Assert.NotNull(res.Headers.RetryAfter);
            Assert.True(res.Headers.CacheControl!.NoStore);
            Assert.Empty(ended);
        }

        private static string B64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
