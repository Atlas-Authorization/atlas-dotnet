using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Atlas.AspNetCore;
using Atlas.Rp;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>The relying-party endpoints end to end on a TestServer, against a fake issuer (discovery, JWKS, token endpoint, session calls).</summary>
    public class RelyingPartyEndpointsTests
    {
        private const string Issuer = "https://id.test";
        private const string Api = "https://api.test";
        private const string Client = "c1";

        private sealed class FakeIssuer : HttpMessageHandler
        {
            private readonly RSA _rsa = RSA.Create(2048);
            public string Nonce = "";
            public List<string> Calls = new List<string>();

            public string Sign(string typ, Action<SecurityTokenDescriptor> shape)
            {
                var d = new SecurityTokenDescriptor
                {
                    Issuer = Issuer, Audience = Client, IssuedAt = DateTime.UtcNow, NotBefore = DateTime.UtcNow.AddMinutes(-1), Expires = DateTime.UtcNow.AddMinutes(5),
                    SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = "k1" }, SecurityAlgorithms.RsaSha256),
                };
                if (typ != null) d.TokenType = typ;
                shape(d);
                return new JsonWebTokenHandler().CreateToken(d);
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, System.Threading.CancellationToken ct)
            {
                var path = r.RequestUri!.AbsoluteUri;
                Calls.Add(r.Method + " " + path);
                string body;
                if (path == Issuer + "/.well-known/openid-configuration")
                    body = "{\"issuer\":\"" + Issuer + "\",\"authorization_endpoint\":\"" + Issuer + "/oauth2/authorize\",\"token_endpoint\":\"" + Issuer + "/oauth2/token\",\"jwks_uri\":\"" + Issuer + "/jwks\",\"end_session_endpoint\":\"" + Issuer + "/oauth2/logout\"}";
                else if (path == Issuer + "/jwks")
                {
                    var p = _rsa.ExportParameters(false);
                    body = "{\"keys\":[{\"kty\":\"RSA\",\"kid\":\"k1\",\"use\":\"sig\",\"alg\":\"RS256\",\"n\":\"" + Base64UrlEncoder.Encode(p.Modulus) + "\",\"e\":\"" + Base64UrlEncoder.Encode(p.Exponent) + "\"}]}";
                }
                else if (path == Issuer + "/oauth2/token")
                {
                    var id = Sign("JWT", d => d.Claims = new Dictionary<string, object>
                    {
                        ["sub"] = "user_1", ["sid"] = "sess_1", ["nonce"] = Nonce, ["email"] = "a@b.test", ["email_verified"] = true,
                        ["auth_time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    });
                    body = "{\"id_token\":\"" + id + "\",\"access_token\":\"at\",\"expires_in\":600}";
                }
                else body = "{}"; // /v1/sessions/{id}/touch and /revoke
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });
            }
        }

        private static TestServer Server(FakeIssuer fake)
        {
            return new TestServer(new WebHostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddRouting();
                    s.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(o => o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; });
                    s.AddAuthorization();
                    s.AddAtlasRelyingParty(o =>
                    {
                        o.Issuer = Issuer; o.ClientId = Client; o.ClientSecret = "shh"; o.RedirectUri = "https://app.test/auth/callback";
                        o.Secret = new string('k', 40); o.SecretKey = "sk_test_1"; o.ApiUrl = Api; o.PostLogoutRedirectUri = "https://app.test/";
                        o.HttpClient = new HttpClient(fake);
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAtlasSession();
                    app.UseAuthorization();
                    app.UseEndpoints(e =>
                    {
                        e.MapAtlasRelyingParty(o => o.BackchannelLogoutPath = "/auth/backchannel");
                        e.MapGet("/me", ctx => ctx.Response.WriteAsync(ctx.User.FindFirst("email")?.Value ?? "")).RequireAuthorization();
                    });
                }));
        }

        private static string CookiePair(HttpResponseMessage r, string namePrefix)
            => r.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(namePrefix)).Split(';')[0];

        /// <summary>Runs sign-in start and callback; returns the session cookie pair.</summary>
        private static async Task<(string Session, HttpResponseMessage Callback)> SignIn(HttpClient http, FakeIssuer fake, string? state = null)
        {
            var start = await http.GetAsync("/auth/sign-in?return_to=/dash&prompt=login");
            Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
            var q = QueryHelpers.ParseQuery(start.Headers.Location!.Query);
            Assert.StartsWith(Issuer + "/oauth2/authorize", start.Headers.Location.GetLeftPart(UriPartial.Path));
            Assert.Equal("S256", q["code_challenge_method"]);
            Assert.Equal("login", q["prompt"]);
            fake.Nonce = q["nonce"]!;
            var flow = CookiePair(start, "atlas_oidc=");
            var req = new HttpRequestMessage(HttpMethod.Get, "/auth/callback?code=abc&state=" + Uri.EscapeDataString(state ?? q["state"]!));
            req.Headers.Add("Cookie", flow);
            var cb = await http.SendAsync(req);
            return (cb.Headers.TryGetValues("Set-Cookie", out var cs) ? cs.Select(c => c.Split(';')[0]).FirstOrDefault(c => c.StartsWith(".AspNetCore.Cookies=")) ?? "" : "", cb);
        }

        private static async Task<HttpResponseMessage> Get(HttpClient http, string path, string cookie)
        {
            var r = new HttpRequestMessage(HttpMethod.Get, path);
            if (cookie.Length > 0) r.Headers.Add("Cookie", cookie);
            return await http.SendAsync(r);
        }

        [Fact]
        public async Task SignIn_Callback_CreatesSession_AndKeepsItAlive()
        {
            var fake = new FakeIssuer();
            using var server = Server(fake);
            var http = server.CreateClient();
            var (session, cb) = await SignIn(http, fake);
            Assert.Equal(HttpStatusCode.Redirect, cb.StatusCode);
            Assert.Equal("/dash", cb.Headers.Location!.OriginalString);
            Assert.NotEmpty(session);
            Assert.Contains(cb.Headers.GetValues("Set-Cookie"), c => c.StartsWith("atlas_oidc=;")); // flow cookie cleared
            var me = await Get(http, "/me", session);
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            Assert.Equal("a@b.test", await me.Content.ReadAsStringAsync());
            Assert.Contains(fake.Calls, c => c == "POST " + Api + "/v1/sessions/sess_1/touch");
            Assert.Equal(HttpStatusCode.Unauthorized, (await Get(http, "/me", "")).StatusCode);
        }

        [Fact]
        public async Task Callback_WithWrongState_Is400_AndCreatesNoSession()
        {
            var fake = new FakeIssuer();
            using var server = Server(fake);
            var (session, cb) = await SignIn(server.CreateClient(), fake, state: "forged");
            Assert.Equal(HttpStatusCode.BadRequest, cb.StatusCode);
            Assert.Equal("", session);
        }

        [Fact]
        public async Task BackchannelLogout_EndsTheBoundSession()
        {
            var fake = new FakeIssuer();
            using var server = Server(fake);
            var http = server.CreateClient();
            var (session, _) = await SignIn(http, fake);
            Assert.Equal(HttpStatusCode.OK, (await Get(http, "/me", session)).StatusCode);
            var token = fake.Sign("logout+jwt", d => d.Claims = new Dictionary<string, object>
            {
                ["jti"] = Guid.NewGuid().ToString("N"), ["sid"] = "sess_1",
                ["events"] = new Dictionary<string, object> { ["http://schemas.openid.net/event/backchannel-logout"] = new Dictionary<string, object>() },
            });
            var res = await http.PostAsync("/auth/backchannel", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("logout_token", token) }));
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Get(http, "/me", session)).StatusCode);
            var bad = await http.PostAsync("/auth/backchannel", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("logout_token", "nope") }));
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        }

        [Fact]
        public async Task SignOut_RevokesAtlasSession_AndRedirectsWithIdTokenHint()
        {
            var fake = new FakeIssuer();
            using var server = Server(fake);
            var http = server.CreateClient();
            var (session, _) = await SignIn(http, fake);
            var res = await Get(http, "/auth/sign-out", session);
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
            Assert.StartsWith(Issuer + "/oauth2/logout", res.Headers.Location!.AbsoluteUri);
            Assert.Contains("id_token_hint=", res.Headers.Location.Query);
            Assert.Contains(fake.Calls, c => c == "POST " + Api + "/v1/sessions/sess_1/revoke");
        }
    }
}
