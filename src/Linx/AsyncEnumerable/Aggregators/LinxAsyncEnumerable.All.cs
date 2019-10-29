namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Determines whether all elements of a sequence satisfy a condition.
        /// </summary>
        public static async Task<bool> All<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            token.ThrowIfCancellationRequested();

            await foreach (var item in source.ConfigureAwait(false).WithCancellation(token))
                if (!predicate(item))
                    return false;
            return true;
        }
    }
}
