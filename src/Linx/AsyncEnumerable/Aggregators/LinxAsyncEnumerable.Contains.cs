namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Determines whether a sequence contains a specified element.
        /// </summary>
        public static async Task<bool> Contains<T>(this IAsyncEnumerable<T> source, T value, CancellationToken token, IEqualityComparer<T>? comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = EqualityComparer<T>.Default;
            token.ThrowIfCancellationRequested();

            await foreach(var item in source.WithCancellation(token).ConfigureAwait(false))
                if (comparer.Equals(item, value))
                    return true;
            return false;
        }
    }
}
