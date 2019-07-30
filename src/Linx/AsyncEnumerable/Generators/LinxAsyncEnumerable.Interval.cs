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
        /// <param name="interval"><see cref="TimeSpan"/> between elements.</param>
        /// <param name="delayFirst">Optional. Whether to delay before emitting the first item.</param>
        public static IAsyncEnumerable<long> Interval(TimeSpan interval, bool delayFirst = false)
        {
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

            return Create<long>(async (yield, token) =>
            {
                var time = Time.Current;
                using (var timer = time.GetTimer(token))
                {
                    var value = 0L;
                    var due = time.Now;
                    if (delayFirst)
                    {
                        due += interval;
                        await timer.Delay(due).ConfigureAwait(false);
                    }
                    do
                    {
                        if (!await yield(value++).ConfigureAwait(false)) return;
                        due += interval;
                        await timer.Delay(due).ConfigureAwait(false);
                    } while (true);
                }

                // ReSharper disable once FunctionNeverReturns
            }, nameof(Interval));
        }
    }
}
