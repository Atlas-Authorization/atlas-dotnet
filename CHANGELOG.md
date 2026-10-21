# Changelog

## 1.7.0 (Atlas.Sdk)

### Added

- **Trusted reverse proxy secret.** `InstanceSecurity.GetProxySecretAsync()` (`ProxySecret`: `Set`, `CreatedAt`,
  `PreviousExpiresAt`, `SecretHeader`, `ClientIpHeader`), `CreateProxySecretAsync(CreateProxySecretBody?)`
  (`OverlapSeconds` on a rotation: default 86400, `0` stops the old secret now, at most 604800; returns
  `ProxySecretCreated` with `Secret`, shown once, and `Rotated`) and `DeleteProxySecretAsync()` (`ProxySecretDeleted`).
- **Webhook secret rotation with an overlap.** `Webhooks.Endpoints.RotateSecretAsync(id, RotateWebhookSecretBody)`
  (`OverlapSeconds`, default 0, at most 604800). `WebhookSecret` gains `Warning`, `OverlapSeconds` and
  `PreviousSecretExpiresAt`; `WebhookEndpoint` gains `PreviousSecretExpiresAt`.
- **Per-instance platform rotation with an overlap.** `Platform.Apps.RotateInstanceSecretAsync(id, instanceId,
  RotatePlatformSecretBody, idempotencyKey?)` (`OverlapSeconds`, default 259200, `0` revokes the prior keys now).
  `PlatformInstanceSecret` gains `AppId`, `InstanceId`, `OverlapSeconds` and `PreviousKeysExpireAt`.
- **Sign in with Atlas apps.** `Platform.AtlasIdApps.RotateSecretAsync(id, RotateAtlasIdAppSecretBody)` (default
  24 hours) and `RotateWebhookSecretAsync(id, RotateAtlasIdAppSecretBody)` (default 0). `AtlasIdApp` and
  `AtlasIdAppWithSecret` gain `PreviousSecretExpiresAt`, `SecretIssuedAt` and `WebhookPreviousSecretExpiresAt`;
  `AtlasIdAppWithSecret` also gains `OverlapSeconds`.

### Changed

- `PlatformInstanceSecret.Id` is obsolete: the API never sent it; read `InstanceId`.
- `Platform.Apps.RotateInstanceSecretAsync(id, instanceId)` now keeps the prior keys valid for 72 hours (the
  server's new default). Pass `new RotatePlatformSecretBody { OverlapSeconds = 0 }` for the old behaviour.

## 1.6.0 (Atlas.Sdk)

### Added

- **OAuth client secret rotation with overlap.** `OAuthClients.RotateSecretAsync(id, RotateOAuthClientSecretBody, idempotencyKey?)`
  takes `OverlapSeconds` (default 86400, `0` revokes the old secret now, at most 604800). `OAuthClientWithSecret` gains
  `PreviousSecretExpiresAt` and `OverlapSeconds`; `OAuthClient` gains `PreviousSecretExpiresAt`.
- **Development email sender.** `Messaging.GetDevelopmentEmailAsync`, `EnableDevelopmentEmailAsync(DevelopmentEmailBody)` and
  `DisableDevelopmentEmailAsync` (`DevelopmentEmail`, `DevelopmentEmailDisabled`). `MessagingProviders` gains
  `DevelopmentEmail` (`DevelopmentEmailSummary`).
- **Managed email on your own provider account.** `ManagedEmailBody` gains `Transport`, `Config` (`ManagedEmailProviderKey`)
  and `ProviderId`. `ManagedEmail` gains `Backend`, `ProviderId` and `FeedbackWebhook`; `Limits` is now null and
  `Reputation` is `your_provider_account`. `ManagedEmailDeleted` gains `ProviderRemoved`.
- **Provider mode.** `MessagingProvider` gains `Id` and `Mode`; `MessagingProviderInput` gains `Mode`; new
  `Messaging.SetEmailAsync(EmailProviderConfig, fromAddress, mode)`.
- **Allowed origins with an integration.** `WithAllowedOriginEntries(IEnumerable<AllowedOriginEntry>)` on
  `UpdateInstanceBody`, `CreatePlatformAppBody`, `UpdatePlatformAppBody`, `CreatePlatformInstanceBody` and
  `CreateAccountlessAppBody` (`AllowedOriginEntry.Redirect`, `Embedded`, `Auto`). `UpdateInstanceBody` gains `Set`.
- **Issuer and verification endpoints.** `Instance` gains `Issuer`, `DiscoveryUrl`, `JwksUri` and
  `AllowedOriginIntegrations`; `InstanceUpdateResult` gains `AllowedOriginIntegrations`; `PlatformApp`,
  `PlatformAppWithSecret`, `PlatformAppProvisioned` and `AcceptedPlatformApp` gain `FrontendApiHost`, `Issuer`,
  `DiscoveryUrl`, `JwksUri` and `AllowedOriginIntegrations`.
- **Login readiness.** `LoginReadiness` gains `Integration` (`LoginReadinessIntegration`); `LoginReadinessChecks` gains `Applies`.
- **Email verification.** `EmailAddress` gains `Verification` (`EmailAddressVerification`: `Status`, `Strategy`).
- **Branding audiences.** `Branding` and `ResolvedBranding` gain `Audiences` (`BrandingAudience`).
- **Error codes.** `ErrorCodes.ReadOnlyField`, `DevelopmentEmailNotAllowed`, `DevelopmentEmailUnavailable`,
  `DevelopmentEmailLimitReached`, `DevelopmentEmailTemplateNotAllowed`.
- **Webhooks, several signatures.** `WebhookVerifier` accepts an `atlas-signature` header with several space-separated
  `v1,<signature>` entries and verifies when any one matches (each compared in constant time). A single signature works
  as before.

## 1.5.0 (Atlas.Sdk) / 1.3.0 (Atlas.AspNetCore)

### Added

- **Back-channel logout, retryable failures.** `LogoutTokenException.Retryable` is true for
  `LogoutTokenFailure.NoKeys`: the issuer's keys could not be fetched (a network error, a JWKS outage, or
  a just-rotated key whose refetch failed). Answer `503` with `Retry-After` then, so Atlas retries the
  delivery, and `400` otherwise. A token whose `kid` is unknown after a failed JWKS refetch is now
  `NoKeys` (was `BadSignature`).
- **Atlas.AspNetCore.** `MapAtlasBackchannelLogout` answers `503` with `Retry-After: 30` for a retryable
  failure (was `400`).
## 1.5.0 (Atlas.Sdk)

### Added

- **Webhook verification.** `new WebhookVerifier(secret).Verify(rawBody, headers, nowMs)` (namespace
  `Atlas.Verification`) returns a `WebhookVerification` (`Verified`, `Reason`, `Event`, `DeliveryId`,
  `Timestamp`). `DeliveryId` is the signed `atlas-id` header, the same on every retry of one event:
  deduplicate on it.
- **Domain moves.** `Instance.MoveOriginAsync(new OriginMoveBody { From, To, DryRun })` returns `OriginMove`;
  `Platform.AtlasIdApps.MoveOriginAsync(id, body)` does it for one app.
- **Connections and integrations.** `Instance.ConnectionsAsync()`, and `Instance.Get/Set/DeleteChatlyIntegrationAsync`
  and `…ForumulateIntegrationAsync`.
- **Logout fan-out.** New `LogoutNotifyEndpoints` resource: `ListAsync`, `CreateAsync`, `GetAsync`, `UpdateAsync`,
  `DeleteAsync`, `ListDeliveriesAsync`.
- **Managed email.** `Messaging.GetManagedEmailAsync`, `CreateManagedEmailAsync`, `VerifyManagedEmailAsync`,
  `DeleteManagedEmailAsync(force)` (`ManagedEmail`).
- **Back-channel logout logs.** `OAuthClients.ListBackchannelDeliveriesAsync` / `TestBackchannelLogoutAsync` and the
  same on `Platform.AtlasIdApps`.
- **Sign in with Atlas apps.** `Platform.AtlasIdApps.PreflightAsync`, `FindUsersAsync`, `RevokeGrantAsync`,
  `RotateWebhookSecretAsync`; `AtlasIdApp` gains `WebhookUrl`, `WebhookEvents`, `WebhookActive` (and
  `AtlasIdAppWithSecret.WebhookSecret`); `UpdateAtlasIdAppBody.WebhookUrl`.
- **Publisher.** `Platform.AtlasIdPublisher`: `GetAsync`, `SetAsync`, `SetSkipConsentAsync`, `DeleteAsync`,
  `VerifyAsync`; and `Platform.ConfigChangelogAsync(params)`.
- **More fields.** `User.Version`; `CookieConsent.Culture` / `DeclaredCookies`; `CookieConsentBody.Culture`;
  `Preflight.SdkVersionsSeen`; `WebhookDelivery.Error` / `Location`; `WebhookEndpointList.EventCatalog` /
  `EventAudiences`; `CustomDomain.Dns` and `CreateDomainBody.Dns`; `ConfigChangelogEntry.Target`;
  `MessagingProviders.ManagedEmail`.

## 1.4.1 (Atlas.Sdk)

### Added

- **Captcha is bring-your-own-keys.** `ErrorCodes.CaptchaKeysRequired` (422) and `ErrorCodes.CaptchaInUse` (409, `meta.flows`).
- `InstanceSecurity.SetCaptchaSecretAsync(secret, provider)` stores the secret for a named provider before captcha is turned on.
- `InstanceSecurity.DeleteCaptchaSecretAsync(provider, force)` removes a secret a flow uses and turns those flows off; `CaptchaSecretResult.FlowsTurnedOff`.

## Atlas.Sdk 1.4.0

All changes are backward compatible: new fields are optional or nullable and the new
method is an overload.

### Added

- `OAuthClient.SkipLogoutConfirmation` and `OAuthClientWithSecret.SkipLogoutConfirmation`
  (bool, default false); optional `CreateOAuthClientBody.SkipLogoutConfirmation` and
  `UpdateOAuthClientBody.SkipLogoutConfirmation` (`bool?`). First-party clients only
  (422 `VALIDATION_FAILED`, param `skip_logout_confirmation`, on a third-party client).
  When true, RP-initiated logout (`/oauth2/logout`) with the client's `client_id` and an
  exactly registered `post_logout_redirect_uri` ends the session and redirects straight
  back with `state`, without the confirmation page.
- Cookie-consent settings link: `CookieConsentBody.SettingsLink`
  (`CookieConsentSettingsLinkInput`: `Label`, `Preset`, `Attributes`; null or omitted =
  no link) and `CookieConsent.SettingsLink` (`CookieConsentSettingsLink`: `Label`,
  `Preset`, `Attributes`, `RenderedAttributes`). Preset values in
  `CookieConsentSettingsLinkPresets` (`Cookiemunch`, `Cookiebot`, `OneTrust`, `Custom`);
  `Preset` stays a string so unknown values decode.
- `Messaging.DeleteChannelAsync(channel, force, ct)` — sends `?force=true` when `force`
  is true to remove the email provider even while email-dependent features are on (no
  query otherwise). The existing `DeleteChannelAsync(channel, ct)` is unchanged.
- `ErrorCodes.EmailProviderInUse` (`EMAIL_PROVIDER_IN_USE`): 409 when removing the email
  provider would leave email-dependent features with no delivery; the error's
  `meta.features` lists them (e.g. `emailCode`, `emailLink`, `emailVerification`,
  `passwordReset`).
- `UserSession` (`Users.ListSessionsAsync` items): `IdleExpiresAt` (epoch ms, nullable),
  `LastActiveOrganizationId` and `ImpersonatedBy` (nullable).

## Atlas.Sdk 1.3.0

### Added

- `Sessions.TouchAsync(id)` — `POST /v1/sessions/{id}/touch`, backend session
  keep-alive; returns `SessionTouch` (the session plus `Extended` and `IdleExpiresAt`).
- `Session.IdleExpiresAt` (epoch ms, nullable).
- API key spend caps: `ApiKeyLimit` (`Unit`, `Amount`, `Period`: `request` | `day` |
  `month` | `total`, see `ApiKeyLimitPeriods`), `CreateApiKeyBody.Limits`,
  `UpdateApiKeyBody.Limits`, `UpdateApiKeyBody.ClearLimits()` (sends `"limits": null`)
  and `UpdateApiKeyBody.Set(key, value)`.
- `ApiKey`, `ApiKeyWithSecret`: `Limits` and `Usage` (`List<ApiKeyLimitState>`:
  `Unit`, `Period`, `Amount`, `Used`, `Remaining`, `WindowStart`, `ResetsAt`).
- `ApiKeyVerification`: `Name`, `Scopes`, `Constraints`, `Limits`, `Usage`.
- `ApiKeys.RecordUsageAsync(id, body)` / `RecordUsageAsync(id, amount, unit, idempotencyKey)` —
  `POST /v1/api_keys/{id}/usage`, returns `ApiKeyUsageDebit` (`KeyId`, `Unit`, `Amount`,
  `Allowed`, `Code`, `Exceeded`, `Usage`, `Counters`, `IdempotentReplay`); body
  `RecordApiKeyUsageBody` (`Amount`, `Unit`, `IdempotencyKey`).
- `ApiKeys.GetUsageAsync(id)` — `GET /v1/api_keys/{id}/usage`, returns `ApiKeyUsage`
  (`KeyId`, `Limits`, `Usage`, `Counters`); `ApiKeyUsageCounter` (`Unit`, `Period`,
  `Used`, `WindowStart`, `ResetsAt`).
- `CreateUserBody.Notify` and `ImportUsersBody.Notify`.
- `ApiKeyWebhookData` — the `data` of the `api_key.created`, `api_key.updated`,
  `api_key.revoked` and `api_key.limit_reached` webhook events (`Name`, `Claims`,
  `Scopes`, `Constraints`, `Limits`, `MaxTokenLifetimeSeconds`, `ExpiresAt`,
  `CreatedAt`, `UpdatedAt`, and `Limit` on `api_key.limit_reached`).
- `ErrorCodes.LimitExceeded`, `SessionRevoked`, `SessionIdleExpired`, `SessionExpired`.

### Changed

- `AtlasIdAppCopy.SignInTitle` / `SignUpTitle` XML docs state the current defaults
  ("Sign in", "Create your Atlas Account"); no API change.

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
