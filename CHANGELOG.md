# Changelog

## Atlas.Sdk 1.2.0 / Atlas.AspNetCore 1.2.0

### Added

- Full API coverage: every secret-key and platform-key operation in the public API
  reference now has a typed method. New namespaces: `Design`, `Platform`, `Machines`,
  `Connectors`, `AuditEvents`, `Notifications`, `OpenFga`, `Analytics`, `Diagnostics`,
  `ConfigChangelog`, `RedirectUrls`, `EntitlementOverrides`, `OrgLdapConnections`,
  `SessionExchangeApps`, `OrganizationSettingsSchema`, `LegacyAuthConnection`, `Mcp`,
  `McpConnector`, `Pca`, `Tickets`, `EmailAddresses`, `Captcha` and `Testing`; many
  new methods on `Users`, `Organizations`, `Webhooks`, `Billing`, `ApiKeys`, `Waitlist`,
  `Instance` and others.
- Platform key support: `AtlasClientOptions.PlatformKey` and `atlas.Platform` for apps,
  Sign in with Atlas apps (logout URIs, branding, design, secret rotation) and
  account-owned design images.
- Sign-in page design: instance and organization designs, previews, the design
  catalog, and image uploads. `AtlasDesign` is fully typed.
- Raw-bytes uploads (`RequestOptions.BinaryBody` / `ContentType`) for design images.
- `atlas.Instance.PreflightAsync()` with typed findings, and cookie-consent settings.
- Strict request bodies: `AtlasClientOptions.Strict` and per-call `atlas.WithStrict(...)`
  send `Atlas-Strict`; ignored fields reported in `Atlas-Unknown-Fields` reach
  `AtlasClientOptions.OnUnknownFields` (default: a `Trace` warning).
- `LogoutTokenVerifier` for OIDC back-channel `logout_token`s, and
  `MapAtlasBackchannelLogout` in Atlas.AspNetCore to map the endpoint.
- OAuth providers: list filters (`provider`, `configured`); `DisplayName`, `IconUrl`,
  `Mode`, `CustomRedirectUri`, `Native` and wallet settings on upsert (with
  `Siwe`/`Siws`/`Siwf` and `ClearSetting` helpers); `IconUrl`, `Mode` and
  `SettingValues` on the provider; `UpdatedFields`/`Warning` on the upsert result.
- Webhooks: update, reveal/rotate secret, test, redeliver, a delivery `limit`,
  `ListWithEventsAsync()`, and `FailingSince` / `EventType` / `Payload` fields.
- Organization policy: typed `AllowedSignInMethods`, `IpAllowlist`,
  `MaxSessionAgeSeconds` and `EndUserPool`, `Clear(key)`, and `OrganizationPolicy.Typed()`.
- `ErrorCodes` constants for common error codes.
- Request bodies generated in this release have `Set(key, value)` to send any field,
  including an explicit `null` to clear a stored value.

### Changed

- `OrganizationEntitlements.Features` is now a `List<string>`, matching the API (it was
  mistyped as a dictionary), and `Limits` was added.
- `AtlasClient` accepts a platform key without a secret key; instance calls then fail
  with a clear message.
