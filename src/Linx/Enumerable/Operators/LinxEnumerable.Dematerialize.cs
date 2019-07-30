namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using AsyncEnumerable;
    using Notifications;
    using Observable;
    using Timing;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Dematerializes the explicit notification in <paramref name="source"/> after an interval.
        /// </summary>
        public static IAsyncEnumerable<T> DematerializeToAsyncEnumerable<T>(this IEnumerable<TimeInterval<Notification<T>>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return LinxAsyncEnumerable.Create<T>(async (yield, token) =>
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
                                if (!await yield(ti.Value.Value).ConfigureAwait(false)) return;
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
            }, source + ".DematerializeToAsyncEnumerable");
        }

        /// <summary>
        /// Dematerializes the explicit notification in <paramref name="source"/> after an interval.
        /// </summary>
        public static ILinxObservable<T> DematerializeToLinxObservable<T>(this IEnumerable<TimeInterval<Notification<T>>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return LinxObservable.Create<T>(async observer =>
            {
                Exception error = null;
                try
                {
                    var time = Time.Current;
                    var t = time.Now;
                    using (var timer = time.GetTimer(observer.Token))
                        foreach (var ti in source)
                        {
                            t += ti.Interval;
                            await timer.Delay(t).ConfigureAwait(false);
                            switch (ti.Value.Kind)
                            {
                                case NotificationKind.Next:
                                    if (!observer.OnNext(ti.Value.Value)) return;
                                    break;
                                case NotificationKind.Error:
                                    throw ti.Value.Error;
                                case NotificationKind.Completed:
                                    return;
                                default:
                                    throw new Exception(ti.Value.Kind + "???");
                            }
                        }
                    await observer.Token.WhenCanceled().ConfigureAwait(false);
                }
                catch (Exception ex) { error = ex; }
                finally
                {
                    if (error == null) observer.OnCompleted();
                    else observer.OnError(error);
                }
            }, source + ".DematerializeToLinxObservable");
        }
    }
}
