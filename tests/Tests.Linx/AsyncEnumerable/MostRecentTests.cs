namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class MostRecentTests
    {
        [Fact]
        public async Task LatestOneSuccess()
        {
            var src = Marble.Parse("abc- -def- -ghi- -|");
            var smp = Marble.Parse("   -x-   -x-x  -x-x*-x");
            var exp = Marble.Parse("   -c-   -f-g  -i-|");
            var testee = src.MostRecent().Zip(smp, (x, y) => x.GetResult().Value);
            await exp.AssertEqual(testee);
        }

        [Fact]
        public async Task LatestOneDispose()
        {
            var src = Marble.Parse("abc- -def-   -ghi- -|");
            var smp = Marble.Parse("   -x-   -x|");
            var exp = Marble.Parse("   -c-   -f|");
            var testee = src.MostRecent().Zip(smp, (x, _) => x.GetResult().Value);
            await exp.AssertEqual(testee);
        }

        [Fact]
        public async Task LatestOneCancel()
        {
            var src = Marble.Parse("abc- -def- -ghi- -|");
            var smp = Marble.Parse("   -x-   -x-x  --x-x*-x");
            var exp = Marble.Parse("   -c-   -f-g  --i-|");
            var testee = src.MostRecent().Zip(smp, (x, y) => x.GetResult().Value);
            await exp.AssertEqualCancel(testee, 5 * MarbleSettings.DefaultFrameSize);
        }
    }
}
