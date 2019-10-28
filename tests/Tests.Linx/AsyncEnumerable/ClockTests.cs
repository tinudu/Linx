namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Timing;
    using Xunit;

    public sealed class ClockTests
    {
        private static readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        private static readonly TimeSpan _winterOffset = TimeSpan.FromTicks(TimeSpan.TicksPerHour);
        private static readonly TimeSpan _summerOffset = TimeSpan.FromTicks(2 * TimeSpan.TicksPerHour);

        [Fact]
        public async Task SummerTimeH()
        {
            var date = new DateTime(2019, 3, 31);
            var testee = LinxAsyncEnumerable.HoursClock(_timeZone).Take(23);
            var w = Enumerable.Range(0, 2).Select(h => new DateTimeOffset(date + new TimeSpan(h, 0, 0), _winterOffset));
            var s = Enumerable.Range(3, 21).Select(h => new DateTimeOffset(date + new TimeSpan(h, 0, 0), _summerOffset));
            var expected = w.Concat(s).ToList();
            using var vt = new VirtualTime(date.ToUniversalTime().AddHours(0.5));
            var tActual = testee.ToList(default);
            vt.Start();
            var actual = await tActual;
            Assert.True(expected.SequenceEqual(actual, ExactDateTimeOffsetComparer.Default));
        }

        [Fact]
        public async Task WinterTimeH()
        {
            var date = new DateTime(2019, 10, 27);
            var testee = LinxAsyncEnumerable.HoursClock(_timeZone).Take(25);
            var s = Enumerable.Range(0, 3).Select(h => new DateTimeOffset(date + new TimeSpan(h, 0, 0), _summerOffset));
            var w = Enumerable.Range(2, 22).Select(h => new DateTimeOffset(date + new TimeSpan(h, 0, 0), _winterOffset));
            var expected = s.Concat(w).ToList();
            using var vt = new VirtualTime(date.ToUniversalTime().AddHours(0.5));
            var tActual = testee.ToList(default);
            vt.Start();
            var actual = await tActual;
            Assert.True(expected.SequenceEqual(actual, ExactDateTimeOffsetComparer.Default));
        }

        private sealed class ExactDateTimeOffsetComparer : IEqualityComparer<DateTimeOffset>
        {
            public static ExactDateTimeOffsetComparer Default { get; } = new ExactDateTimeOffsetComparer();
            private ExactDateTimeOffsetComparer() { }
            public bool Equals(DateTimeOffset x, DateTimeOffset y) => x.Date == y.Date && x.Offset == y.Offset;
            public int GetHashCode(DateTimeOffset obj) => obj.GetHashCode();
        }
    }
}
