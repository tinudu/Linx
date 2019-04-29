namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            return Produce<T>(async (yield, token) =>
            {
                var aeOuter = sources.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await aeOuter.MoveNextAsync())
                        await aeOuter.Current.CopyTo(yield, token).ConfigureAwait(false);
                }
                finally { await aeOuter.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Concat<T>(this IEnumerable<IAsyncEnumerable<T>> sources)
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
        public static IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            return new[] { first, second }.Concat();
        }

        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Concat<T>(params IAsyncEnumerable<T>[] sources) => sources.Concat();
    }
}
