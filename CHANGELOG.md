# Changelog

## 1.19.0 (Atlas.Sdk) — 2026-10-25

### Added

- **Test and review accounts for Sign in with Atlas apps.** `client.Platform.AtlasIdApps.ProvisionedAccounts`: `ListAsync`, `CreateAsync`, `GetAsync`, `RemoveAsync` and `RotatePasswordAsync` for `/v1/platform/atlas_id_apps/{id}/provisioned_accounts`, with typed `AtlasIdProvisionedAccount`, `AtlasIdProvisionedAccountList`, `CreateAtlasIdProvisionedAccountBody` and `RotateAtlasIdProvisionedPasswordBody`. Needs the `platform:atlas_id_apps:provision` scope (or `atlas_id_apps:<app id>:provision` for one app). The password is returned once, on create and on rotation; with `AtlasClientOptions.SecretRecipient` it arrives sealed as `SealedPassword`. Neither the password nor the sealed value is printed by `ToString()` on the models.
- **Verified domains.** `client.Platform.Domains`: `ListAsync`, `ClaimAsync`, `CheckAsync` (alias `VerifyAsync`) and `RemoveAsync` for `/v1/platform/domains`, with `AccountDomain` and `AccountDomainList`.
- **App fields.** `TestMode` and `ProvisionedVerifiedDomainsOnly` on `AtlasIdApp`, `AtlasIdAppWithSecret` and `UpdateAtlasIdAppBody`.
- Error codes `ErrorCodes.TestAccountRestricted` (403), `ErrorCodes.ProvisionedAccountLimitReached` (409) and `ErrorCodes.DomainNotVerified` (422).
- API coverage list: the nine new operations.

## 1.18.0 (Atlas.Sdk) — 2026-10-25

### Added

- **Partner platforms.** `client.PartnerPlatforms` (`PartnerPlatformsResource`): `ListAsync`, `GetAsync`, `SetAsync`, `RemoveAsync`, `RegistrationAsync`, `StoreTokenAsync` and `DeliveriesAsync` for `/v1/partner_platforms`, with typed `PartnerPlatform`, `PartnerPlatformList`, `PartnerPlatformRegistration`, `PartnerDelivery`, `SetPartnerPlatformBody` and `StorePartnerPlatformTokenBody`. The partner token is write-only: stored encrypted, never in a response model, and the body type does not print it.
- **Per-call audience.** `SetIntegrationBody.PerCallAudience` (`Param`, `Template`, `Allowed`, `AnyMatching`; `Set("per_call_audience", null)` turns it off) and `Sessions.GetIntegrationTokenAsync(id, integration, audience, audienceParams)` to mint a token for one audience (a site id, a channel, an organization id).
- **Privacy orchestrator.** `SetPrivacyOrchestratorBody` gains `SiteId`, `PartnerLinkId` and `CancelDeletionOnSignIn`. An outbound partner link's `source` can be `registration`.
- `ListUsersParams.IncludeDeleted` (`GET /v1/users`), and `UserEmailMatch.Deleted`.
- `OperatorConfiguration` gains `Audiences` and `AccessTokenClaims`.
- Error code `ErrorCodes.ManagedWafNotConfigured` (422, `meta.action`).
- The Sign in with Atlas grant routes under `/v1/platform/atlas_id_apps/{id}` also accept an instance secret key (documented on `Platform.AtlasIdApps` and `AtlasIdGrants`).
- API coverage list: the partner platform operations.

## 1.17.0 (Atlas.Sdk) — 2026-10-24

### Added

- **Enterprise SSO claims.** `SessionClaims` gains `SsoOrganizationId` (`sso_organization_id`: the Atlas organization that owns the connection, absent for an instance-wide one, independent of `OrgId`) and `SsoConnectionType` (`sso_connection_type`: `saml` | `oidc`). Scope a per-organization "require SSO" rule on `SsoOrganizationId`, never on "signed in via SSO".
- **Adopting a Sign in with Atlas app.** `Platform.AtlasIdApps.SetManagedByAsync(id, instanceIdOrNull)` (`PUT /v1/platform/atlas_id_apps/{id}/managed_by`, `AtlasIdAppManagedBy`). App objects gain `ManagedVia` (`instance_toggle` | `adopted`); `ManagedAtlasIdApp` also carries `ButtonRenderedOrigins`, `AllowedScopes` and `CreatedAt`.
- **Instance-surface app setup check.** `AtlasIdGrants.PreflightAsync(appId)` (`GET /v1/atlas_id_apps/{id}/preflight`). `AtlasIdAppPreflight` (both surfaces) gains `ButtonRenderedOrigins`, `AuthorizeRequests7d`, `FallbackOrigins` and `HomepageIncludesScript`.
- **Dry run.** `DryRunResult` gains `Warnings`, `WouldReturnStatus`, `Suppressed` and `ValidationSkipped` (`DryRunValidationSkipped`); `Mode` is `native`, `rollback` or `plan`.
- **Sealed secrets.** `SealedSecret`, `SealedClientSecret`, `SealedWebhookSecret` and `SecretsSealed` (`SecretsSealed`) on every secret-bearing response (webhook endpoints and secrets, OAuth clients, Sign in with Atlas apps, SCIM tokens, machines, API keys, platform apps and keys, integrations). `AtlasClientOptions.SecretRecipient` / `WithSecretRecipient` already send `X-Secret-Recipient` on every call, so every newly covered route answers sealed.
- API coverage list: two more operations.

## 1.16.0 (Atlas.Sdk) — 2026-10-24

### Added

- **Credentials.** `SecretKeys` (`SecretKeysResource`): `CreateAsync` (scopes or the `read_only` / `privacy:execute` / `privacy:grants:revoke` presets, `ExpiresAt` as epoch ms or ISO 8601, `AllowedIps`, `AllowedOrigins`, `Owner`, `KeyFormat`, `ExternalRef`, `Metadata`), `ListAsync` (also with a metadata filter), `GetAsync`, `UpdateAsync` (`Set(key, null)` clears), `RotateAsync` (overlap), `RevokeAsync`. `Keys.ListAsync` (every credential, the standard key shape), `Whoami.GetAsync` and `Platform.WhoamiAsync`, `PartnerLinks` (create / list / get / `SetCertFingerprintsAsync` for mTLS / `RotateSecretAsync` / `RevokeAsync`, `Requests` list / get / `ApproveAsync` / `DenyAsync`, `Outbound` list / save / test / remove / `RequestAsync`), `AtlasIdGrants` (`ListAppsAsync`, `LookupAsync`, `RevokeAsync`). `Platform.Keys` (list / create / update / revoke, `AllowedOrigins`) and `Platform.Audit` (`ListAsync`, `Streams` CRUD + test). `PartnerLinkProtocol` is the partner side of the link request protocol (no Atlas credential).
- **Key formats.** `AtlasKeys.TryParse` / `IsSecretKey` / `IsPlatformKey` / `IsPublishableKey` / `IsWebhookSecret` / `IsPartnerLinkSecret` / `IsAccessToken` accept `atlas_<live|test>_<kind>_…` and the legacy `sk_` / `pk_` / `psk_` / `whsec_` / `dsk_`.
- **Operator sign-in and token exchange.** `OperatorClient`: `GetConfigurationAsync`, `StartLoginAsync` (RFC 8628 device flow), `PollLoginAsync` (interval, `slow_down`, expiry), `ExchangeTokenAsync` (RFC 8693, one-hour token bound to one instance, app or account), `PartnerLinkTokenAsync` (`client_credentials`), `RevokeLeakedKeysAsync`, `NewClientFromAccessToken`. Failures are `OAuthFlowException` (`STEP_UP_REQUIRED` included).
- **Sealed secrets.** `AtlasClientOptions.SecretRecipient` (`X-Secret-Recipient`) makes shown-once secrets arrive as `SealedSecret` / `SealedClientSecret`; `SealedBox.GenerateKeyPair`, `OpenSealedSecret`, `Seal`, `Open` use libsodium `crypto_box_seal` through the **Sodium.Core** package (new dependency; nothing is hand-rolled).
- **Provisioning.** `ExternalRef` (idempotent create) and `Metadata` on the create bodies of platform apps, instances, secret keys, OAuth clients, webhook endpoints, Sign in with Atlas apps and integrations; `Metadata` / `ExternalRef` / `IdempotentReplay` on their responses; metadata list filters (`AtlasProvisioning.MetadataQuery`, `ListPlatformAppsParams.Metadata`, `SecretKeys.ListAsync(metadata)`, `OAuthClients.ListAsync(metadata)`, …); `Platform.Apps.CreateSecretKeyAsync`, `CloneConfigAsync`, `SandboxTeardownAsync`.
- **Dry runs and trace context.** `atlas.WithDryRun()` / `AtlasClientOptions.DryRun` send every write with `?dry_run=true` (no `Idempotency-Key`); `atlas.DryRunAsync(method, path, body)` returns the `DryRunResult` plan. `AtlasClientOptions.Traceparent` / `atlas.WithTraceparent(...)` send a W3C `traceparent`; `AtlasTrace.NewTraceparent`, `TryParseTraceparent`, `TraceparentFromHeaders`. `trace_id` on webhook, back-channel and referral call rows and `Trace` filters.
- **Privacy executor.** `Privacy` (`PrivacyResource`): `DiscoverAsync`, `PresetAsync`, `LookupAsync` (`PrincipalMatched`), `CreateRequestAsync` (idempotency key), `GetRequestAsync`, `CancelRequestAsync`, and `Privacy.Orchestrator` (`GetAsync` / `SetAsync` / `DeleteAsync`). `PrivacyCall.Verify` checks a signed case call or `privacy_request.*` event (and reads the `traceparent`). `PrivacyTypes`, `PrivacyStatuses`, `PrivacyEvents`. `DataSubjectRequest` gains `CanonicalType`, `Source`, `Detail`, `CaseId`, `ExternalCase`, `TraceId`, `Steps`, `Retention`, `DeletionEffectiveAt`.
- **Identity integrations.** `Instance.ListIntegrationsAsync` / `GetIntegrationAsync` / `SetIntegrationAsync` / `DeleteIntegrationAsync` / `IntegrationDeliveriesAsync` / `RevealIntegrationEventsSecretAsync` / `IntegrationAtlasIdTokenAsync`, `Sessions.GetIntegrationTokenAsync` (the server-side `getToken({ for })`), `Integrations` (`ReportVerifyErrorAsync`, `CardAsync`, `RevokeSessionsAsync`, `InstanceMetadataAsync`). Cookie consent reports `Source` / `SourceIntegration`.
- **Webhooks.** `Endpoints.GetAsync`, `UpsertByUrlAsync` (find-or-create by url), `TestNowAsync` (`?wait=`), `RedeliverWindowAsync`, `ReplayAsync` (`Since` may be `"failing_since"`), `ListWithEventsAsync(includeDeprecated, metadata)`, `DeliveriesAsync(ListWebhookDeliveriesParams)` (status, event type, time window, cursor, trace; limit up to 1000); endpoint health, destinations (`aws_eventbridge`, `gcp_pubsub`, `cloudflare_queue`), signature versions, alert contacts, auto-disable, retention, `LastFailingSince`; `Delivery` (`EgressIpsAsync`, `HealthAsync`, `SetAlertContactsAsync`). `WebhookVerifier.VerifyUrl` verifies v2 (URL-bound) signatures, ignores unknown versions, `requireV2`. `WebhookEvents`: the canonical names, the alias table (`Aliases`, `Canonical`, `Spellings`, `IsDeprecated`), health / `secret_key.*` / `webhook.test` events, `ScopeWebhooksReveal`.
- **Status, notices, advisories, sub-processors, audit exports, entitlements.** `Status` (service status, incident subscription, incidents, dependencies), `Notices`, `SecurityAdvisories`, `SubProcessors`, `AuditExports` (CRUD, test, rotate; `AuditCloudEvent`), `Entitlements.GetAsync`; `EntitlementOverride.Internal`.
- **Instance diagnostics.** `Instance.PreflightAsync(minSeverity, includeSuppressed)` (+ `Group`, `Suppressed`, `Integrations`, `PreflightCodes`), `UnknownFieldsReportAsync`, `OpenApiDiffAsync`, `IntegrationHealthAsync` / `IntegrationHealthProvidersAsync`, `EmbedAsync`; `Design.ImportBrandUrlAsync` / `ImportBrandAsync` / `RemoveBrandAsync` and the `Brand`, `Precedence`, `Sources`, `Ignored` fields.
- **Lists.** Cursor variants: `Organizations.Memberships.ListPageAsync`, `AppRoles.ListUsersPageAsync`, `LogoutNotifyEndpoints.ListDeliveriesPageAsync`, `ApiKeys.ListRevokedPageAsync`; users `UpdatedAfter` / `UpdatedBefore` and `updated_at` ordering; events `Since` / `Until` / `Order` and `EventsPageAsync` (retention); config changelog `IncludeNoOp`, `NoOp`, `Recorded`, `ValuesRecorded`; `AuditLog.Actor`; `Configured` on the messaging provider, managed email, development email, organization settings schema and publisher singletons; `Messaging.ChannelAsync`.
- **Referrals.** `InstallAsync` (one-click), `ContextCookies`, `ProviderAliases`, `Capabilities`, `OneClick`, `OneClickPresets`, `AdvertisedCapabilities`, `Trace` filter, `ReferralProviders`. The provider id `referally` is now `referalli` (`ReferralProviders.ReferalliLegacy` is still accepted on write).
- **Errors.** `ErrorCodes.InsufficientScope` (`SCOPE_MISSING` is still tolerated: `AtlasException.IsInsufficientScope`, `RequiredScope`), `StepUpRequired`, `RouteNotFound` (`DidYouMean`, `AllowedMethods`, `DocsUrl`), `OriginNotAllowed`, `NotALiveInstance`, `TestModeOnly`, `TestingUnavailable`, `PartnerLinkUnavailable`.
- **Claims.** `SessionClaims.Orgs` (the `organizations` scope) and `SsoConnectionId` / `SsoTenantId` / `SsoIssuer` / `SsoDomain`. The back-channel logout verifier and `MapAtlasBackchannelLogout` are unchanged.
- Sign in with Atlas apps report `ButtonRenderedOrigins`, `AuthorizeRequests7d`, `FallbackOrigins`, `HomepageIncludesScript`.
- API coverage list: 727 operations.

## 1.15.0 (Atlas.Sdk) — 2026-10-24

### Added

- **Policy acceptance.** `Policies` (`PoliciesResource`): `ListAsync`, `CreateAsync`, `GetAsync`, `UpdateAsync`, `DeleteAsync` (a never-published policy is deleted, one with history is retired), `ListVersionsAsync`, `CreateVersionAsync` (a draft, or `Publish` with `EffectiveAt`), `GetVersionAsync`, `UpdateVersionAsync` / `DeleteVersionAsync` (drafts only), `PublishVersionAsync` (optional `effective_at`, epoch ms) / `PublishVersionAtAsync` (ISO 8601), `RetireVersionAsync`, `ListAcceptancesAsync` and `ExportAcceptancesCsvAsync`, `ForUserAsync` (a user's standing on every policy), `GetSettingsAsync` / `UpdateSettingsAsync` (the `pol` session claim). Typed `Policy`, `PolicyVersion`, `PolicyAcceptance`, `PolicyStanding`, `PolicySettings`. `PolicySource` (`PolicySourceResource`): `GetAsync`, `UpdateAsync` (the signing secret is write-only), `DeleteAsync`, `TestAsync`, `SyncAsync`, `ListDeliveriesAsync`.
- **Policy source protocol helpers.** `Atlas.Verification.PolicySourceProtocol`: `VerifyFeedRequest` checks Atlas's signed `GET {endpoint_url}/policies` (`atlas-signature` `v1,<base64 HMAC-SHA256>` over `{id}.{timestamp}.GET\n{path+query}`, millisecond timestamps, several `v1` values accepted, 5-minute window; the result carries the `key` query parameter), and `SignPublished` / `SignRetired` / `SignEvent` sign `policy.published` / `policy.retired` events for Atlas. Cross-checked against the TypeScript reference vectors.
- `SessionClaims.Pol()` and `HasAcceptedPolicy(key, version)` read the `pol` claim. `AtlasConstants.WebhookEventUserPolicyAccepted` and `WebhookEventPolicyVersionPublished`.
- API coverage list: 626 operations.

## 1.14.0 (Atlas.Sdk) / 1.6.0 (Atlas.AspNetCore) — 2026-10-24

### Added

- **Migrate from Better Auth: sealed-bundle imports.** `BetterAuthImports` (`BetterAuthImportsResource`): `GetPublicKeyAsync`, `RotatePublicKeyAsync`, `CreateAsync`, `ListAsync`, `GetAsync`, `UploadPartAsync` (string, or raw bytes as `text/plain`), `FinalizeAsync`, `DryRunAsync`, `ApplyAsync` (resumes a failed import; `confirmConflicts`), `DeleteAsync`, `ListJwksAsync`, `DeleteJwkAsync`. Helpers: `UploadAsync(bundle, options, partBytes, onPart)` creates the import, uploads the bundle in line-aligned parts of at most 8 MiB (`SplitBundleParts`, `PartBytes`) and finalizes it; `WaitForAsync(id, interval, timeout, onProgress)` polls until the import leaves `analyzing` / `queued` / `applying`. Typed `BetterAuthImport`, `BetterAuthImportKey`, `BetterAuthImportOptions` (with `Set`), `ImportedJwk`.
- **Better Auth session bridge.** `BetterAuthBridge` (`BetterAuthBridgeResource`): `GetAsync` / `UpdateAsync` / `DeleteAsync` (migration window, cookie prefix, live fallback; the old app's secret is write-only, `Set("secret", null)` clears it) and `ExchangeAsync(token, clientIp, userAgent, remember)` on the secret-key Backend API route, forwarding the end user's IP and user agent (`X-Atlas-Client-IP`, `X-Atlas-Client-User-Agent`). `BetterAuthExchange.ExchangeAsync` classifies the outcome (`Session`, `NeedsMfa`, `Refused` with the `bridge_outcome`; never throws for an Atlas or transport failure). `BetterAuthSessionBridge` turns a request's cookies into a `BridgeDecision` (Atlas cookies to set, Better Auth cookies to clear only on a final refusal, `ContinuationUrl` for a second factor); `BetterAuthCookies`, `BridgeCookie.Serialize`. Atlas.AspNetCore: `app.UseAtlasBetterAuthBridge(client, o => o.SignInUrl = "/sign-in")` exchanges a GET/HEAD that carries a Better Auth cookie and no Atlas session once, sets `__session` / `__atlas_rt`, clears the old cookies and redirects to the same URL (second factor: to `SignInUrl` with `__atlas_attempt` / `__atlas_status`).
- **Better Auth JWTs during the cutover.** `AtlasBackendOptions.LegacyIssuers` (also `AtlasAuthenticationOptions.LegacyIssuers`): `new LegacyIssuer { Issuer, Until, Jwks | JwksUrl, Audiences, Algorithms, ResolveSubject }`. A token from that issuer, inside its window, is verified against that issuer's keys only (EdDSA, ES256, ES512, RS256, PS256; `exp` required) and its `sub` is mapped to the Atlas user id through `external_id` (Backend API lookup with `SecretKey`, cached 5 minutes, at most 10 000 entries; failures are not cached). The claims carry `external_id` and `legacy` in `SessionClaims.Extra`.
- `RequestOptions.Headers` for extra per-call request headers (never replaces `Authorization`).
- API coverage list: 604 operations.

## 1.13.0 (Atlas.Sdk) / 1.5.0 (Atlas.AspNetCore) — 2026-10-24

### Added

- **DPoP (RFC 9449) on the access-token verifier.** A DPoP-bound token (`cnf.jkt`) is accepted only with the DPoP scheme and a valid proof (never as Bearer): `typ` `dpop+jwt`, `Dpop.Algs` (ES256/384/512, RS256/384/512, PS256/384/512, EdDSA via a built-in RFC 8032 `Ed25519` verifier, since the BCL has none), public embedded JWK, signature, `htm`, normalised `htu` (`Dpop.NormalizeHtu`; `PublicOrigin` for path-only URLs), `iat` window, `ath`, `jti` replay via an `IDpopReplayStore` (`MemoryDpopReplayStore` by default), RFC 7638 `Dpop.JwkThumbprint` match. `RequireDpop` refuses unbound tokens; `DpopNonces` (`HmacDpopNonceSource`) demands a server nonce (`use_dpop_nonce`, `AccessTokenResult.DpopNonce` for the `DPoP-Nonce` header). `VerifyRequestAsync(AccessTokenRequest)`; `AccessTokenResult.DpopBound`. Challenges use the `DPoP` scheme with `algs` (`BuildWwwAuthenticate(..., scheme)`). Atlas.AspNetCore: `UseAtlasAccessToken` checks the proof against the request and sets `DPoP-Nonce`. Metadata advertises `dpop_signing_alg_values_supported`; `ProtectedResourceMetadata(..., dpopBoundAccessTokensRequired)`.
- **Billing: several subscriptions per subject, add-ons, Chargebee / Autumn / Commet.** `Billing.GetSubscriptionAsync`, `SyncPlanEntitlementsAsync`, `GetSettingsAsync` / `UpdateSettingsAsync` (`BillingSettings`, limit merge `max` / `sum`), `CheckFeatureAsync` (`BillingFeatureCheck`). `BillingPlan` gains `Limits`, `Kind`, `SeatFeatureId`; `BillingSubscription` gains `Entitlements`; plan bodies take `Kind`, `Limits`, `SeatFeatureId` (and `Set(key, null)`); `ReportBillingUsageBody` takes `Feature` and `SubscriptionId`. `AtlasConstants.BillingProviders`.
- **Custom OAuth / OIDC providers.** `CustomOAuthProviders`: `ListAsync`, `GetAsync`, `CreateAsync` / `UpdateAsync` (`CustomOAuthProviderBody`), `DeleteAsync`, `DiscoverAsync`, `PreviewProfileMappingAsync` (ad hoc or by id), `ListSigningKeysAsync` / `RotateSigningKeyAsync` / `RetireSigningKeyAsync`. SSO connections gain `PreviewProfileMappingAsync` and the signing-key methods; their bodies take the relying-party settings and `SsoConnection` reports them plus `UpstreamUrls`.
- **Actions gallery.** `ActionGallery` (`ListAsync`, `GetAsync`, `InstallAsync`, `InstallsAsync`, `UpdateAsync`) and `ActionTemplates` (`ListAsync`, `CreateAsync` / `UpdateAsync` with `ActionTemplateBody`, `GetAsync`, `DeleteAsync`, `AddVersionAsync`, `SubmitVersionAsync`, `DeleteVersionAsync`, `PublishAsync`, `UnlistAsync`). `ActionTestResult.HttpRequests`; sample events take `Organizations` and `Country`.
- **Session custom data.** `Sessions.SetCustomDataAsync` (replace; null clears) and `UpdateCustomDataAsync` (merge; null removes a key); `Session.CustomData`, `CreateSessionBody.CustomData`. `AtlasConstants.WebhookEventSessionUpdated`.
- **Sign-in settings.** `InstanceSecurity.SetWalletRpcSecretAsync`; rate-limit `Rules` (`RateLimitRule`) plus the verify / LDAP budgets and the group catalog; `Instance.SetAccountLinkingTrustAsync`, `Instance.SetCaptchaConfigAsync`; `AtlasConstants.CaptchaProviders` (incl. `captchafox`, `vercel_botid`), `LinkingTrustLevels`.
- **Vercel BotID attestations.** `BotId.CreateAttestation(secret, isBot, isVerifiedBot, ttlSeconds, nowSeconds)` in Atlas's `atlas_botid.v1` format.
- **Enterprise SSO.** SAML Single Logout and encrypted-assertion fields on SSO bodies and `SsoConnection`; `SamlMetadata.SloUrl`; `SsoConnections.GetSamlEncryptionKeyAsync`, `CreateSamlEncryptionKeyAsync`, `RetirePreviousSamlEncryptionKeyAsync`, `DeleteSamlEncryptionKeyAsync`. Attack protection `DormantAccounts` (`DormantAccountsSettings`); risk weight / signal `DormantAccount`; `AtlasConstants.WebhookEventUserReactivated`, `EmailTemplateAccountReactivated`.
- **Atlas as OAuth provider.** OAuth clients: `AccessTokenFormat`, `TokenLifetimes` (`OAuthTokenLifetimes`); resource servers: `AccessTokenFormat`, refresh lifetimes, `IntrospectionClientIds`. `InstanceSecurity.GetSigningKeyProviderAsync` / `SetSigningKeyProviderAsync` / `RemoveSigningKeyProviderAsync` (AWS KMS, Google Cloud KMS, Azure Key Vault).
- **Opaque access tokens.** `AccessTokenVerifierOptions.Introspection` (`IntrospectionOptions`) validates non-JWT access tokens at Atlas's RFC 7662 endpoint with your API's client credentials, cached by token hash (TTL, negative TTL, never past `exp`, bounded); `IntrospectJwts` checks JWTs live too. `Introspector` is public. `UseAtlasAccessToken` uses it through the verifier it is given.
- **Device sign-in approval.** `new DeviceAuthorizations(frontendApi, sessionToken)`: `LookupAsync`, `ApproveAsync`, `DenyAsync` with the user's session token; `DeviceAuthorizationException` (`NeedsReverification`, `IsInvalidCode`); `FormatUserCode`, `UserCodeFromUrl`.
- The API coverage list now holds all 588 Backend and Platform API operations.

### Changed

- Without `Introspection`, a token that is not a JWT is refused with "configure Introspection to accept opaque tokens" instead of "malformed".

## 1.12.0 (Atlas.Sdk) / 1.4.0 (Atlas.AspNetCore) — 2026-10-23

### Added

- **App roles.** `AppRoles`: `ListAsync`, `CreateAsync`, `GetAsync`, `UpdateAsync`, `DeleteAsync`, `ListUsersAsync`, `GetForUserAsync`, `SetForUserAsync` (replace), `ChangeForUserAsync` (add/remove), `RemoveFromUserAsync`. `SessionClaims.AppRoles` / `AppPermissions` / `HasAppRole` / `HasAppPermission` and `ProtectCondition.AppRole` / `AppPermission` (no active organization needed).
- **Organization teams and custom roles.** `Organizations.Teams` (list, create, get, update, delete, members list/add/remove) and `Organizations.Roles` (the roles an org can assign; create/update/delete its own, `reassignTo`). `Organizations.GetBySlugAsync`, `CheckSlugAsync`, `Invitations.ResendAsync`.
- **User search and timed bans.** `ListUsersParams`: `Offset`, `Query`, `EmailAddress`, `Username`, `PhoneNumber`, `ExternalId`, `Banned`, `EmailVerified`, `OrganizationId`, `CreatedAfter` / `CreatedBefore`, `OrderBy`, `IncludeTotalCount`. `Users.BanAsync(id, reason, note, expiresInSeconds, expiresAt)`; `User.BanExpiresAt`.
- **API key controls.** `CreateApiKeyBody` `ExpiresInSeconds`, `Prefix`, `Enabled`, `RateLimit` (`ApiKeyRateLimit`), `Remaining`, `Scopes`; `UpdateApiKeyBody` `Enabled`, `RateLimit`, `Remaining`, `Scopes`. `ApiKeys.VerifyAsync(secret, requiredScopes)`; `ApiKeyVerification.Code` (`insufficient_scope`, `rate_limited`, `usage_exhausted`), `MissingScopes`, `RetryAfterMs`, `Remaining`. `ApiKey` reports `Enabled`, `RateLimit`, `Remaining`, `Scopes`.
- **Billing providers (bring your own key).** `Billing.ListProvidersAsync`, `GetProviderAsync`, `SetProviderAsync`, `TestProviderAsync`, `DeleteProviderAsync(force)`, `ChangeSubscriptionAsync`, `CancelSubscriptionAsync(immediately)`, `RestoreSubscriptionAsync`. Plans carry `Provider`, `ProviderPriceId`, `AnnualPriceId`, `AnnualAmount`; subscriptions the provider, account, interval, scheduled change, trial and cancellation fields.
- **OAuth server administration.** `InstanceSecurity.ListSigningKeysAsync` / `RotateSigningKeyAsync(alg, gracePeriodSeconds, emergency)`; `OAuthInitialAccessTokens` (list, create, revoke). OAuth client bodies take `Disabled`, `RequirePkce`, `SubjectType`, `SectorIdentifierUri`, `SecretExpiresInSeconds` and `OAuthClient` reports them (with `ClientSecretExpiresAt`); resource servers take and report `PublishResourceMetadata`.
- **Access-token verification for your API.** `AccessTokenVerifier` checks an Atlas-issued OAuth access token locally (issuer, expiry, `token_use`, audience, scopes) and returns the RFC 6750 / RFC 9728 `WWW-Authenticate` challenge on refusal; `BuildWwwAuthenticate`, `ProtectedResourceMetadata`, `ProtectedResourceMetadataPath`. Atlas.AspNetCore: `app.UseAtlasAccessToken(verifier, scopes)` and `HttpContext.GetAtlasAccessToken()`. DPoP-bound tokens are refused.
- `ScimTokens.UpdateAsync`, `UserHooks.ListBeforeUserCreateCallsAsync`, `Referrals.GetCallAsync`.

### Changed

- Session and logout-token verification accept **ES256** as well as RS256 (`SigningAlgs.All`), and nothing else.

## 1.11.0 (Atlas.Sdk)

### Added

- Referral settings carry browser capture: `browser_sources` (the referral programs whose script or cookie the browser reads; null on update resets to the provider's default), `browser_sources_default`, and `consent_provider` (`auto`, one consent manager, or `app`).

## 1.10.0 (Atlas.Sdk)

### Added

- **Two-way referral sync.** Referral settings carry two-way sync: `inbound` (`url`, `secret_set`, `endpoint_id`, `last_event`) on reads, write-only `inbound_secret` and `inbound_endpoint_id` on updates (null clears), and provider capability `inbound`.

## 1.9.0 (Atlas.Sdk)

### Added

- **Referral attribution v2.** `Referrals.RetryFailedAsync(kind, errorCode, idempotencyKey)`
  (`POST /v1/instance/referrals/calls/retry_failed`, `ReferralRetryFailedResult` with `Requeued`),
  `Referrals.SimulateAsync(code)` (`POST /v1/instance/referrals/simulate`, `ReferralSimulateResult`: `CallId`,
  `CustomerRef`, `Result`, `Response`), `Referrals.BackfillAsync(ReferralBackfillBody, idempotencyKey)` and
  `Referrals.GetBackfillAsync(id)` (`POST /v1/instance/referrals/backfill`, `GET /v1/instance/referrals/backfill/{id}`;
  `ReferralBackfillBody`: `Since`, `Codes`, `CodeMetadataPath`; `ReferralBackfillJob`: `UsersScanned`, `UsersSkipped`,
  `CallsQueued`, `Truncated`, `NextSince`, `Calls`), and `Referrals.GetForUserAsync(userId)`
  (`GET /v1/users/{id}/referral`, `UserReferralResult`).
- **Settings.** `ReferralSettings` gains `TrackingToken`, `CodeLookupUrl`, `SignupCodeField`, `ForwardLifecycle`,
  `MirrorToPublicMetadata` and `IncludeReferralClaims`; `UpdateReferralSettingsBody` gains the same (`Clear` takes
  `tracking_token` and `code_lookup_url`). `Provider` can be `referally`. `ReferralProviderCapabilities` gains `Alias`,
  `User` and `Ping`. `ReferralConnectionStatus` gains `LastPing` (`ReferralPing`) and `Alert` (`ReferralAlert`:
  `signature_rejected` or `connection_paused`). `ReferralConnectionTest` gains `Method` (`ping` or `probe`), `Ping` and
  `Warnings`.
- **Call log.** New `ListCallsAsync(ListReferralCallsQuery)` with `ErrorCode` and `StartingAfter`; `Limit` goes up to
  1000. `ReferralCall.Kind` adds `alias`, `user` and `ping`; calls gain `LatencyMs`, `ErrorCode`, `Response`,
  `Highlights` (`ReferralCallHighlights`), `RequestId` and `BatchId`.
- **Users.** `Users.SetExternalIdAsync(id, externalId)` (`PATCH /v1/users/{id}`; null clears; with referrals on, the
  change is forwarded to the platform as an alias). `UpdateUserBody` gains `ExternalId` and `ClearExternalId()`;
  `User` gains `ExternalId`. `UserReferral` gains `PartnerExternalRef`, `ReferralStatus` and `Notes`. The new
  `user.referral_resolved` webhook carries the full user as `data` (deserialize it as `User`).
- **before_user_create hook.** New `UserHooks` namespace: `GetBeforeUserCreateAsync()`,
  `UpdateBeforeUserCreateAsync(UpdateBeforeUserCreateHookBody, idempotencyKey)` and `TestBeforeUserCreateAsync()`
  (`/v1/instance/hooks/before_user_create`; `BeforeUserCreateHook`, `BeforeUserCreateHookTest`). The hook call is
  signed like a webhook with type `user.before_create`.
- **Preflight** can report `referrals_secret_invalid`, `referrals_program_inactive`, `referrals_unreachable`,
  `referrals_clock_skew` and `referrals_external_id_unset`.

### Changed

- On the server, `User.Referral.Status` is now where attribution stands (`pending`, `attributed`, `none`, `failed`,
  `held`, `flagged`); the platform's own status moved to `ReferralStatus`. `user.created` carries `status: "pending"`
  while the platform has not answered.

## 1.8.0 (Atlas.Sdk)

### Added

- **Referral attribution settings.** New `Referrals` namespace: `GetAsync()` and `UpdateAsync(UpdateReferralSettingsBody,
  idempotencyKey)` (`GET`/`PATCH /v1/instance/referrals`; `ReferralSettings` with `Connection`, `Providers` and
  `Security` as `ReferralSecuritySettings`; `UpdateReferralSecurityBody` on write; `Clear(key)` sends a nullable field
  as null), `TestAsync()` (`ReferralConnectionTest`), `ListCallsAsync(limit, status, kind, userId)` (`ReferralCall`,
  `ReferralRisk`) and `RetryCallAsync(id, idempotencyKey)`.
- **Users.** `User` gains `Referral` (`UserReferral`: `PartnerId`, `ReferrerExternalId`, `ReferrerUserId`,
  `MatchedVia`, `Status`, `RiskLevel`, `Flagged`), `ReferralCode` and `ReferralLink`. `CreateUserBody` gains
  `Referral` (`CreateUserReferral`: `Code`, `ClickId`, `AnonId`) and `ReferralCode`; `UpdateUserBody` gains
  `ReferralCode`, `ClearReferralCode()` and `Set(key, value)`. New `Users.FlagReferralAsync(id, reason,
  idempotencyKey)` (`UserReferralFlag`).
- **Webhooks.** `UserReferralAttributedEvent` types the `data` of the new `user.referral_attributed` event.
- **Error codes.** `ErrorCodes.ReferralsNotConfigured` (422) and `ErrorCodes.ReferralCodeTaken` (409). Preflight can
  report `referrals_enabled_unverified` and `referrals_without_bot_protection`.

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
