using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Atlas.AspNetCore;
using Atlas.Verification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>
    /// End-to-end tests for the ASP.NET Core integration. They stand up a real
    /// request pipeline in memory with <see cref="TestServer"/> — routing,
    /// authentication, authorization — so the assertions exercise the handler,
    /// the DI wiring, and the policy helper together, the way an app would use
    /// them, not a mocked slice of each. The RSA key + stubbed JWKS mirror the
    /// verifier tests so we are signing tokens the real verifier accepts.
    /// </summary>
    public class AspNetCoreTests
    {
        private const string Issuer = "https://issuer.test";
        private const string Kid = "test-key-1";

        private readonly RSA _rsa = RSA.Create(2048);

        private string Jwks()
        {
            var p = _rsa.ExportParameters(false);
            var n = Base64UrlEncoder.Encode(p.Modulus);
            var e = Base64UrlEncoder.Encode(p.Exponent);
            return "{\"keys\":[{\"kty\":\"RSA\",\"kid\":\"" + Kid + "\",\"use\":\"sig\",\"alg\":\"RS256\",\"n\":\"" + n + "\",\"e\":\"" + e + "\"}]}";
        }

        private string Sign(IDictionary<string, object> claims)
        {
            var key = new RsaSecurityKey(_rsa) { KeyId = Kid };
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Claims = claims,
                Expires = DateTime.UtcNow.AddMinutes(1),
                NotBefore = DateTime.UtcNow.AddMinutes(-1),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
            };
            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        private static Dictionary<string, object> SessionClaimsBag(params string[] permissions)
            => new Dictionary<string, object>
            {
                ["sub"] = "user_1",
                ["sid"] = "sess_1",
                ["token_use"] = "session",
                ["org_id"] = "org_1",
                ["org_role"] = "admin",
                ["org_permissions"] = permissions.Length > 0 ? permissions : new[] { "posts:read", "posts:write" },
            };

        // One in-memory server, wired exactly as the README tells an app to: a
        // convenience AddAtlasAuthentication call, a permission policy, and two
        // protected minimal endpoints. The stubbed JWKS rides in on Options.HttpClient.
        private TestServer BuildServer()
        {
            var jwks = Jwks();
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAtlasAuthentication(o =>
                    {
                        o.JwksUrl = "https://jwks.test/keys";
                        o.Issuer = Issuer;
                        o.HttpClient = new HttpClient(MockHttpMessageHandler.Json(jwks));
                    });
                    services.AddAuthorization(o =>
                    {
                        o.AddPolicy("posts:write", p => p.RequireAtlasPermission("posts:write"));
                        o.AddPolicy("billing:write", p => p.RequireAtlasPermission("billing:write"));
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        // Protected by the default policy: any authenticated Atlas user.
                        endpoints.MapGet("/me", async ctx =>
                        {
                            var sub = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            await ctx.Response.WriteAsync(sub ?? "");
                        }).RequireAuthorization();

                        endpoints.MapGet("/write", ctx => ctx.Response.WriteAsync("ok"))
                            .RequireAuthorization("posts:write");

                        endpoints.MapGet("/billing", ctx => ctx.Response.WriteAsync("ok"))
                            .RequireAuthorization("billing:write");
                    });
                });
            return new TestServer(builder);
        }

        [Fact]
        public async Task ValidBearer_IsAuthenticated_AndPrincipalCarriesSub()
        {
            using var server = BuildServer();
            var client = server.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, "/me");
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + Sign(SessionClaimsBag()));

            var res = await client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("user_1", await res.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task NoToken_Is401()
        {
            using var server = BuildServer();
            var res = await server.CreateClient().GetAsync("/me");
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task InvalidToken_Is401()
        {
            using var server = BuildServer();
            var client = server.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, "/me");
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer not-a-real-jwt");

            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task SessionCookie_IsAuthenticated()
        {
            using var server = BuildServer();
            var client = server.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, "/me");
            req.Headers.TryAddWithoutValidation("Cookie", "foo=bar; __session=" + Sign(SessionClaimsBag()));

            var res = await client.SendAsync(req);

            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal("user_1", await res.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task PermissionPolicy_Allows_WhenPermissionPresent()
        {
            using var server = BuildServer();
            var client = server.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, "/write");
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + Sign(SessionClaimsBag("posts:read", "posts:write")));

            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        [Fact]
        public async Task PermissionPolicy_Denies_WhenPermissionMissing()
        {
            using var server = BuildServer();
            var client = server.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, "/billing");
            // The token carries posts:* but not billing:write — authenticated, yet forbidden.
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + Sign(SessionClaimsBag("posts:read", "posts:write")));

            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }

        [Fact]
        public void SingletonVerifier_IsRegistered_AndInjectable()
        {
            using var server = BuildServer();
            // The same verifier the handler uses must be resolvable from DI.
            var backend = server.Services.GetService<AtlasBackend>();
            Assert.NotNull(backend);
        }
    }
}
