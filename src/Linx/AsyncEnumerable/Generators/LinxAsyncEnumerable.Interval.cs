namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns a sequence that produces the current time immediately, then after every interval.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The interval must positive.</exception>
        public static IAsyncEnumerable<DateTimeOffset> Interval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval), "Must be postive.");

            return Create<DateTimeOffset>(async (yield, token) =>
            {
                var time = Time.Current;
                var due = time.Now;
                if (!await yield(due).ConfigureAwait(false)) return;
                using (var timer = time.GetTimer(token))
                    while (true)
                    {
                        due += interval;
                        await timer.Delay(due).ConfigureAwait(false);
                        if (!await yield(due).ConfigureAwait(false)) return;
                    }
            });
        }

        /// <summary>
        /// Returns a sequence that produces the current time immediately, then after every interval.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The interval must positive.</exception>
        public static IAsyncEnumerable<DateTimeOffset> Interval(int intervalMilliseconds)
            => Interval(TimeSpan.FromTicks(intervalMilliseconds * TimeSpan.TicksPerMillisecond));
    }
}
