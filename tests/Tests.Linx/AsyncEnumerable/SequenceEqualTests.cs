namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.AsyncEnumerable.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class SequenceEqualTests
    {
        [Fact]
        public async Task Empty()
        {
            Assert.True(await LinxAsyncEnumerable.Empty<char>().SequenceEqual(LinxAsyncEnumerable.Empty<char>(), default));
            Assert.False(await LinxAsyncEnumerable.Empty<char>().SequenceEqual(LinxAsyncEnumerable.Return('a'), default));
        }

        [Fact]
        public async Task Completed()
        {
            var i1 = Marble.Parse("-a- b-  c-|").Dematerialize();
            var i2 = Marble.Parse(" a--b---c|").Dematerialize();
            var i3 = Marble.Parse("-a- b-  c-d").Dematerialize();
            using (new VirtualTime())
            {
                Assert.True(await i1.SequenceEqual(i2, default));
                Assert.False(await i1.SequenceEqual(i3, default));
            }
        }

        [Fact]
        public async Task Error()
        {
            var i1 = Marble.Parse("-a- b-  c-|").Dematerialize();
            var i2 = Marble.Parse(" a--b---c#").Dematerialize();
            var i3 = Marble.Parse("-a- b-  c-d#").Dematerialize();
            using (new VirtualTime())
            {
                await Assert.ThrowsAsync<MarbleException>(() => i1.SequenceEqual(i2, default));
                Assert.False(await i1.SequenceEqual(i3, default));
            }
        }

        [Fact]
        public async Task Cancel()
        {
            var i1 = Marble.Parse("-a- b-  c").Dematerialize();
            var i2 = Marble.Parse(" a--b---c---#").Dematerialize();

            using (var vt = new VirtualTime())
            {
                var cts = new CancellationTokenSource();
                var t = i1.SequenceEqual(i1, cts.Token);
                await vt.Delay(TimeSpan.FromHours(1), default);
                cts.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => t);
            }

            using (var vt = new VirtualTime())
            {
                var cts = new CancellationTokenSource();
                var t = i1.SequenceEqual(i2, cts.Token);
                await vt.Delay(TimeSpan.FromSeconds(8), default);
                cts.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => t);
            }
        }
    }
}
