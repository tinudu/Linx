namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System.Threading.Tasks;
    using global::Linx.Timing;
    using Xunit;

    public sealed class BufferTests
    {
        [Fact]
        public async Task CompleteWhileIdle()
        {
            var src = Marble.Parse("-a-b--   -c--|");
            var smp = Marble.Parse("- - --xxx     ");
            var exp = Marble.Parse("- - --ab -c--|");
            await exp.AssertEqual(src.Buffer().Zip(smp, (x, y) => x));
        }

        [Fact]
        public async Task CompleteWhileBuffered()
        {
            var src = Marble.Parse("-a-b--   -c-def-|");
            var smp = Marble.Parse("- - --xxx- -   - -x-xx");
            var exp = Marble.Parse("- - --ab -c-   - -d-ef|");
            await exp.AssertEqual(src.Buffer().Zip(smp, (x, y) => x));
        }

        [Fact]
        public async Task FailWhileIdle()
        {
            var src = Marble.Parse("-a-b--   -c--#");
            var smp = Marble.Parse("- - --xxx     ");
            var exp = Marble.Parse("- - --ab -c--#");
            await exp.AssertEqual(src.Buffer().Zip(smp, (x, y) => x));
        }

        [Fact]
        public async Task FailWhileBuffered()
        {
            var src = Marble.Parse("-a-b--   -c-def-#   ");
            var smp = Marble.Parse("- - --xxx- -   - -x ");
            var exp = Marble.Parse("- - --ab -c-   - -d#");
            await exp.AssertEqual(src.Buffer().Zip(smp, (x, y) => x));
        }
    }
}
