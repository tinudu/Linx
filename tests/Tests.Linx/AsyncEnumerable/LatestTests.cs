namespace Tests.Linx.AsyncEnumerable
{
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Observable;
    using global::Linx.Testing;
    using Xunit;

    public sealed class LatestTests
    {
        [Fact]
        public async Task Bla()
        {
            var src = Marble.Parse("abc-- -- -def- -geh-|");
            var smp = Marble.Parse("   --x--x-   -x-   --x *--x");
            var exp = Marble.Parse("   --c-- -d  -f-   --h|");
            var testee = src.Latest().Zip(smp, (x, y) => x);
            await exp.AssertEqual(testee);
        }
    }
}
