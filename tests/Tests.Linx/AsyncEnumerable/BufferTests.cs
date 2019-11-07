namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class BufferTests
    {
        [Fact]
        public async Task CompleteWhileIdle()
        {
            var src = Marble.Parse("abcd-  -ef-    ---|");
            var smp = Marble.Parse("x   -xx-  -xxxx");
            var exp = Marble.Parse("a   -bc-  -def ---|");
            await exp.AssertEqual(smp.Zip(src.Buffer(), (_, x) => x));
        }

        [Fact]
        public async Task CompleteWhileBuffered()
        {
            var src = Marble.Parse("abcd-  -ef|");
            var smp = Marble.Parse("x   -xx-  -xxxx");
            var exp = Marble.Parse("a   -bc-  -def|");
            await exp.AssertEqual(smp.Zip(src.Buffer(), (_, x) => x));
        }

        [Fact]
        public async Task FailWhileIdle()
        {
            var src = Marble.Parse("abcd-  -ef-    ---#");
            var smp = Marble.Parse("x   -xx-  -xxxx");
            var exp = Marble.Parse("a   -bc-  -def ---#");
            await exp.AssertEqual(smp.Zip(src.Buffer(), (_, x) => x));
        }

        [Fact]
        public async Task FailWhileBuffered()
        {
            var src = Marble.Parse("abcd-  -ef#");
            var smp = Marble.Parse("x   -xx-   -xxx");
            var exp = Marble.Parse("a   -bc-   -def#");
            await exp.AssertEqual(smp.Zip(src.Buffer(), (_, x) => x));
        }
    }
}
