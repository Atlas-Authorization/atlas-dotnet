using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
    /// Billing add-ons / settings / checks, custom OAuth providers, the Actions gallery, session custom data,
    /// sign-in settings, SAML encryption keys, dormant accounts, token formats / lifetimes, KMS signing keys,
    /// opaque-token introspection, device approval and BotID attestations.
    /// </summary>
    public class BySurfaceTests
    {
        private static AtlasClient Client(MockHttpMessageHandler h) => new AtlasClient(new AtlasClientOptions
        {
            SecretKey = "sk_test_123",
            ApiUrl = "https://api.test.example",
            HttpClient = new HttpClient(h),
        });

        private static JsonElement Sent(RecordedRequest r) => JsonDocument.Parse(r.Body).RootElement;

        [Fact]
        public async Task Billing()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"billing_subscription\",\"id\":\"bsub_1\",\"kind\":\"addon\",\"seat_feature_id\":\"seats\",\"entitlements\":{\"features\":[\"sso\"],\"limits\":{\"projects\":10}},\"default_limit_merge\":\"sum\",\"limit_merge\":{\"seats\":\"max\"},\"allowed\":true,\"source\":\"provider\",\"remaining\":5}");
            using var c = Client(h);
            var sub = await c.Billing.GetSubscriptionAsync("bsub_1");
            Assert.Equal("/v1/billing/subscriptions/bsub_1", h.Requests[0].Path);
            Assert.Equal("sso", sub.Entitlements!.Features[0]);
            Assert.Equal(10, sub.Entitlements.Limits["projects"]);
            var plan = await c.Billing.SyncPlanEntitlementsAsync("bp_1");
            Assert.Equal("POST", h.Requests[1].Method);
            Assert.Equal("/v1/billing/plans/bp_1/sync_entitlements", h.Requests[1].Path);
            Assert.Equal("addon", plan.Kind);
            Assert.Equal("seats", plan.SeatFeatureId);
            var set = await c.Billing.UpdateSettingsAsync("sum", new Dictionary<string, string> { ["seats"] = "max" });
            Assert.Equal("PUT", h.Requests[2].Method);
            Assert.Equal("max", Sent(h.Requests[2]).GetProperty("limit_merge").GetProperty("seats").GetString());
            Assert.Equal("sum", set.DefaultLimitMerge);
            await c.Billing.GetSettingsAsync();
            Assert.Equal("/v1/billing/settings", h.Requests[3].Path);
            var chk = await c.Billing.CheckFeatureAsync("org", "org_1", "api", 2);
            Assert.Equal("/v1/billing/check", h.Requests[4].Path);
            Assert.Equal(2, Sent(h.Requests[4]).GetProperty("required").GetDouble());
            Assert.True(chk.Allowed);
            Assert.Equal(5, chk.Remaining);
            await c.Billing.ReportUsageAsync(new ReportBillingUsageBody { SubjectType = "user", SubjectId = "u_1", Quantity = 1, IdempotencyKey = "k", Feature = "api", SubscriptionId = "bsub_2" });
            Assert.Equal("bsub_2", Sent(h.Requests[5]).GetProperty("subscription_id").GetString());
            await c.Billing.UpdatePlanAsync("bp_1", new UpdateBillingPlanBody { Kind = "addon" }.Set("seat_feature_id", null));
            Assert.Equal(JsonValueKind.Null, Sent(h.Requests[6]).GetProperty("seat_feature_id").ValueKind);
            Assert.Equal("addon", Sent(h.Requests[6]).GetProperty("kind").GetString());
        }

        [Fact]
        public async Task CustomProvidersAndSso()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"custom_oauth_provider\",\"id\":\"coap_1\",\"provider_key\":\"custom_acme\",\"client_jwks_uri\":\"https://fapi/jwks\",\"has_secret\":true,\"profile_mapping\":{\"fields\":{\"email\":\"mail\"}},\"signing_keys\":[{\"kid\":\"k1\",\"alg\":\"ES256\",\"status\":\"active\"}],\"certificate\":\"PEM\",\"require_encrypted_assertions\":true,\"data\":[]}");
            using var c = Client(h);
            var p = await c.CustomOAuthProviders.CreateAsync(new CustomOAuthProviderBody
            {
                ProviderKey = "custom_acme",
                DisplayName = "Acme",
                Issuer = "https://idp.test",
                TokenEndpointAuthMethod = "private_key_jwt",
                ProfileMapping = new Dictionary<string, object?> { ["fields"] = new Dictionary<string, object?> { ["email"] = "mail" } },
            });
            Assert.Equal("/v1/custom_oauth_providers", h.Requests[0].Path);
            Assert.Equal("mail", Sent(h.Requests[0]).GetProperty("profile_mapping").GetProperty("fields").GetProperty("email").GetString());
            Assert.Equal("https://fapi/jwks", p.ClientJwksUri);
            Assert.True(p.HasSecret);
            Assert.Equal("k1", p.SigningKeys[0].Kid);
            await c.CustomOAuthProviders.UpdateAsync("coap_1", new CustomOAuthProviderBody().Set("icon_url", null));
            Assert.Equal("PATCH", h.Requests[1].Method);
            Assert.Equal(JsonValueKind.Null, Sent(h.Requests[1]).GetProperty("icon_url").ValueKind);
            await c.CustomOAuthProviders.DiscoverAsync("https://idp.test");
            Assert.False(Sent(h.Requests[2]).TryGetProperty("discovery_url", out _));
            await c.CustomOAuthProviders.PreviewProfileMappingAsync("coap_1", new Dictionary<string, object?> { ["id_token"] = "a.b.c" });
            Assert.Equal("/v1/custom_oauth_providers/coap_1/profile_mapping/preview", h.Requests[3].Path);
            await c.CustomOAuthProviders.RotateSigningKeyAsync("coap_1", "PS256");
            Assert.Equal("PS256", Sent(h.Requests[4]).GetProperty("alg").GetString());
            await c.CustomOAuthProviders.RetireSigningKeyAsync("coap_1", "k1");
            Assert.Equal("/v1/custom_oauth_providers/coap_1/signing_keys/k1/retire", h.Requests[5].Path);
            await c.SsoConnections.UpdateAsync("sso_1", new UpdateSsoConnectionBody
            {
                SamlSingleLogout = true,
                SamlIdpSloSoapUrl = "https://idp.test/soap",
                SamlRequireEncryptedAssertions = true,
            }.Set("saml_idp_slo_response_url", null));
            var sent = Sent(h.Requests[6]);
            Assert.Equal("https://idp.test/soap", sent.GetProperty("saml_idp_slo_soap_url").GetString());
            Assert.True(sent.GetProperty("saml_require_encrypted_assertions").GetBoolean());
            Assert.Equal(JsonValueKind.Null, sent.GetProperty("saml_idp_slo_response_url").ValueKind);
            var key = await c.SsoConnections.CreateSamlEncryptionKeyAsync("sso_1");
            Assert.Equal("/v1/sso_connections/sso_1/saml_encryption_key", h.Requests[7].Path);
            Assert.Equal("PEM", key.Certificate);
            Assert.True(key.RequireEncryptedAssertions);
            await c.SsoConnections.RetirePreviousSamlEncryptionKeyAsync("sso_1");
            Assert.Equal("DELETE", h.Requests[8].Method);
            Assert.Equal("/v1/sso_connections/sso_1/saml_encryption_key/previous", h.Requests[8].Path);
            await c.SsoConnections.RotateSigningKeyAsync("sso_1");
            Assert.Equal("{}", h.Requests[9].Body);
        }

        [Fact]
        public async Task ActionsGallery()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[{\"slug\":\"geo-fence\",\"uses_http\":true}],\"versions\":[]}");
            using var c = Client(h);
            var list = await c.ActionGallery.ListAsync(q: "geo", trigger: "post_login");
            Assert.Equal("/v1/action_gallery", h.Requests[0].Path);
            Assert.Equal("?q=geo&trigger=post_login", h.Requests[0].Query);
            Assert.True(list.Data[0].UsesHttp);
            await c.ActionGallery.InstallAsync("geo-fence", secrets: new Dictionary<string, string> { ["K"] = "v" });
            Assert.Equal("v", Sent(h.Requests[1]).GetProperty("secrets").GetProperty("K").GetString());
            await c.ActionGallery.UpdateAsync("act_1");
            Assert.Equal("/v1/actions/act_1/gallery_update", h.Requests[2].Path);
            await c.ActionTemplates.CreateAsync(new ActionTemplateBody { Slug = "mine", Name = "Mine", AuthorName = "Me", Trigger = "post_login", Submit = true });
            Assert.Equal("Me", Sent(h.Requests[3]).GetProperty("author_name").GetString());
            await c.ActionTemplates.SubmitVersionAsync("atpl_1", "1.0.0");
            Assert.Equal("/v1/action_templates/atpl_1/versions/1.0.0/submit", h.Requests[4].Path);
            await c.ActionTemplates.UnlistAsync("atpl_1");
            Assert.Equal("/v1/action_templates/atpl_1/unlist", h.Requests[5].Path);
        }

        [Fact]
        public async Task SignInEnterpriseAndOp()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"session\",\"id\":\"sess_1\",\"custom_data\":{\"plan\":\"pro\"},\"rules\":[{\"group\":\"fapi.sign_in\",\"window_seconds\":60,\"max\":5,\"key\":\"ip\"}],\"access_token_format\":\"opaque\",\"token_lifetimes\":{\"access_token_ttl_seconds\":600},\"provider\":\"aws_kms\",\"active\":true,\"rotation\":{\"kid\":\"k2\"}}");
            using var c = Client(h);
            var s = await c.Sessions.UpdateCustomDataAsync("sess_1", new Dictionary<string, object?> { ["plan"] = null, ["seat"] = 2 });
            Assert.Equal("PATCH", h.Requests[0].Method);
            Assert.Equal(JsonValueKind.Null, Sent(h.Requests[0]).GetProperty("custom_data").GetProperty("plan").ValueKind);
            Assert.Equal("pro", s.CustomData!["plan"].GetString());
            await c.Sessions.SetCustomDataAsync("sess_1", null);
            Assert.Equal("PUT", h.Requests[1].Method);
            Assert.Equal(JsonValueKind.Null, Sent(h.Requests[1]).GetProperty("custom_data").ValueKind);
            await c.InstanceSecurity.SetWalletRpcSecretAsync(1, "https://rpc.test");
            Assert.Equal("/v1/instance/wallet_rpc_secret", h.Requests[2].Path);
            Assert.Equal(1, Sent(h.Requests[2]).GetProperty("chain_id").GetInt32());
            var pol = await c.RateLimitPolicy.UpdateAsync(new UpdateRateLimitPolicyBody { Rules = new List<RateLimitRule> { new RateLimitRule { Group = "fapi.sign_in", WindowSeconds = 60, Max = 5, Key = "ip" } } });
            Assert.Equal(60, Sent(h.Requests[3]).GetProperty("rules")[0].GetProperty("window_seconds").GetInt32());
            Assert.Equal("ip", pol.Rules[0].Key);
            await c.Instance.SetAccountLinkingTrustAsync(new Dictionary<string, string> { ["google"] = "always" });
            Assert.Equal("always", Sent(h.Requests[4]).GetProperty("auth_config").GetProperty("accountLinkingTrust").GetProperty("google").GetString());
            await c.Instance.SetCaptchaConfigAsync(new Dictionary<string, object?> { ["provider"] = "vercel_botid", ["attestationUrl"] = "/api/botid" });
            Assert.Equal("/api/botid", Sent(h.Requests[5]).GetProperty("auth_config").GetProperty("captcha").GetProperty("attestationUrl").GetString());
            await c.AttackProtection.UpdateAsync(new UpdateAttackProtectionBody { DormantAccounts = new DormantAccountsSettings { Enabled = true, ThresholdDays = 120 } });
            Assert.Equal(120, Sent(h.Requests[6]).GetProperty("dormant_accounts").GetProperty("threshold_days").GetInt32());
            var oc = await c.OAuthClients.UpdateAsync("oc_1", new UpdateOAuthClientBody { TokenLifetimes = new OAuthTokenLifetimes { AccessTokenTtlSeconds = 600 } }.Set("access_token_format", null));
            Assert.Equal(600, Sent(h.Requests[7]).GetProperty("token_lifetimes").GetProperty("access_token_ttl_seconds").GetInt32());
            Assert.Equal(JsonValueKind.Null, Sent(h.Requests[7]).GetProperty("access_token_format").ValueKind);
            Assert.Equal("opaque", oc.AccessTokenFormat);
            Assert.Equal(600, oc.TokenLifetimes!.AccessTokenTtlSeconds);
            await c.ResourceServers.UpdateAsync("rs_1", new UpdateResourceServerBody { AccessTokenFormat = "opaque", IntrospectionClientIds = new List<string> { "oc_api" } });
            Assert.Equal("oc_api", Sent(h.Requests[8]).GetProperty("introspection_client_ids")[0].GetString());
            var kp = await c.InstanceSecurity.SetSigningKeyProviderAsync("aws_kms", new Dictionary<string, object?> { ["region"] = "us-east-1" }, new Dictionary<string, object?> { ["access_key_id"] = "a" });
            Assert.Equal("PUT", h.Requests[9].Method);
            Assert.Equal("/v1/instance/signing_key_provider", h.Requests[9].Path);
            Assert.Equal("a", Sent(h.Requests[9]).GetProperty("credentials").GetProperty("access_key_id").GetString());
            Assert.Equal("k2", kp.Rotation!.Value.GetProperty("kid").GetString());
            await c.InstanceSecurity.RemoveSigningKeyProviderAsync(emergency: true);
            Assert.Equal("DELETE", h.Requests[10].Method);
            Assert.True(Sent(h.Requests[10]).GetProperty("emergency").GetBoolean());
            Assert.Contains("captchafox", AtlasConstants.CaptchaProviders);
            Assert.Equal("user.reactivated", AtlasConstants.WebhookEventUserReactivated);
        }

        [Fact]
        public void BotIdAttestation()
        {
            var secret = new string('k', 32);
            var tok = BotId.CreateAttestation(secret, false, false, 3600, 1_800_000_000);
            Assert.StartsWith(BotId.Prefix, tok);
            var rest = tok.Substring(BotId.Prefix.Length).Split('.');
            using var mac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            Assert.Equal(Base64UrlEncoder.Encode(mac.ComputeHash(Encoding.UTF8.GetBytes(BotId.Prefix + rest[0]))), rest[1]);
            var p = JsonDocument.Parse(Base64UrlEncoder.Decode(rest[0])).RootElement;
            Assert.Equal(1, p.GetProperty("v").GetInt32());
            Assert.False(p.GetProperty("is_bot").GetBoolean());
            Assert.Equal(300, p.GetProperty("exp").GetInt64() - p.GetProperty("iat").GetInt64());
            Assert.True(p.GetProperty("jti").GetString()!.Length >= 20);
            Assert.Throws<ArgumentException>(() => BotId.CreateAttestation("short", false));
        }

        [Fact]
        public async Task DeviceAuthorizationsLookupApproveDeny()
        {
            var status = HttpStatusCode.OK;
            var h = new MockHttpMessageHandler((req, body) => status == HttpStatusCode.OK
                ? (HttpStatusCode.OK, "{\"object\":\"device_authorization\",\"id\":\"da_1\",\"user_code\":\"WDJB-MJHT\",\"client\":{\"name\":\"TV\"},\"requires_step_up\":true}")
                : (status, "{\"errors\":[{\"code\":\"STEP_UP_REQUIRED\",\"message\":\"Re-authenticate.\"}]}"));
            var d = new DeviceAuthorizations("https://fapi.test/", "sess_jwt", "pk_x", new HttpClient(h));
            var auth = await d.LookupAsync(" wdjb mjht ");
            Assert.Equal("/v1/client/me/device_authorizations/lookup", h.Requests[0].Path);
            Assert.Equal("Bearer sess_jwt", h.Requests[0].Authorization);
            Assert.Equal("pk_x", h.Requests[0].Header("x-publishable-key"));
            Assert.Equal("WDJB-MJHT", Sent(h.Requests[0]).GetProperty("user_code").GetString());
            Assert.Equal("TV", auth.GetProperty("client").GetProperty("name").GetString());
            await d.ApproveAsync(auth);
            Assert.Equal("/v1/client/me/device_authorizations/da_1/approve", h.Requests[1].Path);
            await d.DenyAsync(auth);
            Assert.Equal("/v1/client/me/device_authorizations/da_1/deny", h.Requests[2].Path);
            status = HttpStatusCode.Forbidden;
            var e = await Assert.ThrowsAsync<DeviceAuthorizationException>(() => d.ApproveAsync(auth));
            Assert.True(e.NeedsReverification);
            Assert.False(e.IsInvalidCode);
            Assert.Equal("abc", DeviceAuthorizations.FormatUserCode("abc"));
            Assert.Equal("WDJB-MJHT", DeviceAuthorizations.UserCodeFromUrl("https://x.test/d?user_code=wdjbmjht"));
            Assert.Null(DeviceAuthorizations.UserCodeFromUrl("nope"));
        }

        private const string Issuer = "https://issuer.test";
        private const string Aud = "https://api.test";
        private readonly ECDsa _ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        private string Jwks()
        {
            var p = _ec.ExportParameters(false);
            return "{\"keys\":[{\"kty\":\"EC\",\"crv\":\"P-256\",\"kid\":\"ec-1\",\"alg\":\"ES256\",\"x\":\"" + Base64UrlEncoder.Encode(p.Q.X)
                + "\",\"y\":\"" + Base64UrlEncoder.Encode(p.Q.Y) + "\"}]}";
        }

        [Fact]
        public async Task OpaqueTokensViaIntrospection()
        {
            var active = true;
            var introspections = 0;
            var h = new MockHttpMessageHandler((req, body) =>
            {
                if (req.RequestUri!.AbsolutePath == "/jwks") return (HttpStatusCode.OK, Jwks());
                introspections++;
                var form = body.Split('&').Select(kv => kv.Split('=')).ToDictionary(kv => Uri.UnescapeDataString(kv[0]), kv => Uri.UnescapeDataString(kv[1]));
                var token = form["token"];
                if (token == "oat_down") return (HttpStatusCode.InternalServerError, "");
                if (!active || token == "oat_dead") return (HttpStatusCode.OK, "{\"active\":false}");
                var iss = token == "oat_foreign" ? "https://other.test" : Issuer;
                return (HttpStatusCode.OK, "{\"active\":true,\"iss\":\"" + iss + "\",\"sub\":\"u_1\",\"aud\":\"" + Aud
                    + "\",\"scope\":\"read\",\"client_id\":\"oc_1\",\"exp\":" + (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600) + "}");
            });
            var http = new HttpClient(h);
            var v = new AccessTokenVerifier(new AccessTokenVerifierOptions
            {
                Issuer = Issuer,
                Audiences = new[] { Aud },
                JwksUrl = "https://iss.test/jwks",
                HttpClient = http,
                Introspection = new IntrospectionOptions { ClientId = "oc_api", ClientSecret = "s3cret" },
            });
            var ok = await v.VerifyAsync("Bearer oat_live", new[] { "read" });
            Assert.True(ok.Ok, ok.Description);
            Assert.Equal("u_1", ok.Subject);
            Assert.Equal("oc_1", ok.ClientId);
            Assert.Equal("/oauth2/introspect", h.Requests[0].Path);
            Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("oc_api:s3cret")), h.Requests[0].Authorization);
            Assert.Contains("token_type_hint=access_token", h.Requests[0].Body);
            await v.VerifyAsync("Bearer oat_live");
            Assert.Equal(1, introspections);
            Assert.Equal(403, (await v.VerifyAsync("Bearer oat_live", new[] { "admin" })).Status);
            Assert.False((await v.VerifyAsync("Bearer oat_dead")).Ok);
            var n = introspections;
            await v.VerifyAsync("Bearer oat_dead");
            Assert.Equal(n, introspections);
            var foreign = await v.VerifyAsync("Bearer oat_foreign");
            Assert.False(foreign.Ok);
            Assert.Contains("another authorization server", foreign.Description);
            var down = await v.VerifyAsync("Bearer oat_down");
            Assert.False(down.Ok);
            Assert.Contains("could not be validated", down.Description);

            var plain = new AccessTokenVerifier(new AccessTokenVerifierOptions { Issuer = Issuer, Audiences = new[] { Aud }, JwksUrl = "https://iss.test/jwks", HttpClient = http });
            Assert.Contains("Introspection", (await plain.VerifyAsync("Bearer oat_live")).Description);

            var live = new AccessTokenVerifier(new AccessTokenVerifierOptions
            {
                Issuer = Issuer,
                Audiences = new[] { Aud },
                JwksUrl = "https://iss.test/jwks",
                HttpClient = http,
                Introspection = new IntrospectionOptions { ClientId = "oc_api", ClientSecret = "s3cret", IntrospectJwts = true },
            });
            var jwt = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = Aud,
                Claims = new Dictionary<string, object> { ["sub"] = "u_2", ["token_use"] = "access_token" },
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(_ec) { KeyId = "ec-1" }, SecurityAlgorithms.EcdsaSha256),
            });
            Assert.True((await plain.VerifyAsync("Bearer " + jwt)).Ok);
            active = false;
            Assert.False((await live.VerifyAsync("Bearer " + jwt)).Ok);
            Assert.Throws<ArgumentException>(() => new Introspector(Issuer, new IntrospectionOptions()));
        }

        [Fact]
        public async Task IntrospectorCacheBoundsAndExpiry()
        {
            long clock = 1_800_000_000_000;
            var hits = 0;
            var h = new MockHttpMessageHandler((req, body) => { hits++; return (HttpStatusCode.OK, "{\"active\":true,\"exp\":1800000005}"); });
            var i = new Introspector(Issuer, new IntrospectionOptions { ClientId = "a", ClientSecret = "b", CacheMax = 2 }, new HttpClient(h), () => clock);
            foreach (var t in new[] { "t1", "t2", "t3" }) Assert.NotNull(await i.IntrospectAsync(t));
            Assert.Equal(2, i.CacheSize);
            await i.IntrospectAsync("t3");
            var n = hits;
            clock += 6000;
            await i.IntrospectAsync("t3");
            Assert.Equal(n + 1, hits);
        }
    }
}
