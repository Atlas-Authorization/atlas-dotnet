using System.Net.Http;

namespace Atlas
{
    /// <summary>Configuration for an <see cref="AtlasClient"/>.</summary>
    public sealed class AtlasClientOptions
    {
        /// <summary>The default BAPI origin, overridable per instance via <see cref="ApiUrl"/>.</summary>
        public const string DefaultApiUrl = "https://api.atlasauth.net";

        /// <summary>
        /// The instance secret key (<c>sk_...</c>). Sent as
        /// <c>Authorization: Bearer &lt;key&gt;</c> on every request; never logged,
        /// never placed in a URL.
        /// </summary>
        public string SecretKey { get; set; } = "";

        /// <summary>
        /// Base URL of the instance's Backend API, e.g. <c>https://api.atlasauth.net</c>.
        /// The <c>/v1/...</c> path is appended by each method. Trailing slashes are
        /// tolerated.
        /// </summary>
        public string ApiUrl { get; set; } = DefaultApiUrl;

        /// <summary>
        /// An optional pre-configured <see cref="HttpClient"/> (for connection
        /// pooling, proxies, retries, or a mocked handler in tests). One is created
        /// if omitted. When you pass your own, you own its lifetime.
        /// </summary>
        public HttpClient? HttpClient { get; set; }
    }
}
