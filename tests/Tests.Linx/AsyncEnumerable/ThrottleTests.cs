namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class ThrottleTests
    {
        [Fact]
        public async Task CompleteWhileIdle()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-- -|").DematerializeAsyncEnumerable();
            var expect = Marble.Parse("- -  - --d- -  --g-|");
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);

            using (var vt = new VirtualTime())
            {
                var eq = testee.AssertEqual(expect, default);
                vt.Start();
                await eq;
            }
        }

        [Fact]
        public async Task CompleteWhileThrottling()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-|").DematerializeAsyncEnumerable();
            var expect = Marble.Parse("- -  - --d- -  -|");
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);

            using (var vt = new VirtualTime())
            {
                var eq = testee.AssertEqual(expect, default);
                vt.Start();
                await eq;
            }
        }

        [Fact]
        public async Task FailWhileIdle()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg-- -#").DematerializeAsyncEnumerable();
            var expect = Marble.Parse("- -  - --d- -  --g-#");
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);

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
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);

            using (var vt = new VirtualTime())
            {
                var eq = testee.AssertEqual(expect, default);
                vt.Start();
                await eq;
            }
        }

        [Fact]
        public async Task CancelWhileIdle()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg").DematerializeAsyncEnumerable();
            var expect = Marble.Parse("- -  - --d- -  --g-#", new MarbleSettings { Error = new OperationCanceledException() });
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);

            using (var vt = new VirtualTime())
            {
                var cts = new CancellationTokenSource();
                var cancel = vt.Schedule(() => cts.Cancel(), 10 * MarbleSettings.DefaultFrameSize, default);
                var eq = testee.AssertEqual(expect, cts.Token);
                vt.Start();
                await cancel;
                await eq;
            }
        }

        [Fact]
        public async Task CancelWhileThrottling()
        {
            var source = Marble.Parse("-a-bc-d-- -e-fg").DematerializeAsyncEnumerable();
            var expect = Marble.Parse("- -  - --d- -  -#", new MarbleSettings { Error = new OperationCanceledException() });
            var testee = source.Throttle(2 * MarbleSettings.DefaultFrameSize);

            using (var vt = new VirtualTime())
            {
                var cts = new CancellationTokenSource();
                var cancel = vt.Schedule(() => cts.Cancel(), 8 * MarbleSettings.DefaultFrameSize, default);
                var eq = testee.AssertEqual(expect, cts.Token);
                vt.Start();
                await cancel;
                await eq;
            }
        }
    }
}
