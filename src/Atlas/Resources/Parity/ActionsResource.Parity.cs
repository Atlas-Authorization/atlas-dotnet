using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>More operations of <c>actions</c>.</summary>
    public sealed partial class ActionsResource
    {
        /// <summary><c>GET /v1/actions/{id}/calls</c> - The call log of an `http` Action (an Action that calls your own endpoint), newest first, with 24 hour stats: calls, denied, failed, timeouts and p95 latency. Real calls and dry-run tests are both logged (`test`). 409 for a `node` Action.</summary>
        /// <remarks>Needs the <c>actions:read</c> scope.</remarks>
        public Task<JsonElement> CallsAsync(string id, long? limit = null, string? status = null, bool? test = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/actions/{Enc(id)}/calls", query: Q(("limit", limit), ("status", status), ("test", test), ("starting_after", startingAfter)), cancellationToken: ct);
    }
}
