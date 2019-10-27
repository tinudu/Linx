namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Notifications;
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

            var notifications = source.Materialize().Timestamp().Buffer();
            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                using var timer = Time.Current.GetTimer(token);
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in notifications.WithCancellation(token).ConfigureAwait(false))
                {
                    await timer.Delay(item.Timestamp + delay).ConfigureAwait(false);
                    switch (item.Value.Kind)
                    {
                        case NotificationKind.Next:
                            yield return item.Value.Value;
                            break;
                        case NotificationKind.Error:
                            throw item.Value.Error;
                        case NotificationKind.Completed:
                            yield break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
