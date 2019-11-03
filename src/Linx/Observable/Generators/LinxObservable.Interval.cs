namespace Linx.Observable
{
    using System;
    using Timing;

    partial class LinxObservable
    {
        /// <summary>
        /// Returns a sequence that produces the current time immediately, then after every interval.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The interval must positive.</exception>
        public static ILinxObservable<DateTimeOffset> Interval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

            return Create<DateTimeOffset>(async observer =>
            {
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                try
                {
                    observer.Token.ThrowIfCancellationRequested();

                    var time = Time.Current;
                    var due = time.Now;
                    if (!observer.OnNext(due)) return;
                    using var timer = time.GetTimer(observer.Token);
                    while (true)
                    {
                        due += interval;
                        await timer.Delay(due).ConfigureAwait(false);
                        if (!observer.OnNext(due)) return;
                    }
                }
                catch (Exception ex) { observer.OnError(ex); }
            });
        }
    }
}
