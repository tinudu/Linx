namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Observable;
    using global::Linx.Testing;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class SampleTests
    {
        [Fact]
        public async Task Success()
        {
            var source = Marble.Parse(" -abc- -- -- -def- -- -efg- -- -|");
            var sample = Marble.Parse("x-   -x--x--x-   -x--x-   -x--x- -x*--x").ToLinxObservable();
            var expect = Marble.Parse(" -a  -c-- -- -d  -f-- -e  -g-- -|");
            var testee = source.Sample(sample).Latest();
            await expect.AssertEqual(testee);
        }
    }
}
