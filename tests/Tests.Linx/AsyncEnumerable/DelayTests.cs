namespace Tests.Linx.AsyncEnumerable
{
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class DelayTests
    {
        [Fact]
        public async Task TestDelay()
        {
            var delay = 3 * MarbleSettings.DefaultFrameSize;
            var source = Marble.Parse("   -a-bc-d-|");
            var expect = Marble.Parse("----a-bc-d-|");
            var testee = source.Delay(delay);
            using var vt = new VirtualTime();
            var eq = expect.AssertEqual(testee, default);
            vt.Start();
            await eq;
        }

    }
}
