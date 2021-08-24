namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Wraps a synchronous <see cref="IEnumerable{T}"/> into an <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<T>(async (yield, token) =>
            {
                foreach (var item in source)
                {
                    if (!await yield(item).ConfigureAwait(false))
                        return;
                }
            });
        }
    }
}
