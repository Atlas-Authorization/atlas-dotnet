using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>
    /// Shared plumbing for every resource namespace: a config-bound
    /// <see cref="AtlasTransport"/> and terse request helpers so a resource method
    /// is a one-liner naming a verb, a path, and its shapes — the same shape the
    /// TypeScript and Python SDKs take.
    /// </summary>
    public abstract class ResourceBase
    {
        protected readonly AtlasTransport Transport;

        protected ResourceBase(AtlasTransport transport)
        {
            Transport = transport;
        }

        /// <summary>Percent-encode a single path segment (an id, slug, provider name).</summary>
        protected static string Enc(string segment) => Uri.EscapeDataString(segment);

        /// <summary>Build a query key/value list; null values are dropped at serialization.</summary>
        protected static IEnumerable<KeyValuePair<string, object?>> Q(params (string Key, object? Value)[] pairs)
        {
            var list = new List<KeyValuePair<string, object?>>(pairs.Length);
            foreach (var p in pairs)
            {
                list.Add(new KeyValuePair<string, object?>(p.Key, p.Value));
            }
            return list;
        }

        /// <summary>
        /// Build an inline request body from snake_case key/value pairs, DROPPING
        /// any whose value is null — the deterministic equivalent of the
        /// TypeScript SDK omitting an <c>undefined</c> field. (Keys are written
        /// verbatim, so pass them already snake_cased.)
        /// </summary>
        protected static Dictionary<string, object?> Body(params (string Key, object? Value)[] pairs)
        {
            var dict = new Dictionary<string, object?>(pairs.Length);
            foreach (var p in pairs)
            {
                if (p.Value != null) dict[p.Key] = p.Value;
            }
            return dict;
        }

        /// <summary>Issue a request expecting a body; returns the decoded value (non-null on 2xx).</summary>
        protected async Task<T> Req<T>(
            HttpVerb method,
            string path,
            object? body = null,
            IEnumerable<KeyValuePair<string, object?>>? query = null,
            string? idempotencyKey = null,
            bool raw = false,
            CancellationToken cancellationToken = default)
        {
            var result = await Transport.SendAsync<T>(
                new RequestOptions
                {
                    Method = method,
                    Path = path,
                    Body = body,
                    Query = query,
                    IdempotencyKey = idempotencyKey,
                    Raw = raw,
                },
                cancellationToken).ConfigureAwait(false);
            return result!;
        }

        /// <summary>Issue a request whose response body is ignored (e.g. a 204 delete).</summary>
        protected Task ReqVoid(
            HttpVerb method,
            string path,
            object? body = null,
            IEnumerable<KeyValuePair<string, object?>>? query = null,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            return Transport.SendAsync<object>(
                new RequestOptions
                {
                    Method = method,
                    Path = path,
                    Body = body,
                    Query = query,
                    IdempotencyKey = idempotencyKey,
                },
                cancellationToken);
        }
    }
}
