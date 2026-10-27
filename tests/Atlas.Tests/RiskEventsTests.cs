using System.Net.Http;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>The risk-event timeline, event feedback, fraud lookup and the extra adaptive-MFA config fields.</summary>
    public class RiskEventsTests
    {
        private const string Event = "{\"object\":\"risk_event\",\"id\":\"re_1\",\"user_id\":\"usr_1\",\"level\":\"high\",\"score\":80,"
            + "\"signals\":[\"newDevice\"],\"action\":\"mfa\",\"mode\":\"audit\",\"ip\":\"1.2.3.4\",\"feedback\":null,\"feedback_at\":null,"
            + "\"created_at\":\"2026-01-01T00:00:00Z\"";

        private static AtlasClient Client(MockHttpMessageHandler h) => new AtlasClient(new AtlasClientOptions
        {
            SecretKey = "sk_test_123",
            ApiUrl = "https://api.test.example",
            HttpClient = new HttpClient(h),
        });

        [Fact]
        public async Task TimelineMetricsAndFeedback()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[" + Event + "}]}");
            using var c = Client(h);
            var page = await c.RiskBasedMfa.ListEventsAsync(new ListRiskEventsParams { UserId = "usr_1", Level = "high", Limit = 10 });
            var req = h.Requests[0];
            Assert.Equal("GET", req.Method);
            Assert.Equal("/v1/risk_events", req.Path);
            Assert.Equal("?user_id=usr_1&level=high&limit=10", req.Query);
            Assert.Equal("usr_1", page.Data[0].UserId);
            Assert.Equal("newDevice", page.Data[0].Signals[0]);
            Assert.Null(page.Data[0].Feedback);

            h = MockHttpMessageHandler.Json("{\"object\":\"risk_event_metrics\",\"days\":14,\"total\":3,\"buckets\":[{\"level\":\"high\",\"action\":\"mfa\",\"mode\":\"audit\",\"count\":3}]}");
            using var c2 = Client(h);
            var m = await c2.RiskBasedMfa.EventMetricsAsync(14);
            Assert.Equal("/v1/risk_events/metrics", h.Requests[0].Path);
            Assert.Equal("?days=14", h.Requests[0].Query);
            Assert.Equal(3, m.Buckets[0].Count);

            h = MockHttpMessageHandler.Json(Event.Replace("\"feedback\":null", "\"feedback\":\"fraud\"") + "}");
            using var c3 = Client(h);
            var f = await c3.RiskBasedMfa.EventFeedbackAsync("re_1", new RiskEventFeedbackBody { Verdict = "fraud" });
            Assert.Equal("POST", h.Requests[0].Method);
            Assert.Equal("/v1/risk_events/re_1/feedback", h.Requests[0].Path);
            Assert.Contains("\"verdict\":\"fraud\"", h.Requests[0].Body);
            Assert.Equal("fraud", f.Feedback);
        }

        [Fact]
        public async Task FraudLookupAndConfigFields()
        {
            var h = MockHttpMessageHandler.Json("{\"object\":\"fraud_lookup\",\"verdict\":\"challenge\",\"reasons\":[\"r\"],\"matched_rules\":[\"fr_1\"],\"device_id\":\"d1\",\"signals\":{\"country\":\"US\"}}");
            using var c = Client(h);
            var l = await c.RiskBasedMfa.FraudLookupAsync(new FraudLookupBody { Ip = "1.2.3.4", UserAgent = "ua", DeviceId = "d1" });
            Assert.Equal("/v1/fraud/lookup", h.Requests[0].Path);
            Assert.Contains("\"ip\":\"1.2.3.4\"", h.Requests[0].Body);
            Assert.Contains("\"user_agent\":\"ua\"", h.Requests[0].Body);
            Assert.Contains("\"device_id\":\"d1\"", h.Requests[0].Body);
            Assert.Equal("challenge", l.Verdict);
            Assert.Equal("fr_1", l.MatchedRules[0]);

            h = MockHttpMessageHandler.Json("{\"object\":\"risk_based_mfa\",\"enabled\":true,\"step_up_threshold\":\"medium\",\"on_high_risk_no_factor\":\"block\",\"weights\":{},\"signals\":{},\"mode\":\"audit\"}");
            using var c2 = Client(h);
            var cfg = await c2.RiskBasedMfa.UpdateAsync(new UpdateRiskBasedMfaBody { Mode = "audit" });
            Assert.Equal("PATCH", h.Requests[0].Method);
            Assert.Contains("\"mode\":\"audit\"", h.Requests[0].Body);
            Assert.Equal("audit", cfg.Mode);
        }
    }
}
