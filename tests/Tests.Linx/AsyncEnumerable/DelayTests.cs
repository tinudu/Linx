//namespace Tests.Linx.AsyncEnumerable
//{
//    using global::Linx.AsyncEnumerable;
//    using global::Linx.Testing;
//    using System.Threading.Tasks;
//    using Xunit;

//    public sealed class DelayTests
//    {
//        [Fact]
//        public async Task TestDelay()
//        {
//            var delay = 3 * MarbleSettings.DefaultFrameSize;
//            var source = TestSequence.Parse("   -a-bc-d-|");
//            var expect = TestSequence.Parse("----a-bc-d-|");
//            var testee = source.Delay(delay);
//            await expect.AssertEqual(testee);
//        }

//    }
//}
