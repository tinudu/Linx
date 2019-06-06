namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns a sequence that produces a value after each period.
        /// </summary>
        /// <param name="period"><see cref="TimeSpan"/> between elements.</param>
        /// <param name="initial">Optioinal. <see cref="TimeSpan"/> before the first element.</param>
        public static IAsyncEnumerable<long> Interval(TimeSpan period, TimeSpan initial = default)
        {
            if (period <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(period));

            return Produce<long>(async (yield, token) =>
            {
                var time = Time.Current;
                using (var timer = time.GetTimer(token))
                {
                    var value = 0;
                    var due = time.Now;
                    if (initial >= TimeSpan.Zero)
                    {
                        due += initial;
                        await timer.Delay(due).ConfigureAwait(false);
                    }
                    do
                    {
                        await yield(value++).ConfigureAwait(false);
                        due += period;
                        await timer.Delay(due).ConfigureAwait(false);
                    } while (true);
                }

                // ReSharper disable once FunctionNeverReturns
            });
        }
    }
}
