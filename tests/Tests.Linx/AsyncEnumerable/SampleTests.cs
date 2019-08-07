namespace Tests.Linx.AsyncEnumerable
{
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Observable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class SampleTests
    {
        [Fact]
        public async Task Success()
        {
            var interval = 2 * MarbleSettings.DefaultFrameSize;
            var source = Marble.Parse(" -abc- -----def- ---efg- ----|").DematerializeAsyncEnumerable();
            var expect = Marble.Parse(" -a  -c-----d  -f---e  -g----|");
            var testee = source.Sample(interval).Latest();
            using (var vt = new VirtualTime())
            {
                var eq = testee.AssertEqual(expect, default);
                vt.Start();
                await eq;
            }
        }

    }
}
