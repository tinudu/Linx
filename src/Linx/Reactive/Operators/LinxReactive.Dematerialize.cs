namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using Timing;

    partial class LinxReactive
    {
        /// <summary>
        /// Dematerializes the explicit notification values of an observable sequence as implicit notifications.
        /// </summary>
        public static IAsyncEnumerable<T> Dematerialize<T>(this IAsyncEnumerable<INotification<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        switch (current.Kind)
                        {
                            case NotificationKind.OnNext:
                                await yield(current.Value);
                                break;
                            case NotificationKind.OnCompleted:
                                return;
                            case NotificationKind.OnError:
                                throw current.Error;
                            default:
                                throw new Exception(current.Kind + "???");
                        }
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }

        /// <summary>
        /// Dematerializes the explicit notification in <paramref name="source"/> after an interval.
        /// </summary>
        public static IAsyncEnumerable<T> Dematerialize<T>(this IEnumerable<TimeInterval<INotification<T>>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                var time = Time.Current;
                var t = time.Now;
                foreach (var ti in source)
                {
                    t += ti.Interval;
                    await time.Delay(t, token).ConfigureAwait(false);
                    switch (ti.Value.Kind)
                    {
                        case NotificationKind.OnNext:
                            await yield(ti.Value.Value);
                            break;
                        case NotificationKind.OnError:
                            throw ti.Value.Error;
                        case NotificationKind.OnCompleted:
                            return;
                        default:
                            throw new Exception(ti.Value.Kind + "???");
                    }
                }
            });
        }
    }
}
