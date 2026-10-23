using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Atlas.Verification;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>
    /// App roles, org teams/roles, user search + timed bans, API key controls, billing providers and
    /// subscription management, OAuth server administration, ES256 verification, app-claim helpers and the
    /// access-token verifier.
    /// </summary>
    public class BxSurfaceTests
    {
        private static AtlasClient Client(MockHttpMessageHandler h) => new AtlasClient(new AtlasClientOptions
        {
            SecretKey = "sk_test_123",
            ApiUrl = "https://api.test.example",
            HttpClient = new HttpClient(h),
        });

        private static JsonElement Sent(RecordedRequest r) => JsonDocument.Parse(r.Body).RootElement;

        [Fact]
        public async Task AppRolesTeamsAndOrgRoles()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"x\"}");
            using var c = Client(h);
            await c.AppRoles.CreateAsync("support", "Support", permissions: new[] { "tickets:read" });
            Assert.Equal("/v1/app_roles", h.Requests[0].Path);
            Assert.Equal("support", Sent(h.Requests[0]).GetProperty("key").GetString());
            await c.AppRoles.ListUsersAsync("ar_1", 5, 10);
            Assert.Equal("?limit=5&offset=10", h.Requests[1].Query);
            await c.AppRoles.ChangeForUserAsync("u_1", add: new[] { "support" });
            Assert.Equal("/v1/users/u_1/app_roles", h.Requests[2].Path);
            Assert.False(Sent(h.Requests[2]).TryGetProperty("remove", out _));
            await c.Organizations.Teams.ListAsync("org_1", "u_1");
            Assert.Equal("/v1/organizations/org_1/teams", h.Requests[3].Path);
            Assert.Equal("?user_id=u_1", h.Requests[3].Query);
            await c.Organizations.Teams.AddMemberAsync("org_1", "tm_1", "u_2");
            Assert.Equal("u_2", Sent(h.Requests[4]).GetProperty("user_id").GetString());
            await c.Organizations.Roles.DeleteAsync("org_1", "r_1", "member");
            Assert.Equal("DELETE", h.Requests[5].Method);
            Assert.Equal("?reassign_to=member", h.Requests[5].Query);
            await c.Organizations.CheckSlugAsync("acme");
            Assert.Equal("?slug=acme", h.Requests[6].Query);
            await c.Organizations.Invitations.ResendAsync("org_1", "inv_1");
            Assert.Equal("/v1/organizations/org_1/invitations/inv_1/resend", h.Requests[7].Path);
        }

        [Fact]
        public async Task UserSearchBanAndApiKeyControls()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"api_key_verification\",\"valid\":false,\"code\":\"rate_limited\",\"retry_after_ms\":900,\"data\":[],\"has_more\":false}");
            using var c = Client(h);
            await c.Users.ListAsync(new ListUsersParams { Query = "ada", Banned = true, CreatedAfter = 1000, OrderBy = "-created_at" });
            Assert.Equal("?query=ada&banned=true&created_after=1000&order_by=-created_at", h.Requests[0].Query);
            await c.Users.BanAsync("u_1", "spam", null, 3600, null);
            Assert.Equal(3600, Sent(h.Requests[1]).GetProperty("expires_in_seconds").GetInt32());
            var v = await c.ApiKeys.VerifyAsync("ak_x", new[] { "read" });
            Assert.Equal("read", Sent(h.Requests[2]).GetProperty("required_scopes")[0].GetString());
            Assert.False(v.Valid);
            Assert.Equal("rate_limited", v.Code);
            Assert.Equal(900, v.RetryAfterMs);
            await c.ApiKeys.CreateAsync(new CreateApiKeyBody
            {
                SubjectType = "user", SubjectId = "u_1", Prefix = "acme", Enabled = false,
                RateLimit = new ApiKeyRateLimit { Max = 10, WindowSeconds = 60 }, Remaining = 5,
            });
            var b = Sent(h.Requests[3]);
            Assert.Equal("acme", b.GetProperty("prefix").GetString());
            Assert.Equal(60, b.GetProperty("rate_limit").GetProperty("window_seconds").GetInt32());
        }

        [Fact]
        public async Task BillingProvidersAndSubscriptions()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"billing_subscription\",\"id\":\"sub_1\",\"scheduled\":true,\"cancel_at_period_end\":true,\"provider\":\"polar\"}");
            using var c = Client(h);
            await c.Billing.SetProviderAsync("polar", new Dictionary<string, string> { ["access_token"] = "x" }, "test");
            Assert.Equal("PUT", h.Requests[0].Method);
            Assert.Equal("x", Sent(h.Requests[0]).GetProperty("config").GetProperty("access_token").GetString());
            await c.Billing.DeleteProviderAsync("polar", force: true);
            Assert.Equal("?force=true", h.Requests[1].Query);
            var sub = await c.Billing.ChangeSubscriptionAsync("sub_1", "p_2", "year", true);
            Assert.Equal("/v1/billing/subscriptions/sub_1/change", h.Requests[2].Path);
            Assert.True(sub.Scheduled);
            Assert.True(sub.CancelAtPeriodEnd);
            Assert.Equal("polar", sub.Provider);
            await c.Billing.CancelSubscriptionAsync("sub_1", immediately: true);
            Assert.True(Sent(h.Requests[3]).GetProperty("immediately").GetBoolean());
            await c.Billing.RestoreSubscriptionAsync("sub_1");
            Assert.Equal("/v1/billing/subscriptions/sub_1/restore", h.Requests[4].Path);
        }

        [Fact]
        public async Task OAuthServerAdministration()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"x\"}");
            using var c = Client(h);
            await c.InstanceSecurity.RotateSigningKeyAsync("ES256");
            Assert.Equal("ES256", Sent(h.Requests[0]).GetProperty("alg").GetString());
            await c.OAuthInitialAccessTokens.CreateAsync(maxUses: 3);
            Assert.Equal(3, Sent(h.Requests[1]).GetProperty("max_uses").GetInt32());
            await c.OAuthClients.UpdateAsync("oc_1", new UpdateOAuthClientBody { Disabled = true, RequirePkce = true, SubjectType = "pairwise" });
            var b = Sent(h.Requests[2]);
            Assert.True(b.GetProperty("disabled").GetBoolean());
            Assert.True(b.GetProperty("require_pkce").GetBoolean());
            Assert.Equal("pairwise", b.GetProperty("subject_type").GetString());
            await c.UserHooks.ListBeforeUserCreateCallsAsync(status: "failed", test: false);
            Assert.Equal("?status=failed&test=false", h.Requests[3].Query);
            await c.Referrals.GetCallAsync("rc_1");
            Assert.Equal("/v1/instance/referrals/calls/rc_1", h.Requests[4].Path);
        }

        // ── Verification ──────────────────────────────────────────────────────

        private const string Issuer = "https://issuer.test";
        private const string Aud = "https://api.test";
        private readonly ECDsa _ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        private readonly RSA _rsa = RSA.Create(2048);

        private string Jwks()
        {
            var p = _ec.ExportParameters(false);
            var r = _rsa.ExportParameters(false);
            return "{\"keys\":[{\"kty\":\"EC\",\"crv\":\"P-256\",\"kid\":\"ec-1\",\"alg\":\"ES256\",\"x\":\"" + Base64UrlEncoder.Encode(p.Q.X)
                + "\",\"y\":\"" + Base64UrlEncoder.Encode(p.Q.Y) + "\"},{\"kty\":\"RSA\",\"kid\":\"rs-1\",\"alg\":\"RS256\",\"n\":\""
                + Base64UrlEncoder.Encode(r.Modulus) + "\",\"e\":\"" + Base64UrlEncoder.Encode(r.Exponent) + "\"}]}";
        }

        private string Sign(IDictionary<string, object> claims, bool ec = true, string? kid = null, string? audience = null, DateTime? expires = null)
        {
            SecurityKey key = ec ? new ECDsaSecurityKey(_ec) { KeyId = kid ?? "ec-1" } : new RsaSecurityKey(_rsa) { KeyId = kid ?? "rs-1" };
            return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = audience,
                Claims = claims,
                Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
                NotBefore = DateTime.UtcNow.AddMinutes(-10),
                IssuedAt = DateTime.UtcNow.AddMinutes(-10),
                SigningCredentials = new SigningCredentials(key, ec ? SecurityAlgorithms.EcdsaSha256 : SecurityAlgorithms.RsaSha256),
            });
        }

        [Fact]
        public async Task SessionVerifierAcceptsEs256AndAppClaims()
        {
            var backend = new AtlasBackend(new AtlasBackendOptions
            {
                JwksUrl = "https://jwks.test/keys",
                Issuer = Issuer,
                HttpClient = new HttpClient(MockHttpMessageHandler.Json(Jwks())),
            });
            var claims = new Dictionary<string, object>
            {
                ["sub"] = "user_1",
                ["app_roles"] = new List<string> { "support" },
                ["app_permissions"] = new List<string> { "tickets:read" },
            };
            var r = await backend.VerifyAsync(Sign(claims));
            Assert.True(r.Ok);
            Assert.True(r.Session!.Claims.HasAppRole("support"));
            Assert.True(r.Session.Claims.HasAppPermission("tickets:read"));
            Assert.False(r.Session.Has(new ProtectCondition { AppPermission = "tickets:write" }));
            Assert.True(r.Session.Has(new ProtectCondition { AppRole = "support" }));
            Assert.True((await backend.VerifyAsync(Sign(new Dictionary<string, object> { ["sub"] = "u" }, ec: false))).Ok);
            Assert.False((await backend.VerifyAsync(Sign(new Dictionary<string, object> { ["sub"] = "u" }, kid: "rs-1"))).Ok);
            Assert.Equal(new[] { "RS256", "ES256" }, SigningAlgs.All);
        }

        private Dictionary<string, object> Access(string scope = "read write") => new Dictionary<string, object>
        {
            ["sub"] = "user_1",
            ["token_use"] = "access_token",
            ["scope"] = scope,
            ["client_id"] = "oc_1",
        };

        [Fact]
        public async Task AccessTokenVerifierChecksAndChallenges()
        {
            var v = new AccessTokenVerifier(new AccessTokenVerifierOptions
            {
                Issuer = Issuer + "/",
                Audiences = new[] { Aud },
                RequiredScopes = new[] { "read" },
                JwksUrl = "https://jwks.test/keys",
                ResourceMetadataUrl = Aud + "/.well-known/oauth-protected-resource",
                HttpClient = new HttpClient(MockHttpMessageHandler.Json(Jwks())),
            });
            var ok = await v.VerifyAsync("Bearer " + Sign(Access(), audience: Aud));
            Assert.True(ok.Ok, ok.Description);
            Assert.Equal("user_1", ok.Subject);
            Assert.Equal("oc_1", ok.ClientId);
            Assert.Equal(new[] { "read", "write" }, ok.Scopes);

            var none = await v.VerifyAsync(null);
            Assert.Equal(401, none.Status);
            Assert.Null(none.Error);
            Assert.Equal("Bearer scope=\"read\", resource_metadata=\"" + Aud + "/.well-known/oauth-protected-resource\"", none.WwwAuthenticate);
            Assert.Equal(400, (await v.VerifyAsync("Basic abc")).Status);
            var wrong = await v.VerifyAsync("Bearer " + Sign(Access(), audience: "https://other.test"));
            Assert.Equal(401, wrong.Status);
            Assert.Contains("error=\"invalid_token\"", wrong.WwwAuthenticate);
            var session = Access();
            session["token_use"] = "session";
            Assert.False((await v.VerifyAsync("Bearer " + Sign(session, audience: Aud))).Ok);
            var bound = Access();
            bound["cnf"] = new Dictionary<string, object> { ["jkt"] = "x" };
            Assert.False((await v.VerifyAsync("Bearer " + Sign(bound, audience: Aud))).Ok);
            var shortScope = await v.VerifyAsync("Bearer " + Sign(Access("write"), ec: false, audience: Aud), new[] { "admin" });
            Assert.Equal(403, shortScope.Status);
            Assert.Equal("insufficient_scope", shortScope.Error);
            Assert.Contains("scope=\"read admin\"", shortScope.WwwAuthenticate);
            Assert.Throws<ArgumentException>(() => new AccessTokenVerifier(new AccessTokenVerifierOptions { Issuer = Issuer }));
            Assert.Equal("/.well-known/oauth-protected-resource/v1", AccessTokenVerifier.ProtectedResourceMetadataPath("https://api.test/v1/"));
        }
    }
}
