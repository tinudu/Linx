//namespace Tests.Linx.AsyncEnumerable
//{
//    using System.Threading.Tasks;
//    using global::Linx.AsyncEnumerable;
//    using global::Linx.Testing;
//    using Xunit;

//    public sealed class ThrottleTests
//    {
//        [Fact]
//        public async Task CompleteWhileIdle()
//        {
//            var source = TestSequence.Parse("-a-bc-d-- -e-fg-- -|");
//            var expect = TestSequence.Parse("- -  - --d- -  --g-|");
//            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);
//            await expect.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task CompleteWhileThrottling()
//        {
//            var source = TestSequence.Parse("-a-bc-d-- -e-fg-|");
//            var expect = TestSequence.Parse("- -  - --d- -  -g|");
//            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);
//            await expect.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task FailWhileIdle()
//        {
//            var source = TestSequence.Parse("-a-bc-d-- -e-fg-- -#");
//            var expect = TestSequence.Parse("- -  - --d- -  --g-#");
//            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);
//            await expect.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task FailWhileThrottling()
//        {
//            var source = TestSequence.Parse("-a-bc-d-- -e-fg-#");
//            var expect = TestSequence.Parse("- -  - --d- -  -#");
//            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);
//            await expect.AssertEqual(testee);
//        }

//        [Fact]
//        public async Task CancelWhileIdle()
//        {
//            var source = TestSequence.Parse("-a-bc-d-- -e-fg");
//            var expect = TestSequence.Parse("- -  - --d- -  --g-#");
//            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);
//            await expect.AssertEqualCancel(testee, 10 * MarbleSettings.DefaultFrameSize);
//        }

//        [Fact]
//        public async Task CancelWhileThrottling()
//        {
//            var source = TestSequence.Parse("-a-bc-d-- -e-fg");
//            var expect = TestSequence.Parse("- -  - --d- -  -#");
//            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize).MostRecent().Select(x => x.GetResult().Value);
//            await expect.AssertEqualCancel(testee, 8 * MarbleSettings.DefaultFrameSize);
//        }
//    }
//}
