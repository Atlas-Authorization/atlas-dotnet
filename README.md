# Atlas Backend SDK for .NET

The official .NET / C# backend SDK for the [Atlas](https://atlasauth.net) authentication
platform. Two surfaces, one package:

- **`AtlasClient`** — the typed, secret-key **management client** (the peer of the
  TypeScript SDK's `createAtlasClient` and the Python SDK's `AtlasClient`). Users,
  sessions, organizations, roles, SSO, SCIM, FGA, webhooks, billing, and the rest of
  the Backend API, plus the platform API — every secret-key and platform-key operation
  in the public API reference.
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
    ApiUrl    = "https://api.atlasauth.net",   // per-instance BAPI origin
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

`ErrorCodes` names the stable codes you are most likely to branch on, for example
`ErrorCodes.UnknownField`, `EmailDeliveryNotConfigured`, `EmailProviderRequired`,
`PlanInUse`, `WrongKeySurface` and `InvalidKey`:

```csharp
if (ex.HasCode(ErrorCodes.EmailDeliveryNotConfigured)) { /* set up email first */ }
```

### Strict request bodies

By default a write that includes a field the endpoint does not accept still succeeds,
and the server lists the ignored fields in an `Atlas-Unknown-Fields` response header.
The SDK passes those to `OnUnknownFields` (by default a `Trace` warning):

```csharp
using var atlas = new AtlasClient(new AtlasClientOptions
{
    SecretKey = secret,
    OnUnknownFields = w => logger.LogWarning("Ignored {Fields} on {Method} {Path}", w.Fields, w.Method, w.Path),
});
```

Set `Strict = true` to get a `422` with code `UNKNOWN_FIELD` instead, before anything
changes. Override it per call with `WithStrict`:

```csharp
await atlas.WithStrict(true).Users.UpdateAsync("user_123", body);   // just this call
await atlas.WithStrict(false).Users.UpdateAsync("user_123", body);  // opt out when the instance defaults to strict
```

### Clearing a field

Unset (null) properties are never sent. To clear a stored value on a partial update,
send an explicit null with `Set(...)`, which request bodies have (it also reaches fields
this SDK version does not model yet):

```csharp
await atlas.Design.UpdateAsync(new AtlasDesign().Set("paletteDark", null));
await atlas.Organizations.UpdatePolicyAsync("org_1", new OrganizationPolicyPatch().Clear("ipAllowlist"));
```

### Platform API (platform key)

Provisioning apps and registering "Sign in with Atlas" apps uses a platform key
(`psk_...`). Set `PlatformKey` (alone, or next to `SecretKey`) and use `atlas.Platform`:

```csharp
using var atlas = new AtlasClient(new AtlasClientOptions
{
    PlatformKey = Environment.GetEnvironmentVariable("ATLAS_PLATFORM_KEY"),
});

var app = await atlas.Platform.AtlasIdApps.CreateAsync(new CreateAtlasIdAppBody
{
    Name = "Acme",
    RedirectUris = new List<string> { "https://acme.example/callback" },
    PostLogoutRedirectUris = new List<string> { "https://acme.example/" },
    BackchannelLogoutUri = "https://acme.example/logout/backchannel",
    Branding = new AtlasIdAppBrandingInput
    {
        AccentColor = "#4f46e5",
        Copy = new AtlasIdAppCopy { SignInTitle = "Sign in to {app}" },
    },
});
var clientSecret = app.ClientSecret; // shown once; RotateSecretAsync issues a new one
```

`atlas.Platform.Apps` provisions and manages instances, and
`atlas.Platform.DesignAssets` holds images for app designs.

### Sign-in page design

`atlas.Design` reads and changes the instance design (`GetAsync`, `ReplaceAsync`,
`UpdateAsync`, `PreviewAsync`), an organization's override (`GetForOrganizationAsync`,
…), and the public `CatalogAsync()` of presets and fonts. `AtlasDesign` is fully typed
(palette, typography, shape, card, layout, buttons, inputs, background, logo); wire keys
keep their camelCase.

```csharp
var settings = await atlas.Design.UpdateAsync(new AtlasDesign
{
    Preset = "glass",
    Palette = new AtlasDesignPalette { Primary = "#4f46e5" },
});

// Images are uploaded as raw bytes with their content type.
var logo = await atlas.Design.UploadAssetAsync(File.ReadAllBytes("logo.png"), "image/png", kind: "logo");
await atlas.Design.UpdateAsync(new AtlasDesign { Logo = new AtlasDesignLogo { AssetId = logo.AssetId } });
```

### Setup checks and more

- `atlas.Instance.PreflightAsync()` lists configuration problems, each with a
  `Severity`, `Code`, `Message`, `Fix` and `Subject`.
- `atlas.Instance.GetCookieConsentAsync()` / `ReplaceCookieConsentAsync(...)` /
  `DeleteCookieConsentAsync()` manage the cookie-consent script on the hosted pages.
- `atlas.OAuthProviders.ListAsync(new[] { "google" }, configured: true)` filters the
  catalog; the upsert body takes `DisplayName`, `IconUrl`, `Mode`, and wallet settings
  (`UpsertOAuthProviderBody.Siwe(...)`, `Siws(...)`, `Siwf(...)`).
- `atlas.Webhooks.Endpoints` updates endpoints, reveals or rotates the signing secret
  and sends a test event; `atlas.Webhooks.RedeliverAsync(id, deliveryId)` retries a
  past delivery.
- `atlas.EntitlementOverrides` grants a plan, features or limits to a user or
  organization outside of billing.
- Machines, connectors, custom audit events, notifications, OpenFGA, analytics,
  diagnostics, redirect URLs and more each have their own namespace on the client.

## Verifying session JWTs

```csharp
using Atlas.Verification;

var backend = new AtlasBackend(new AtlasBackendOptions
{
    JwksUrl = "https://api.atlasauth.net/.well-known/jwks.json",
    Issuer  = "https://auth.yourdomain.com",
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

## Back-channel logout

When a user signs out, Atlas POSTs a signed `logout_token` to your app's
`backchannel_logout_uri`. `LogoutTokenVerifier` checks it against the instance JWKS
(signature, `typ`, issuer, audience, `iat`/`exp`, `jti`, the logout event, `sid`/`sub`,
and no `nonce`):

```csharp
using Atlas.Verification;

var logout = new LogoutTokenVerifier(new LogoutTokenVerifierOptions
{
    JwksUrl  = "https://api.atlasauth.net/v1/jwks",
    Issuer   = "https://your-issuer.example",
    ClientId = "your_client_id",
});

// Framework-agnostic handler: form body in, status code out.
async Task<int> HandleBackchannelLogout(string formBody, IDictionary<string, string> responseHeaders)
{
    responseHeaders["Cache-Control"] = "no-store";
    try
    {
        var claims = await logout.VerifyFormBodyAsync(formBody);
        if (!await seenJtis.TryAddAsync(claims.Jti)) return 400;  // replayed token
        if (claims.Sid != null) await sessions.EndBySessionIdAsync(claims.Sid);
        else await sessions.EndAllForUserAsync(claims.Sub!);
        return 200;
    }
    catch (LogoutTokenException)  // ex.Reason says why
    {
        return 400;
    }
}
```

With ASP.NET Core, `Atlas.AspNetCore` maps the whole endpoint (200 or 400, always
`Cache-Control: no-store`):

```csharp
app.MapAtlasBackchannelLogout("/logout/backchannel", logout, async (claims, http) =>
{
    if (claims.Sid != null) await sessions.EndBySessionIdAsync(claims.Sid);
    else await sessions.EndAllForUserAsync(claims.Sub!);
});
```

The verifier does not remember tokens. To reject replays, keep each `Jti` for a few
minutes (longer than the token lifetime) in a shared store such as Redis or your
database, and refuse one you have already seen.

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
