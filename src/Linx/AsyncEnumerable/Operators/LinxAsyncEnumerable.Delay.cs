namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Notifications;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Indicates the sequence by <paramref name="delay"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Delay<T>(this IAsyncEnumerable<T> source, TimeSpan delay, bool delayError = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (delay <= TimeSpan.Zero) return source;

            var notifications = delayError ? source.Materialize() : source.Select(Notification.Next).Append(Notification.Completed<T>());
            var timestamped = notifications.Timestamp().Buffer();

            return Create<T>(async (yield, token) =>
            {
                var ae = timestamped.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    using (var timer = Time.Current.GetTimer(token))
                        while (await ae.MoveNextAsync())
                        {
                            var current = ae.Current;
                            await timer.Delay(current.Timestamp + delay).ConfigureAwait(false);
                            switch (current.Value.Kind)
                            {
                                case NotificationKind.Next:
                                    if (!await yield(current.Value.Value).ConfigureAwait(false)) return;
                                    break;
                                case NotificationKind.Completed:
                                    return;
                                case NotificationKind.Error:
                                    throw current.Value.Error;
                                default:
                                    throw new Exception(current.Value.Kind + "???");
                            }
                        }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
