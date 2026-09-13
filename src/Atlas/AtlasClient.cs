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

        public AtlasClient(AtlasClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.SecretKey))
                throw new ArgumentException("AtlasClient requires a SecretKey.", nameof(options));

            _ownsHttp = options.HttpClient == null;
            _http = options.HttpClient ?? new HttpClient();
            var transport = new AtlasTransport(options.SecretKey, options.ApiUrl, _http);

            Users = new UsersResource(transport);
            Sessions = new SessionsResource(transport);
            Organizations = new OrganizationsResource(transport);
            Roles = new RolesResource(transport);
            Permissions = new PermissionsResource(transport);
            OAuthClients = new OAuthClientsResource(transport);
            ResourceServers = new ResourceServersResource(transport);
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
            Tokens = new TokensResource(transport);
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
        public TokensResource Tokens { get; }

        /// <summary>Dispose the underlying <see cref="HttpClient"/>, if this client created it.</summary>
        public void Dispose()
        {
            if (_ownsHttp) _http.Dispose();
        }
    }
}
