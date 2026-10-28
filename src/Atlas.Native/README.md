# Atlas.Native

Sign in and keep an Atlas session in a native .NET app. It is the engine under `Atlas.Maui`, and works on its own in WPF, WinForms or a console tool by supplying an `IAuthBrowser` and an `ISessionStore`.

- Hosted sign-in: authorization code with PKCE (S256) in the system browser. The `state` is checked, and the authorize and token endpoints must be on the issuer's origin.
- The OAuth access token is traded for an Atlas session (RFC 8693), so the app carries a short-lived session JWT instead of a cookie.
- `GetTokenAsync()` rotates the session lazily and single-flight, and stores the new refresh token every time. A dead refresh token signs the person out; a network error keeps the current token.
- `CreateHandler(...)` returns a message handler that adds the token only to the API URLs you list.

```csharp
var client = new AtlasNativeClient(new AtlasNativeOptions
{
    Issuer = "https://auth.example.com",
    PublishableKey = "pk_live_...",
    ClientId = "your_first_party_client",
    RedirectUri = "myapp://callback",
}, browser, store);

await client.SignInAsync();
var http = new HttpClient(client.CreateHandler(new[] { "https://api.example.com" }));
var orders = await http.GetStringAsync("https://api.example.com/orders"); // verified server side with Atlas.AspNetCore
```
