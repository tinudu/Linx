namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using System;
    using System.Threading.Tasks;
    using global::Linx;
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

            var t0 = DateTimeOffset.Now;
            var delay = TimeSpan.FromDays(30);
            var actual = await VirtualTime.Run(async () =>
            {
                await Time.Current.Delay(delay, default).ConfigureAwait(false);
                return 42;
            }, t0);
            Assert.Equal(new Timestamped<int>(t0 + delay, 42), actual);
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
