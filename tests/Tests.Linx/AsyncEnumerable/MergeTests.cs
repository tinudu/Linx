//namespace Tests.Linx.AsyncEnumerable
//{
//    using global::Linx.AsyncEnumerable;
//    using global::Linx.Testing;
//    using global::Linx.Timing;
//    using System.Threading.Tasks;
//    using Xunit;

//    public sealed class MergeTests
//    {
//        [Fact]
//        public async Task Success()
//        {
//            var s1 = TestSequence.Parse("-a- -c- -e|");
//            var s2 = TestSequence.Parse("- -b- -d- -f|");
//            var ex = TestSequence.Parse("-a-b-c-d-e-f|");
//            var testee = s1.Merge(s2);
//            await ex.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task Dispose()
//        {
//            var s1 = TestSequence.Parse("-a- -c- -e|");
//            var s2 = TestSequence.Parse("- -b- -d- -f|");
//            var ex = TestSequence.Parse("-a-b-c|");
//            var testee = s1.Merge(s2).Take(3);
//            await ex.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task MaxConcurrent()
//        {
//            var s1 = TestSequence.Parse("-a- -c- -e|");
//            var s2 = TestSequence.Parse("- -b- -d- -f|");
//            var s3 = TestSequence.Parse("          - -g|");
//            var ex = TestSequence.Parse("-a-b-c-d-e-f-g|");
//            var testee = new[] { s1, s2, s3 }.ToAsyncEnumerable().Merge(2);
//            await ex.AssertEqual(testee);
//        }
//    }
//}
