using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Referral attribution: settings, connection test, call log, user referral fields and the fraud flag.</summary>
    public class ReferralsTests
    {
        private const string Settings = "{\"object\":\"referral_settings\",\"enabled\":true,\"provider\":\"webhook\","
            + "\"endpoint_url\":\"https://referrals.example.com/atlas\",\"program_id\":null,\"connection_id\":null,"
            + "\"signing_secret_set\":true,\"api_key_set\":false,\"url_params\":[\"ref\",\"via\",\"aff\"],\"context_cookie\":null,"
            + "\"consent_category\":\"referrals\",\"consent_fallback_category\":\"marketing\",\"auto_enroll_users\":true,"
            + "\"link_template\":\"https://app.example.com/join?ref={code}\",\"timeout_ms\":3000,"
            + "\"security\":{\"challenge_referred_signups\":\"always\",\"on_high_risk\":\"flag\",\"hold_until_email_verified\":false,\"max_referred_signups_per_ip_hour\":5},"
            + "\"missing\":[],\"connection\":{\"last_test\":{\"ok\":true,\"at\":1,\"http_status\":404,\"error\":null},\"failed_calls_7d\":2,\"retrying_calls\":1,\"last_success_at\":1},"
            + "\"providers\":[{\"id\":\"webhook\",\"name\":\"Atlas referral protocol (any platform)\",\"capabilities\":{\"signup\":true,\"erase\":true,\"export\":true,\"partner\":true,\"auto_enroll\":true,\"signal\":true},"
            + "\"needs\":[\"endpoint_url\",\"signing_secret\"],\"default_context_cookie\":null,\"docs_url\":null}],\"updated_at\":1}";

        private const string Call = "{\"object\":\"referral_call\",\"id\":\"rfc_1\",\"kind\":\"signup\",\"status\":\"succeeded\",\"outcome\":\"referred\","
            + "\"customer_ref\":\"user_1\",\"user_id\":\"user_1\",\"http_status\":200,\"last_error\":null,\"attempts\":2,\"next_attempt_at\":null,"
            + "\"risk\":{\"level\":\"low\",\"score\":5,\"signals\":[],\"captcha\":\"passed\",\"emailVerified\":true},\"payload\":{\"customerRef\":\"user_1\"},"
            + "\"created_at\":1,\"updated_at\":2}";

        private static AtlasClient Client(MockHttpMessageHandler handler) =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

        [Fact]
        public async Task Settings_GetUpdateAndTest()
        {
            var handler = MockHttpMessageHandler.Json(Settings);
            using var client = Client(handler);

            var view = await client.Referrals.GetAsync();
            Assert.Equal("GET", handler.Requests[0].Method);
            Assert.Equal("/v1/instance/referrals", handler.Requests[0].Path);
            Assert.Equal("flag", view.Security.OnHighRisk);
            Assert.Equal(5, view.Security.MaxReferredSignupsPerIpHour);
            Assert.True(view.Providers[0].Capabilities.Signal);
            Assert.Equal(2, view.Connection!.FailedCalls7d);
            Assert.Equal(404, view.Connection.LastTest!.HttpStatus);

            await client.Referrals.UpdateAsync(new UpdateReferralSettingsBody
            {
                Enabled = true,
                SigningSecret = new string('x', 32),
                Security = new UpdateReferralSecurityBody { OnHighRisk = "hold" },
            }.Clear("link_template"), idempotencyKey: "idem_1");
            Assert.Equal("PATCH", handler.Requests[1].Method);
            using (var doc = JsonDocument.Parse(handler.Requests[1].Body))
            {
                var root = doc.RootElement;
                Assert.True(root.GetProperty("enabled").GetBoolean());
                Assert.Equal("hold", root.GetProperty("security").GetProperty("on_high_risk").GetString());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("link_template").ValueKind);
                Assert.False(root.TryGetProperty("endpoint_url", out _));
            }
            Assert.Equal("idem_1", handler.Requests[1].IdempotencyKey);
        }

        [Fact]
        public async Task ConnectionTest_Decodes()
        {
            var handler = MockHttpMessageHandler.Json("{\"ok\":false,\"http_status\":401,\"latency_ms\":12,\"error\":{\"code\":\"signature_rejected\",\"message\":\"m\"}}");
            using var client = Client(handler);
            var test = await client.Referrals.TestAsync();
            Assert.Equal("POST", handler.Requests[0].Method);
            Assert.Equal("/v1/instance/referrals/test", handler.Requests[0].Path);
            Assert.False(test.Ok);
            Assert.Equal("signature_rejected", test.Error!.Code);
        }

        [Fact]
        public async Task CallLog_AndRetry()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[" + Call + "],\"has_more\":false}");
            using var client = Client(handler);
            var page = await client.Referrals.ListCallsAsync(limit: 10, status: "failed", kind: "signup", userId: "user_1");
            Assert.Equal("/v1/instance/referrals/calls", handler.Requests[0].Path);
            Assert.Equal("?limit=10&status=failed&kind=signup&user_id=user_1", handler.Requests[0].Query);
            Assert.True(page.Data[0].Risk!.EmailVerified);

            var retry = MockHttpMessageHandler.Json(Call);
            using var client2 = Client(retry);
            var call = await client2.Referrals.RetryCallAsync("rfc_1", "idem_2");
            Assert.Equal("POST", retry.Requests[0].Method);
            Assert.Equal("/v1/instance/referrals/calls/rfc_1/retry", retry.Requests[0].Path);
            Assert.Equal("idem_2", retry.Requests[0].IdempotencyKey);
            Assert.Equal("referred", call.Outcome);
        }

        [Fact]
        public async Task UserReferralFields_AndFlag()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"user\",\"id\":\"user_1\",\"referral\":{\"partner_id\":\"pn_1\",\"referrer_external_id\":\"cust_9\","
                + "\"referrer_user_id\":\"user_9\",\"matched_via\":\"code\",\"status\":\"lead\",\"risk_level\":\"low\",\"flagged\":false},"
                + "\"referral_code\":\"ada-1\",\"referral_link\":\"https://app.example.com/join?ref=ada-1\"}");
            using var client = Client(handler);

            var created = await client.Users.CreateAsync(new CreateUserBody
            {
                EmailAddress = "a@example.com",
                Referral = new CreateUserReferral { Code = "ACME10", ClickId = "clk_123" },
                ReferralCode = "ada-1",
            });
            Assert.Equal("{\"email_address\":\"a@example.com\",\"referral\":{\"code\":\"ACME10\",\"click_id\":\"clk_123\"},\"referral_code\":\"ada-1\"}", handler.Requests[0].Body);
            Assert.Equal("user_9", created.Referral!.ReferrerUserId);
            Assert.Equal("ada-1", created.ReferralCode);

            await client.Users.UpdateAsync("user_1", new UpdateUserBody().ClearReferralCode());
            Assert.Equal("{\"referral_code\":null}", handler.Requests[1].Body);
            await client.Users.UpdateAsync("user_1", new UpdateUserBody { ReferralCode = "ada-2" });
            Assert.Equal("{\"referral_code\":\"ada-2\"}", handler.Requests[2].Body);

            var flagHandler = MockHttpMessageHandler.Json("{\"object\":\"user_referral\",\"user_id\":\"user_1\",\"status\":\"flagged\",\"flagged\":true,\"flag_reason\":\"ring\",\"flagged_at\":5,\"risk_level\":\"high\"}");
            using var flagClient = Client(flagHandler);
            var flag = await flagClient.Users.FlagReferralAsync("user_1", "ring", "idem_3");
            Assert.Equal("/v1/users/user_1/referral/flag", flagHandler.Requests[0].Path);
            Assert.Equal("{\"reason\":\"ring\"}", flagHandler.Requests[0].Body);
            Assert.Equal("idem_3", flagHandler.Requests[0].IdempotencyKey);
            Assert.True(flag.Flagged);
            Assert.Equal(5L, flag.FlaggedAt);
            await flagClient.Users.FlagReferralAsync("user_1");
            Assert.Equal("{}", flagHandler.Requests[1].Body);
        }

        [Fact]
        public async Task ReferralCodeTaken_Surfaces()
        {
            var handler = MockHttpMessageHandler.Json("{\"errors\":[{\"code\":\"REFERRAL_CODE_TAKEN\",\"message\":\"taken\",\"param\":\"referral_code\"}]}", HttpStatusCode.Conflict);
            using var client = Client(handler);
            var e = await Assert.ThrowsAnyAsync<AtlasException>(() => client.Users.UpdateAsync("user_1", new UpdateUserBody { ReferralCode = "ada-1" }));
            Assert.Equal(409, e.Status);
            Assert.Equal(ErrorCodes.ReferralCodeTaken, e.Code);
            Assert.Equal("REFERRALS_NOT_CONFIGURED", ErrorCodes.ReferralsNotConfigured);
        }
        [Fact]
        public async Task Inbound_TwoWaySyncFields()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"referral_settings\",\"enabled\":true,\"provider\":\"webhook\",\"signing_secret_set\":true,\"api_key_set\":false,"
                + "\"url_params\":[\"ref\"],"
                + "\"inbound\":{\"url\":\"https://api.example.test/v1/webhooks/referrals/ins_1\",\"secret_set\":true,\"endpoint_id\":\"we_1\","
                + "\"last_event\":{\"at\":1700000000000,\"type\":\"referral.updated\",\"outcome\":\"applied\"}},"
                + "\"providers\":[{\"id\":\"webhook\",\"capabilities\":{\"signup\":true,\"erase\":true,\"export\":true,\"partner\":true,\"auto_enroll\":true,\"signal\":true,\"inbound\":true},"
                + "\"needs\":[\"endpoint_url\",\"signing_secret\"]}]}");
            using var client = Client(handler);
            var view = await client.Referrals.GetAsync();
            Assert.Equal("https://api.example.test/v1/webhooks/referrals/ins_1", view.Inbound!.Url);
            Assert.True(view.Inbound.SecretSet);
            Assert.Equal("we_1", view.Inbound.EndpointId);
            Assert.Equal(1700000000000L, view.Inbound.LastEvent!.At);
            Assert.Equal("referral.updated", view.Inbound.LastEvent.Type);
            Assert.Equal("applied", view.Inbound.LastEvent.Outcome);
            Assert.True(view.Providers[0].Capabilities.Inbound);

            await client.Referrals.UpdateAsync(new UpdateReferralSettingsBody
            {
                InboundSecret = "whsec_" + new string('y', 32),
                InboundEndpointId = "we_1",
            });
            using (var doc = JsonDocument.Parse(handler.Requests[1].Body))
            {
                var root = doc.RootElement;
                Assert.Equal("whsec_" + new string('y', 32), root.GetProperty("inbound_secret").GetString());
                Assert.Equal("we_1", root.GetProperty("inbound_endpoint_id").GetString());
                Assert.False(root.TryGetProperty("signing_secret", out _));
            }

            await client.Referrals.UpdateAsync(new UpdateReferralSettingsBody().Clear("inbound_secret").Clear("inbound_endpoint_id"));
            using (var doc = JsonDocument.Parse(handler.Requests[2].Body))
            {
                var root = doc.RootElement;
                Assert.Equal(JsonValueKind.Null, root.GetProperty("inbound_secret").ValueKind);
                Assert.Equal(JsonValueKind.Null, root.GetProperty("inbound_endpoint_id").ValueKind);
            }
        }

        [Fact]
        public async Task BrowserCaptureFields()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"referral_settings\",\"enabled\":true,\"provider\":\"dub\",\"signing_secret_set\":false,\"api_key_set\":true,"
                + "\"url_params\":[\"ref\"],\"browser_sources\":[\"rewardful\",\"dub\"],\"browser_sources_default\":false,\"consent_provider\":\"onetrust\",\"providers\":[]}");
            using var client = Client(handler);
            var view = await client.Referrals.GetAsync();
            Assert.Equal(new System.Collections.Generic.List<string> { "rewardful", "dub" }, view.BrowserSources);
            Assert.False(view.BrowserSourcesDefault);
            Assert.Equal("onetrust", view.ConsentProvider);

            await client.Referrals.UpdateAsync(new UpdateReferralSettingsBody
            {
                BrowserSources = new System.Collections.Generic.List<string> { "rewardful", "dub" },
                ConsentProvider = "app",
            });
            using (var doc = JsonDocument.Parse(handler.Requests[1].Body))
            {
                var root = doc.RootElement;
                Assert.Equal(2, root.GetProperty("browser_sources").GetArrayLength());
                Assert.Equal("dub", root.GetProperty("browser_sources")[1].GetString());
                Assert.Equal("app", root.GetProperty("consent_provider").GetString());
                Assert.False(root.TryGetProperty("inbound_secret", out _));
            }

            await client.Referrals.UpdateAsync(new UpdateReferralSettingsBody().Clear("browser_sources"));
            using (var doc = JsonDocument.Parse(handler.Requests[2].Body))
            {
                Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("browser_sources").ValueKind);
            }
        }

            [Fact]
        public async Task V2_SettingsFields_PingTest_AndCallPaging()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"referral_settings\",\"enabled\":true,\"provider\":\"referalli\",\"signing_secret_set\":true,\"api_key_set\":false,"
                + "\"url_params\":[\"ref\"],\"tracking_token\":\"tt_public_1\",\"code_lookup_url\":null,\"signup_code_field\":true,\"forward_lifecycle\":true,"
                + "\"mirror_to_public_metadata\":false,\"include_referral_claims\":true,"
                + "\"connection\":{\"last_test\":null,\"last_ping\":{\"ok\":true,\"org_name\":\"Acme\",\"clock_skew_ms\":90000,\"clock_skew_warning\":true},"
                + "\"alert\":{\"code\":\"signature_rejected\",\"message\":\"401\",\"at\":9,\"call_id\":\"rfc_9\"},\"failed_calls_7d\":1,\"retrying_calls\":0,\"last_success_at\":null},"
                + "\"providers\":[{\"id\":\"referalli\",\"capabilities\":{\"signup\":true,\"erase\":true,\"export\":true,\"partner\":true,\"auto_enroll\":true,\"signal\":true,\"alias\":true,\"user\":true,\"ping\":true},"
                + "\"needs\":[\"endpoint_url\",\"connection_id\",\"signing_secret\"]}]}");
            using var client = Client(handler);
            var view = await client.Referrals.GetAsync();
            Assert.Equal("tt_public_1", view.TrackingToken);
            Assert.True(view.SignupCodeField && view.ForwardLifecycle && view.IncludeReferralClaims);
            Assert.Equal("signature_rejected", view.Connection!.Alert!.Code);
            Assert.Equal(90000L, view.Connection.LastPing!.ClockSkewMs);
            Assert.True(view.Providers[0].Capabilities.Ping && view.Providers[0].Capabilities.Alias && view.Providers[0].Capabilities.User);

            await client.Referrals.UpdateAsync(new UpdateReferralSettingsBody
            {
                Provider = "referalli",
                TrackingToken = "tt_public_1",
                ForwardLifecycle = false,
                MirrorToPublicMetadata = true,
                IncludeReferralClaims = true,
                SignupCodeField = true,
            }.Clear("code_lookup_url"));
            using (var doc = JsonDocument.Parse(handler.Requests[1].Body))
            {
                var root = doc.RootElement;
                Assert.Equal("tt_public_1", root.GetProperty("tracking_token").GetString());
                Assert.False(root.GetProperty("forward_lifecycle").GetBoolean());
                Assert.True(root.GetProperty("mirror_to_public_metadata").GetBoolean());
                Assert.True(root.GetProperty("include_referral_claims").GetBoolean());
                Assert.True(root.GetProperty("signup_code_field").GetBoolean());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("code_lookup_url").ValueKind);
            }

            var testHandler = MockHttpMessageHandler.Json("{\"ok\":true,\"http_status\":200,\"latency_ms\":31,\"method\":\"ping\",\"ping\":{\"ok\":true,\"org_name\":\"Acme\"},\"warnings\":[\"clock skew\"]}");
            using var testClient = Client(testHandler);
            var test = await testClient.Referrals.TestAsync();
            Assert.Equal("ping", test.Method);
            Assert.Equal("Acme", test.Ping!.OrgName);
            Assert.Equal("clock skew", test.Warnings![0]);

            var call = "{\"object\":\"referral_call\",\"id\":\"rfc_2\",\"kind\":\"alias\",\"status\":\"failed\",\"customer_ref\":\"cust_1\",\"attempts\":1,"
                + "\"error_code\":\"signature_rejected\",\"latency_ms\":44,\"request_id\":\"req_1\",\"response\":{\"ok\":false},"
                + "\"highlights\":{\"matched_via\":\"code\",\"partner_external_ref\":\"cust_9\",\"notes\":[\"n\"]},\"batch_id\":\"rbf_1\",\"created_at\":1,\"updated_at\":2}";
            var listHandler = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[" + call + "],\"has_more\":true}");
            using var listClient = Client(listHandler);
            var page = await listClient.Referrals.ListCallsAsync(new ListReferralCallsQuery { Limit = 1000, ErrorCode = "signature_rejected", StartingAfter = "rfc_0" });
            Assert.Equal("?limit=1000&error_code=signature_rejected&starting_after=rfc_0", listHandler.Requests[0].Query);
            Assert.Equal("signature_rejected", page.Data[0].ErrorCode);
            Assert.Equal(44, page.Data[0].LatencyMs);
            Assert.Equal("rbf_1", page.Data[0].BatchId);
            Assert.Equal("n", page.Data[0].Highlights!.Notes[0]);
        }

        [Fact]
        public async Task V2_RetryFailed_Simulate_Backfill_AndUserResult()
        {
            var retry = MockHttpMessageHandler.Json("{\"object\":\"referral_retry\",\"requeued\":4}");
            using (var client = Client(retry))
            {
                var res = await client.Referrals.RetryFailedAsync(errorCode: "signature_rejected", idempotencyKey: "k1");
                Assert.Equal("POST", retry.Requests[0].Method);
                Assert.Equal("/v1/instance/referrals/calls/retry_failed", retry.Requests[0].Path);
                Assert.Equal("{\"error_code\":\"signature_rejected\"}", retry.Requests[0].Body);
                Assert.Equal("k1", retry.Requests[0].IdempotencyKey);
                Assert.Equal(4, res.Requeued);
            }

            var sim = MockHttpMessageHandler.Json("{\"object\":\"referral_simulation\",\"ok\":true,\"call_id\":\"rfc_5\",\"customer_ref\":\"atlas_test_abc\",\"http_status\":200,\"latency_ms\":20,"
                + "\"result\":{\"referral\":{\"partner_id\":\"pn_1\",\"partner_external_ref\":\"cust_9\",\"matched_via\":\"code\",\"status\":\"active\"},\"partner\":null,\"notes\":[\"matched code\"]},"
                + "\"response\":{\"referral\":{}},\"error\":null}");
            using (var client = Client(sim))
            {
                var res = await client.Referrals.SimulateAsync("ACME10");
                Assert.Equal("/v1/instance/referrals/simulate", sim.Requests[0].Path);
                Assert.Equal("{\"code\":\"ACME10\"}", sim.Requests[0].Body);
                Assert.Equal("cust_9", res.Result!.Referral!.PartnerExternalRef);
                Assert.Equal("matched code", res.Result.Notes[0]);
                await client.Referrals.SimulateAsync();
                Assert.Equal("{}", sim.Requests[1].Body);
            }

            var job = "{\"object\":\"referral_backfill\",\"id\":\"rbf_1\",\"status\":\"running\",\"since\":1790000000000,\"code_metadata_path\":null,\"codes_given\":1,"
                + "\"users_scanned\":12,\"users_skipped\":2,\"calls_queued\":10,\"truncated\":false,\"next_since\":null,"
                + "\"calls\":{\"pending\":10,\"succeeded\":0,\"failed\":0,\"skipped\":0,\"held\":0},\"created_at\":1,\"finished_at\":null}";
            var bf = MockHttpMessageHandler.Json(job);
            using (var client = Client(bf))
            {
                var res = await client.Referrals.BackfillAsync(new ReferralBackfillBody
                {
                    Since = "2026-01-01",
                    Codes = new System.Collections.Generic.Dictionary<string, string> { ["cust_1"] = "ACME10" },
                }, idempotencyKey: "k2");
                Assert.Equal("/v1/instance/referrals/backfill", bf.Requests[0].Path);
                Assert.Equal("{\"since\":\"2026-01-01\",\"codes\":{\"cust_1\":\"ACME10\"}}", bf.Requests[0].Body);
                Assert.Equal("k2", bf.Requests[0].IdempotencyKey);
                Assert.Equal(10, res.Calls.Pending);
                Assert.Equal(1790000000000L, res.Since);
                await client.Referrals.GetBackfillAsync("rbf_1");
                Assert.Equal("GET", bf.Requests[1].Method);
                Assert.Equal("/v1/instance/referrals/backfill/rbf_1", bf.Requests[1].Path);
            }

            var ur = MockHttpMessageHandler.Json("{\"object\":\"user_referral\",\"user_id\":\"user_1\",\"status\":\"attributed\",\"customer_ref\":\"cust_1\",\"partner_id\":\"pn_1\","
                + "\"partner_external_ref\":\"cust_9\",\"referrer_user_id\":\"user_9\",\"matched_via\":\"code\",\"referral_status\":\"active\",\"notes\":[\"n\"],"
                + "\"referral_code\":\"ada-1\",\"referral_link\":null,\"context\":{\"code\":\"ACME10\"},\"risk_level\":\"low\",\"flagged\":false,\"hold_reason\":null,"
                + "\"signed_up_at\":1,\"updated_at\":2}");
            using (var client = Client(ur))
            {
                var res = await client.Referrals.GetForUserAsync("user_1");
                Assert.Equal("/v1/users/user_1/referral", ur.Requests[0].Path);
                Assert.Equal("attributed", res.Status);
                Assert.Equal("active", res.ReferralStatus);
                Assert.Equal("cust_1", res.CustomerRef);
                Assert.True(res.Context!.ContainsKey("code"));
            }
        }

        [Fact]
        public async Task V2_SetExternalId_UserFields_AndBeforeUserCreateHook()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"user\",\"id\":\"user_1\",\"external_id\":\"cust_1\",\"referral\":{\"status\":\"pending\",\"partner_id\":null,"
                + "\"partner_external_ref\":\"cust_9\",\"referrer_external_id\":\"cust_9\",\"referrer_user_id\":null,\"matched_via\":null,\"referral_status\":\"active\","
                + "\"notes\":[\"n\"],\"risk_level\":null,\"flagged\":false}}");
            using (var client = Client(handler))
            {
                var u = await client.Users.SetExternalIdAsync("user_1", "cust_1");
                Assert.Equal("PATCH", handler.Requests[0].Method);
                Assert.Equal("/v1/users/user_1", handler.Requests[0].Path);
                Assert.Equal("{\"external_id\":\"cust_1\"}", handler.Requests[0].Body);
                Assert.Equal("cust_1", u.ExternalId);
                Assert.Equal("pending", u.Referral!.Status);
                Assert.Equal("active", u.Referral.ReferralStatus);
                Assert.Equal("cust_9", u.Referral.PartnerExternalRef);
                Assert.Equal("n", u.Referral.Notes![0]);
                await client.Users.SetExternalIdAsync("user_1", null);
                Assert.Equal("{\"external_id\":null}", handler.Requests[1].Body);
                await client.Users.UpdateAsync("user_1", new UpdateUserBody { ExternalId = "cust_2" });
                Assert.Equal("{\"external_id\":\"cust_2\"}", handler.Requests[2].Body);
                await client.Users.UpdateAsync("user_1", new UpdateUserBody().ClearExternalId());
                Assert.Equal("{\"external_id\":null}", handler.Requests[3].Body);
            }

            var hook = MockHttpMessageHandler.Json("{\"object\":\"before_user_create_hook\",\"enabled\":true,\"url\":\"https://app.example.com/hooks/before-create\","
                + "\"signing_secret_set\":true,\"timeout_ms\":3000,\"last_call\":{\"at\":3,\"status\":\"ok\",\"error\":null},\"updated_at\":3}");
            using (var client = Client(hook))
            {
                var got = await client.UserHooks.GetBeforeUserCreateAsync();
                Assert.Equal("GET", hook.Requests[0].Method);
                Assert.Equal("/v1/instance/hooks/before_user_create", hook.Requests[0].Path);
                Assert.True(got.SigningSecretSet);
                Assert.Equal("ok", got.LastCall!.Status);

                await client.UserHooks.UpdateBeforeUserCreateAsync(new UpdateBeforeUserCreateHookBody
                {
                    Enabled = true,
                    SigningSecret = new string('s', 32),
                    TimeoutMs = 2000,
                }.Clear("url"), idempotencyKey: "k3");
                Assert.Equal("PATCH", hook.Requests[1].Method);
                using (var doc = JsonDocument.Parse(hook.Requests[1].Body))
                {
                    var root = doc.RootElement;
                    Assert.True(root.GetProperty("enabled").GetBoolean());
                    Assert.Equal(2000, root.GetProperty("timeout_ms").GetInt32());
                    Assert.Equal(JsonValueKind.Null, root.GetProperty("url").ValueKind);
                }
                Assert.Equal("k3", hook.Requests[1].IdempotencyKey);
            }

            var test = MockHttpMessageHandler.Json("{\"ok\":true,\"latency_ms\":12,\"answer\":{\"external_id\":\"cust_1\",\"public_metadata\":null,\"private_metadata_set\":true,\"unsafe_metadata\":null}}");
            using (var client = Client(test))
            {
                var res = await client.UserHooks.TestBeforeUserCreateAsync();
                Assert.Equal("POST", test.Requests[0].Method);
                Assert.Equal("/v1/instance/hooks/before_user_create/test", test.Requests[0].Path);
                Assert.Equal("cust_1", res.Answer!.ExternalId);
                Assert.True(res.Answer.PrivateMetadataSet);
            }
        }
    }
}
