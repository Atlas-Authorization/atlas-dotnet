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

        /// <summary>
        /// The platform key (<c>psk_...</c>) used by <see cref="AtlasClient.Platform"/> to
        /// provision apps and register Sign in with Atlas apps. Optional; may be set
        /// without <see cref="SecretKey"/>. Sent as a Bearer token on platform calls only.
        /// </summary>
        public string? PlatformKey { get; set; }

        /// <summary>
        /// Strict request bodies. <c>true</c> sends <c>Atlas-Strict: true</c> on every
        /// write, so a body field the endpoint does not accept is a <c>422 UNKNOWN_FIELD</c>
        /// before anything changes; <c>false</c> sends <c>Atlas-Strict: false</c> to opt
        /// out of an instance that defaults to strict; <c>null</c> (default) sends no
        /// header. Override per call with <see cref="AtlasClient.WithStrict"/>.
        /// </summary>
        public bool? Strict { get; set; }

        /// <summary>
        /// Called when a successful write ignored body fields the endpoint does not
        /// accept (the <c>Atlas-Unknown-Fields</c> response header). Defaults to
        /// <see cref="System.Diagnostics.Trace.TraceWarning(string)"/>.
        /// </summary>
        public System.Action<UnknownFieldsWarning>? OnUnknownFields { get; set; }

        internal AtlasClientOptions Copy() => (AtlasClientOptions)MemberwiseClone();
    }
}
