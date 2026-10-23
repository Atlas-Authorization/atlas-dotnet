using System;
using System.Net.Http;
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
                "This call needs an instance secret key: set AtlasClientOptions.SecretKey (sk_...).");
            var platformTransport = new AtlasTransport(options.PlatformKey ?? "", options.ApiUrl, _http, options.Strict,
                options.OnUnknownFields,
                "Platform calls need a platform key: set AtlasClientOptions.PlatformKey (psk_...).");

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
            Billing = new BillingResource(transport);
            Messaging = new MessagingResource(transport);
            ImportExport = new ImportExportResource(transport);
            DataSubjectRequests = new DataSubjectRequestsResource(transport);
            RadiusClients = new RadiusClientsResource(transport);
            LtiPlatforms = new LtiPlatformsResource(transport);
            Instance = new InstanceResource(transport);
            InstanceSecurity = new InstanceSecurityResource(transport);
            Referrals = new ReferralsResource(transport);
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

        /// <summary>Convenience overload: just a secret key, default API URL.</summary>
        public AtlasClient(string secretKey)
            : this(new AtlasClientOptions { SecretKey = secretKey }) { }

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
