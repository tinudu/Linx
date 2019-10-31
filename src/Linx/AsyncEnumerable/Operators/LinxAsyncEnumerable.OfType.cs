namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Filters the elements of an observable sequence based on the specified type.
        /// </summary>
        public static IAsyncEnumerable<TResult> OfType<TResult>(this IAsyncEnumerable<object> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var obj in source.WithCancellation(token).ConfigureAwait(false))
                    if (obj is TResult r)
                        yield return r;
            }
        }
    }
}
