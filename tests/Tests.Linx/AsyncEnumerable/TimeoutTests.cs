namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class TimeoutTests
    {
        [Fact]
        public async Task TestNoTimeout()
        {
            var seq = Marble.Parse("a-b--c--d-|");
            var testee = seq.Timeout(3 * MarbleSettings.DefaultFrameSize);

            using var vt = new VirtualTime();
            var eq = seq.AssertEqual(testee, default);
            vt.Start();
            await eq;
        }

        [Fact]
        public async Task TestTimeout()
        {
            var testee = Marble.Parse("a-b--c-----d|").Timeout(3 * MarbleSettings.DefaultFrameSize);
            var expect = Marble.Parse("a-b--c---#", new MarbleSettings { Error = new TimeoutException() });

            using var vt = new VirtualTime();
            var eq = expect.AssertEqual(testee, default);
            vt.Start();
            await eq;
        }
    }
}
