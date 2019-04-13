namespace Linx.Enumerable
{
    using System.Collections.Generic;

    partial class LinxEnumerableExtensions
    {
        /// <summary>
        /// Conveniance method to create a <see cref="LookAhead{T}"/>.
        /// </summary>
        public static LookAhead<T> GetLookAhead<T>(this IEnumerable<T> source) => new LookAhead<T>(source);
    }
}
