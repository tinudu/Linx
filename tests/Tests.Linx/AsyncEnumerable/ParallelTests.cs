//namespace Tests.Linx.AsyncEnumerable
//{
//    using global::Linx.AsyncEnumerable;
//    using global::Linx.Testing;
//    using global::Linx.Timing;
//    using System.Threading.Tasks;
//    using Xunit;

//    public sealed class ParallelTests
//    {
//        [Fact]
//        public async Task TestSuccess()
//        {
//            var testee = new[] { 3, 1, 4, 0, 2 }.Parallel(async (i, t) =>
//            {
//                await Time.Current.Delay(i * MarbleSettings.DefaultFrameSize, t).ConfigureAwait(false);
//                return i;
//            });
//            var expected = TestSequence.Parse("0-1-2-3-4|", (c, i) => c - '0');
//            await expected.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task TestSuccessPreserveOrder()
//        {
//            var testee = new[] { 3, 1, 4, 0, 2 }.Parallel(async (i, t) =>
//            {
//                await Time.Current.Delay(i * MarbleSettings.DefaultFrameSize, t).ConfigureAwait(false);
//                return i;
//            }, true);
//            var expected = TestSequence.Parse("---31-402|", (c, i) => c - '0');
//            await expected.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task TestSuccessMaxConcurrent()
//        {
//            var testee = new[] { 3, 1, 4, 0, 2 }.Parallel(async (i, t) =>
//            {
//                await Time.Current.Delay(i * MarbleSettings.DefaultFrameSize, t).ConfigureAwait(false);
//                return i;
//            }, false, 3);
//            var expected = TestSequence.Parse("-10--32-4|", (c, i) => c - '0');
//            await expected.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task TestSuccessPreserveOrderMaxConcurrent()
//        {
//            var testee = new[] { 3, 1, 4, 0, 2 }.Parallel(async (i, t) =>
//            {
//                await Time.Current.Delay(i * MarbleSettings.DefaultFrameSize, t).ConfigureAwait(false);
//                return i;
//            }, true, 3);
//            var expected = TestSequence.Parse("---31-40-2|", (c, i) => c - '0');
//            await expected.AssertEqual(testee);
//        }
//    }
//}
