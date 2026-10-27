using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>Operations of <c>deliveryHealth</c>.</summary>
    public sealed partial class DeliveryHealthResource : ResourceBase
    {
        public DeliveryHealthResource(AtlasTransport t) : base(t) { }

        /// <summary><c>GET /v1/delivery_health/{kind}/{id}</c> - Delivery health of one target. 404 until a delivery to it has been recorded.</summary>
        /// <remarks>Needs the <c>webhooks:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string kind, string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/delivery_health/{Enc(kind)}/{Enc(id)}", cancellationToken: ct);
    }
}
