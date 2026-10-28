using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Blazor.Server;
using Atlas.Blazor.WebAssembly;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Atlas.Frameworks.Tests
{
    public class BlazorServerTests
    {
        private sealed class StubValidator : IAtlasSessionValidator
        {
            public string? Asked;
            public bool Answer;
            public Task<bool> IsActiveAsync(string sessionId, CancellationToken cancellationToken) { Asked = sessionId; return Task.FromResult(Answer); }
        }

        private static AtlasRevalidatingAuthenticationStateProvider Provider(IAtlasSessionValidator v)
            => new AtlasRevalidatingAuthenticationStateProvider(NullLoggerFactory.Instance, v, Options.Create(new AtlasBlazorServerOptions()));

        private static ClaimsPrincipal User(string? sid)
        {
            var id = new ClaimsIdentity("Atlas");
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user_1"));
            if (sid != null) id.AddClaim(new Claim("sid", sid));
            return new ClaimsPrincipal(id);
        }

        [Fact]
        public async Task SignedInUserIsCheckedAgainstTheSessionId()
        {
            var v = new StubValidator { Answer = true };
            Assert.True(await Provider(v).ValidateAsync(User("sess_1"), default));
            Assert.Equal("sess_1", v.Asked);
            v.Answer = false;
            Assert.False(await Provider(v).ValidateAsync(User("sess_1"), default));
        }

        [Fact]
        public async Task SignedInUserWithoutASessionIdIsRefusedAndAnonymousIsLeftAlone()
        {
            var v = new StubValidator { Answer = true };
            Assert.False(await Provider(v).ValidateAsync(User(null), default));
            Assert.True(await Provider(v).ValidateAsync(new ClaimsPrincipal(new ClaimsIdentity()), default));
            Assert.Null(v.Asked);
        }

        private static AtlasClientSessionValidator ValidatorFor(HttpStatusCode status, string body)
        {
            var client = new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_x",
                HttpClient = new HttpClient(new FuncHandler((_, __) => (status, body))),
            });
            return new AtlasClientSessionValidator(client, () => 1_000);
        }

        [Fact]
        public async Task OnlyAnActiveUnexpiredSessionPasses()
        {
            Assert.True(await ValidatorFor(HttpStatusCode.OK, "{\"object\":\"session\",\"id\":\"sess_1\",\"status\":\"active\",\"expire_at\":5000}").IsActiveAsync("sess_1", default));
            Assert.False(await ValidatorFor(HttpStatusCode.OK, "{\"object\":\"session\",\"id\":\"sess_1\",\"status\":\"revoked\",\"expire_at\":5000}").IsActiveAsync("sess_1", default));
            Assert.False(await ValidatorFor(HttpStatusCode.OK, "{\"object\":\"session\",\"id\":\"sess_1\",\"status\":\"active\",\"expire_at\":500}").IsActiveAsync("sess_1", default));
        }

        [Fact]
        public async Task ErrorsAndUnknownSessionsFailClosed()
        {
            Assert.False(await ValidatorFor(HttpStatusCode.NotFound, "{\"errors\":[{\"code\":\"NOT_FOUND\",\"message\":\"no\"}]}").IsActiveAsync("sess_1", default));
            Assert.False(await ValidatorFor(HttpStatusCode.InternalServerError, "{}").IsActiveAsync("sess_1", default));
            Assert.False(await ValidatorFor(HttpStatusCode.OK, "{}").IsActiveAsync("", default));
        }
    }

    public class BlazorWebAssemblyTests
    {
        private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

        private static string Token(long exp, string iss = "https://auth.example.com", string? azp = "https://app.example.com", string alg = "RS256", string? sub = "user_1")
        {
            var p = new Dictionary<string, object?> { ["iss"] = iss, ["sub"] = sub, ["sid"] = "sess_1", ["exp"] = exp, ["org_id"] = "o1", ["org_role"] = "admin", ["org_permissions"] = new[] { "a:b", "c:d" } };
            if (azp != null) p["azp"] = azp;
            return Jwt.Make(p, alg);
        }

        private static AtlasWasmOptions Opts()
        {
            var o = new AtlasWasmOptions { Issuer = "https://auth.example.com" };
            o.AuthorizedParties.Add("https://app.example.com");
            o.AuthorizedUrls.Add("https://api.example.com/v1");
            return o;
        }

        [Fact]
        public void ValidTokenDecodesAndBecomesAPrincipal()
        {
            var payload = AtlasSessionToken.TryDecode(Token(Now.ToUnixTimeSeconds() + 60), Opts(), Now);
            Assert.NotNull(payload);
            var principal = AtlasSessionToken.ToPrincipal(payload!.Value);
            Assert.Equal("user_1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.True(principal.IsInRole("admin"));
            Assert.Equal(2, principal.FindAll("org_permissions").Count());
        }

        [Fact]
        public void ExpiredWrongIssuerWrongAzpAlgNoneAndGarbageAreRefused()
        {
            var t = Now.ToUnixTimeSeconds();
            Assert.Null(AtlasSessionToken.TryDecode(Token(t - 60), Opts(), Now));
            Assert.NotNull(AtlasSessionToken.TryDecode(Token(t - 3), Opts(), Now)); // inside the 5 s skew
            Assert.Null(AtlasSessionToken.TryDecode(Token(t + 60, iss: "https://evil.example.com"), Opts(), Now));
            Assert.Null(AtlasSessionToken.TryDecode(Token(t + 60, azp: "https://other.example.com"), Opts(), Now));
            Assert.Null(AtlasSessionToken.TryDecode(Token(t + 60, azp: null), Opts(), Now));
            Assert.Null(AtlasSessionToken.TryDecode(Token(t + 60, alg: "none"), Opts(), Now));
            Assert.Null(AtlasSessionToken.TryDecode(Token(t + 60, sub: null), Opts(), Now));
            Assert.Null(AtlasSessionToken.TryDecode("not.a.jwt", Opts(), Now));
            Assert.Null(AtlasSessionToken.TryDecode(null, Opts(), Now));
        }

        private sealed class Source : IAtlasSessionSource
        {
            public string? Token;
            public ValueTask<string?> GetTokenAsync() => new ValueTask<string?>(Token);
        }

        [Fact]
        public async Task StateProviderReflectsTheCookie()
        {
            var src = new Source();
            var provider = new AtlasAuthenticationStateProvider(src, Options.Create(Opts()), () => Now);
            Assert.False((await provider.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
            src.Token = Token(Now.ToUnixTimeSeconds() + 60);
            Assert.True((await provider.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
            provider.Dispose();
        }

        private static async Task<HttpRequestMessage> Send(AtlasBearerHandler handler, Source src, string url)
        {
            handler.InnerHandler = new FuncHandler((_, __) => (HttpStatusCode.OK, "{}"));
            using var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            await invoker.SendAsync(request, default);
            return request;
        }

        [Fact]
        public async Task TheTokenIsSentOnlyToAuthorizedUrls()
        {
            var src = new Source { Token = Token(Now.ToUnixTimeSeconds() + 60) };
            var handler = new AtlasBearerHandler(src, Options.Create(Opts()), () => Now);
            Assert.Equal("Bearer " + src.Token, (await Send(handler, src, "https://api.example.com/v1/orders")).Headers.Authorization?.ToString());
            Assert.Null((await Send(new AtlasBearerHandler(src, Options.Create(Opts()), () => Now), src, "https://evil.example.com/v1/orders")).Headers.Authorization);
            Assert.Null((await Send(new AtlasBearerHandler(src, Options.Create(Opts()), () => Now), src, "https://api.example.com/v10/orders")).Headers.Authorization);
            Assert.Null((await Send(new AtlasBearerHandler(src, Options.Create(Opts()), () => Now), src, "https://api.example.com:8443/v1/orders")).Headers.Authorization);
        }

        [Fact]
        public async Task NoTokenIsSentWhenTheSessionIsExpired()
        {
            var src = new Source { Token = Token(Now.ToUnixTimeSeconds() - 600) };
            var request = await Send(new AtlasBearerHandler(src, Options.Create(Opts()), () => Now), src, "https://api.example.com/v1/orders");
            Assert.Null(request.Headers.Authorization);
        }
    }
}
