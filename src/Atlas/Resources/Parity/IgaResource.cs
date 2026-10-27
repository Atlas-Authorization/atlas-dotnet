using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Operations of <c>iga</c>.</summary>
    public sealed partial class IgaResource : ResourceBase
    {
        public IgaResource(AtlasTransport t) : base(t) { }

        private IgaEntitlementsResource? _entitlements;
        /// <summary>The <c>iga.entitlements</c> operations.</summary>
        public IgaEntitlementsResource Entitlements => _entitlements ??= new IgaEntitlementsResource(Transport);

        private IgaRequestsResource? _requests;
        /// <summary>The <c>iga.requests</c> operations.</summary>
        public IgaRequestsResource Requests => _requests ??= new IgaRequestsResource(Transport);

        private IgaGrantsResource? _grants;
        /// <summary>The <c>iga.grants</c> operations.</summary>
        public IgaGrantsResource Grants => _grants ??= new IgaGrantsResource(Transport);

        private IgaDelegationsResource? _delegations;
        /// <summary>The <c>iga.delegations</c> operations.</summary>
        public IgaDelegationsResource Delegations => _delegations ??= new IgaDelegationsResource(Transport);

        private IgaSodResource? _sod;
        /// <summary>The <c>iga.sod</c> operations.</summary>
        public IgaSodResource Sod => _sod ??= new IgaSodResource(Transport);

        private IgaLifecycleResource? _lifecycle;
        /// <summary>The <c>iga.lifecycle</c> operations.</summary>
        public IgaLifecycleResource Lifecycle => _lifecycle ??= new IgaLifecycleResource(Transport);

        private IgaCampaignsResource? _campaigns;
        /// <summary>The <c>iga.campaigns</c> operations.</summary>
        public IgaCampaignsResource Campaigns => _campaigns ??= new IgaCampaignsResource(Transport);

    }
}
