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
        /// Skip items until the specified condition is true.
        /// </summary>
        public static IAsyncEnumerable<T> SkipUntil<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return Iterator();

            async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                while (true)
                {
                    if (!await ae.MoveNextAsync())
                        yield break;
                    var item = ae.Current;
                    if (!predicate(item)) continue;
                    yield return item;
                    break;
                }

                while (await ae.MoveNextAsync())
                    yield return ae.Current;
            }
        }
    }
}
