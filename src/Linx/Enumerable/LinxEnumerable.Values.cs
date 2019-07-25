namespace Linx.Enumerable
{
    using System.Collections.Generic;
    using System.Linq;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Gets the values of the item that have a value.
        /// </summary>
        public static IEnumerable<T> Values<T>(this IEnumerable<T?> source) where T : struct
            => source.Where(x => x.HasValue).Select(x => x.GetValueOrDefault());

        /// <summary>
        /// Gets the values of the item that have a value.
        /// </summary>
        public static IEnumerable<T> Values<T>(this IEnumerable<Maybe<T>> source)
            => source.Where(x => x.HasValue).Select(x => x.GetValueOrDefault());
    }
}
