using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Native;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Maui
{
    /// <summary>Runs the hosted sign-in in the system browser with <see cref="WebAuthenticator"/>.</summary>
    public sealed class MauiAuthBrowser : IAuthBrowser
    {
        private readonly IWebAuthenticator _authenticator;

        public MauiAuthBrowser(IWebAuthenticator? authenticator = null) => _authenticator = authenticator ?? WebAuthenticator.Default;

        public async Task<Uri> AuthenticateAsync(Uri authorizeUrl, Uri callbackUrl, CancellationToken cancellationToken = default)
        {
            // WebAuthenticator hands back the callback's query parameters, not the URL; rebuild the URL for the shared client code.
            var result = await _authenticator.AuthenticateAsync(new WebAuthenticatorOptions
            {
                Url = authorizeUrl,
                CallbackUrl = callbackUrl,
                PrefersEphemeralWebBrowserSession = false,
            }).ConfigureAwait(false);
            var builder = new UriBuilder(callbackUrl);
            var query = new System.Text.StringBuilder();
            foreach (var kv in result.Properties)
            {
                if (query.Length > 0) query.Append('&');
                query.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
            }
            builder.Query = query.ToString();
            return builder.Uri;
        }
    }

    /// <summary>Keeps the session in <see cref="SecureStorage"/> (Keychain on Apple platforms, encrypted preferences on Android).</summary>
    public sealed class SecureStorageSessionStore : ISessionStore
    {
        private const string Key = "atlas.native.session";
        private readonly ISecureStorage _storage;

        public SecureStorageSessionStore(ISecureStorage? storage = null) => _storage = storage ?? SecureStorage.Default;

        public async Task<NativeSession?> LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var json = await _storage.GetAsync(Key).ConfigureAwait(false);
                return json == null ? null : JsonSerializer.Deserialize<NativeSession>(json);
            }
            catch (Exception)
            {
                // Undecryptable (restored backup, changed keys) or corrupt: treat as signed out and clear it.
                _storage.Remove(Key);
                return null;
            }
        }

        public Task SaveAsync(NativeSession session, CancellationToken cancellationToken = default)
            => _storage.SetAsync(Key, JsonSerializer.Serialize(session));

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _storage.Remove(Key);
            return Task.CompletedTask;
        }
    }

    public static class AtlasMauiExtensions
    {
        /// <summary>
        /// Register <see cref="AtlasNativeClient"/> with the MAUI browser and secure storage:
        /// <code>builder.UseAtlas(o => { o.Issuer = "..."; o.PublishableKey = "pk_..."; o.ClientId = "..."; o.RedirectUri = "myapp://callback"; });</code>
        /// The app still needs the platform callback registration MAUI documents for WebAuthenticator
        /// (an Android <c>WebAuthenticatorCallbackActivity</c>, an iOS URL scheme).
        /// </summary>
        public static MauiAppBuilder UseAtlas(this MauiAppBuilder builder, Action<AtlasNativeOptions> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            var options = new AtlasNativeOptions();
            configure(options);
            builder.Services.AddSingleton<IAuthBrowser, MauiAuthBrowser>();
            builder.Services.AddSingleton<ISessionStore, SecureStorageSessionStore>();
            builder.Services.AddSingleton(sp => new AtlasNativeClient(options, sp.GetRequiredService<IAuthBrowser>(), sp.GetRequiredService<ISessionStore>()));
            return builder;
        }
    }
}
