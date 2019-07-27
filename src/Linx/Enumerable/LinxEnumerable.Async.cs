namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using AsyncEnumerable;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Convert a <see cref="IEnumerable{T}"/> to a <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Async<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return LinxAsyncEnumerable.Generate<T>(async (yield, token) =>
            {
                foreach (var element in source)
                    if (!await yield(element).ConfigureAwait(false))
                        return;
            });
        }
    }
}
