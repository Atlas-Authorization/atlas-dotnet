using System;
using System.Collections.Generic;
using System.Net.Http;
using Atlas.Verification;
using Microsoft.AspNetCore.Authentication;

namespace Atlas.AspNetCore
{
    /// <summary>
    /// Options for the Atlas authentication handler. These mirror
    /// <see cref="AtlasBackendOptions"/> because the whole handler is a thin
    /// ASP.NET Core shell around the SDK verifier — configuring the handler IS
    /// configuring the verifier, and splitting the two vocabularies would only
    /// invite them to drift apart.
    /// </summary>
    public sealed class AtlasAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>The instance's JWKS URL. Required.</summary>
        public string JwksUrl { get; set; } = "";

        /// <summary>Expected <c>iss</c>. Required — an unchecked issuer accepts any Atlas instance.</summary>
        public string Issuer { get; set; } = "";

        /// <summary>
        /// Optional <c>azp</c> allowlist. When set, a token minted for a different
        /// origin is refused — the defence against replaying one app's token at another.
        /// </summary>
        public IReadOnlyList<string>? AuthorizedParties { get; set; }

        /// <summary>
        /// The secret key. Only needed if a caller opts a scheme into online
        /// (revocation-checking) verification; local verification needs none.
        /// </summary>
        public string? SecretKey { get; set; }

        /// <summary>The Backend-API base URL, needed only for online verification.</summary>
        public string? BapiBaseUrl { get; set; }

        /// <summary>
        /// The cookie the handler falls back to when there is no
        /// <c>Authorization: Bearer</c> header. Defaults to Atlas's <c>__session</c>.
        /// </summary>
        public string CookieName { get; set; } = "__session";

        /// <summary>
        /// Verify against Atlas on every request (fails closed on an outage).
        /// Off by default: local JWKS verification is the hot path, and a per-request
        /// round trip is a deliberate choice for the few endpoints that cannot
        /// tolerate the ~60s revocation window, not a default to pay everywhere.
        /// </summary>
        public bool UseOnlineVerification { get; set; }

        /// <summary>An optional shared <see cref="HttpClient"/> for JWKS fetches (and online verify).</summary>
        public HttpClient? HttpClient { get; set; }

        /// <summary>An optional clock override (epoch milliseconds) for tests.</summary>
        public Func<long>? Now { get; set; }

        // The verifier is built once from these options and cached. IOptionsMonitor
        // hands out one options instance per scheme, so this lazily becomes a single
        // verifier per scheme — the handler is transient but the JWKS cache it leans
        // on must not be. The lock guards the first concurrent request after startup.
        private readonly object _gate = new object();
        private AtlasBackend? _backend;

        /// <summary>
        /// The verifier these options describe, created on first use and reused
        /// thereafter. Exposed to the handler and to DI so an app can inject the
        /// very same <see cref="AtlasBackend"/> the handler uses.
        /// </summary>
        public AtlasBackend ResolveBackend()
        {
            if (_backend != null) return _backend;
            lock (_gate)
            {
                return _backend ??= new AtlasBackend(new AtlasBackendOptions
                {
                    JwksUrl = JwksUrl,
                    Issuer = Issuer,
                    AuthorizedParties = AuthorizedParties,
                    SecretKey = SecretKey,
                    BapiBaseUrl = BapiBaseUrl,
                    HttpClient = HttpClient,
                    Now = Now,
                });
            }
        }

        /// <summary>
        /// Fail fast at startup on a misconfiguration rather than turning every
        /// request into an opaque 500. The verifier itself enforces the same two
        /// invariants; validating here means the error surfaces when the app boots.
        /// </summary>
        public override void Validate()
        {
            base.Validate();
            if (string.IsNullOrEmpty(JwksUrl))
                throw new InvalidOperationException("AtlasAuthenticationOptions.JwksUrl is required.");
            if (string.IsNullOrEmpty(Issuer))
                throw new InvalidOperationException(
                    "AtlasAuthenticationOptions.Issuer is required — an unchecked issuer accepts any Atlas instance.");
        }
    }
}
