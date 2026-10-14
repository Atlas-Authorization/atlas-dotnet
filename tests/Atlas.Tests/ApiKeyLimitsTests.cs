using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>API key spend caps and usage metering, session keep-alive, and the notify flag.</summary>
    public class ApiKeyLimitsTests
    {
        private const string State = "{\"unit\":\"requests\",\"period\":\"day\",\"amount\":1000,\"used\":10,"
            + "\"remaining\":990,\"window_start\":1760400000000,\"resets_at\":1760486400000}";
        private const string Limits = "[{\"unit\":\"requests\",\"amount\":1000,\"period\":\"day\"},"
            + "{\"unit\":\"usd_cents\",\"amount\":500,\"period\":\"request\"}]";

        private static AtlasClient Client(MockHttpMessageHandler handler) =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

        private static JsonElement Sent(RecordedRequest r) => JsonDocument.Parse(r.Body).RootElement;

        private static void AssertJson(string expected, string actual) =>
            Assert.Equal(JsonSerializer.Serialize(JsonDocument.Parse(expected).RootElement),
                JsonSerializer.Serialize(JsonDocument.Parse(actual).RootElement));

        [Fact]
        public async Task Create_SendsLimitsAndDecodesUsage()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"api_key\",\"id\":\"ak_1\",\"secret\":\"ak_s\",\"created_at\":1,"
                + "\"limits\":" + Limits + ",\"usage\":[" + State + "]}", HttpStatusCode.Created);
            using var c = Client(h);

            var key = await c.ApiKeys.CreateAsync(new CreateApiKeyBody
            {
                SubjectType = "user",
                SubjectId = "user_1",
                Limits = new List<ApiKeyLimit>
                {
                    new ApiKeyLimit("requests", 1000, ApiKeyLimitPeriods.Day),
                    new ApiKeyLimit("usd_cents", 500, ApiKeyLimitPeriods.Request),
                },
            });

            var r = h.Requests[0];
            Assert.Equal("POST", r.Method);
            Assert.Equal("/v1/api_keys", r.Path);
            AssertJson(Limits, Sent(r).GetProperty("limits").GetRawText());
            Assert.Equal(2, key.Limits!.Count);
            Assert.Equal(990, key.Usage[0].Remaining);
            Assert.Equal(1760486400000L, key.Usage[0].ResetsAt);
        }

        [Fact]
        public async Task Get_KeyWithoutCapsHasNullLimits()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"api_key\",\"id\":\"ak_1\",\"created_at\":1,\"limits\":null,\"usage\":[]}");
            using var c = Client(h);
            var key = await c.ApiKeys.GetAsync("ak_1");
            Assert.Null(key.Limits);
            Assert.Empty(key.Usage);
        }

        [Fact]
        public async Task Update_ReplacesOrClearsLimits()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"api_key\",\"id\":\"ak_1\",\"created_at\":1}");
            using var c = Client(h);

            await c.ApiKeys.UpdateAsync("ak_1", new UpdateApiKeyBody { Limits = new List<ApiKeyLimit> { new ApiKeyLimit("tokens", 5, "total") } });
            Assert.Equal("PATCH", h.Requests[0].Method);
            Assert.Equal("/v1/api_keys/ak_1", h.Requests[0].Path);
            AssertJson("{\"limits\":[{\"unit\":\"tokens\",\"amount\":5,\"period\":\"total\"}]}", h.Requests[0].Body);

            await c.ApiKeys.UpdateAsync("ak_1", new UpdateApiKeyBody().ClearLimits());
            AssertJson("{\"limits\":null}", h.Requests[1].Body);
        }

        [Fact]
        public async Task RecordUsage_SendsAmountUnitAndBodyIdempotencyKey()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"api_key_usage_debit\",\"key_id\":\"ak_1\",\"unit\":\"requests\",\"amount\":1,"
                + "\"allowed\":true,\"exceeded\":[],\"usage\":[" + State + "],"
                + "\"counters\":[{\"unit\":\"requests\",\"period\":\"day\",\"used\":11,\"window_start\":1,\"resets_at\":2},"
                + "{\"unit\":\"requests\",\"period\":\"total\",\"used\":11,\"window_start\":0,\"resets_at\":null}],"
                + "\"idempotent_replay\":false}");
            using var c = Client(h);

            var debit = await c.ApiKeys.RecordUsageAsync("ak_1", 1, "requests", "req_42");

            var r = h.Requests[0];
            Assert.Equal("POST", r.Method);
            Assert.Equal("/v1/api_keys/ak_1/usage", r.Path);
            AssertJson("{\"amount\":1,\"unit\":\"requests\",\"idempotency_key\":\"req_42\"}", r.Body);
            Assert.Null(r.IdempotencyKey);
            Assert.True(debit.Allowed);
            Assert.Null(debit.Code);
            Assert.Equal("ak_1", debit.KeyId);
            Assert.Equal(11, debit.Counters[0].Used);
            Assert.Null(debit.Counters[1].ResetsAt);
            Assert.False(debit.IdempotentReplay);
        }

        [Fact]
        public async Task RecordUsage_RefusalIsAValueNotAnException()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"api_key_usage_debit\",\"key_id\":\"ak_1\",\"unit\":\"usd_cents\",\"amount\":900,"
                + "\"allowed\":false,\"code\":\"LIMIT_EXCEEDED\",\"exceeded\":[" + State + "],\"usage\":[" + State + "],"
                + "\"counters\":[],\"idempotent_replay\":true}");
            using var c = Client(h);

            var debit = await c.ApiKeys.RecordUsageAsync("ak_1", new RecordApiKeyUsageBody { Amount = 900, Unit = "usd_cents" });

            AssertJson("{\"amount\":900,\"unit\":\"usd_cents\"}", h.Requests[0].Body);
            Assert.False(debit.Allowed);
            Assert.Equal(ErrorCodes.LimitExceeded, debit.Code);
            Assert.Single(debit.Exceeded);
            Assert.True(debit.IdempotentReplay);
        }

        [Fact]
        public async Task RecordUsage_IdempotencyConflictThrows()
        {
            var h = MockHttpMessageHandler.Json("{\"errors\":[{\"code\":\"IDEMPOTENCY_CONFLICT\",\"message\":\"reused\"}]}", HttpStatusCode.Conflict);
            using var c = Client(h);
            var ex = await Assert.ThrowsAsync<AtlasConflictException>(() => c.ApiKeys.RecordUsageAsync("ak_1", 2, "requests", "req_42"));
            Assert.True(ex.HasCode(ErrorCodes.IdempotencyConflict));
        }

        [Fact]
        public async Task GetUsage()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"api_key_usage\",\"key_id\":\"ak_1\",\"limits\":" + Limits
                + ",\"usage\":[" + State + "],\"counters\":[]}");
            using var c = Client(h);

            var usage = await c.ApiKeys.GetUsageAsync("ak_1");

            Assert.Equal("GET", h.Requests[0].Method);
            Assert.Equal("/v1/api_keys/ak_1/usage", h.Requests[0].Path);
            Assert.Equal("api_key_usage", usage.ObjectType);
            Assert.Equal("usd_cents", usage.Limits![1].Unit);
            Assert.Equal(500, usage.Limits[1].Amount);
            Assert.Equal("request", usage.Limits[1].Period);
            Assert.Equal(10, usage.Usage[0].Used);
        }

        [Fact]
        public async Task Verify_DecodesNameScopesConstraintsLimitsAndUsage()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"api_key_verification\",\"valid\":true,\"id\":\"ak_1\",\"name\":\"CI\","
                + "\"scopes\":[\"read\"],\"constraints\":{\"ip\":\"10.0.0.0/8\"},\"limits\":" + Limits
                + ",\"usage\":[" + State + "]}");
            using var c = Client(h);

            var v = await c.ApiKeys.VerifyAsync("ak_secret");

            Assert.True(v.Valid);
            Assert.Equal("CI", v.Name);
            Assert.Equal(new[] { "read" }, v.Scopes);
            Assert.Equal("10.0.0.0/8", v.Constraints!["ip"]!.ToString());
            Assert.Equal(2, v.Limits!.Count);
            Assert.Equal(990, v.Usage![0].Remaining);
        }

        [Fact]
        public async Task SessionTouch()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"session\",\"id\":\"sess_1\",\"user_id\":\"user_1\",\"status\":\"active\","
                + "\"last_active_at\":5,\"expire_at\":9,\"abandon_at\":7,\"idle_expires_at\":7,\"created_at\":1,\"extended\":true}");
            using var c = Client(h);

            var s = await c.Sessions.TouchAsync("sess_1");

            Assert.Equal("POST", h.Requests[0].Method);
            Assert.Equal("/v1/sessions/sess_1/touch", h.Requests[0].Path);
            Assert.Equal("", h.Requests[0].Body);
            Assert.True(s.Extended);
            Assert.Equal(7, s.IdleExpiresAt);
            Assert.Equal("user_1", s.UserId);
        }

        [Fact]
        public async Task SessionTouch_OnAnEndedSessionThrows()
        {
            var h = MockHttpMessageHandler.Json("{\"errors\":[{\"code\":\"SESSION_IDLE_EXPIRED\",\"message\":\"ended\"}]}", HttpStatusCode.Conflict);
            using var c = Client(h);
            var ex = await Assert.ThrowsAsync<AtlasConflictException>(() => c.Sessions.TouchAsync("sess_1"));
            Assert.True(ex.HasCode(ErrorCodes.SessionIdleExpired));
        }

        [Fact]
        public async Task SessionReads_CarryIdleExpiresAt()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"session\",\"id\":\"sess_1\",\"abandon_at\":7,\"idle_expires_at\":7,\"created_at\":1}");
            using var c = Client(h);
            var s = await c.Sessions.GetAsync("sess_1");
            Assert.Equal(7L, s.IdleExpiresAt);
        }

        [Fact]
        public async Task UserCreateAndImport_SendNotify()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"user\",\"id\":\"user_1\"}");
            using var c = Client(h);

            await c.Users.CreateAsync(new CreateUserBody { EmailAddress = "ada@example.com", Notify = false });
            Assert.False(Sent(h.Requests[0]).GetProperty("notify").GetBoolean());

            await c.ImportExport.ImportUsersAsync(new ImportUsersBody { Notify = false });
            Assert.Equal("/v1/user_imports", h.Requests[1].Path);
            Assert.False(Sent(h.Requests[1]).GetProperty("notify").GetBoolean());
        }

        [Fact]
        public void ApiKeyWebhookData_DecodesFromAPayload()
        {
            var payload = JsonSerializer.Deserialize<WebhookPayload>("{\"id\":\"evt_1\",\"type\":\"api_key.limit_reached\",\"timestamp\":1,"
                + "\"data\":{\"object\":\"api_key\",\"id\":\"ak_1\",\"prefix\":\"ak_x\",\"subject_type\":\"user\",\"subject_id\":\"u\","
                + "\"name\":\"CI\",\"claims\":{},\"scopes\":null,\"constraints\":{},\"limits\":" + Limits
                + ",\"max_token_lifetime_seconds\":3600,\"expires_at\":null,\"revoked_at\":null,\"created_at\":1,\"updated_at\":2,"
                + "\"limit\":" + State + "}}", AtlasJson.Options)!;

            var data = payload.Data!.Value.Deserialize<ApiKeyWebhookData>(AtlasJson.Options)!;

            Assert.Equal("CI", data.Name);
            Assert.Equal(3600L, data.MaxTokenLifetimeSeconds);
            Assert.Equal(2L, data.UpdatedAt);
            Assert.Equal(2, data.Limits!.Count);
            Assert.Equal("requests", data.Limit!.Unit);
            Assert.Null(data.Scopes);
        }
    }
}
