using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Linx.Timing;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns a sequence that produces the current time immediately, then after every interval.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The interval must positive.</exception>
        public static IAsyncEnumerable<DateTimeOffset> Interval(TimeSpan interval, ITime time)
        {
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
            if (time is null) time = Time.RealTime;

            return Iterator();

            async IAsyncEnumerable<DateTimeOffset> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                var due = time.Now;
                yield return due;

                using var timer = time.GetTimer(token);
                while (true)
                {
                    due += interval;
                    await timer.Delay(due).ConfigureAwait(false);
                    yield return due;
                }
            }
        }
    }
}
