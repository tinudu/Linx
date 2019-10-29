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

            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                seed = accumulator(seed, item);
            return seed;
        }

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        public static async Task<TResult> Aggregate<TSource, TAccumulate, TResult>(
            this IAsyncEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            Func<TAccumulate, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            token.ThrowIfCancellationRequested();

            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                seed = accumulator(seed, item);
            return resultSelector(seed);
        }
    }
}
