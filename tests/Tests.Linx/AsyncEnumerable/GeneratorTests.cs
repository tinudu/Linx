namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx;
    using global::Linx.Expressions;
    using Xunit;

    public class GeneratorTests
    {
        [Fact]
        public async Task TestInterval()
        {
            var testee = LinxAsyncEnumerable.Interval(MarbleSettings.DefaultFrameSize).Take(5);
            var t0 = new DateTimeOffset(2019, 8, 6, 0, 0, 0, TimeSpan.FromHours(2));
            var expect = Marble.Parse("x-x-x-x-x|", (ch, i) => t0 + i * MarbleSettings.DefaultFrameSize);
            await expect.AssertEqual(testee, t0);
        }

        [Fact]
        public async Task TestReturn()
        {
            var result = await LinxAsyncEnumerable.Return(42).Single(default);
            Assert.Equal(42, result);

            result = await LinxAsyncEnumerable.Return(() => 42).Single(default);
            Assert.Equal(42, result);

            var delay = TimeSpan.FromDays(30);
            var testee = Express.Func(async (CancellationToken t) =>
            {
                await Time.Current.Delay(delay, t).ConfigureAwait(false);
                return new Timestamped<int>(Time.Current.Now, 42);
            });
            var t0 = DateTimeOffset.Now;
            var actual = await Marble.OnVirtualTime(testee, t0);
            Assert.Equal(t0 + delay, actual.Timestamp);
            Assert.Equal(42, actual.Value);
        }

        [Fact]
        public async Task TestThrows()
        {
            try
            {
                await LinxAsyncEnumerable.Throw<int>(new Exception("Boom!")).Sum(default);
                throw new Exception();
            }
            catch (Exception ex) when (ex.Message == "Boom!") { }
        }
    }
}
