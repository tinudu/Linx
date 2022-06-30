using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Linx.Timing;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Returns a sequence that produces the current time immediately, then after every interval.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The interval must positive.</exception>
    public static IAsyncEnumerable<DateTimeOffset> Interval(TimeSpan period)
    {
        if (period <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(period));

        return Iterator();

        async IAsyncEnumerable<DateTimeOffset> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var time = Time.Current;
            var due = time.Now;
            yield return due;

            using var timer = Time.Current.GetTimer(token);
            while (true)
            {
                due += period;
                await timer.Delay(due).ConfigureAwait(false);
                yield return due;
            }
        }
    }
}
