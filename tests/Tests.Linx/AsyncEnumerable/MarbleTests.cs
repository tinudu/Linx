namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Linq;
    using global::Linx;
    using global::Linx.Notifications;
    using global::Linx.Testing;
    using Xunit;

    /// <summary>
    /// Tests of the <see cref="Marble"/>.
    /// </summary>
    public sealed class MarbleTests
    {
        private static TimeInterval<Notification<T>> Next<T>(int seconds, T value) => new TimeInterval<Notification<T>>(TimeSpan.FromTicks(seconds * TimeSpan.TicksPerSecond), Notification.Next(value));
        private static TimeInterval<Notification<T>> Completed<T>(int seconds) => new TimeInterval<Notification<T>>(TimeSpan.FromTicks(seconds * TimeSpan.TicksPerSecond), Notification.Completed<T>());
        private static TimeInterval<Notification<T>> Error<T>(int seconds, Exception error) => new TimeInterval<Notification<T>>(TimeSpan.FromTicks(seconds * TimeSpan.TicksPerSecond), Notification.Error<T>(error));

        [Fact]
        public void TestNever()
        {
            var m = Marble.Parse("a-b--c").ToList();
            var x = new[]
            {
                Next(0, 'a'),
                Next(1, 'b'),
                Next(2, 'c')
            };
            Assert.True(x.SequenceEqual(m));
        }

        [Fact]
        public void TestComplete()
        {
            var m = Marble.Parse("a-b--|").ToList();
            var x = new[]
            {
                Next(0, 'a'),
                Next(1, 'b'),
                Completed<char>(2)
            };
            Assert.True(x.SequenceEqual(m));
        }

        [Fact]
        public void TestError()
        {
            var m = Marble.Parse("a-b--#").ToList();
            var x = new[]
            {
                Next(0, 'a'),
                Next(1, 'b'),
                Error<char>(2, MarbleException.Singleton)
            };
            Assert.True(x.SequenceEqual(m));
        }

        [Fact]
        public void TestForever()
        {
            var m = Marble.Parse("a-b*--c---d").Take(7).ToList();
            var x = new[]
            {
                Next(0, 'a'),
                Next(1, 'b'),
                Next(2, 'c'),
                Next(3, 'd'),
                Next(2, 'c'),
                Next(3, 'd'),
                Next(2, 'c')
            };
            Assert.True(x.SequenceEqual(m));
        }

        [Fact]
        public void TestReplaceFunc()
        {
            var m = Marble.Parse("X-X--X---|", (ch, i) => i).ToList();
            var x = new[]
            {
                Next(0, 0),
                Next(1, 1),
                Next(2, 2),
                Completed<int>(3)
            };
            Assert.True(x.SequenceEqual(m));
        }

        [Fact]
        public void TestReplaceElements()
        {
            var m = Marble.Parse("X-X--X---|", null, 1, 2, 3).ToList();
            var x = new[]
            {
                Next(0, 1),
                Next(1, 2),
                Next(2, 3),
                Completed<int>(3)
            };
            Assert.True(x.SequenceEqual(m));
        }

        [Fact]
        public void TestSettings()
        {
            var ms = new MarbleParserSettings { Error = new TimeoutException(), FrameSize = TimeSpan.FromSeconds(2) };
            var m = Marble.Parse("a-b--#", ms).ToList();
            var x = new[]
            {
                Next(0, 'a'),
                Next(2, 'b'),
                Error<char>(4, new TimeoutException())
            };
            Assert.True(x.SequenceEqual(m));
        }
    }
}
