using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas
{
    /// <summary>
    /// Cursor-pagination helpers. Walk every page of a cursor-paginated BAPI list
    /// (<c>users.list</c>, <c>organizations.list</c>, <c>invitations.list</c>,
    /// <c>auditLogs.list</c>, <c>importExport.listJobs</c>, …), yielding items one
    /// at a time so a caller opts into the extra round trips explicitly.
    ///
    /// <code>
    /// await foreach (var user in AtlasPagination.PaginateAsync(
    ///     (cursor, ct) => client.Users.ListAsync(new ListUsersParams { StartingAfter = cursor }, ct)))
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </summary>
    public static class AtlasPagination
    {
        /// <summary>
        /// Yield every item across every cursor page. <paramref name="fetchPage"/>
        /// receives the <c>starting_after</c> cursor (null for the first page) and
        /// returns that page.
        /// </summary>
        public static async IAsyncEnumerable<T> PaginateAsync<T>(
            Func<string?, CancellationToken, Task<CursorPage<T>>> fetchPage,
            string? startingAfter = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var cursor = startingAfter;
            while (true)
            {
                var page = await fetchPage(cursor, cancellationToken).ConfigureAwait(false);
                foreach (var item in page.Data)
                {
                    yield return item;
                }
                if (!page.HasMore || string.IsNullOrEmpty(page.NextCursor))
                {
                    yield break;
                }
                cursor = page.NextCursor;
            }
        }

        /// <summary>Collect every page of a cursor-paginated list into a single list.</summary>
        public static async Task<List<T>> CollectAsync<T>(
            Func<string?, CancellationToken, Task<CursorPage<T>>> fetchPage,
            string? startingAfter = null,
            CancellationToken cancellationToken = default)
        {
            var output = new List<T>();
            await foreach (var item in PaginateAsync(fetchPage, startingAfter, cancellationToken).ConfigureAwait(false))
            {
                output.Add(item);
            }
            return output;
        }
    }
}
