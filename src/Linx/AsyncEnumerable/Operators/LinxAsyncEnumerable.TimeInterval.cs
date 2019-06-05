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

            return Produce<TimeInterval<T>>(async (yield, token) =>
            {
                var time = Time.Current;
                var t = time.Now;
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var now = time.Now;
                        var i = now - t;
                        t = now;
                        await yield(new TimeInterval<T>(i, ae.Current)).ConfigureAwait(false);
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
