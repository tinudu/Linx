namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns an observable sequence that produces a value after each period.
        /// </summary>
        public static IAsyncEnumerable<long> Interval(TimeSpan period)
        {
            if (period <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(period));

            return Produce<long>(async (yield, token) =>
            {
                var time = Time.Current;
                var value = 0;
                var due = time.Now;
                using (var timer = time.GetTimer(token))
                    do
                    {
                        await yield(value++).ConfigureAwait(false);
                        due += period;
                        await timer.Delay(due).ConfigureAwait(false);
                    } while (true);
                // ReSharper disable once FunctionNeverReturns
            });
        }
    }
}
