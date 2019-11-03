namespace Linx.Observable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxObservable
    {
        /// <summary>
        /// Aggregate to <see cref="List{T}"/>.
        /// </summary>
        public static async Task<List<T>> ToList<T>(this ILinxObservable<T> source, CancellationToken token)
        {
            return await source.Aggregate(new List<T>(), (a, c) =>
            {
                a.Add(c);
                return (a, true);
            }, token).ConfigureAwait(false);
        }
    }
}
