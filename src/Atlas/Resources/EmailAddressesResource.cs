using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{

    /// <summary>The <c>/v1/email_addresses</c> resource.</summary>
    public sealed class EmailAddressesResource : ResourceBase
    {
        public EmailAddressesResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>POST /v1/email_addresses/{eid}/verify</c> — Mark an address verified — flat Clerk-shape alias (user derived from the address).</summary>
        public Task<EmailAddress> VerifyAsync(string eid, CancellationToken ct = default)
            => Req<EmailAddress>(HttpVerb.Post, $"/v1/email_addresses/{Enc(eid)}/verify", cancellationToken: ct);

        /// <summary><c>POST /v1/email_addresses/{eid}/primary</c> — Make a verified address primary — flat Clerk-shape alias (user derived from the address).</summary>
        public Task<EmailAddress> SetPrimaryAsync(string eid, CancellationToken ct = default)
            => Req<EmailAddress>(HttpVerb.Post, $"/v1/email_addresses/{Enc(eid)}/primary", cancellationToken: ct);
    }
}
