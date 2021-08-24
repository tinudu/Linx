namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
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
            var i1 = Marble.Parse("-a- b-  c-|");
            var i2 = Marble.Parse(" a--b---c|");
            var i3 = Marble.Parse("-a- b-  c-d");
            Assert.True((await VirtualTime.Run(() => i1.SequenceEqual(i2, default))).Value);
            Assert.False((await VirtualTime.Run(() => i1.SequenceEqual(i3, default))).Value);
        }

        [Fact]
        public async Task Error()
        {
            var i1 = Marble.Parse("-a- b-  c-|");
            var i2 = Marble.Parse(" a--b---c#");
            var i3 = Marble.Parse("-a- b-  c-d#");
            await Assert.ThrowsAsync<MarbleException>(() => VirtualTime.Run(() => i1.SequenceEqual(i2, default)));
            Assert.False((await VirtualTime.Run(() => i1.SequenceEqual(i3, default))).Value);
        }

        [Fact]
        public async Task Cancel()
        {
            var i1 = Marble.Parse(" -a-bc");
            var i2 = Marble.Parse("a- -b ---c---#");
            await Marble.AssertCancel(t => i1.SequenceEqual(i1, t), TimeSpan.FromHours(1));
            await Marble.AssertCancel(t => i1.SequenceEqual(i2, t), TimeSpan.FromSeconds(7));
        }
    }
}
