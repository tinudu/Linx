namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class ClockTests
    {
        private static readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        private static readonly TimeSpan _winterOffset = TimeSpan.FromTicks(TimeSpan.TicksPerHour);
        private static readonly TimeSpan _summerOffset = TimeSpan.FromTicks(2 * TimeSpan.TicksPerHour);

        [Fact]
        public async Task SummerTimeH()
        {
            var date = new DateTime(2019, 3, 31, 0, 0, 0, DateTimeKind.Unspecified);
            var t0 = new DateTimeOffset(date.AddHours(0.5), _winterOffset).ToUniversalTime();
            var testee = LinxAsyncEnumerable.HoursClock(_timeZone).Take(23).Timestamp();
            var w = Enumerable.Range(0, 2)
                .Select(h =>
                {
                    var t = new DateTimeOffset(date + new TimeSpan(h, 0, 0), _winterOffset);
                    return new Timestamped<DateTimeOffset>(h == 0 ? t0 : t, t);
                });
            var s = Enumerable.Range(3, 21).Select(h =>
            {
                var t = new DateTimeOffset(date + new TimeSpan(h, 0, 0), _summerOffset);
                return new Timestamped<DateTimeOffset>(t, t);
            });
            var expected = w.Concat(s).ToList();
            var actual = await Marble.OnVirtualTime(t => testee.ToList(t), t0);
            Assert.True(expected.SequenceEqual(actual, ExactComparer.Default));
        }

        [Fact]
        public async Task WinterTimeH()
        {
            var date = new DateTime(2019, 10, 27, 0, 0, 0, DateTimeKind.Unspecified);
            var t0 = new DateTimeOffset(date.AddHours(0.5), _summerOffset).ToUniversalTime();
            var testee = LinxAsyncEnumerable.HoursClock(_timeZone).Take(25).Timestamp();
            var s = Enumerable.Range(0, 3).Select(h =>
            {
                var t = new DateTimeOffset(date + new TimeSpan(h, 0, 0), _summerOffset);
                return new Timestamped<DateTimeOffset>(h == 0 ? t0 : t, t);
            });
            var w = Enumerable.Range(2, 22).Select(h =>
            {
                var t = new DateTimeOffset(date + new TimeSpan(h, 0, 0), _winterOffset);
                return new Timestamped<DateTimeOffset>(t, t);
            });
            var expected = s.Concat(w).ToList();
            var actual = await Marble.OnVirtualTime(t => testee.ToList(t), t0);
            Assert.True(expected.SequenceEqual(actual, ExactComparer.Default));
        }

        private sealed class ExactComparer : IEqualityComparer<Timestamped<DateTimeOffset>>
        {
            public static ExactComparer Default { get; } = new ExactComparer();
            private ExactComparer() { }

            public bool Equals(Timestamped<DateTimeOffset> x, Timestamped<DateTimeOffset> y) =>
                x.Timestamp == y.Timestamp &&
                x.Value.Date == y.Value.Date &&
                x.Value.Offset == y.Value.Offset;

            public int GetHashCode(Timestamped<DateTimeOffset> obj) => throw new NotSupportedException();
        }
    }
}
