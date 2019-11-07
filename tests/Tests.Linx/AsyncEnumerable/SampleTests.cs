namespace Tests.Linx.AsyncEnumerable
{
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using Xunit;

    public sealed class SampleTests
    {
        [Fact]
        public async Task Success()
        {
            var source = Marble.Parse(" -abc- - - - -def- - -ghi- - -|");
            var sample = Marble.Parse("x-   -x-x-x-x-   -x-x-   -x-x- -x*-x", (x, i) => i);
            var expect = Marble.Parse(" -a  -c- - - -d  -f- -g  -i- -|");
            var testee = source.Sample(sample);
            await expect.AssertEqual(testee);
        }
    }
}
