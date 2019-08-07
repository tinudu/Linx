namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public class GeneratorTests
    {
        [Fact]
        public async Task TestInterval()
        {
            var testee = LinxAsyncEnumerable.Interval(MarbleSettings.DefaultFrameSize).Take(5);
            var t0 = new DateTimeOffset(2019, 8, 6, 0, 0, 0, TimeSpan.FromHours(2));
            var expect = Marble.Parse("x-x-x-x-x|", (ch, i) => t0 + i * MarbleSettings.DefaultFrameSize);
            using (var vt = new VirtualTime(t0))
            {
                var eq = testee.AssertEqual(expect, default);
                vt.Start();
                await eq;
            }
        }

        [Fact]
        public async Task TestReturn()
        {
            var result = await LinxAsyncEnumerable.Return(42).Single(default);
            Assert.Equal(42, result);

            result = await LinxAsyncEnumerable.Return(() => 42).Single(default);
            Assert.Equal(42, result);

            var now = DateTimeOffset.Now;
            var delay = TimeSpan.FromDays(30);
            using (var vt = new VirtualTime(now))
            {
                var tResult = LinxAsyncEnumerable.Return(async () =>
                {
                    await Time.Current.Delay(delay, default).ConfigureAwait(false);
                    return 42;
                }).Single(default);
                vt.Start();
                Assert.Equal(42, await tResult);
                Assert.Equal(vt.Now, now + delay);
            }
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
