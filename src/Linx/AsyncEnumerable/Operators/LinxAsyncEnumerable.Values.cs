namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Gets the values of the items that have a value.
        /// </summary>
        public static IAsyncEnumerable<T> Values<T>(this IAsyncEnumerable<T?> source) where T : struct
            => source.Where(x => x.HasValue).Select(x => x.GetValueOrDefault());

        /// <summary>
        /// Gets the values of the items that have a value.
        /// </summary>
        public static IAsyncEnumerable<T> Values<T>(this IAsyncEnumerable<Maybe<T>> source)
            => source.Where(x => x.HasValue).Select(x => x.GetValueOrDefault());
    }
}
