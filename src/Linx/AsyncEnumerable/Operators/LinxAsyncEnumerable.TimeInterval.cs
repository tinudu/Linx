namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Records the time interval between consecutive values.
        /// </summary>
        public static IAsyncEnumerable<TimeInterval<T>> TimeInterval<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create<TimeInterval<T>>(async (yield, token) =>
            {
                var time = Time.Current;
                var t0 = time.Now;
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var t = time.Now;
                        if (!await yield(new TimeInterval<T>(t - t0, ae.Current)).ConfigureAwait(false))
                            return;
                        t0 = t;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
