﻿namespace Linx.Reactive
{
    using System;
    using Timing;

    partial class LinxReactive
    {
        /// <summary>
        /// Records the time interval between consecutive values.
        /// </summary>
        public static IAsyncEnumerableObs<TimeInterval<T>> TimeInterval<T>(this IAsyncEnumerableObs<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<TimeInterval<T>>(async (yield, token) =>
            {
                var time = Time.Current;
                var t = time.Now;
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var now = time.Now;
                        var i = now - t;
                        t = now;
                        await yield(new TimeInterval<T>(i, ae.Current));
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
