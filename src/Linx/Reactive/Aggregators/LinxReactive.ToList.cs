﻿namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Aggregate elements into a list.
        /// </summary>
        public static async Task<List<T>> ToList<T>(this IAsyncEnumerableObs<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                var list = new List<T>();
                while (await ae.MoveNextAsync())
                    list.Add(ae.Current);
                return list;
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }
    }
}