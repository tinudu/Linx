namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Dematerializes the explicit notification values of an observable sequence as implicit notifications.
        /// </summary>
        public static IAsyncEnumerable<T> Dematerialize<T>(this IAsyncEnumerable<Notification<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        switch (current.Kind)
                        {
                            case NotificationKind.Next:
                                await yield(current.Value).ConfigureAwait(false);
                                break;
                            case NotificationKind.Completed:
                                return;
                            case NotificationKind.Error:
                                throw current.Error;
                            default:
                                throw new Exception(current.Kind + "???");
                        }
                    }
                }
                finally { await ae.DisposeAsync(); }
                await token.WhenCanceled().ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Dematerializes the explicit notification in <paramref name="source"/> after an interval.
        /// </summary>
        public static IAsyncEnumerable<T> Dematerialize<T>(this IEnumerable<TimeInterval<Notification<T>>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                var time = Time.Current;
                var t = time.Now;
                using (var timer = time.GetTimer(token))
                    foreach (var ti in source)
                    {
                        t += ti.Interval;
                        await timer.Delay(t).ConfigureAwait(false);
                        switch (ti.Value.Kind)
                        {
                            case NotificationKind.Next:
                                await yield(ti.Value.Value).ConfigureAwait(false);
                                break;
                            case NotificationKind.Error:
                                throw ti.Value.Error;
                            case NotificationKind.Completed:
                                return;
                            default:
                                throw new Exception(ti.Value.Kind + "???");
                        }
                    }
                await token.WhenCanceled().ConfigureAwait(false);
            });
        }
    }
}
