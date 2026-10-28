# Atlas.Blazor.Server

Blazor Server (interactive server render mode) on Atlas. `Atlas.AspNetCore` authenticates the HTTP request that opens the circuit; this package keeps the circuit honest afterwards: every few minutes it checks that the Atlas session behind it is still active, and signs the circuit out when the session was revoked, expired or ended elsewhere.

```csharp
builder.Services.AddSingleton(new AtlasClient(new AtlasClientOptions { SecretKey = builder.Configuration["Atlas:SecretKey"]! }));
builder.Services.AddAtlasAuthentication(o =>
{
    o.Issuer = "https://auth.example.com";
    o.JwksUrl = "https://auth.example.com/.well-known/jwks.json";
    o.AuthorizedParties = new[] { "https://app.example.com" };
});
builder.Services.AddAtlasBlazorServer(o => o.RevalidationInterval = TimeSpan.FromMinutes(2));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
```

Use `<AuthorizeView>`, `[Authorize]` and the cascading `Task<AuthenticationState>` as usual. Claims are the ones `Atlas.AspNetCore` emits (`sub` as the name identifier, `sid`, `org_id`, `org_role`, one `org_permissions` claim per permission).

Fail closed: a session that is not `active`, cannot be found, or cannot be checked (Atlas unreachable) ends the signed-in state. Provide your own `IAtlasSessionValidator` to change how the check is made.
