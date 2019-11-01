namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Skip the first <paramref name="count"/> items.
        /// </summary>
        public static IAsyncEnumerable<T> Skip<T>(this IAsyncEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return source;

            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                var skip = count;
                while (true)
                {
                    if(!await ae.MoveNextAsync())
                        yield break;
                    if (skip-- == 0)
                        break;
                }

                do yield return ae.Current;
                while (await ae.MoveNextAsync());
            }
        }
    }
}
