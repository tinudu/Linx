//namespace Tests.Linx.AsyncEnumerable
//{
//    using global::Linx.AsyncEnumerable;
//    using global::Linx.Testing;
//    using System.Threading.Tasks;
//    using Xunit;

//    public sealed class BufferTests : VirtualTime
//    {
//        [Fact]
//        public async Task CompleteWhileIdle()
//        {
//            var src = Parse("abcd-  -ef-    ---|");
//            var smp = Parse("x   -xx-  -xxxx");
//            var exp = Parse("a   -bc-  -def ---|");
//            await exp.AssertEqual(smp.Zip(src.Buffer(), (_, x) => x));
//        }

//        [Fact]
//        public async Task CompleteWhileBuffered()
//        {
//            var src = TestSequence.Parse("abcd-  -ef|");
//            var smp = TestSequence.Parse("x   -xx-  -xxxx");
//            var exp = TestSequence.Parse("a   -bc-  -def|");
//            await exp.AssertEqual(smp.Zip(src.Buffer(), (_, x) => x));
//        }

//        [Fact]
//        public async Task FailWhileIdle()
//        {
//            var src = TestSequence.Parse("abcd-  -ef-    ---#");
//            var smp = TestSequence.Parse("x   -xx-  -xxxx");
//            var exp = TestSequence.Parse("a   -bc-  -def ---#");
//            await exp.AssertEqual(smp.Zip(src.Buffer(), (_, x) => x));
//        }

//        [Fact]
//        public async Task FailWhileBuffered()
//        {
//            var src = TestSequence.Parse("abcd-  -ef#");
//            var smp = TestSequence.Parse("x   -xx-   -xxx");
//            var exp = TestSequence.Parse("a   -bc-   -def#");
//            await exp.AssertEqual(smp.Zip(src.Buffer(), (_, x) => x));
//        }
//    }
//}
