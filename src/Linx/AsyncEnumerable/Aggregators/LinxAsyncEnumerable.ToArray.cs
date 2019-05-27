namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Aggregate elements into an array.
        /// </summary>
        public static async Task<T[]> ToArray<T>(this IAsyncEnumerable<T> source, CancellationToken token) => (await source.ToList(token)).ToArray();
    }
}
