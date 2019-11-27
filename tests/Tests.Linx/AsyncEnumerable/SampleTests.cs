namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using Xunit;

    public sealed class SampleTests
    {
        [Fact]
        public void TestArguments()
        {
            Assert.Throws<ArgumentNullException>("source", () => LinxAsyncEnumerable.Sample<int, int>(null, LinxAsyncEnumerable.Empty<int>()));
            Assert.Throws<ArgumentNullException>("sampler", () => LinxAsyncEnumerable.Empty<int>().Sample<int, int>(null));
            Assert.Throws<ArgumentNullException>("source", () => LinxAsyncEnumerable.Sample<int>(null, TimeSpan.MaxValue));
        }

        [Fact]
        public async Task TestInitial() => await Marble.Parse("abc").Sample(TimeSpan.MaxValue).AssertThrowsInitial();

        [Fact]
        public async Task Success()
        {
            var pul = Marble.Parse("p-   - -p-  - -  -p- -  -  -p-  -p- -  -   -pp");
            var src = Marble.Parse(" -abc- - -  -d-ef- - -gh-  - -  - -i-  -jk|");
            var smp = Marble.Parse(" -   -x- -xx- -  - -x-  -xx- -xx- - -xx");
            var exp = Marble.Parse(" -   -c- -  -d-  - -f-  -  -h-  - -i-  -   -k|");
            var testee = pul.Select(src.Sample(smp));
            await exp.AssertEqual(testee);
        }

        [Fact]
        public async Task Empty()
        {
            var expect = Marble.Parse("|");
            var testee = LinxAsyncEnumerable.Empty<char>().Sample(TimeSpan.MaxValue);
            await expect.AssertEqual(testee);
        }
    }
}
