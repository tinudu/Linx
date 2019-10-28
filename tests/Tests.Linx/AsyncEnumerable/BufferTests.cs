namespace Tests.Linx.AsyncEnumerable
{
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class BufferTests
    {
        [Fact]
        public async Task CompleteWhileIdle()
        {
            var source = Marble.Parse("-a-b--   -c--|");
            var sample = Marble.Parse("- - --xxx     ");
            var expect = Marble.Parse("- - --ab -c--|");
            using var vt = new VirtualTime();
            var eq = expect.AssertEqual(source.Buffer().Zip(sample, (x, y) => x), default);
            vt.Start();
            await eq;
        }

        [Fact]
        public async Task CompleteWhileBuffered()
        {
            var source = Marble.Parse("-a-b--   -c-def-|");
            var sample = Marble.Parse("- - --xxx- -   - -x-xx");
            var expect = Marble.Parse("- - --ab -c-   - -d-ef|");
            using var vt = new VirtualTime();
            var eq = expect.AssertEqual(source.Buffer().Zip(sample, (x, y) => x), default);
            vt.Start();
            await eq;
        }

        [Fact]
        public async Task FailWhileIdle()
        {
            var source = Marble.Parse("-a-b--   -c--#");
            var sample = Marble.Parse("- - --xxx     ");
            var expect = Marble.Parse("- - --ab -c--#");
            using var vt = new VirtualTime();
            var eq = expect.AssertEqual(source.Buffer().Zip(sample, (x, y) => x), default);
            vt.Start();
            await eq;
        }

        [Fact]
        public async Task FailWhileBuffered()
        {
            var source = Marble.Parse("-a-b--   -c-def-#   ");
            var sample = Marble.Parse("- - --xxx- -   - -x ");
            var expect = Marble.Parse("- - --ab -c-   - -d#");
            using var vt = new VirtualTime();
            var eq = expect.AssertEqual(source.Buffer().Zip(sample, (x, y) => x), default);
            vt.Start();
            await eq;
        }
    }
}
