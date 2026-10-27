using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Operations of <c>ssf</c>.</summary>
    public sealed partial class SsfResource : ResourceBase
    {
        public SsfResource(AtlasTransport t) : base(t) { }

        private SsfStreamsResource? _streams;
        /// <summary>The <c>ssf.streams</c> operations.</summary>
        public SsfStreamsResource Streams => _streams ??= new SsfStreamsResource(Transport);

        private SsfReceiversResource? _receivers;
        /// <summary>The <c>ssf.receivers</c> operations.</summary>
        public SsfReceiversResource Receivers => _receivers ??= new SsfReceiversResource(Transport);

    }
}
