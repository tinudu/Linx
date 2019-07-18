namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Timing;

    partial class LinxAsyncEnumerable
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
                var ae = source
                    .Select(Notification.Next)
                    .Append(Notification.Completed<T>())
                    .Timestamp()
                    .Buffer()
                    .WithCancellation(token)
                    .ConfigureAwait(false)
                    .GetAsyncEnumerator();
                try
                {
                    using (var timer = Time.Current.GetTimer(token))
                        while (await ae.MoveNextAsync())
                        {
                            var current = ae.Current;
                            await timer.Delay(current.Timestamp + delay).ConfigureAwait(false);
                            if (current.Value.Kind == NotificationKind.Completed) return;
                            if (!await yield(current.Value.Value).ConfigureAwait(false)) return;
                        }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
