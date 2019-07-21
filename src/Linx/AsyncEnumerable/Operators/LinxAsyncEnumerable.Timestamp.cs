namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Records the timestamp for each value.
        /// </summary>
        public static IAsyncEnumerable<Timestamped<T>> Timestamp<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Generate<Timestamped<T>>(async (yield, token) =>
            {
                var time = Time.Current;
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                        if(!await yield(new Timestamped<T>(time.Now, ae.Current)).ConfigureAwait(false))
                            return;
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
