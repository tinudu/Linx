namespace Linx.Reactive
{
    using System;
    using Timing;

    partial class LinxReactive
    {
        /// <summary>
        /// Indicates the sequence by <paramref name="delay"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Delay<T>(this IAsyncEnumerable<T> source, TimeSpan delay)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (delay <= TimeSpan.Zero) return source;

            return Produce<T>(async (yield, token) =>
            {
                var time = Time.Current;
                var ae = source
                    .Select(value => (Value: value, Due: time.Now + delay))
                    .Buffer()
                    .GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var (value, due) = ae.Current;
                        await time.Wait(due, token).ConfigureAwait(false);
                        await yield(value);
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
