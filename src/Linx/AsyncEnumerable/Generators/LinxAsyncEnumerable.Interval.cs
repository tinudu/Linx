namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns a sequence that produces the current time immediately, then after every interval.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The interval must positive.</exception>
        public static IAsyncEnumerable<DateTimeOffset> Interval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
            return Create(GetEnumerator);

            async IAsyncEnumerator<DateTimeOffset> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var time = Time.Current;
                var due = time.Now;
                yield return due;
                using var timer = time.GetTimer(token);
                while (true)
                {
                    due += interval;
                    await timer.Delay(due).ConfigureAwait(false);
                    yield return due;
                }
                // ReSharper disable once IteratorNeverReturns
            }
        }
    }
}
