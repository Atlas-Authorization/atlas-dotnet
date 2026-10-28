using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Native;
using Xunit;

namespace Atlas.Frameworks.Tests
{
    public class NativeTests
    {
        private const string Issuer = "https://auth.example.com";

        private sealed class FakeBrowser : IAuthBrowser
        {
            public Uri? Opened;
            public Func<Uri, Uri> Callback = u => u;
            public Task<Uri> AuthenticateAsync(Uri authorizeUrl, Uri callbackUrl, CancellationToken cancellationToken = default)
            {
                Opened = authorizeUrl;
                return Task.FromResult(Callback(authorizeUrl));
            }
        }

        private static Dictionary<string, string> Q(Uri u) => AtlasNativeClient.ParseQuery(u.Query);

        private static Uri Redirect(Uri authorize, string code = "code_1", string? state = null)
            => new Uri($"myapp://callback?code={code}&state={Uri.EscapeDataString(state ?? Q(authorize)["state"])}");

        private static string Form(string body, string key)
            => AtlasNativeClient.ParseQuery(body).TryGetValue(key, out var v) ? v : "";

        private sealed class World
        {
            public DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            public string? AuthEndpoint = Issuer + "/oauth2/authorize";
            public HttpStatusCode RefreshStatus = HttpStatusCode.OK;
            public bool RefreshThrows;
            public int Refreshes;
            public string RefreshToken = "r1";
            public readonly FuncHandler Handler;
            public readonly FakeBrowser Browser = new FakeBrowser();
            public readonly MemorySessionStore Store = new MemorySessionStore();

            public World()
            {
                Handler = new FuncHandler((req, body) =>
                {
                    var path = req.RequestUri!.AbsolutePath;
                    if (path == "/.well-known/openid-configuration")
                        return (HttpStatusCode.OK, $"{{\"issuer\":\"{Issuer}\",\"authorization_endpoint\":\"{AuthEndpoint}\",\"token_endpoint\":\"{Issuer}/oauth2/token\"}}");
                    if (path == "/oauth2/token" && Form(body, "grant_type") == "authorization_code")
                        return (HttpStatusCode.OK, "{\"access_token\":\"oat_1\",\"token_type\":\"Bearer\"}");
                    if (path == "/oauth2/token")
                        return (HttpStatusCode.OK, "{\"access_token\":\"jwt_1\",\"session_id\":\"sess_1\",\"refresh_token\":\"r1\",\"expires_in\":60}");
                    if (path == "/v1/client/sessions/sess_1/tokens")
                    {
                        Refreshes++;
                        if (RefreshThrows) throw new HttpRequestException("offline");
                        return (RefreshStatus, $"{{\"jwt\":\"jwt_{Refreshes + 1}\",\"session_id\":\"sess_1\",\"refresh_token\":\"r{Refreshes + 1}\",\"expires_in\":60}}");
                    }
                    return (HttpStatusCode.NotFound, "{}");
                });
            }

            public AtlasNativeClient Client() => new AtlasNativeClient(new AtlasNativeOptions
            {
                Issuer = Issuer, PublishableKey = "pk_test_1", ClientId = "client_1", RedirectUri = "myapp://callback",
                HttpClient = new HttpClient(Handler), Now = () => Now,
            }, Browser, Store);
        }

        [Fact]
        public async Task SignInRunsCodeWithPkceThenExchangesForASession()
        {
            var w = new World();
            w.Browser.Callback = u => Redirect(u);
            using var client = w.Client();
            await client.SignInAsync();

            var q = Q(w.Browser.Opened!);
            Assert.Equal("code", q["response_type"]);
            Assert.Equal("S256", q["code_challenge_method"]);
            var tokenCall = w.Handler.Calls.Find(c => c.Request.RequestUri!.AbsolutePath == "/oauth2/token" && Form(c.Body, "grant_type") == "authorization_code");
            Assert.Equal(q["code_challenge"], Pkce.ChallengeFor(Form(tokenCall.Body, "code_verifier")));
            var exchange = w.Handler.Calls.FindLast(c => c.Request.RequestUri!.AbsolutePath == "/oauth2/token");
            Assert.Equal("oat_1", Form(exchange.Body, "subject_token"));
            Assert.Equal("urn:atlas:token-type:session", Form(exchange.Body, "requested_token_type"));
            Assert.Equal("jwt_1", (await w.Store.LoadAsync())!.SessionToken);
            Assert.Equal("jwt_1", await client.GetTokenAsync());
        }

        [Fact]
        public async Task AWrongStateAnErrorOrNoCodeIsRefused()
        {
            foreach (var cb in new Func<Uri, Uri>[]
            {
                u => Redirect(u, state: "forged"),
                u => new Uri("myapp://callback?error=access_denied&state=" + Q(u)["state"]),
                u => new Uri("myapp://callback?state=" + Q(u)["state"]),
            })
            {
                var w = new World();
                w.Browser.Callback = cb;
                using var client = w.Client();
                await Assert.ThrowsAsync<AtlasNativeException>(() => client.SignInAsync());
                Assert.Null(await w.Store.LoadAsync());
            }
        }

        [Fact]
        public async Task EndpointsOnAnotherOriginAreRefused()
        {
            var w = new World { AuthEndpoint = "https://evil.example.com/oauth2/authorize" };
            using var client = w.Client();
            await Assert.ThrowsAsync<AtlasNativeException>(() => client.SignInAsync());
            Assert.Null(w.Browser.Opened);
        }

        [Fact]
        public void TheIssuerMustBeHttps()
        {
            Assert.Throws<ArgumentException>(() => new AtlasNativeClient(new AtlasNativeOptions
            { Issuer = "http://auth.example.com", PublishableKey = "pk", ClientId = "c", RedirectUri = "myapp://cb" }, new FakeBrowser(), new MemorySessionStore()));
        }

        private static async Task<(World, AtlasNativeClient)> SignedIn()
        {
            var w = new World();
            w.Browser.Callback = u => Redirect(u);
            var client = w.Client();
            await client.SignInAsync();
            return (w, client);
        }

        [Fact]
        public async Task TheTokenIsRotatedNearExpiryAndTheNewRefreshTokenIsStored()
        {
            var (w, client) = await SignedIn();
            using (client)
            {
                Assert.Equal("jwt_1", await client.GetTokenAsync());
                Assert.Equal(0, w.Refreshes);
                w.Now = w.Now.AddSeconds(55); // within the 10 s lead
                Assert.Equal("jwt_2", await client.GetTokenAsync());
                Assert.Equal("r2", (await w.Store.LoadAsync())!.RefreshToken);
                Assert.Equal("pk_test_1", w.Handler.Calls[^1].Request.Headers.GetValues("x-publishable-key").First());
            }
        }

        [Fact]
        public async Task AFailedRefreshKeepsTheTokenButADeadRefreshTokenSignsOut()
        {
            var (w, client) = await SignedIn();
            using (client)
            {
                w.Now = w.Now.AddSeconds(55);
                w.RefreshThrows = true;
                Assert.Equal("jwt_1", await client.GetTokenAsync());
                w.RefreshThrows = false;
                w.RefreshStatus = HttpStatusCode.Unauthorized;
                Assert.Null(await client.GetTokenAsync());
                Assert.False(await client.IsSignedInAsync());
                Assert.Null(await w.Store.LoadAsync());
            }
        }

        [Fact]
        public async Task SignOutForgetsTheSession()
        {
            var (w, client) = await SignedIn();
            using (client)
            {
                await client.SignOutAsync();
                Assert.Null(await client.GetTokenAsync());
                Assert.Null(await w.Store.LoadAsync());
            }
        }

        [Fact]
        public async Task TheHandlerAddsTheTokenOnlyToAuthorizedUrls()
        {
            var (w, client) = await SignedIn();
            using (client)
            {
                var inner = new FuncHandler((_, __) => (HttpStatusCode.OK, "{}"));
                using var invoker = new HttpMessageInvoker(client.CreateHandler(new[] { "https://api.example.com" }, inner));
                var ok = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/orders");
                var other = new HttpRequestMessage(HttpMethod.Get, "https://evil.example.com/orders");
                await invoker.SendAsync(ok, default);
                await invoker.SendAsync(other, default);
                Assert.Equal("Bearer jwt_1", ok.Headers.Authorization?.ToString());
                Assert.Null(other.Headers.Authorization);
            }
        }

        [Fact]
        public void PkceChallengeIsTheS256OfTheVerifier()
        {
            var (verifier, challenge) = Pkce.Create();
            Assert.Equal(Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_'), challenge);
            Assert.Equal(43, verifier.Length);
        }
    }
}
