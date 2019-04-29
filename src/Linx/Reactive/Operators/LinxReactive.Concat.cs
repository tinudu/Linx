namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;

    partial class LinxReactive
    {
        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerableObs<T> Concat<T>(this IAsyncEnumerableObs<IAsyncEnumerableObs<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            return Produce<T>(async (yield, token) =>
            {
                var aeOuter = sources.GetAsyncEnumerator(token);
                try
                {
                    while (await aeOuter.MoveNextAsync())
                        await aeOuter.Current.CopyTo(yield, token).ConfigureAwait(false);
                }
                finally { await aeOuter.DisposeAsync().ConfigureAwait(false); }
            });
        }

        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerableObs<T> Concat<T>(this IEnumerable<IAsyncEnumerableObs<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            return Produce<T>(async (yield, token) =>
            {
                foreach (var source in sources)
                    await source.CopyTo(yield, token).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerableObs<T> Concat<T>(this IAsyncEnumerableObs<T> first, IAsyncEnumerableObs<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            return new[] { first, second }.Concat();
        }

        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerableObs<T> Concat<T>(params IAsyncEnumerableObs<T>[] sources) => sources.Concat();
    }
}
