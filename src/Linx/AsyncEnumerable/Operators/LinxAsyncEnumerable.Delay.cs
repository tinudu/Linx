using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using global::Linx.Notifications;
using global::Linx.Timing;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Indicates the sequence by <paramref name="delay"/>.
    /// </summary>
    public static IAsyncEnumerable<T> Delay<T>(this IAsyncEnumerable<T> source, TimeSpan delay, ITime time)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (delay <= TimeSpan.Zero) return source;
        if (time is null) time = Time.RealTime;

        var notifications = source.Materialize().Timestamp(time).Buffer();

        return Iterator().Dematerialize();

        async IAsyncEnumerable<Notification<T>> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            using var timer = time.GetTimer(token);

            await foreach (var tn in notifications.WithCancellation(token).ConfigureAwait(false))
            {
                await timer.Delay(tn.Timestamp + delay).ConfigureAwait(false);
                yield return tn.Value;
            }
        }
    }
}