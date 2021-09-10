namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Days in a time zone.
        /// </summary>
        /// <remarks>Immediately emits the floor of the current time.</remarks>
        public static IAsyncEnumerable<DateTimeOffset> DaysClock(TimeZoneInfo timeZone) => Clock(TimeSpan.FromTicks(TimeSpan.TicksPerDay), timeZone);

        /// <summary>
        /// Hours in a time zone.
        /// </summary>
        /// <remarks>Immediately emits the floor of the current time.</remarks>
        public static IAsyncEnumerable<DateTimeOffset> HoursClock(TimeZoneInfo timeZone) => Clock(TimeSpan.FromTicks(TimeSpan.TicksPerHour), timeZone);

        /// <summary>
        /// Minutes in a time zone.
        /// </summary>
        /// <remarks>Immediately emits the floor of the current time.</remarks>
        public static IAsyncEnumerable<DateTimeOffset> MinutesClock(TimeZoneInfo timeZone) => Clock(TimeSpan.FromTicks(TimeSpan.TicksPerMinute), timeZone);

        /// <summary>
        /// Seconds in a time zone.
        /// </summary>
        /// <remarks>Immediately emits the floor of the current time.</remarks>
        public static IAsyncEnumerable<DateTimeOffset> SecondsClock(TimeZoneInfo timeZone) => Clock(TimeSpan.FromTicks(TimeSpan.TicksPerSecond), timeZone);

        /// <summary>
        /// Time in a time zone.
        /// </summary>
        /// <param name="resolution">Clock resolution.</param>
        /// <param name="timeZone">The time zone.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="resolution"/> must be at least 100ms.</exception>
        /// <remarks>Immediately emits the floor of the current time.</remarks>
        public static IAsyncEnumerable<DateTimeOffset> Clock(TimeSpan resolution, TimeZoneInfo timeZone)
        {
            if (timeZone == null) throw new ArgumentNullException(nameof(timeZone));
            if (resolution.Ticks < 100 * TimeSpan.TicksPerMillisecond) throw new ArgumentOutOfRangeException(nameof(resolution), "Must be at least 100ms");
            return Iterator();

            DateTimeOffset ValidClockTime(DateTimeOffset t)
            {
                while (true)
                {
                    // assert correct time zone
                    t = TimeZoneInfo.ConvertTime(t, timeZone);

                    // assert multiple of resolution
                    var floor = new DateTime(t.DateTime.Ticks / resolution.Ticks * resolution.Ticks);
                    if (floor == t.DateTime) return t;

                    // retry at next resolution
                    t = new DateTimeOffset(floor + resolution, t.Offset);
                }
            }

            async IAsyncEnumerable<DateTimeOffset> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                var time = Time.Current;
                var due = TimeZoneInfo.ConvertTime(time.Now, timeZone);
                due = ValidClockTime(new DateTimeOffset(due.Ticks / resolution.Ticks * resolution.Ticks, due.Offset));
                using var timer = time.GetTimer(token);
                while (true)
                {
                    await timer.Delay(due).ConfigureAwait(false);
                    yield return due;
                    due = ValidClockTime(new DateTimeOffset(due.DateTime + resolution, due.Offset));
                }
            }
        }
    }
}