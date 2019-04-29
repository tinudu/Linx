namespace Tests.Linx.Reactive
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Linx.Reactive;
    using global::Linx.Reactive.Timing;
    using Xunit;

    public class GeneratorTests
    {
        [Fact]
        public async Task TestInterval()
        {
            using(new VirtualTime())
            {
                var t = LinxReactive.Interval(TimeSpan.FromSeconds(1)).Select(i => (int)i).Take(5).ToList(default);
                var result = await t;
                Assert.True(Enumerable.Range(0, 5).SequenceEqual(result));
            }
            Time.Current = new VirtualTime();
        }

        [Fact]
        public async Task TestReturn()
        {
            var result = await LinxReactive.Return(42).Single(default);
            Assert.Equal(42, result);

            result = await LinxReactive.Return(() => 42).Single(default);
            Assert.Equal(42, result);

            var now = DateTimeOffset.Now;
            var delay = TimeSpan.FromDays(30);
            using (var vt = new VirtualTime(now))
            {
                var tResult = LinxReactive.Return(async () =>
                {
                    await Time.Current.Delay(delay, default).ConfigureAwait(false);
                    return 42;
                }).Single(default);
                Assert.Equal(42, await tResult);
                Assert.Equal(vt.Now, now + delay);
            }
        }

        [Fact]
        public async Task TestThrows()
        {
            try
            {
                await LinxReactive.Throw<int>(new Exception("Boom!")).Sum(default);
                throw new Exception();
            }
            catch (Exception ex) when (ex.Message == "Boom!") { }
        }
    }
}
