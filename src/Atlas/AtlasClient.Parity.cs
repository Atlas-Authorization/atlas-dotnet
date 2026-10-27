using Atlas.Resources;

namespace Atlas
{
    public sealed partial class AtlasClient
    {
        private AppCatalogResource? _appCatalog;
        /// <summary>The <c>appCatalog</c> operations.</summary>
        public AppCatalogResource AppCatalog => _appCatalog ??= new AppCatalogResource(_transport);

        private LdapServerClientsResource? _ldapServerClients;
        /// <summary>The <c>ldapServerClients</c> operations.</summary>
        public LdapServerClientsResource LdapServerClients => _ldapServerClients ??= new LdapServerClientsResource(_transport);

        private JourneysResource? _journeys;
        /// <summary>The <c>journeys</c> operations.</summary>
        public JourneysResource Journeys => _journeys ??= new JourneysResource(_transport);

        private GroupsResource? _groups;
        /// <summary>The <c>groups</c> operations.</summary>
        public GroupsResource Groups => _groups ??= new GroupsResource(_transport);

        private MigrationsResource? _migrations;
        /// <summary>The <c>migrations</c> operations.</summary>
        public MigrationsResource Migrations => _migrations ??= new MigrationsResource(_transport);

        private ManagedGrantsResource? _managedGrants;
        /// <summary>The <c>managedGrants</c> operations.</summary>
        public ManagedGrantsResource ManagedGrants => _managedGrants ??= new ManagedGrantsResource(_transport);

        private LeastPrivilegeResource? _leastPrivilege;
        /// <summary>The <c>leastPrivilege</c> operations.</summary>
        public LeastPrivilegeResource LeastPrivilege => _leastPrivilege ??= new LeastPrivilegeResource(_transport);

        private DeliveryHealthResource? _deliveryHealth;
        /// <summary>The <c>deliveryHealth</c> operations.</summary>
        public DeliveryHealthResource DeliveryHealth => _deliveryHealth ??= new DeliveryHealthResource(_transport);

        private FlagsResource? _flags;
        /// <summary>The <c>flags</c> operations.</summary>
        public FlagsResource Flags => _flags ??= new FlagsResource(_transport);

        private MfaFactorsResource? _mfaFactors;
        /// <summary>The <c>mfaFactors</c> operations.</summary>
        public MfaFactorsResource MfaFactors => _mfaFactors ??= new MfaFactorsResource(_transport);

        private MarketingResource? _marketing;
        /// <summary>The <c>marketing</c> operations.</summary>
        public MarketingResource Marketing => _marketing ??= new MarketingResource(_transport);

        private AuditEventSchemasResource? _auditEventSchemas;
        /// <summary>The <c>auditEventSchemas</c> operations.</summary>
        public AuditEventSchemasResource AuditEventSchemas => _auditEventSchemas ??= new AuditEventSchemasResource(_transport);

        private DirectoryPullSourcesResource? _directoryPullSources;
        /// <summary>The <c>directoryPullSources</c> operations.</summary>
        public DirectoryPullSourcesResource DirectoryPullSources => _directoryPullSources ??= new DirectoryPullSourcesResource(_transport);

        private RelayAgentsResource? _relayAgents;
        /// <summary>The <c>relayAgents</c> operations.</summary>
        public RelayAgentsResource RelayAgents => _relayAgents ??= new RelayAgentsResource(_transport);

        private OAuthClientProfilesResource? _oAuthClientProfiles;
        /// <summary>The <c>oauthClientProfiles</c> operations.</summary>
        public OAuthClientProfilesResource OAuthClientProfiles => _oAuthClientProfiles ??= new OAuthClientProfilesResource(_transport);

        private PasskeyApiResource? _passkeyApi;
        /// <summary>The <c>passkeyApi</c> operations.</summary>
        public PasskeyApiResource PasskeyApi => _passkeyApi ??= new PasskeyApiResource(_transport);

        private VcResource? _vc;
        /// <summary>The <c>vc</c> operations.</summary>
        public VcResource Vc => _vc ??= new VcResource(_transport);

        private ListenResource? _listen;
        /// <summary>The <c>listen</c> operations.</summary>
        public ListenResource Listen => _listen ??= new ListenResource(_transport);

        private AgentRegistrationResource? _agentRegistration;
        /// <summary>The <c>agentRegistration</c> operations.</summary>
        public AgentRegistrationResource AgentRegistration => _agentRegistration ??= new AgentRegistrationResource(_transport);

        private AgentsResource? _agents;
        /// <summary>The <c>agents</c> operations.</summary>
        public AgentsResource Agents => _agents ??= new AgentsResource(_transport);

        private AgentAccessRequestsResource? _agentAccessRequests;
        /// <summary>The <c>agentAccessRequests</c> operations.</summary>
        public AgentAccessRequestsResource AgentAccessRequests => _agentAccessRequests ??= new AgentAccessRequestsResource(_transport);

        private WorkflowsResource? _workflows;
        /// <summary>The <c>workflows</c> operations.</summary>
        public WorkflowsResource Workflows => _workflows ??= new WorkflowsResource(_transport);

        private PageTemplatesResource? _pageTemplates;
        /// <summary>The <c>pageTemplates</c> operations.</summary>
        public PageTemplatesResource PageTemplates => _pageTemplates ??= new PageTemplatesResource(_transport);

        private ServiceConnectorsResource? _serviceConnectors;
        /// <summary>The <c>serviceConnectors</c> operations.</summary>
        public ServiceConnectorsResource ServiceConnectors => _serviceConnectors ??= new ServiceConnectorsResource(_transport);

        private DesktopMfaResource? _desktopMfa;
        /// <summary>The <c>desktopMfa</c> operations.</summary>
        public DesktopMfaResource DesktopMfa => _desktopMfa ??= new DesktopMfaResource(_transport);

        private IdentityVerificationResource? _identityVerification;
        /// <summary>The <c>identityVerification</c> operations.</summary>
        public IdentityVerificationResource IdentityVerification => _identityVerification ??= new IdentityVerificationResource(_transport);

        private IgaResource? _iga;
        /// <summary>The <c>iga</c> operations.</summary>
        public IgaResource Iga => _iga ??= new IgaResource(_transport);

        private ScimProvisioningPresetsResource? _scimProvisioningPresets;
        /// <summary>The <c>scimProvisioningPresets</c> operations.</summary>
        public ScimProvisioningPresetsResource ScimProvisioningPresets => _scimProvisioningPresets ??= new ScimProvisioningPresetsResource(_transport);

        private VaultResource? _vault;
        /// <summary>The <c>vault</c> operations.</summary>
        public VaultResource Vault => _vault ??= new VaultResource(_transport);

        private ConnectionsResource? _connections;
        /// <summary>The <c>connections</c> operations.</summary>
        public ConnectionsResource Connections => _connections ??= new ConnectionsResource(_transport);

        private SsfResource? _ssf;
        /// <summary>The <c>ssf</c> operations.</summary>
        public SsfResource Ssf => _ssf ??= new SsfResource(_transport);

        private NetworkZonesResource? _networkZones;
        /// <summary>The <c>networkZones</c> operations.</summary>
        public NetworkZonesResource NetworkZones => _networkZones ??= new NetworkZonesResource(_transport);

        private AccessPoliciesResource? _accessPolicies;
        /// <summary>The <c>accessPolicies</c> operations.</summary>
        public AccessPoliciesResource AccessPolicies => _accessPolicies ??= new AccessPoliciesResource(_transport);

        private AbacResource? _abac;
        /// <summary>The <c>abac</c> operations.</summary>
        public AbacResource Abac => _abac ??= new AbacResource(_transport);

        private ResourcesResource? _resources;
        /// <summary>The <c>resources</c> operations.</summary>
        public ResourcesResource Resources => _resources ??= new ResourcesResource(_transport);

        private OrgRoleGrantsResource? _orgRoleGrants;
        /// <summary>The <c>orgRoleGrants</c> operations.</summary>
        public OrgRoleGrantsResource OrgRoleGrants => _orgRoleGrants ??= new OrgRoleGrantsResource(_transport);

        private DevicesResource? _devices;
        /// <summary>The <c>devices</c> operations.</summary>
        public DevicesResource Devices => _devices ??= new DevicesResource(_transport);

        private AuthzDecisionsResource? _authzDecisions;
        /// <summary>The <c>authzDecisions</c> operations.</summary>
        public AuthzDecisionsResource AuthzDecisions => _authzDecisions ??= new AuthzDecisionsResource(_transport);

        private PolicyBundlesResource? _policyBundles;
        /// <summary>The <c>policyBundles</c> operations.</summary>
        public PolicyBundlesResource PolicyBundles => _policyBundles ??= new PolicyBundlesResource(_transport);

        private ProxyAppsResource? _proxyApps;
        /// <summary>The <c>proxyApps</c> operations.</summary>
        public ProxyAppsResource ProxyApps => _proxyApps ??= new ProxyAppsResource(_transport);

        private McpGatewaysResource? _mcpGateways;
        /// <summary>The <c>mcpGateways</c> operations.</summary>
        public McpGatewaysResource McpGateways => _mcpGateways ??= new McpGatewaysResource(_transport);

    }
}
