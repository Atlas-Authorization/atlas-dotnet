using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>More operations of <c>instance</c>.</summary>
    public sealed partial class InstanceResource
    {
        private InstanceIntegrationsResource? _integrations;
        /// <summary>The <c>instance.integrations</c> operations.</summary>
        public InstanceIntegrationsResource Integrations => _integrations ??= new InstanceIntegrationsResource(Transport);

    }
}
