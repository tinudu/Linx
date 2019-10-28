namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
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
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public async Task Completed()
        {
            var i1 = Marble.Parse("-a- b-  c-|");
            var i2 = Marble.Parse(" a--b---c|");
            var i3 = Marble.Parse("-a- b-  c-d");
            using (var vt = new VirtualTime())
            {
                var testee = i1.SequenceEqual(i2, default);
                vt.Start();
                Assert.True(await testee);
            }
            using (var vt = new VirtualTime())
            {
                var testee = i1.SequenceEqual(i3, default);
                vt.Start();
                Assert.False(await testee);
            }
        }

        [Fact]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public async Task Error()
        {
            var i1 = Marble.Parse("-a- b-  c-|");
            var i2 = Marble.Parse(" a--b---c#");
            var i3 = Marble.Parse("-a- b-  c-d#");
            using (var vt = new VirtualTime())
            {
                var testee = i1.SequenceEqual(i2, default);
                vt.Start();
                await Assert.ThrowsAsync<MarbleException>(() => testee);
            }
            using (var vt = new VirtualTime())
            {
                var testee = i1.SequenceEqual(i3, default);
                vt.Start();
                Assert.False(await testee);
            }
        }

        [Fact]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public async Task Cancel()
        {
            var i1 = Marble.Parse(" -a-bc");
            var i2 = Marble.Parse("a- -b ---c---#");

            using (var vt = new VirtualTime())
            {
#pragma warning disable IDE0067 // Dispose objects before losing scope
                var cts = new CancellationTokenSource();
#pragma warning restore IDE0067 // Dispose objects before losing scope
                var t = i1.SequenceEqual(i1, cts.Token);
                var tCancel = vt.Schedule(() => cts.Cancel(), TimeSpan.FromHours(1), default);
                vt.Start();
                await tCancel;
                await Assert.ThrowsAsync<OperationCanceledException>(() => t);
            }

            using (var vt = new VirtualTime())
            {
#pragma warning disable IDE0067 // Dispose objects before losing scope
                var cts = new CancellationTokenSource();
#pragma warning restore IDE0067 // Dispose objects before losing scope
                var t = i1.SequenceEqual(i2, cts.Token);
                var tCancel = vt.Schedule(() => cts.Cancel(), TimeSpan.FromSeconds(7), default);
                vt.Start();
                await tCancel;
                await Assert.ThrowsAsync<OperationCanceledException>(() => t);
            }
        }
    }
}
