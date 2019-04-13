namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;

    partial class LinxReactive
    {
        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Concat<T>(this IEnumerable<IAsyncEnumerable<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            return Produce<T>(async (yield, token) =>
            {
                foreach (var source in sources)
                {
                    var ae = source.GetAsyncEnumerator(token);
                    try
                    {
                        while (await ae.MoveNextAsync())
                            await yield(ae.Current);
                    }
                    finally { await ae.DisposeAsync().ConfigureAwait(false); }
                }
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
