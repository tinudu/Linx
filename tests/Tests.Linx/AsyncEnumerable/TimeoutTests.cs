namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class TimeoutTests
    {
        [Fact]
        public async Task TestNoTimeout()
        {
            var seq = Marble.Parse("a-b--c--d-|");
            var testee = seq.Timeout(3 * MarbleSettings.DefaultFrameSize);
            await seq.AssertEqual(testee);
        }

        [Fact]
        public async Task TestTimeout()
        {
            var testee = Marble.Parse("a-b--c-----d|").Timeout(3 * MarbleSettings.DefaultFrameSize);
            var expect = Marble.Parse("a-b--c---#", new MarbleSettings { Error = new TimeoutException() });
            await expect.AssertEqual(testee);
        }
    }
}
