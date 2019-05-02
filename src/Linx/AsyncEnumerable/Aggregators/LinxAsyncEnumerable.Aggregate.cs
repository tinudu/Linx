namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        public static async Task<TAccumulate> Aggregate<TSource, TAccumulate>(
            this IAsyncEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                while (await ae.MoveNextAsync())
                    seed = accumulator(seed, ae.Current);
                return seed;
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}
