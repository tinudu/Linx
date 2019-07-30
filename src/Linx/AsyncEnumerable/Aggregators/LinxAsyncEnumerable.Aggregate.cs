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

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                while (await ae.MoveNextAsync())
                    seed = accumulator(seed, ae.Current);
                return resultSelector(seed);
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
        public static async Task<T> Aggregate<T>(
            this IAsyncEnumerable<T> source,
            Func<T, T, T> accumulator,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsNoElement);
                var seed = ae.Current;
                while (await ae.MoveNextAsync())
                    seed = accumulator(seed, ae.Current);
                return seed;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        public static async Task<Maybe<T>> AggregateMaybe<T>(
            this IAsyncEnumerable<T> source,
            Func<T, T, T> accumulator,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var seed = ae.Current;
                while (await ae.MoveNextAsync())
                    seed = accumulator(seed, ae.Current);
                return seed;
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}
