using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Atlas;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>A connection reset before any response is retried once, for safe requests only.</summary>
    public class ConnectionResetTests
    {
        private sealed class FlakyHandler : HttpMessageHandler
        {
            private readonly int _failures;
            private readonly Func<Exception> _error;
            public int Calls;

            public FlakyHandler(int failures, Func<Exception>? error = null)
            {
                _failures = failures;
                _error = error ?? (() => new HttpRequestException("reset", new IOException("Connection reset by peer")));
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                if (++Calls <= _failures) throw _error();
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"user_1\"}", System.Text.Encoding.UTF8, "application/json"),
                });
            }
        }

        private static AtlasClient Client(HttpMessageHandler h) =>
            new AtlasClient(new AtlasClientOptions { SecretKey = "sk_test_123", ApiUrl = "https://api.test.example", HttpClient = new HttpClient(h) });

        [Fact]
        public async Task Get_IsRetriedOnce()
        {
            var h = new FlakyHandler(1);
            Assert.Equal("user_1", (await Client(h).Users.GetAsync("user_1")).Id);
            Assert.Equal(2, h.Calls);
        }

        [Fact]
        public async Task KeyedPost_IsRetriedOnce()
        {
            var h = new FlakyHandler(1);
            await Client(h).Users.CreateAsync(new Atlas.Resources.CreateUserBody(), "idem-1");
            Assert.Equal(2, h.Calls);
        }

        [Fact]
        public async Task PlainPost_IsNotRetried()
        {
            var h = new FlakyHandler(1);
            await Assert.ThrowsAsync<HttpRequestException>(() => Client(h).Users.CreateAsync(new Atlas.Resources.CreateUserBody()));
            Assert.Equal(1, h.Calls);
        }

        [Fact]
        public async Task Timeout_IsNotRetried_AndSecondResetIsRaised()
        {
            var slow = new FlakyHandler(1, () => new TaskCanceledException("timeout"));
            await Assert.ThrowsAsync<TaskCanceledException>(() => Client(slow).Users.GetAsync("user_1"));
            Assert.Equal(1, slow.Calls);

            var twice = new FlakyHandler(2);
            await Assert.ThrowsAsync<HttpRequestException>(() => Client(twice).Users.GetAsync("user_1"));
            Assert.Equal(2, twice.Calls);
        }
    }
}
