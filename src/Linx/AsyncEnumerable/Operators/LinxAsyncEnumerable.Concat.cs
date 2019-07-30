namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            return Create<T>(async (yield, token) =>
            {
                var aeOuter = sources.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await aeOuter.MoveNextAsync())
                        if (!await aeOuter.Current.CopyTo(yield, token).ConfigureAwait(false))
                            return;
                }
                finally { await aeOuter.DisposeAsync(); }
            }, "Concat");
        }

        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Concat<T>(this IEnumerable<IAsyncEnumerable<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            return Create<T>(async (yield, token) =>
            {
                foreach (var source in sources)
                    if (!await source.CopyTo(yield, token).ConfigureAwait(false))
                        return;
            }, "Concat");
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
        public static IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<T> source, params IAsyncEnumerable<T>[] sources)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            return sources.Prepend(source).Concat();
        }
    }
}
