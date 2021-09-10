namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
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
            return Iterator();

            async IAsyncEnumerable<TResult> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                await foreach (var obj in source.WithCancellation(token).ConfigureAwait(false))
                    if (obj is TResult r)
                        yield return r;
            }
        }
    }
}
