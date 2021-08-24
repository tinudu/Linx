namespace Linx.Enumerable
{
    using System.Collections.Generic;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Convenience method to create a <see cref="LookAhead{T}"/>.
        /// </summary>
        public static LookAhead<T> LookAhead<T>(this IEnumerable<T> source) => new(source);
    }
}
