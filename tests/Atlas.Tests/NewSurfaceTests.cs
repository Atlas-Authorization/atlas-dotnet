using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Request shaping and response decoding for the 1.1.0 surface (strict bodies, platform, design, …).</summary>
    public class NewSurfaceTests
    {
        private static AtlasClient Client(MockHttpMessageHandler handler, bool? strict = null, Action<UnknownFieldsWarning>? onUnknown = null,
            string? secretKey = "sk_test_123", string? platformKey = "psk_test_456") =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = secretKey ?? "",
                PlatformKey = platformKey,
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
                Strict = strict,
                OnUnknownFields = onUnknown,
            });

        private static JsonElement Sent(RecordedRequest r) => JsonDocument.Parse(r.Body).RootElement;

        private const string RedirectUrlJson = "{\"object\":\"redirect_url\",\"id\":\"ru_1\",\"url\":\"https://a\"}";

        // ---- strict bodies + unknown fields ------------------------------------

        [Fact]
        public async Task Strict_NotSentByDefault()
        {
            var h = MockHttpMessageHandler.Json(RedirectUrlJson);
            using var c = Client(h);
            await c.RedirectUrls.CreateAsync("https://a");
            Assert.Null(h.Requests[0].Header("Atlas-Strict"));
        }

        [Fact]
        public async Task StrictClient_SendsHeaderOnWritesOnly()
        {
            var h = MockHttpMessageHandler.Json(RedirectUrlJson);
            using var c = Client(h, strict: true);
            await c.RedirectUrls.CreateAsync("https://a");
            Assert.Equal("true", h.Requests[0].Header("Atlas-Strict"));
            await c.RedirectUrls.GetAsync("ru_1");
            Assert.Null(h.Requests[1].Header("Atlas-Strict"));
        }

        [Fact]
        public async Task WithStrict_OverridesPerCall()
        {
            var h = MockHttpMessageHandler.Json(RedirectUrlJson);
            using var c = Client(h);
            await c.WithStrict(false).RedirectUrls.CreateAsync("https://a");
            await c.WithStrict(true).RedirectUrls.CreateAsync("https://a");
            await c.RedirectUrls.CreateAsync("https://a");
            Assert.Equal("false", h.Requests[0].Header("Atlas-Strict"));
            Assert.Equal("true", h.Requests[1].Header("Atlas-Strict"));
            Assert.Null(h.Requests[2].Header("Atlas-Strict"));
        }

        [Fact]
        public async Task UnknownFieldsHeader_ReachesTheCallback()
        {
            var seen = new List<UnknownFieldsWarning>();
            var h = MockHttpMessageHandler.Json(RedirectUrlJson);
            h.ResponseHeaders["Atlas-Unknown-Fields"] = "colour, branding.font";
            using var c = Client(h, onUnknown: seen.Add);
            await c.RedirectUrls.CreateAsync("https://a");
            var w = Assert.Single(seen);
            Assert.Equal("POST", w.Method);
            Assert.Equal("/v1/redirect_urls", w.Path);
            Assert.Equal(new[] { "colour", "branding.font" }, w.Fields);
        }

        [Fact]
        public async Task StrictRefusal_SurfacesUnknownFieldCode()
        {
            var h = MockHttpMessageHandler.Json(
                "{\"errors\":[{\"code\":\"UNKNOWN_FIELD\",\"message\":\"colour is not accepted\",\"param\":\"colour\"}]}",
                (HttpStatusCode)422);
            using var c = Client(h);
            var e = await Assert.ThrowsAnyAsync<AtlasException>(() => c.WithStrict(true).RedirectUrls.CreateAsync("https://a"));
            Assert.Equal(ErrorCodes.UnknownField, e.Code);
            Assert.Equal(422, e.Status);
        }

        // ---- platform: Sign in with Atlas apps ---------------------------------

        private const string AtlasIdAppJson = @"{""object"":""atlas_id_app"",""id"":""aia_1"",""client_id"":""atlas_client_1"",""name"":""Acme"",
            ""branding"":{""theme"":""dark"",""accent_color"":""#112233"",""copy"":{""sign_in_title"":""Welcome to {app}""}},
            ""redirect_uris"":[""https://acme.test/cb""],""post_logout_redirect_uris"":[""https://acme.test/bye""],
            ""backchannel_logout_uri"":""https://acme.test/bcl"",""backchannel_logout_session_required"":true,
            ""frontchannel_logout_uri"":null,""frontchannel_logout_session_required"":false,
            ""allowed_scopes"":[""openid""],""created_at"":1,""updated_at"":2,""client_secret"":""secret_x""}";

        [Fact]
        public async Task CreateAtlasIdApp_UsesPlatformKey_AndSendsLogoutAndBranding()
        {
            var h = MockHttpMessageHandler.Json(AtlasIdAppJson);
            using var c = Client(h);
            var app = await c.Platform.AtlasIdApps.CreateAsync(new CreateAtlasIdAppBody
            {
                Name = "Acme",
                RedirectUris = new List<string> { "https://acme.test/cb" },
                PostLogoutRedirectUris = new List<string> { "https://acme.test/bye" },
                BackchannelLogoutUri = "https://acme.test/bcl",
                BackchannelLogoutSessionRequired = true,
                FrontchannelLogoutUri = "https://acme.test/fcl",
                Branding = new AtlasIdAppBrandingInput
                {
                    Theme = "dark",
                    AccentColor = "#112233",
                    Copy = new AtlasIdAppCopy { SignInTitle = "Welcome to {app}" },
                    Design = new AtlasDesign { Preset = "glass", Palette = new AtlasDesignPalette { Primary = "#112233" } },
                },
            }, idempotencyKey: "idem-1");

            var r = Assert.Single(h.Requests);
            Assert.Equal("POST", r.Method);
            Assert.Equal("/v1/platform/atlas_id_apps", r.Path);
            Assert.Equal("Bearer psk_test_456", r.Authorization);
            Assert.Equal("idem-1", r.IdempotencyKey);
            var j = Sent(r);
            Assert.Equal("https://acme.test/bye", j.GetProperty("post_logout_redirect_uris")[0].GetString());
            Assert.Equal("https://acme.test/bcl", j.GetProperty("backchannel_logout_uri").GetString());
            Assert.True(j.GetProperty("backchannel_logout_session_required").GetBoolean());
            Assert.Equal("https://acme.test/fcl", j.GetProperty("frontchannel_logout_uri").GetString());
            Assert.Equal("#112233", j.GetProperty("branding").GetProperty("accent_color").GetString());
            Assert.Equal("Welcome to {app}", j.GetProperty("branding").GetProperty("copy").GetProperty("sign_in_title").GetString());
            Assert.Equal("glass", j.GetProperty("branding").GetProperty("design").GetProperty("preset").GetString());
            Assert.Equal("#112233", j.GetProperty("branding").GetProperty("design").GetProperty("palette").GetProperty("primary").GetString());
            Assert.False(j.TryGetProperty("frontchannel_logout_session_required", out _));

            Assert.Equal("atlas_client_1", app.ClientId);
            Assert.Equal("dark", app.Branding!.Theme);
            Assert.Equal("Welcome to {app}", app.Branding.Copy!.SignInTitle);
            Assert.True(app.BackchannelLogoutSessionRequired);
        }

        [Fact]
        public async Task UpdateAtlasIdApp_CanClearAFieldWithExplicitNull()
        {
            var h = MockHttpMessageHandler.Json(AtlasIdAppJson);
            using var c = Client(h);
            await c.Platform.AtlasIdApps.UpdateAsync("aia_1", new UpdateAtlasIdAppBody { Name = "Acme 2" }.Set("frontchannel_logout_uri", null));
            var r = h.Requests[0];
            Assert.Equal("PATCH", r.Method);
            Assert.Equal("/v1/platform/atlas_id_apps/aia_1", r.Path);
            var j = Sent(r);
            Assert.Equal("Acme 2", j.GetProperty("name").GetString());
            Assert.Equal(JsonValueKind.Null, j.GetProperty("frontchannel_logout_uri").ValueKind);
        }

        [Fact]
        public async Task AtlasIdApp_RotateDeletePreview()
        {
            var h = new MockHttpMessageHandler((req, _) => (HttpStatusCode.OK,
                req.RequestUri!.AbsolutePath.EndsWith("/preview")
                    ? "{\"object\":\"design_preview\",\"scope\":\".atlas-root\",\"css\":\"\",\"html\":\"\"}"
                    : req.Method == HttpMethod.Delete ? "{\"object\":\"atlas_id_app\",\"id\":\"aia_1\",\"deleted\":true}" : AtlasIdAppJson));
            using var c = Client(h);
            await c.Platform.AtlasIdApps.RotateSecretAsync("aia_1");
            Assert.Equal("/v1/platform/atlas_id_apps/aia_1/rotate_secret", h.Requests[0].Path);
            Assert.True((await c.Platform.AtlasIdApps.DeleteAsync("aia_1")).Deleted);
            var p = await c.Platform.AtlasIdApps.PreviewDesignAsync(new AtlasDesign { Preset = "bold" });
            Assert.Equal(".atlas-root", p.Scope);
            Assert.Equal("/v1/platform/atlas_id_apps/design/preview", h.Requests[2].Path);
            Assert.Equal("bold", Sent(h.Requests[2]).GetProperty("design").GetProperty("preset").GetString());
        }

        [Fact]
        public async Task PlatformCall_WithoutPlatformKey_FailsClearly()
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var c = Client(h, platformKey: null);
            var e = await Assert.ThrowsAsync<InvalidOperationException>(() => c.Platform.AtlasIdApps.ListAsync());
            Assert.Contains("PlatformKey", e.Message);
            Assert.Empty(h.Requests);
        }

        [Fact]
        public async Task PlatformOnlyClient_IsAllowed()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[]}");
            using var c = Client(h, secretKey: null, platformKey: "psk_x");
            await c.Platform.Apps.ListAsync();
            Assert.Equal("Bearer psk_x", h.Requests[0].Authorization);
            await Assert.ThrowsAsync<InvalidOperationException>(() => c.Users.GetAsync("user_1"));
        }

        // ---- raw uploads -------------------------------------------------------

        private const string AssetJson = "{\"object\":\"design_asset\",\"asset_id\":\"dasset_1\",\"kind\":\"logo\",\"url\":\"https://cdn/x.webp\","
                                         + "\"width\":10,\"height\":20,\"dominant_color\":null,\"bytes\":99,\"created_at\":\"2026-10-13T00:00:00Z\"}";

        [Fact]
        public async Task DesignAssetUpload_SendsRawBytesWithContentType()
        {
            var h = MockHttpMessageHandler.Json(AssetJson, HttpStatusCode.Created);
            using var c = Client(h);
            var png = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 1, 2, 3 };
            var asset = await c.Design.UploadAssetAsync(png, "image/png", "logo");
            var r = h.Requests[0];
            Assert.Equal("POST", r.Method);
            Assert.Equal("/v1/design/assets", r.Path);
            Assert.Equal("?kind=logo", r.Query);
            Assert.Equal("image/png", r.ContentType);
            Assert.Equal(png, r.BodyBytes);
            Assert.Equal("dasset_1", asset.AssetId);
            Assert.Equal(20, asset.Height);
        }

        [Fact]
        public async Task PlatformDesignAssetUpload_UsesPlatformKey()
        {
            var h = MockHttpMessageHandler.Json(AssetJson, HttpStatusCode.Created);
            using var c = Client(h);
            await c.Platform.DesignAssets.UploadAsync(new byte[] { 1 }, "image/webp");
            var r = h.Requests[0];
            Assert.Equal("/v1/platform/design/assets", r.Path);
            Assert.Equal("image/webp", r.ContentType);
            Assert.Equal("Bearer psk_test_456", r.Authorization);
            Assert.Equal("", r.Query);
        }

        // ---- design ------------------------------------------------------------

        private const string DesignSettingsJson = @"{""object"":""design"",""design"":{""preset"":""glass"",""colorScheme"":""dark"",""palette"":{""primary"":""#123456""}},
            ""resolved"":{""version"":1,""preset"":""glass"",""colorScheme"":""dark"",""palette"":{""primary"":""#123456"",""primaryInk"":""#ffffff""},
              ""typography"":{""font"":""inter"",""baseSize"":16},""elevation"":{""cardShadow"":""md""},""logo"":{}},
            ""warnings"":[{""code"":""LOW_CONTRAST"",""message"":""low""}],""legacy"":false}";

        [Fact]
        public async Task DesignGet_DecodesTypedSections()
        {
            var h = MockHttpMessageHandler.Json(DesignSettingsJson);
            using var c = Client(h);
            var d = await c.Design.GetAsync();
            Assert.Equal("/v1/instance/design", h.Requests[0].Path);
            Assert.Equal("dark", d.Design!.ColorScheme);
            Assert.Equal("#123456", d.Design.Palette!.Primary);
            Assert.Equal("#ffffff", d.Resolved!.Palette!.PrimaryInk);
            Assert.Equal(16, d.Resolved.Typography!.BaseSize);
            Assert.Equal("md", d.Resolved.Elevation!.CardShadow);
            Assert.Single(d.Warnings);
        }

        [Fact]
        public async Task DesignPatch_SendsCamelCaseAndExplicitNulls()
        {
            var h = MockHttpMessageHandler.Json(DesignSettingsJson);
            using var c = Client(h);
            await c.Design.UpdateAsync(new AtlasDesign { ColorScheme = "light" }.Set("paletteDark", null));
            var r = h.Requests[0];
            Assert.Equal("PATCH", r.Method);
            var design = Sent(r).GetProperty("design");
            Assert.Equal("light", design.GetProperty("colorScheme").GetString());
            Assert.Equal(JsonValueKind.Null, design.GetProperty("paletteDark").ValueKind);
            Assert.False(design.TryGetProperty("color_scheme", out _));
        }

        [Fact]
        public async Task OrganizationDesign_AndPreview_Paths()
        {
            var h = MockHttpMessageHandler.Json(DesignSettingsJson);
            using var c = Client(h);
            await c.Design.ReplaceForOrganizationAsync("org_1", new AtlasDesign { Preset = "minimal" });
            await c.Design.GetForOrganizationAsync("org_1");
            await c.Design.PreviewAsync(new AtlasDesign { Preset = "bold" });
            Assert.Equal("PUT", h.Requests[0].Method);
            Assert.Equal("/v1/organizations/org_1/design", h.Requests[0].Path);
            Assert.Equal("GET", h.Requests[1].Method);
            Assert.Equal("/v1/instance/design/preview", h.Requests[2].Path);
        }

        [Fact]
        public async Task DesignCatalog_Decodes()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"design_catalog\",\"version\":1,"
                + "\"presets\":[{\"id\":\"glass\",\"label\":\"Glass\",\"description\":\"d\",\"resolved\":{\"preset\":\"glass\"}}],"
                + "\"fonts\":[{\"id\":\"inter\",\"label\":\"Inter\",\"family\":\"Inter\",\"category\":\"sans\",\"web_font\":true,\"stylesheet\":\"/v1/design/fonts/inter.css\"}],"
                + "\"css_variables\":[{\"name\":\"--atlas-color-primary\",\"type\":\"color\",\"perMode\":true,\"description\":\"x\"}]}");
            using var c = Client(h);
            var cat = await c.Design.CatalogAsync();
            Assert.Equal("/v1/design/catalog", h.Requests[0].Path);
            Assert.Equal("glass", cat.Presets[0].Resolved!.Preset);
            Assert.True(cat.Fonts[0].WebFont);
            Assert.True(cat.CssVariables[0].PerMode);
        }

        // ---- preflight + cookie consent ----------------------------------------

        [Fact]
        public async Task Preflight_DecodesTypedFindings()
        {
            var h = MockHttpMessageHandler.Json(@"{""object"":""preflight"",""ok"":false,""checked_at"":1700000000000,
                ""findings"":[{""severity"":""error"",""code"":""NO_SIGN_IN_METHOD"",""message"":""No sign-in method is enabled."",
                ""fix"":""Enable one."",""subject"":{""type"":""instance""}}]}");
            using var c = Client(h);
            var p = await c.Instance.PreflightAsync();
            Assert.Equal("/v1/instance/preflight", h.Requests[0].Path);
            Assert.False(p.Ok);
            var f = Assert.Single(p.Findings);
            Assert.Equal("error", f.Severity);
            Assert.Equal("NO_SIGN_IN_METHOD", f.Code);
            Assert.Equal("Enable one.", f.Fix);
            Assert.Equal("instance", f.Subject!["type"]);
        }

        [Fact]
        public async Task CookieConsent_RoundTrip()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"cookie_consent\",\"enabled\":true,\"script_url\":\"https://cmp.test/s.js\","
                + "\"site_id\":\"site_1\",\"blocking_mode\":\"auto\",\"extra_origins\":[\"https://cmp.test\"]}");
            using var c = Client(h);
            var cc = await c.Instance.ReplaceCookieConsentAsync(new CookieConsentBody
            {
                ScriptUrl = "https://cmp.test/s.js",
                SiteId = "site_1",
                BlockingMode = "auto",
                ExtraOrigins = new List<string> { "https://cmp.test" },
            });
            Assert.Equal("PUT", h.Requests[0].Method);
            Assert.Equal("/v1/instance/cookie_consent", h.Requests[0].Path);
            Assert.Equal("site_1", Sent(h.Requests[0]).GetProperty("site_id").GetString());
            Assert.True(cc.Enabled);
            Assert.Equal("https://cmp.test", cc.ExtraOrigins![0]);
            await c.Instance.GetCookieConsentAsync();
            await c.Instance.DeleteCookieConsentAsync();
            Assert.Equal("GET", h.Requests[1].Method);
            Assert.Equal("DELETE", h.Requests[2].Method);
        }

        // ---- oauth providers ---------------------------------------------------

        [Fact]
        public async Task OAuthProviders_ListFilters_AndNewFields()
        {
            var h = MockHttpMessageHandler.Json(@"{""object"":""list"",""data"":[{""object"":""oauth_provider"",""provider"":""siwe"",
                ""display_name"":""Ethereum"",""configured"":true,""enabled"":true,""allow_sign_in"":true,""allow_sign_up"":true,""native"":false,
                ""mode"":""redirect"",""icon_url"":""https://i/e.svg"",""setting_values"":{""domain"":""acme.test"",""chain_ids"":""1,10""}}]}");
            using var c = Client(h);
            var page = await c.OAuthProviders.ListAsync(new[] { "siwe", "google" }, configured: true);
            Assert.Equal("?provider=siwe&provider=google&configured=true", h.Requests[0].Query);
            var p = page.Data[0];
            Assert.Equal("redirect", p.Mode);
            Assert.Equal("https://i/e.svg", p.IconUrl);
            Assert.Equal("1,10", p.SettingValues!["chain_ids"]);
        }

        [Fact]
        public async Task OAuthProviders_Upsert_DisplayAndWalletSettings()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"oauth_provider\",\"provider\":\"siwe\",\"configured\":true,\"updated_fields\":[\"settings\"]}");
            using var c = Client(h);
            var body = UpsertOAuthProviderBody.Siwe(domain: "acme.test", chainIds: "1,10").ClearSetting("rpc_url");
            body.DisplayName = "Ethereum";
            body.IconUrl = "https://i/e.svg";
            body.Mode = "redirect";
            var res = await c.OAuthProviders.UpsertAsync("siwe", body);
            var r = h.Requests[0];
            Assert.Equal("PUT", r.Method);
            Assert.Equal("/v1/oauth_providers/siwe", r.Path);
            var j = Sent(r);
            Assert.Equal("Ethereum", j.GetProperty("display_name").GetString());
            Assert.Equal("https://i/e.svg", j.GetProperty("icon_url").GetString());
            Assert.Equal("redirect", j.GetProperty("mode").GetString());
            Assert.Equal("acme.test", j.GetProperty("settings").GetProperty("domain").GetString());
            Assert.Equal(JsonValueKind.Null, j.GetProperty("settings").GetProperty("rpc_url").ValueKind);
            Assert.False(j.TryGetProperty("client_id", out _));
            Assert.Equal("settings", res.UpdatedFields![0]);
        }

        // ---- webhooks ----------------------------------------------------------

        [Fact]
        public async Task Webhooks_EndpointManagement()
        {
            var h = new MockHttpMessageHandler((req, _) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                if (path.EndsWith("_secret")) return (HttpStatusCode.OK, "{\"object\":\"webhook_secret\",\"secret\":\"whsec_1\"}");
                if (path.EndsWith("/test") || path.EndsWith("/redeliver"))
                    return (HttpStatusCode.OK, "{\"object\":\"webhook_delivery\",\"id\":\"wd_1\",\"event_id\":\"ev_1\",\"status\":\"pending\",\"note\":\"queued\"}");
                if (path.EndsWith("/deliveries"))
                    return (HttpStatusCode.OK, "{\"object\":\"list\",\"data\":[{\"id\":\"wd_1\",\"event_id\":\"ev_1\",\"event_type\":\"user.created\","
                        + "\"attempt_number\":1,\"max_attempts\":8,\"payload\":{\"a\":1},\"status\":\"success\",\"created_at\":1}]}");
                if (req.Method == HttpMethod.Get)
                    return (HttpStatusCode.OK, "{\"object\":\"list\",\"data\":[],\"available_events\":[\"user.created\"],\"retry_schedule_ms\":[1000,5000],\"max_attempts\":8}");
                return (HttpStatusCode.OK, "{\"object\":\"webhook_endpoint\",\"id\":\"we_1\",\"url\":\"https://h\",\"enabled_events\":[\"*\"],"
                    + "\"active\":false,\"failing_since\":5,\"created_at\":1}");
            });
            using var c = Client(h);

            var ep = await c.Webhooks.Endpoints.UpdateAsync("we_1", new UpdateWebhookEndpointBody { Active = false });
            Assert.Equal("PATCH", h.Requests[0].Method);
            Assert.False(Sent(h.Requests[0]).GetProperty("active").GetBoolean());
            Assert.Equal(5, ep.FailingSince);

            Assert.Equal("whsec_1", (await c.Webhooks.Endpoints.RevealSecretAsync("we_1")).Secret);
            Assert.Equal("/v1/webhook_endpoints/we_1/reveal_secret", h.Requests[1].Path);
            await c.Webhooks.Endpoints.RotateSecretAsync("we_1");
            Assert.Equal("/v1/webhook_endpoints/we_1/rotate_secret", h.Requests[2].Path);
            Assert.Equal("pending", (await c.Webhooks.Endpoints.TestAsync("we_1")).Status);
            await c.Webhooks.RedeliverAsync("we_1", "wd_9");
            Assert.Equal("/v1/webhook_endpoints/we_1/deliveries/wd_9/redeliver", h.Requests[4].Path);

            var deliveries = await c.Webhooks.DeliveriesAsync("we_1", 10);
            Assert.Equal("?limit=10", h.Requests[5].Query);
            Assert.Equal("user.created", deliveries.Data[0].EventType);
            Assert.Equal(1, deliveries.Data[0].Payload!.Value.GetProperty("a").GetInt32());

            var all = await c.Webhooks.Endpoints.ListWithEventsAsync();
            Assert.Equal("user.created", all.AvailableEvents[0]);
            Assert.Equal(new long[] { 1000, 5000 }, all.RetryScheduleMs);
            Assert.Equal(8, all.MaxAttempts);
        }

        // ---- entitlements + org policy -----------------------------------------

        [Fact]
        public async Task EntitlementOverrides_AndOrgEntitlements()
        {
            const string row = "{\"object\":\"entitlement_override\",\"id\":\"eo_1\",\"subject_type\":\"org\",\"subject_id\":\"org_1\","
                               + "\"plan\":\"enterprise\",\"features\":[\"sso\"],\"limits\":{\"seats\":500},\"expires_at\":null,\"created_by\":null,\"created_at\":1}";
            var h = new MockHttpMessageHandler((req, _) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                if (path.EndsWith("/entitlements"))
                    return (HttpStatusCode.OK, "{\"object\":\"organization_entitlements\",\"organization_id\":\"org_1\",\"plan\":\"pro\",\"features\":[\"sso\",\"audit\"],\"limits\":{\"seats\":10}}");
                if (req.Method == HttpMethod.Delete) return (HttpStatusCode.OK, "{\"object\":\"entitlement_override\",\"id\":\"eo_1\",\"deleted\":true}");
                if (req.Method == HttpMethod.Get) return (HttpStatusCode.OK, "{\"object\":\"list\",\"data\":[" + row + "]}");
                return (HttpStatusCode.OK, row);
            });
            using var c = Client(h);
            var eo = await c.EntitlementOverrides.CreateAsync(new CreateEntitlementOverrideBody
            {
                SubjectType = "org",
                SubjectId = "org_1",
                Plan = "enterprise",
                Features = new List<string> { "sso" },
                Limits = new Dictionary<string, object?> { ["seats"] = 500 },
            });
            var j = Sent(h.Requests[0]);
            Assert.Equal("org", j.GetProperty("subject_type").GetString());
            Assert.Equal(500, j.GetProperty("limits").GetProperty("seats").GetInt32());
            Assert.Equal("enterprise", eo.Plan);

            await c.EntitlementOverrides.ListAsync("org", "org_1");
            Assert.Equal("?subject_type=org&subject_id=org_1", h.Requests[1].Query);
            Assert.True((await c.EntitlementOverrides.DeleteAsync("eo_1")).Deleted);

            var ent = await c.Organizations.EntitlementsAsync("org_1");
            Assert.Equal(new[] { "sso", "audit" }, ent.Features);
            Assert.Equal(10, ent.Limits["seats"].GetInt32());
        }

        [Fact]
        public async Task OrgPolicyPatch_TypedFields_AndClear()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"organization_policy\",\"organization_id\":\"org_1\",\"policy\":{"
                + "\"requireMfa\":true,\"allowedSignInMethods\":[\"passkey\"],\"ipAllowlist\":[\"10.0.0.0/8\"],"
                + "\"maxSessionAgeSeconds\":3600,\"endUserPool\":{\"enabled\":true,\"requireApproval\":false},\"future\":1}}");
            using var c = Client(h);
            var res = await c.Organizations.UpdatePolicyAsync("org_1", new OrganizationPolicyPatch
            {
                RequireMfa = true,
                AllowedSignInMethods = new List<string> { "passkey" },
                MaxSessionAgeSeconds = 3600,
                EndUserPool = new EndUserPoolPolicy { Enabled = true, RequireApproval = false },
            }.Clear("ipAllowlist"));
            var j = Sent(h.Requests[0]);
            Assert.True(j.GetProperty("requireMfa").GetBoolean());
            Assert.Equal("passkey", j.GetProperty("allowedSignInMethods")[0].GetString());
            Assert.Equal(3600, j.GetProperty("maxSessionAgeSeconds").GetInt32());
            Assert.True(j.GetProperty("endUserPool").GetProperty("enabled").GetBoolean());
            Assert.Equal(JsonValueKind.Null, j.GetProperty("ipAllowlist").ValueKind);

            var typed = res.Typed();
            Assert.True(typed.RequireMfa);
            Assert.Equal("10.0.0.0/8", typed.IpAllowlist![0]);
            Assert.Equal(3600, typed.MaxSessionAgeSeconds);
            Assert.False(typed.EndUserPool!.RequireApproval);
            Assert.Equal(1, typed.Extra!["future"].GetInt32());
        }

        // ---- a few Phase B shapes ----------------------------------------------

        [Fact]
        public async Task Machines_List_UsesParamsObject()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[],\"has_more\":true,\"next_cursor\":\"m_9\"}");
            using var c = Client(h);
            var page = await c.Machines.ListAsync(new ListMachinesParams { Status = "active", Limit = 5 });
            Assert.Equal("?status=active&limit=5", h.Requests[0].Query);
            Assert.True(page.HasMore);
            Assert.Equal("m_9", page.NextCursor);
        }

        [Fact]
        public async Task AuditEventExport_ReturnsRawText()
        {
            var h = MockHttpMessageHandler.Json("{\"id\":\"a1\"}\n{\"id\":\"a2\"}\n");
            using var c = Client(h);
            var ndjson = await c.AuditEvents.ExportAsync(new ExportAuditEventsParams { Format = "jsonl" });
            Assert.Equal("/v1/audit_events/export", h.Requests[0].Path);
            Assert.Equal("?format=jsonl", h.Requests[0].Query);
            Assert.Equal(2, ndjson.Trim().Split('\n').Length);
        }
    }
}
