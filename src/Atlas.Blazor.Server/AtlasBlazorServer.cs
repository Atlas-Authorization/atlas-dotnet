using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Atlas.AspNetCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atlas.Blazor.Server
{
    /// <summary>Settings for the Blazor Server integration.</summary>
    public sealed class AtlasBlazorServerOptions
    {
        /// <summary>
        /// How often a live circuit re-checks that the Atlas session behind it is still active.
        /// A revoked, expired or signed-out session ends the circuit's signed-in state within this
        /// interval. Default 5 minutes.
        /// </summary>
        public TimeSpan RevalidationInterval { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>Answers whether an Atlas session is still active.</summary>
    public interface IAtlasSessionValidator
    {
        /// <summary>True only when the session exists and is active; any doubt is false (fail closed).</summary>
        Task<bool> IsActiveAsync(string sessionId, CancellationToken cancellationToken);
    }

    /// <summary>
    /// The default validator: reads the session through the Backend API (<c>AtlasClient</c>) and
    /// accepts only the <c>active</c> status that has not passed its end time. An error talking to
    /// Atlas counts as not active.
    /// </summary>
    public sealed class AtlasClientSessionValidator : IAtlasSessionValidator
    {
        private readonly AtlasClient _client;
        private readonly Func<long> _now;

        public AtlasClientSessionValidator(AtlasClient client, Func<long>? now = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _now = now ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        public async Task<bool> IsActiveAsync(string sessionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sessionId)) return false;
            try
            {
                var session = await _client.Sessions.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
                return string.Equals(session.Status, "active", StringComparison.Ordinal)
                    && (session.ExpireAt == 0 || session.ExpireAt > _now());
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// The circuit's authentication state, revalidated on an interval. The state starts as the
    /// principal <c>Atlas.AspNetCore</c> authenticated on the HTTP request that opened the circuit;
    /// every <see cref="AtlasBlazorServerOptions.RevalidationInterval"/> the session id in that
    /// principal is checked again, and the circuit is signed out when it is no longer active.
    /// </summary>
    public sealed class AtlasRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
    {
        private readonly IAtlasSessionValidator _validator;
        private readonly AtlasBlazorServerOptions _options;

        public AtlasRevalidatingAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IAtlasSessionValidator validator,
            IOptions<AtlasBlazorServerOptions> options)
            : base(loggerFactory)
        {
            _validator = validator;
            _options = options.Value;
        }

        protected override TimeSpan RevalidationInterval => _options.RevalidationInterval;

        protected override Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
            => ValidateAsync(authenticationState.User, cancellationToken);

        internal async Task<bool> ValidateAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            // Anonymous stays anonymous; a signed-in user without a session id cannot be checked, so it is refused.
            if (user.Identity?.IsAuthenticated != true) return true;
            var sid = user.FindFirst(AtlasClaimTypes.SessionId)?.Value;
            if (string.IsNullOrEmpty(sid)) return false;
            return await _validator.IsActiveAsync(sid!, cancellationToken).ConfigureAwait(false);
        }
    }

    public static class AtlasBlazorServerExtensions
    {
        /// <summary>
        /// Register the revalidating state provider and cascading authentication state. Call after
        /// <c>AddAtlasAuthentication</c> / <c>AddAtlas</c>, and register an <see cref="AtlasClient"/>
        /// (with your secret key) as a singleton, or your own <see cref="IAtlasSessionValidator"/>.
        /// </summary>
        public static IServiceCollection AddAtlasBlazorServer(
            this IServiceCollection services, Action<AtlasBlazorServerOptions>? configure = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            var builder = services.AddOptions<AtlasBlazorServerOptions>();
            if (configure != null) builder.Configure(configure);

            if (!services.Any(d => d.ServiceType == typeof(IAtlasSessionValidator)))
            {
                services.AddSingleton<IAtlasSessionValidator>(sp =>
                    new AtlasClientSessionValidator(sp.GetRequiredService<AtlasClient>()));
            }
            services.AddCascadingAuthenticationState();
            services.AddScoped<AuthenticationStateProvider, AtlasRevalidatingAuthenticationStateProvider>();
            return services;
        }
    }
}
