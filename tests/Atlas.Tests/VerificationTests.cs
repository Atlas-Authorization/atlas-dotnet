using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Atlas.Verification;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Atlas.Tests
{
    public class VerificationTests
    {
        private const string Issuer = "https://issuer.test";
        private const string Kid = "test-key-1";

        // A per-test RSA key: its public half becomes the served JWKS, its private
        // half signs the tokens under test.
        private readonly RSA _rsa = RSA.Create(2048);

        private string Jwks()
        {
            var p = _rsa.ExportParameters(false);
            var n = Base64UrlEncoder.Encode(p.Modulus);
            var e = Base64UrlEncoder.Encode(p.Exponent);
            return "{\"keys\":[{\"kty\":\"RSA\",\"kid\":\"" + Kid + "\",\"use\":\"sig\",\"alg\":\"RS256\",\"n\":\"" + n + "\",\"e\":\"" + e + "\"}]}";
        }

        private string Sign(
            IDictionary<string, object> claims,
            string issuer = Issuer,
            string? kid = Kid,
            DateTime? expires = null,
            DateTime? notBefore = null,
            string? audience = null)
        {
            var key = new RsaSecurityKey(_rsa) { KeyId = kid };
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Claims = claims,
                Expires = expires ?? DateTime.UtcNow.AddMinutes(1),
                NotBefore = notBefore ?? DateTime.UtcNow.AddMinutes(-1),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
            };
            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        private AtlasBackend Backend(IReadOnlyList<string>? authorizedParties = null)
            => new AtlasBackend(new AtlasBackendOptions
            {
                JwksUrl = "https://jwks.test/keys",
                Issuer = Issuer,
                AuthorizedParties = authorizedParties,
                HttpClient = new HttpClient(MockHttpMessageHandler.Json(Jwks())),
            });

        private static Dictionary<string, object> SessionClaimsBag() => new Dictionary<string, object>
        {
            ["sub"] = "user_1",
            ["sid"] = "sess_1",
            ["token_use"] = "session",
            ["org_role"] = "admin",
            ["org_permissions"] = new[] { "posts:read", "posts:write" },
        };

        [Fact]
        public async Task Verify_ValidToken_Succeeds_AndBindsClaims()
        {
            var token = Sign(SessionClaimsBag());
            var result = await Backend().VerifyAsync(token);

            Assert.True(result.Ok);
            Assert.NotNull(result.Session);
            Assert.Equal("user_1", result.Claims!.Sub);
            Assert.Equal("sess_1", result.Claims.Sid);
            Assert.Equal("admin", result.Claims.OrgRole);
        }

        [Fact]
        public async Task Verify_BoundHasAndProtect_EvaluateAgainstClaims()
        {
            var token = Sign(SessionClaimsBag());
            var result = await Backend().VerifyAsync(token);
            var session = result.Session!;

            Assert.True(session.Has(new ProtectCondition { Role = "admin" }));
            Assert.True(session.Has(new ProtectCondition { Permission = "posts:read" }));
            Assert.False(session.Has(new ProtectCondition { Permission = "billing:write" }));
            Assert.True(session.Has()); // empty condition = signed in

            session.Protect(new ProtectCondition { Permission = "posts:write" }); // no throw
            Assert.Throws<ForbiddenError>(() => session.Protect(new ProtectCondition { Role = "owner" }));
        }

        [Fact]
        public async Task Verify_WrongIssuer_IsInvalid()
        {
            var token = Sign(SessionClaimsBag(), issuer: "https://evil.test");
            var result = await Backend().VerifyAsync(token);

            Assert.False(result.Ok);
            Assert.Equal(VerifyFailureReason.Invalid, result.Reason);
        }

        [Fact]
        public async Task Verify_ExpiredToken_IsInvalid()
        {
            var token = Sign(SessionClaimsBag(),
                notBefore: DateTime.UtcNow.AddMinutes(-10),
                expires: DateTime.UtcNow.AddMinutes(-5));
            var result = await Backend().VerifyAsync(token);

            Assert.False(result.Ok);
            Assert.Equal(VerifyFailureReason.Invalid, result.Reason);
        }

        [Fact]
        public async Task Verify_MalformedToken_IsMalformed()
        {
            var result = await Backend().VerifyAsync("not-a-jwt");
            Assert.False(result.Ok);
            Assert.Equal(VerifyFailureReason.Malformed, result.Reason);
        }

        [Fact]
        public async Task Verify_TokenConfusion_OpAccessTokenIsRejected()
        {
            var bag = SessionClaimsBag();
            bag["token_use"] = "access_token"; // OP marker, not a session
            var token = Sign(bag);
            var result = await Backend().VerifyAsync(token);

            Assert.False(result.Ok);
            Assert.Equal(VerifyFailureReason.Invalid, result.Reason);
        }

        [Fact]
        public async Task Verify_TokenConfusion_IdTokenWithAudIsRejected()
        {
            var token = Sign(SessionClaimsBag(), audience: "some-rp-client-id");
            var result = await Backend().VerifyAsync(token);

            Assert.False(result.Ok);
            Assert.Equal(VerifyFailureReason.Invalid, result.Reason);
        }

        [Fact]
        public async Task Verify_MissingTokenUseAndAud_StillValid_ForBackwardCompat()
        {
            var bag = SessionClaimsBag();
            bag.Remove("token_use");
            var token = Sign(bag);
            var result = await Backend().VerifyAsync(token);

            Assert.True(result.Ok);
        }

        [Fact]
        public async Task Verify_AuthorizedParties_RejectsForeignAzp()
        {
            var bag = SessionClaimsBag();
            bag["azp"] = "https://other-app.test";
            var token = Sign(bag);
            var result = await Backend(new[] { "https://app.test" }).VerifyAsync(token);

            Assert.False(result.Ok);
            Assert.Equal(VerifyFailureReason.UnauthorizedParty, result.Reason);
        }

        [Fact]
        public async Task Verify_AuthorizedParties_AcceptsListedAzp()
        {
            var bag = SessionClaimsBag();
            bag["azp"] = "https://app.test";
            var token = Sign(bag);
            var result = await Backend(new[] { "https://app.test" }).VerifyAsync(token);

            Assert.True(result.Ok);
        }

        [Fact]
        public async Task AuthenticateRequest_ReadsBearerHeader()
        {
            var token = Sign(SessionClaimsBag());
            var headers = new Dictionary<string, string> { ["authorization"] = "Bearer " + token };
            var result = await Backend().AuthenticateRequestAsync(headers);

            Assert.True(result.Ok);
            Assert.Equal("user_1", result.Claims!.Sub);
        }

        [Fact]
        public async Task AuthenticateRequest_FallsBackToSessionCookie()
        {
            var token = Sign(SessionClaimsBag());
            var headers = new Dictionary<string, string> { ["Cookie"] = "foo=bar; __session=" + token };
            var result = await Backend().AuthenticateRequestAsync(headers);

            Assert.True(result.Ok);
            Assert.Equal("sess_1", result.Claims!.Sid);
        }

        [Fact]
        public async Task AuthenticateRequest_NoToken_IsMalformed()
        {
            var result = await Backend().AuthenticateRequestAsync(new Dictionary<string, string>());
            Assert.False(result.Ok);
            Assert.Equal(VerifyFailureReason.Malformed, result.Reason);
        }

        // ---------------- JWKS cache throttle ----------------

        [Fact]
        public async Task JwksCache_KidMiss_IsThrottledToOncePerMinute()
        {
            long now = 1_000;
            var handler = MockHttpMessageHandler.Json(Jwks());
            var cache = new JwksCache("https://jwks.test/keys", new HttpClient(handler), () => now);

            // First lookup: cache empty, so it fetches.
            var first = await cache.GetAsync(Kid);
            Assert.NotNull(first);
            Assert.Equal(FetchOutcome.Fresh, cache.LastOutcome);
            Assert.Single(handler.Requests);

            // Known kid, still fresh: served from cache, no fetch.
            await cache.GetAsync(Kid);
            Assert.Equal(FetchOutcome.Cached, cache.LastOutcome);
            Assert.Single(handler.Requests);

            // Unknown kid inside the window: throttled — the amplification guard.
            await cache.GetAsync("attacker-random-kid");
            Assert.Equal(FetchOutcome.Throttled, cache.LastOutcome);
            Assert.Single(handler.Requests);

            // Past the refetch interval, an unknown kid is allowed to refetch once.
            now += JwksCache.RefetchIntervalMs + 1;
            await cache.GetAsync("another-unknown-kid");
            Assert.Equal(FetchOutcome.Refetched, cache.LastOutcome);
            Assert.Equal(2, handler.Requests.Count);
        }

        [Fact]
        public void ReadKid_ExtractsHeaderKid()
        {
            var token = Sign(SessionClaimsBag());
            Assert.Equal(Kid, JwksCache.ReadKid(token));
        }

        // ---------------- Handshake PKCE (RFC 7636, S256) ----------------

        private static string Base64UrlNoPad(byte[] bytes)
            => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        [Fact]
        public void GeneratePkce_ChallengeIsBase64UrlSha256OfVerifier_NoPadding()
        {
            var pair = Handshake.GeneratePkce();

            // Verifier: base64url (no padding) of 32 random bytes = 43 chars, and
            // strictly within the base64url alphabet (no '+', '/', or '=').
            Assert.Equal(43, pair.Verifier.Length);
            Assert.DoesNotContain('+', pair.Verifier);
            Assert.DoesNotContain('/', pair.Verifier);
            Assert.DoesNotContain('=', pair.Verifier);
            Assert.DoesNotContain('+', pair.Challenge);
            Assert.DoesNotContain('/', pair.Challenge);
            Assert.DoesNotContain('=', pair.Challenge);

            // Challenge == base64url( SHA256( ASCII bytes of verifier ) ), recomputed
            // independently here — this is exactly what the Atlas server checks.
            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(Encoding.ASCII.GetBytes(pair.Verifier));
            }
            Assert.Equal(Base64UrlNoPad(hash), pair.Challenge);
        }

        [Fact]
        public void GeneratePkce_ProducesAFreshPairEachCall()
        {
            var a = Handshake.GeneratePkce();
            var b = Handshake.GeneratePkce();
            Assert.NotEqual(a.Verifier, b.Verifier);
            Assert.NotEqual(a.Challenge, b.Challenge);
        }

        [Fact]
        public void BuildHandshakeUrl_CarriesChallengeInUrl_AndReturnsMatchingVerifier()
        {
            var bounce = Handshake.BuildHandshakeUrl("https://id.atlas.test/");

            Assert.StartsWith("https://id.atlas.test/v1/client/handshake?code_challenge=", bounce.Url);
            // The verifier must NOT ride the URL — only the derived challenge does.
            Assert.DoesNotContain(bounce.CodeVerifier, bounce.Url);

            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(Encoding.ASCII.GetBytes(bounce.CodeVerifier));
            }
            Assert.Contains("code_challenge=" + Base64UrlNoPad(hash), bounce.Url);
        }

        [Fact]
        public async Task RedeemHandshake_SendsCodeVerifierInBody_AndParsesSession()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"jwt\":\"ey.j.wt\",\"refresh_token\":\"rt_1\",\"session_id\":\"sess_1\",\"expires_in\":60}");

            var session = await Handshake.RedeemHandshakeAsync(
                "https://id.atlas.test",
                "pk_test_123",
                "user_1",
                "nonce_abc",
                "verifier_xyz",
                httpClient: new HttpClient(handler));

            Assert.NotNull(session);
            Assert.Equal("ey.j.wt", session!.Jwt);
            Assert.Equal("rt_1", session.RefreshToken);

            var req = Assert.Single(handler.Requests);
            Assert.Equal("POST", req.Method);
            Assert.Equal("/v1/client/handshake/redeem", req.Path);
            // The new PKCE binding: code_verifier must be in the redeem body,
            // alongside the existing user_id and nonce.
            Assert.Contains("\"code_verifier\":\"verifier_xyz\"", req.Body);
            Assert.Contains("\"nonce\":\"nonce_abc\"", req.Body);
            Assert.Contains("\"user_id\":\"user_1\"", req.Body);
        }

        [Fact]
        public async Task RedeemHandshake_MissingVerifier_ReturnsNull_WithoutCallingServer()
        {
            var handler = MockHttpMessageHandler.Json("{}", HttpStatusCode.OK);

            var session = await Handshake.RedeemHandshakeAsync(
                "https://id.atlas.test", "pk_test_123", "user_1", "nonce_abc", "",
                httpClient: new HttpClient(handler));

            // A verifier is mandatory now; without it there is nothing to redeem and
            // we short-circuit to the signed-out signal rather than hit the server.
            Assert.Null(session);
            Assert.Empty(handler.Requests);
        }
    }
}
