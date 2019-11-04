namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class LatestTests
    {
        [Fact]
        public async Task LatestOneSuccess()
        {
            var src = Marble.Parse("abc- - -def-  -ghi-|");
            var smp = Marble.Parse("   -x-x-   -xx-   --x--x*--x");
            var exp = Marble.Parse("   -a-c-   -df-   --g--i|");
            var testee = src.Latest().Zip(smp, (x, y) => x);
            await exp.AssertEqual(testee);
        }

        [Fact]
        public async Task LatestOneDispose()
        {
            var src = Marble.Parse("abc- - -def-  -ghi-|");
            var smp = Marble.Parse("   -x-x-   -xx-   --x --x*--x");
            var exp = Marble.Parse("   -a-c-   -df-   --g|");
            var testee = src.Latest().Take(5).Zip(smp, (x, y) => x);
            await exp.AssertEqual(testee);
        }

        [Fact]
        public async Task LatestOneCancel()
        {
            var src = Marble.Parse("abc- - -def-  -geh");
            var smp = Marble.Parse("   -x-x-   -xx");
            var exp = Marble.Parse("   -a-c-   -df");
            var testee = src.Latest().Zip(smp, (x, y) => x);
            await exp.AssertEqualCancel(testee, 8 * MarbleSettings.DefaultFrameSize);
        }
    }
}
