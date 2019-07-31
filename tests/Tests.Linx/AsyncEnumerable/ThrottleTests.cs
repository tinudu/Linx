namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Observable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class ThrottleTests
    {
        [Fact]
        public async Task Success()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-- -|").DematerializeAsyncEnumerable();
            var expect = Marble.Parse("- -  - --d- -  --g-|");
            var testee = source.Throttle(2 * MarbleParserSettings.DefaultFrameSize).Latest();

            using (var vt = new VirtualTime())
            {
                var eq = testee.AssertEqual(expect, default);
                vt.Start();
                await eq;
            }
        }

        [Fact]
        public async Task FailWhileThrottling()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-#").DematerializeAsyncEnumerable();
            var expect = Marble.Parse("- -  - --d- -  -#");
            var testee = source.Throttle(2 * MarbleParserSettings.DefaultFrameSize).Latest();

            using (var vt = new VirtualTime())
            {
                var eq = testee.AssertEqual(expect, default);
                vt.Start();
                await eq;
            }
        }

        [Fact]
        public async Task FailWhileWaiting()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-- -#").DematerializeAsyncEnumerable();
            var expect = Marble.Parse("- -  - --d- -  --g-#");
            var testee = source.Throttle(2 * MarbleParserSettings.DefaultFrameSize).Latest();

            using (var vt = new VirtualTime())
            {
                var eq = testee.AssertEqual(expect, default);
                vt.Start();
                await eq;
            }
        }

        [Fact]
        public async Task CancelWhileThrottling()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg").DematerializeAsyncEnumerable();
            var expect = Marble.Parse("- -  - --d- -  -#", new MarbleParserSettings { Error = new OperationCanceledException() });
            var testee = source.Throttle(2 * MarbleParserSettings.DefaultFrameSize).Latest();

            using (var vt = new VirtualTime())
            {
                var cts = new CancellationTokenSource();
                var cancel = vt.Schedule(() => cts.Cancel(), 8 * MarbleParserSettings.DefaultFrameSize, default);
                var eq = testee.AssertEqual(expect, cts.Token);
                vt.Start();
                await cancel;
                await eq;
            }
        }
    }
}
