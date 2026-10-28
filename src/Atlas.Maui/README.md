# Atlas.Maui

.NET MAUI (Android, iOS, Mac Catalyst) on Atlas. `WebAuthenticator` runs the hosted sign-in in the system browser, `SecureStorage` keeps the session (Keychain / encrypted preferences), and `Atlas.Native` does the OAuth, session exchange and refresh.

```csharp
// MauiProgram.cs
builder.UseAtlas(o =>
{
    o.Issuer = "https://auth.example.com";
    o.PublishableKey = "pk_live_...";
    o.ClientId = "your_first_party_client";
    o.RedirectUri = "myapp://callback";
});
```

```csharp
// anywhere in the app
await atlas.SignInAsync();
var http = new HttpClient(atlas.CreateHandler(new[] { "https://api.example.com" }));
```

Platform setup is the one MAUI documents for `WebAuthenticator`: on Android a `WebAuthenticatorCallbackActivity` subclass with an intent filter for your scheme (and the `queries` entry for custom tabs), on iOS and Mac Catalyst the URL scheme in `Info.plist`. On Android, exclude the secure-storage preferences file from Auto Backup (selective backup rules) so a restored backup cannot hold undecryptable values; an unreadable value is treated as signed out and cleared.

Windows is not supported: MAUI's `WebAuthenticator` does not work there.
