# Atlas Backend SDK for .NET

The official .NET / C# backend SDK for the [Atlas](https://atlasauth.net) authentication
platform. Two surfaces, one package:

- **`AtlasClient`** — the typed, secret-key **management client** (the peer of the
  TypeScript SDK's `createAtlasClient` and the Python SDK's `AtlasClient`). Users,
  sessions, organizations, roles, SSO, SCIM, FGA, webhooks, billing, and the rest of
  the Backend API — 45 resource namespaces in all.
- **`AtlasBackend`** — local **session-JWT verification** against your instance's JWKS.
  Fast, offline, and correct within the token lifetime; it never calls Atlas on the hot
  path.

Targets `netstandard2.1` and `net8.0`.

## Install

```bash
dotnet add package Atlas.Sdk
```

## The management client

```csharp
using Atlas;
using Atlas.Resources;

using var atlas = new AtlasClient("sk_live_...");

// Fetch a user.
User user = await atlas.Users.GetAsync("user_123");

// Create one (with an idempotency key).
User created = await atlas.Users.CreateAsync(
    new CreateUserBody { EmailAddress = "ada@example.com", Password = "..." },
    idempotencyKey: "signup-8f3c");

// Page every organization.
await foreach (var org in AtlasPagination.PaginateAsync(
    (cursor, ct) => atlas.Organizations.ListAsync(new CursorParams { StartingAfter = cursor }, ct)))
{
    Console.WriteLine(org.Slug);
}
```

### Configuration

```csharp
using var atlas = new AtlasClient(new AtlasClientOptions
{
    SecretKey = Environment.GetEnvironmentVariable("ATLAS_SECRET_KEY")!,
    ApiUrl    = "https://api.atlas.dev",   // per-instance BAPI origin
    HttpClient = myPooledHttpClient,        // optional; you own its lifetime if you pass it
});
```

The secret key is sent only as `Authorization: Bearer <key>`; it is never logged and
never placed in a URL.

### Errors

Every non-2xx response throws an `AtlasException` subclass carrying the HTTP `Status`
and the parsed `{ errors: [...] }` envelope. Branch on the stable `Code`:

```csharp
try
{
    await atlas.Users.GetAsync("user_missing");
}
catch (AtlasNotFoundException ex)
{
    Console.WriteLine(ex.Code); // "NOT_FOUND"
}
catch (AtlasRateLimitException ex)
{
    await Task.Delay(TimeSpan.FromSeconds(ex.RetryAfter ?? 1));
}
```

Subclasses: `AtlasBadRequestException` (400/422), `AtlasAuthenticationException` (401),
`AtlasAuthorizationException` (403), `AtlasNotFoundException` (404),
`AtlasConflictException` (409), `AtlasRateLimitException` (429),
`AtlasServerException` (5xx).

## Verifying session JWTs

```csharp
using Atlas.Verification;

var backend = new AtlasBackend(new AtlasBackendOptions
{
    JwksUrl = "https://api.atlas.dev/.well-known/jwks.json",
    Issuer  = "https://your-instance.atlas.dev",
    // Optional azp allowlist — refuse a token minted for a different origin.
    AuthorizedParties = new[] { "https://app.example.com" },
});

VerifyResult result = await backend.VerifyAsync(token);
if (result.Ok)
{
    SessionClaims claims = result.Claims!;
    // Ergonomic authorization, bound to the verified claims:
    if (result.Session!.Has(new ProtectCondition { Permission = "posts:write" }))
    {
        // ...
    }
    // Or assert (throws ForbiddenError, which you map to 401/403):
    result.Session.Protect(new ProtectCondition { Role = "admin" });
}
```

`AtlasBackend` verifies signature (RS256, pinned), `iss`, and `exp`/`nbf` with a
5-second clock-skew tolerance, applies the §13.1 token-confusion guard (an OP
access-token or an id_token carrying `aud` is rejected), and — when configured —
checks `azp` against your allowlist. JWKS is cached in-process with a kid-miss refetch
capped at once per minute, so key rotation needs no redeploy and a flood of random
`kid`s cannot amplify traffic at your JWKS endpoint.

Verify straight from an incoming request's headers:

```csharp
var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
var result = await backend.AuthenticateRequestAsync(headers);
```

For the rare operation where a 60-second revocation window is unacceptable (deleting an
account, moving money), `VerifyOnlineAsync` additionally asks Atlas whether the session
is still live — it costs a round trip and **fails closed** on an outage. Set
`SecretKey` and `BapiBaseUrl` in the options to use it.

## ASP.NET Core registration

Register both surfaces as singletons — they are thread-safe and hold pooled
`HttpClient`s, so you want exactly one of each per process. Use `IHttpClientFactory`
so the sockets are managed for you:

```csharp
using Atlas;
using Atlas.Verification;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

// The management client.
builder.Services.AddSingleton<AtlasClient>(sp => new AtlasClient(new AtlasClientOptions
{
    SecretKey  = builder.Configuration["Atlas:SecretKey"]!,
    ApiUrl     = builder.Configuration["Atlas:ApiUrl"] ?? AtlasClientOptions.DefaultApiUrl,
    HttpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("atlas"),
}));

// The verifier.
builder.Services.AddSingleton<AtlasBackend>(sp => new AtlasBackend(new AtlasBackendOptions
{
    JwksUrl           = builder.Configuration["Atlas:JwksUrl"]!,
    Issuer            = builder.Configuration["Atlas:Issuer"]!,
    AuthorizedParties = builder.Configuration.GetSection("Atlas:AuthorizedParties").Get<string[]>(),
    HttpClient        = sp.GetRequiredService<IHttpClientFactory>().CreateClient("atlas-jwks"),
}));

var app = builder.Build();
```

Then inject `AtlasClient` / `AtlasBackend` into any controller, minimal-API handler, or
middleware. A minimal auth middleware:

```csharp
app.Use(async (context, next) =>
{
    var backend = context.RequestServices.GetRequiredService<AtlasBackend>();
    var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
    var result = await backend.AuthenticateRequestAsync(headers);
    if (result.Ok)
    {
        context.Items["AtlasClaims"] = result.Claims;
    }
    await next();
});
```

## License

MIT — see [LICENSE](./LICENSE).
