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
        /// <param name="delayFirst">Optional. Whether to delay before emitting the first item.</param>
        public static IAsyncEnumerable<long> Interval(TimeSpan period, bool delayFirst = false)
        {
            if (period <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(period));

            return Generate<long>(async (yield, token) =>
            {
                var time = Time.Current;
                using (var timer = time.GetTimer(token))
                {
                    var value = 0;
                    var due = time.Now;
                    if (delayFirst)
                    {
                        due += period;
                        await timer.Delay(due).ConfigureAwait(false);
                    }
                    do
                    {
                        if (!await yield(value++).ConfigureAwait(false)) return;
                        due += period;
                        await timer.Delay(due).ConfigureAwait(false);
                    } while (true);
                }

                // ReSharper disable once FunctionNeverReturns
            });
        }
    }
}
