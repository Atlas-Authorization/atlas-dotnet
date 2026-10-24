using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Resources;

namespace Atlas
{
    /// <summary>
    /// The typed management client for the Atlas Backend API — the secret-key
    /// surface, the .NET peer of the TypeScript SDK's <c>createAtlasClient</c> and
    /// the Python SDK's <c>AtlasClient</c>.
    ///
    /// Each namespace is one property handing the shared, config-bound
    /// <see cref="AtlasTransport"/> to a resource class, so this file reads as a
    /// table of contents you can scan top to bottom to see the whole surface.
    ///
    /// <code>
    /// using var atlas = new AtlasClient("sk_live_...");
    /// var user = await atlas.Users.GetAsync("user_123");
    /// </code>
    /// </summary>
    public sealed class AtlasClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly bool _ownsHttp;

        private readonly AtlasClientOptions _options;
        private readonly AtlasTransport _transport;
        private readonly AtlasTransport _platformTransport;

        public AtlasClient(AtlasClientOptions options)
            : this(options, null) { }

        private AtlasClient(AtlasClientOptions options, HttpClient? shared)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.SecretKey) && string.IsNullOrEmpty(options.PlatformKey))
                throw new ArgumentException("AtlasClient requires a SecretKey (sk_...) or a PlatformKey (psk_...).", nameof(options));

            _options = options.Copy();
            _ownsHttp = shared == null && options.HttpClient == null;
            _http = shared ?? options.HttpClient ?? new HttpClient();
            var transport = new AtlasTransport(options.SecretKey ?? "", options.ApiUrl, _http, options.Strict,
                options.OnUnknownFields,
                "This call needs an instance secret key: set AtlasClientOptions.SecretKey (sk_...).",
                options.DryRun, options.Traceparent, options.SecretRecipient);
            var platformTransport = new AtlasTransport(options.PlatformKey ?? "", options.ApiUrl, _http, options.Strict,
                options.OnUnknownFields,
                "Platform calls need a platform key: set AtlasClientOptions.PlatformKey (psk_...).",
                options.DryRun, options.Traceparent, options.SecretRecipient);
            _transport = transport;
            _platformTransport = platformTransport;

            Users = new UsersResource(transport);
            Sessions = new SessionsResource(transport);
            Organizations = new OrganizationsResource(transport);
            Roles = new RolesResource(transport);
            Permissions = new PermissionsResource(transport);
            OAuthClients = new OAuthClientsResource(transport);
            ResourceServers = new ResourceServersResource(transport);
            AppRoles = new AppRolesResource(transport);
            OAuthInitialAccessTokens = new OAuthInitialAccessTokensResource(transport);
            SsoConnections = new SsoConnectionsResource(transport);
            ScimTokens = new ScimTokensResource(transport);
            Domains = new DomainsResource(transport);
            Waitlist = new WaitlistResource(transport);
            Allowlist = new RestrictionResource(transport, "/v1/allowlist_identifiers");
            Blocklist = new RestrictionResource(transport, "/v1/blocklist_identifiers");
            AttackProtection = new AttackProtectionResource(transport);
            ActorTokens = new ActorTokensResource(transport);
            Invitations = new InvitationsResource(transport);
            Webhooks = new WebhooksResource(transport);
            SignInTokens = new SignInTokensResource(transport);
            AuditLogs = new AuditLogsResource(transport);
            JwtTemplates = new JwtTemplatesResource(transport);
            ApiKeys = new ApiKeysResource(transport);
            OAuthProviders = new OAuthProvidersResource(transport);
            CustomOAuthProviders = new CustomOAuthProvidersResource(transport);
            SsoOnboarding = new SsoOnboardingResource(transport);
            ScimProvisioning = new ScimProvisioningResource(transport);
            Fga = new FgaResource(transport);
            RateLimitPolicy = new RateLimitPolicyResource(transport);
            RiskBasedMfa = new RiskBasedMfaResource(transport);
            BotSignals = new BotSignalsResource(transport);
            NetworkAcls = new NetworkAclsResource(transport);
            ManagedWaf = new ManagedWafResource(transport);
            LogStreams = new LogStreamsResource(transport);
            LogoutNotifyEndpoints = new LogoutNotifyEndpointsResource(transport);
            Branding = new BrandingResource(transport);
            EmailTemplates = new EmailTemplatesResource(transport);
            SmsTemplates = new SmsTemplatesResource(transport);
            Localizations = new LocalizationsResource(transport);
            Actions = new ActionsResource(transport);
            ActionGallery = new ActionGalleryResource(transport);
            ActionTemplates = new ActionTemplatesResource(transport);
            Billing = new BillingResource(transport);
            Messaging = new MessagingResource(transport);
            ImportExport = new ImportExportResource(transport);
            DataSubjectRequests = new DataSubjectRequestsResource(transport);
            RadiusClients = new RadiusClientsResource(transport);
            LtiPlatforms = new LtiPlatformsResource(transport);
            Instance = new InstanceResource(transport);
            InstanceSecurity = new InstanceSecurityResource(transport);
            Referrals = new ReferralsResource(transport);
            Policies = new PoliciesResource(transport);
            PolicySource = new PolicySourceResource(transport);
            UserHooks = new UserHooksResource(transport);
            Tokens = new TokensResource(transport);
            Design = new DesignResource(transport);
            Platform = new PlatformResource(platformTransport);
            EmailAddresses = new EmailAddressesResource(transport);
            Tickets = new TicketsResource(transport);
            OrganizationSettingsSchema = new OrganizationSettingsSchemaResource(transport);
            Notifications = new NotificationsResource(transport);
            Analytics = new AnalyticsResource(transport);
            LegacyAuthConnection = new LegacyAuthConnectionResource(transport);
            BetterAuthImports = new BetterAuthImportsResource(transport);
            BetterAuthBridge = new BetterAuthBridgeResource(transport);
            Mcp = new McpResource(transport);
            Pca = new PcaResource(transport);
            McpConnector = new McpConnectorResource(transport);
            OrgLdapConnections = new OrgLdapConnectionsResource(transport);
            SessionExchangeApps = new SessionExchangeAppsResource(transport);
            Diagnostics = new DiagnosticsResource(transport);
            ConfigChangelog = new ConfigChangelogResource(transport);
            RedirectUrls = new RedirectUrlsResource(transport);
            Machines = new MachinesResource(transport);
            EntitlementOverrides = new EntitlementOverridesResource(transport);
            Connectors = new ConnectorsResource(transport);
            AuditEvents = new AuditEventsResource(transport);
            Testing = new TestingResource(transport);
            OpenFga = new OpenFgaResource(transport);
            Captcha = new CaptchaResource(transport);
            SecretKeys = new SecretKeysResource(transport);
            Keys = new KeysResource(transport);
            PartnerLinks = new PartnerLinksResource(transport);
            AtlasIdGrants = new AtlasIdGrantsResource(transport);
            Privacy = new PrivacyResource(transport);
            Integrations = new IntegrationsResource(transport);
            Entitlements = new EntitlementsResource(transport);
            Notices = new NoticesResource(transport);
            SecurityAdvisories = new SecurityAdvisoriesResource(transport);
            SubProcessors = new SubProcessorsResource(transport);
            AuditExports = new AuditExportsResource(transport);
            Status = new StatusResource(transport);
            Delivery = new DeliveryResource(transport);
            Whoami = new WhoamiResource(transport);
        }

        /// <summary>
        /// A client that sends <c>Atlas-Strict: &lt;strict&gt;</c> on every write — a per-call
        /// override of <see cref="AtlasClientOptions.Strict"/>. It shares this client's
        /// <see cref="HttpClient"/>; disposing it does not dispose that.
        /// <code>
        /// await atlas.WithStrict(true).Users.UpdateAsync("user_123", body);
        /// </code>
        /// </summary>
        public AtlasClient WithStrict(bool strict)
        {
            var o = _options.Copy();
            o.Strict = strict;
            return new AtlasClient(o, _http);
        }

        /// <summary>
        /// A client that sends every write as a dry run (<c>?dry_run=true</c>): Atlas validates and answers with the
        /// changes it would make, nothing changes. Shares this client's <see cref="HttpClient"/>.
        /// <code>
        /// var plan = await atlas.DryRunAsync(HttpVerb.Patch, "/v1/platform/apps/ins_1", new { name = "New" }, platform: true);
        /// </code>
        /// </summary>
        public AtlasClient WithDryRun(bool dryRun = true)
        {
            var o = _options.Copy();
            o.DryRun = dryRun;
            return new AtlasClient(o, _http);
        }

        /// <summary>A client that sends a W3C <c>traceparent</c> on every call (see <see cref="AtlasTrace.NewTraceparent"/>).</summary>
        public AtlasClient WithTraceparent(string? traceparent)
        {
            var o = _options.Copy();
            o.Traceparent = traceparent;
            return new AtlasClient(o, _http);
        }

        /// <summary>A client whose shown-once secrets come back sealed to <paramref name="recipient"/> (<c>x25519:…</c>).</summary>
        public AtlasClient WithSecretRecipient(string? recipient)
        {
            var o = _options.Copy();
            o.SecretRecipient = recipient;
            return new AtlasClient(o, _http);
        }

        /// <summary>
        /// Send one write as a dry run and return the plan (<c>POST</c>, <c>PUT</c>, <c>PATCH</c> or <c>DELETE</c>).
        /// <paramref name="platform"/> sends it with the platform key.
        /// </summary>
        public async Task<DryRunResult> DryRunAsync(HttpVerb method, string path, object? body = null, bool platform = false, CancellationToken ct = default)
        {
            if (method == HttpVerb.Get) throw new ArgumentException("A dry run is for writes (POST, PUT, PATCH, DELETE).", nameof(method));
            var t = (platform ? _platformTransport : _transport).WithDryRun(true);
            var result = await t.SendAsync<DryRunResult>(new RequestOptions { Method = method, Path = path, Body = body }, ct).ConfigureAwait(false);
            return result ?? new DryRunResult();
        }

        /// <summary>Convenience overload: just a secret key, default API URL.</summary>
        public AtlasClient(string secretKey)
            : this(new AtlasClientOptions { SecretKey = secretKey }) { }

        /// <summary><c>/v1/secret_keys</c>: scoped, expiring, rotatable instance secret keys.</summary>
        public SecretKeysResource SecretKeys { get; }
        /// <summary><c>/v1/keys</c>: every credential on the instance in one standard shape.</summary>
        public KeysResource Keys { get; }
        /// <summary><c>/v1/partner_links</c>: credentials granted to (and held for) an integrated partner service.</summary>
        public PartnerLinksResource PartnerLinks { get; }
        /// <summary><c>/v1/atlas_id_apps</c>: the Sign in with Atlas apps this instance manages, and their per-user grants.</summary>
        public AtlasIdGrantsResource AtlasIdGrants { get; }
        /// <summary>The data-rights executor protocol (<c>/v1/privacy</c>) and the instance's orchestrator connection.</summary>
        public PrivacyResource Privacy { get; }
        /// <summary>The integration-facing surface (<c>/v1/integrations/*</c>).</summary>
        public IntegrationsResource Integrations { get; }
        /// <summary><c>/v1/entitlements</c>: the standard entitlements document.</summary>
        public EntitlementsResource Entitlements { get; }
        /// <summary><c>/v1/instance/notices</c>: behaviour changes that affect this instance.</summary>
        public NoticesResource Notices { get; }
        /// <summary><c>/v1/security_advisories</c>.</summary>
        public SecurityAdvisoriesResource SecurityAdvisories { get; }
        /// <summary><c>/v1/instance/sub_processors</c>.</summary>
        public SubProcessorsResource SubProcessors { get; }
        /// <summary><c>/v1/audit_exports</c>: continuous audit-log forwarding.</summary>
        public AuditExportsResource AuditExports { get; }
        /// <summary>The service status, the instance's incident subscription and its status dependencies.</summary>
        public StatusResource Status { get; }
        /// <summary>Outbound delivery health (<c>/v1/delivery_health</c>) and the fixed egress addresses (<c>/v1/meta/egress_ips</c>).</summary>
        public DeliveryResource Delivery { get; }
        /// <summary><c>GET /v1/whoami</c>: what the key in use is and what it may do.</summary>
        public WhoamiResource Whoami { get; }

        public UsersResource Users { get; }
        public SessionsResource Sessions { get; }
        public OrganizationsResource Organizations { get; }
        public RolesResource Roles { get; }
        public PermissionsResource Permissions { get; }
        public OAuthClientsResource OAuthClients { get; }
        public ResourceServersResource ResourceServers { get; }
        /// <summary><c>/v1/app_roles</c> — instance-wide user roles.</summary>
        public AppRolesResource AppRoles { get; }
        /// <summary><c>/v1/oauth_initial_access_tokens</c> — OAuth dynamic client registration gating.</summary>
        public OAuthInitialAccessTokensResource OAuthInitialAccessTokens { get; }
        public SsoConnectionsResource SsoConnections { get; }
        public ScimTokensResource ScimTokens { get; }
        public DomainsResource Domains { get; }
        public WaitlistResource Waitlist { get; }
        public RestrictionResource Allowlist { get; }
        public RestrictionResource Blocklist { get; }
        public AttackProtectionResource AttackProtection { get; }
        public ActorTokensResource ActorTokens { get; }
        public InvitationsResource Invitations { get; }
        public WebhooksResource Webhooks { get; }
        public SignInTokensResource SignInTokens { get; }
        public AuditLogsResource AuditLogs { get; }
        public JwtTemplatesResource JwtTemplates { get; }
        public ApiKeysResource ApiKeys { get; }
        public OAuthProvidersResource OAuthProviders { get; }
        /// <summary>Your own upstream OAuth / OIDC login providers.</summary>
        public CustomOAuthProvidersResource CustomOAuthProviders { get; }
        public SsoOnboardingResource SsoOnboarding { get; }
        public ScimProvisioningResource ScimProvisioning { get; }
        public FgaResource Fga { get; }
        public RateLimitPolicyResource RateLimitPolicy { get; }
        public RiskBasedMfaResource RiskBasedMfa { get; }
        public BotSignalsResource BotSignals { get; }
        public NetworkAclsResource NetworkAcls { get; }
        public ManagedWafResource ManagedWaf { get; }
        public LogStreamsResource LogStreams { get; }
        /// <summary>Logout fan-out endpoints (<c>/v1/logout_notify_endpoints</c>).</summary>
        public LogoutNotifyEndpointsResource LogoutNotifyEndpoints { get; }
        public BrandingResource Branding { get; }
        public EmailTemplatesResource EmailTemplates { get; }
        public SmsTemplatesResource SmsTemplates { get; }
        public LocalizationsResource Localizations { get; }
        public ActionsResource Actions { get; }
        /// <summary>The community Actions gallery: browse, install and update templates.</summary>
        public ActionGalleryResource ActionGallery { get; }
        /// <summary>Action templates this instance publishes to the gallery.</summary>
        public ActionTemplatesResource ActionTemplates { get; }
        public BillingResource Billing { get; }
        public MessagingResource Messaging { get; }
        public ImportExportResource ImportExport { get; }
        public DataSubjectRequestsResource DataSubjectRequests { get; }
        public RadiusClientsResource RadiusClients { get; }
        public LtiPlatformsResource LtiPlatforms { get; }
        public InstanceResource Instance { get; }
        public InstanceSecurityResource InstanceSecurity { get; }
        /// <summary>Referral attribution: the instance's referral platform connection, connection test and call log.</summary>
        public ReferralsResource Referrals { get; }
        /// <summary>Policy acceptance: policies and versions, acceptances, a user's standing and the <c>pol</c> claim setting.</summary>
        public PoliciesResource Policies { get; }
        /// <summary>The external policy source: endpoint, sync mode, write-only secret, test, sync and delivery log.</summary>
        public PolicySourceResource PolicySource { get; }
        /// <summary>The <c>before_user_create</c> hook: settings and a test call.</summary>
        public UserHooksResource UserHooks { get; }
        public TokensResource Tokens { get; }
        public DesignResource Design { get; }
        public PlatformResource Platform { get; }
        public EmailAddressesResource EmailAddresses { get; }
        public TicketsResource Tickets { get; }
        public OrganizationSettingsSchemaResource OrganizationSettingsSchema { get; }
        public NotificationsResource Notifications { get; }
        public AnalyticsResource Analytics { get; }
        public LegacyAuthConnectionResource LegacyAuthConnection { get; }
        /// <summary>Migrate from Better Auth: sealed-bundle imports.</summary>
        public BetterAuthImportsResource BetterAuthImports { get; }
        /// <summary>Migrate from Better Auth: the session bridge settings and secret-key exchange.</summary>
        public BetterAuthBridgeResource BetterAuthBridge { get; }
        public McpResource Mcp { get; }
        public PcaResource Pca { get; }
        public McpConnectorResource McpConnector { get; }
        public OrgLdapConnectionsResource OrgLdapConnections { get; }
        public SessionExchangeAppsResource SessionExchangeApps { get; }
        public DiagnosticsResource Diagnostics { get; }
        public ConfigChangelogResource ConfigChangelog { get; }
        public RedirectUrlsResource RedirectUrls { get; }
        public MachinesResource Machines { get; }
        public EntitlementOverridesResource EntitlementOverrides { get; }
        public ConnectorsResource Connectors { get; }
        public AuditEventsResource AuditEvents { get; }
        public TestingResource Testing { get; }
        public OpenFgaResource OpenFga { get; }
        public CaptchaResource Captcha { get; }

        /// <summary>Dispose the underlying <see cref="HttpClient"/>, if this client created it.</summary>
        public void Dispose()
        {
            if (_ownsHttp) _http.Dispose();
        }
    }
}
