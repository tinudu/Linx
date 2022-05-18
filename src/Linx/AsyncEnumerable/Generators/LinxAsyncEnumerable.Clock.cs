using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using global::Linx.Timing;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Days in a time zone.
    /// </summary>
    /// <remarks>Immediately emits the floor of the current time.</remarks>
    public static IAsyncEnumerable<DateTimeOffset> DaysClock(TimeZoneInfo timeZone, ITime time) => Clock(TimeSpan.FromTicks(TimeSpan.TicksPerDay), timeZone, time);

    /// <summary>
    /// Hours in a time zone.
    /// </summary>
    /// <remarks>Immediately emits the floor of the current time.</remarks>
    public static IAsyncEnumerable<DateTimeOffset> HoursClock(TimeZoneInfo timeZone, ITime time) => Clock(TimeSpan.FromTicks(TimeSpan.TicksPerHour), timeZone, time);

    /// <summary>
    /// Minutes in a time zone.
    /// </summary>
    /// <remarks>Immediately emits the floor of the current time.</remarks>
    public static IAsyncEnumerable<DateTimeOffset> MinutesClock(TimeZoneInfo timeZone, ITime time) => Clock(TimeSpan.FromTicks(TimeSpan.TicksPerMinute), timeZone, time);

    /// <summary>
    /// Seconds in a time zone.
    /// </summary>
    /// <remarks>Immediately emits the floor of the current time.</remarks>
    public static IAsyncEnumerable<DateTimeOffset> SecondsClock(TimeZoneInfo timeZone, ITime time) => Clock(TimeSpan.FromTicks(TimeSpan.TicksPerSecond), timeZone, time);

    /// <summary>
    /// Time in a time zone.
    /// </summary>
    /// <param name="resolution">Clock resolution.</param>
    /// <param name="timeZone">The time zone.</param>
    /// <param name="time">The time.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="resolution"/> must be at least 100ms.</exception>
    /// <remarks>Immediately emits the floor of the current time.</remarks>
    public static IAsyncEnumerable<DateTimeOffset> Clock(TimeSpan resolution, TimeZoneInfo timeZone, ITime time)
    {
        if (timeZone == null) throw new ArgumentNullException(nameof(timeZone));
        if (resolution.Ticks < 100 * TimeSpan.TicksPerMillisecond) throw new ArgumentOutOfRangeException(nameof(resolution), "Must be at least 100ms");
        if (time is null) time = Time.RealTime;

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