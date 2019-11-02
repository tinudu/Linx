namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Observable;
    using global::Linx.Testing;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class ThrottleTests
    {
        [Fact]
        public async Task CompleteWhileIdle()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-- -|");
            var expect = Marble.Parse("- -  - --d- -  --g-|");
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize).Latest();
            await expect.AssertEqual(testee);
        }

        [Fact]
        public async Task CompleteWhileThrottling()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-|");
            var expect = Marble.Parse("- -  - --d- -  -|");
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize).Latest();
            await expect.AssertEqual(testee);
        }

        [Fact]
        public async Task FailWhileIdle()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-- -#");
            var expect = Marble.Parse("- -  - --d- -  --g-#");
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize).Latest();
            await expect.AssertEqual(testee);
        }

        [Fact]
        public async Task FailWhileThrottling()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-#");
            var expect = Marble.Parse("- -  - --d- -  -#");
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize).Latest();
            await expect.AssertEqual(testee);
        }

        [Fact]
        public async Task CancelWhileIdle()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg");
            var expect = Marble.Parse("- -  - --d- -  --g-#");
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize).Latest();
            await expect.AssertCancel(testee, 10 * MarbleSettings.DefaultFrameSize);
        }

        [Fact]
        public async Task CancelWhileThrottling()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg");
            var expect = Marble.Parse("- -  - --d- -  -#");
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize).Latest();
            await expect.AssertCancel(testee, 8 * MarbleSettings.DefaultFrameSize);
        }
    }
}
