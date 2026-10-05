using System;
using Atlas.Verification;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Atlas.AspNetCore
{
    /// <summary>
    /// Registration helpers for the Atlas authentication integration. The two
    /// entry points mirror the shape of every other provider: an
    /// <see cref="AuthenticationBuilder"/> extension for apps already composing
    /// <c>AddAuthentication()</c>, and a one-liner on
    /// <see cref="IServiceCollection"/> for the common case.
    /// </summary>
    public static class AtlasAuthenticationExtensions
    {
        /// <summary>
        /// Register the Atlas authentication handler under the default scheme
        /// (<see cref="AtlasDefaults.AuthenticationScheme"/>).
        /// </summary>
        public static AuthenticationBuilder AddAtlas(
            this AuthenticationBuilder builder,
            Action<AtlasAuthenticationOptions> configureOptions)
            => builder.AddAtlas(AtlasDefaults.AuthenticationScheme, configureOptions);

        /// <summary>
        /// Register the Atlas authentication handler under a named scheme. Also
        /// exposes the scheme's verifier as a singleton <see cref="AtlasBackend"/>
        /// in DI, so application code can inject the very same verifier the handler
        /// uses (e.g. to run an online check on a sensitive action) instead of
        /// standing up a second one with its own JWKS cache.
        /// </summary>
        public static AuthenticationBuilder AddAtlas(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<AtlasAuthenticationOptions> configureOptions)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            // TryAdd: multiple schemes resolve to the same singleton verifier by
            // reading their own options, and registering the authz handler twice
            // would make every policy evaluate twice.
            builder.Services.TryAddSingleton<AtlasBackend>(sp =>
                sp.GetRequiredService<IOptionsMonitor<AtlasAuthenticationOptions>>()
                  .Get(authenticationScheme)
                  .ResolveBackend());

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IAuthorizationHandler, AtlasPermissionAuthorizationHandler>());

            return builder.AddScheme<AtlasAuthenticationOptions, AtlasAuthenticationHandler>(
                authenticationScheme, configureOptions);
        }

        /// <summary>
        /// The convenience one-liner: call <c>AddAuthentication</c> with Atlas as the
        /// default scheme and register the handler, in a single step. Equivalent to
        /// <c>services.AddAuthentication(AtlasDefaults.AuthenticationScheme).AddAtlas(...)</c>.
        /// </summary>
        public static AuthenticationBuilder AddAtlasAuthentication(
            this IServiceCollection services,
            Action<AtlasAuthenticationOptions> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return services
                .AddAuthentication(AtlasDefaults.AuthenticationScheme)
                .AddAtlas(configureOptions);
        }
    }
}
