# Atlas.Blazor.WebAssembly

Blazor WebAssembly on Atlas. The browser keeps the Atlas session in the script-readable `__session` cookie (the Atlas frontend refreshes it); this package turns it into Blazor authentication state and sends it to your API.

```csharp
// Program.cs (WebAssembly host)
builder.Services.AddAtlasWebAssembly(o =>
{
    o.Issuer = "https://auth.example.com";
    o.AuthorizedParties.Add("https://app.example.com");
    o.AuthorizedUrls.Add("https://api.example.com");   // the token goes only here
});
builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddHttpMessageHandler<AtlasBearerHandler>();
```

```razor
<AuthorizeView>
    <Authorized>Signed in as @context.User.Identity?.Name</Authorized>
    <NotAuthorized><a href="https://auth.example.com/sign-in">Sign in</a></NotAuthorized>
</AuthorizeView>
```

The browser cannot be trusted to enforce anything, so the state here only drives UI: the token is decoded and checked for shape, issuer, `azp`, `exp` and `nbf` (5 seconds of skew) but its signature is not verified. Protect the API with `Atlas.AspNetCore` (`AddAtlasAuthentication` + `[Authorize]`), which verifies the signature against the cached key set. The state is re-read every 15 seconds (`PollInterval`) so a refreshed or ended session is picked up. A session cookie that is expired, malformed, `alg: none`, or for another issuer is shown as signed out.
