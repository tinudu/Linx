namespace Linx.Reactive
{
    using System;
    using Timing;

    partial class LinxReactive
    {
        /// <summary>
        /// Records the timestamp for each value.
        /// </summary>
        public static IAsyncEnumerable<Timestamped<T>> Timestamp<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<Timestamped<T>>(async (yield, token) =>
            {
                var time = Time.Current;
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                        await yield(new Timestamped<T>(time.Now, ae.Current));
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
