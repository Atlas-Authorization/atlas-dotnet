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
`EmailProviderInUse`, `PlanInUse`, `WrongKeySurface` and `InvalidKey`:

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
  `SettingsLink` adds a "Cookie settings" link that reopens your CMP; the response's
  `SettingsLink.RenderedAttributes` shows what the rendered link carries (your
  attributes plus the preset's trigger):

  ```csharp
  await atlas.Instance.ReplaceCookieConsentAsync(new CookieConsentBody
  {
      ScriptUrl = "https://consent.cookiebot.com/uc.js",
      SiteId = "your-cbid",
      SettingsLink = new CookieConsentSettingsLinkInput
      {
          Label = "Cookie settings",
          Preset = CookieConsentSettingsLinkPresets.Cookiebot,
          Attributes = new Dictionary<string, string> { ["class"] = "footer-link" },
      },
  });
  ```
- `atlas.Messaging.DeleteChannelAsync("email")` removes a provider. Removing the
  email provider while email-dependent features are on (and nothing else would
  deliver email) throws `AtlasConflictException` with `EMAIL_PROVIDER_IN_USE`; the
  error's `meta.features` lists those features. `DeleteChannelAsync("email", force: true)`
  removes it anyway.
- `CreateOAuthClientBody.SkipLogoutConfirmation` / `UpdateOAuthClientBody.SkipLogoutConfirmation`
  (first-party clients only): logout through `/oauth2/logout` with the client's
  `client_id` and an exactly registered `post_logout_redirect_uri` ends the session
  and redirects straight back, without the confirmation page.
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

### Domain moves, logout fan-out and managed email

```csharp
// Move every URL from the old origin to the new one. DryRun first shows the diff.
var preview = await atlas.Instance.MoveOriginAsync(new OriginMoveBody { From = "https://old.example.com", To = "https://example.com", DryRun = true });
foreach (var c in preview.Changes) Console.WriteLine($"{c.ObjectType} {c.Field}: {c.Before} -> {c.After}");

// Send the same logout_token your OIDC apps get to another service.
await atlas.LogoutNotifyEndpoints.CreateAsync(new LogoutNotifyEndpointBody { Url = "https://api.example.com/atlas/logout" });

// Set your domain up on your own Resend or Brevo account, then publish the returned DNS records.
var managed = await atlas.Messaging.CreateManagedEmailAsync(new ManagedEmailBody
{
    FromAddress = "Acme <no-reply@example.com>",
    Transport = "resend",
    Config = new ManagedEmailProviderKey { ApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY")! },
});

// A development instance can send sign-in emails through your provider without a verified domain.
await atlas.Messaging.EnableDevelopmentEmailAsync(new DevelopmentEmailBody { ProviderId = "mp_123" });
await atlas.Messaging.DisableDevelopmentEmailAsync();
```

### OAuth client secret rotation

The previous secret keeps working for `OverlapSeconds` (default 24 hours, at most 7 days), so you can deploy the
new one without downtime. Pass `0` to revoke it now, for example after a leak:

```csharp
var rotated = await atlas.OAuthClients.RotateSecretAsync(clientId, new RotateOAuthClientSecretBody { OverlapSeconds = 3600 });
// rotated.ClientSecret is shown once; rotated.PreviousSecretExpiresAt says when the old one stops.
```

### Webhooks

`WebhookVerifier` accepts an `atlas-signature` header that carries several signatures separated by spaces
(`v1,<sig> v1,<sig>`). While an endpoint secret rotates, Atlas may sign each delivery with the old and the new
secret; the delivery verifies when any one signature matches.

A rotation stops the old secret at once by default. Pass an overlap (at most 7 days) to keep it signing each
delivery next to the new one while you deploy; choose an overlap only once every receiver accepts several
signatures:

```csharp
var rotated = await atlas.Webhooks.Endpoints.RotateSecretAsync(endpointId, new RotateWebhookSecretBody { OverlapSeconds = 3600 });
// rotated.PreviousSecretExpiresAt says when the old secret stops signing.
```

### Trusted reverse proxy

If a reverse proxy serves the Frontend API from your own origin, give it a proxy secret. It sends the secret in
`X-Atlas-Proxy-Secret` and the browser's IP in `X-Atlas-Client-IP`; Atlas then uses that IP for rate limits,
lockout, risk signals and audit logs.

```csharp
var created = await atlas.InstanceSecurity.CreateProxySecretAsync();
// created.Secret is shown once; store it in your proxy's environment.
var state = await atlas.InstanceSecurity.GetProxySecretAsync(); // Set, CreatedAt, PreviousExpiresAt (never the secret)
await atlas.InstanceSecurity.CreateProxySecretAsync(new CreateProxySecretBody { OverlapSeconds = 3600 }); // rotate
await atlas.InstanceSecurity.DeleteProxySecretAsync();
```

Platform rotations take an overlap too: `Platform.Apps.RotateInstanceSecretAsync(appId, instanceId, new RotatePlatformSecretBody
{ OverlapSeconds = 0 })` (default 72 hours), `Platform.AtlasIdApps.RotateSecretAsync(id, new RotateAtlasIdAppSecretBody { ... })`
(default 24 hours) and `Platform.AtlasIdApps.RotateWebhookSecretAsync(id, body)` (default 0).

With a platform key, `Platform.AtlasIdApps.FindUsersAsync(id, email)` answers a privacy request for one app,
`RevokeGrantAsync(id, grantId)` revokes one person's approval, and `Platform.AtlasIdPublisher` claims and verifies
the publisher name shown on consent screens.

### Referral attribution

With referral attribution on, Atlas reports every interactive sign-up to your referral platform before
`user.created` is sent. `User` gains `Referral` (where attribution stands and, once attributed, who referred them;
null when never reported), `ReferralCode` and `ReferralLink`. `Referral.Status` is `pending`, `attributed`, `none`,
`failed`, `held` or `flagged`; the platform's own status is `Referral.ReferralStatus`.

```csharp
await atlas.Referrals.UpdateAsync(new UpdateReferralSettingsBody
{
    Enabled = true,
    Provider = "webhook", // the Atlas referral protocol; or "referally" or another built-in adapter
    EndpointUrl = "https://referrals.example.com/atlas",
    SigningSecret = Environment.GetEnvironmentVariable("REFERRAL_SIGNING_SECRET"),
    Security = new UpdateReferralSecurityBody { OnHighRisk = "hold" },
}.Clear("link_template")); // sent as null
var test = await atlas.Referrals.TestAsync(); // Ok, HttpStatus, LatencyMs, Error
var failed = await atlas.Referrals.ListCallsAsync(limit: 20, status: "failed");
await atlas.Referrals.RetryCallAsync(failed.Data[0].Id);

await atlas.Users.UpdateAsync("user_123", new UpdateUserBody { ReferralCode = "ada-1" }); // 409 REFERRAL_CODE_TAKEN if held
await atlas.Users.FlagReferralAsync("user_123", "self-referral");

await atlas.Referrals.SimulateAsync("ACME10");                          // a made-up user; nothing is stored
await atlas.Referrals.RetryFailedAsync(errorCode: "signature_rejected"); // after pasting the new secret
await atlas.Referrals.ListCallsAsync(new ListReferralCallsQuery { Limit = 1000, ErrorCode = "timeout", StartingAfter = "rfc_123" });
var job = await atlas.Referrals.BackfillAsync(new ReferralBackfillBody { Since = "2026-01-01", CodeMetadataPath = "public_metadata.referral_code" });
await atlas.Referrals.GetBackfillAsync(job.Id);                         // running, then completed
await atlas.Referrals.GetForUserAsync("user_123");                      // the stored result, no call to the platform
await atlas.Users.SetExternalIdAsync("user_123", "cust_42");            // forwarded to the platform as an alias
```

Enabling without what the provider needs is `422 REFERRALS_NOT_CONFIGURED`. When the platform has not answered by the
time `user.created` is sent, `Referral.Status` is `pending` and the `user.referral_resolved` webhook follows; its `data`
is the full user (deserialize it as `User`), with `attributed`, `none` or `failed`. `user.referral_attributed`
(`UserReferralAttributedEvent`) still fires for a late attribution.

Two-way sync: a provider with the `inbound` capability sends referral changes made after sign-up back to Atlas. The
settings report `Inbound` (`Url` to give the platform, `SecretSet`, `EndpointId`, and `LastEvent` with `At`, `Type`,
`Outcome`). Store the platform's event secret with `InboundSecret` (write-only, 16-512 characters) and its
subscription id with `InboundEndpointId`; `Clear("inbound_secret")` sends null.

To give each user your own id before the first referral call, turn on the `before_user_create` hook. Atlas then sends
your endpoint a signed `POST` (verify it like a webhook) of type `user.before_create` for every self-service sign-up,
and stamps the `external_id` and metadata you answer:

```csharp
await atlas.UserHooks.UpdateBeforeUserCreateAsync(new UpdateBeforeUserCreateHookBody
{
    Enabled = true,
    Url = "https://app.example.com/hooks/before-create",
    SigningSecret = Environment.GetEnvironmentVariable("BEFORE_CREATE_SECRET"),
});
var tested = await atlas.UserHooks.TestBeforeUserCreateAsync(); // Ok, Answer, Error
```

### API key spend caps

Give an end-user API key caps in your own units, then meter what your API consumed
against them. A refused debit records nothing and is not an exception, so you
enforce it inside your own transaction:

```csharp
var key = await atlas.ApiKeys.CreateAsync(new CreateApiKeyBody
{
    SubjectType = "user",
    SubjectId = "user_123",
    Name = "CI",
    Limits = new List<ApiKeyLimit>
    {
        new ApiKeyLimit("requests", 10_000, ApiKeyLimitPeriods.Day),       // UTC calendar day
        new ApiKeyLimit("usd_cents", 500, ApiKeyLimitPeriods.Request),     // caps one debit
    },
});

var debit = await atlas.ApiKeys.RecordUsageAsync(key.Id, 1, "requests", idempotencyKey: requestId);
if (!debit.Allowed)                         // debit.Code == ErrorCodes.LimitExceeded
    return Results.StatusCode(429);

var usage = await atlas.ApiKeys.GetUsageAsync(key.Id); // caps with Used/Remaining/ResetsAt + counters
await atlas.ApiKeys.UpdateAsync(key.Id, new UpdateApiKeyBody().ClearLimits());
```

Periods are `request` (caps a single debit), `day` and `month` (UTC calendar
windows) and `total` (never resets). Reusing an idempotency key replays the first
outcome (`IdempotentReplay`) without counting again; reusing it with a different
unit or amount throws `AtlasConflictException` with `IDEMPOTENCY_CONFLICT`.
`ApiKeys.VerifyAsync` also returns the key's `Name`, `Scopes`, `Constraints`,
`Limits` and `Usage`. The `api_key.created`, `api_key.updated` and
`api_key.limit_reached` webhooks carry the key's configuration; deserialize their
`Data` as `ApiKeyWebhookData` with `AtlasJson.Options`.

### Session keep-alive

If your app loads Atlas on only some pages, keep the session's inactivity window
moving from your backend:

```csharp
var s = await atlas.Sessions.TouchAsync(sessionId);
// s.Extended: false when a touch within the last minute already moved it
// s.IdleExpiresAt: epoch ms the session ends from inactivity
```

It never extends past the absolute expiry and never revives an ended session
(`AtlasConflictException` with `SESSION_REVOKED`, `SESSION_IDLE_EXPIRED` or
`SESSION_EXPIRED`). Calling it on every request is safe.

`CreateUserBody.Notify` and `ImportUsersBody.Notify` set to `false` (the default)
guarantee the user is sent no email, SMS or push as a consequence of the create.

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
