namespace Tests.Linx.AsyncEnumerable
{
    using System.Linq;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.AsyncEnumerable.Testing;
    using global::Linx.AsyncEnumerable.Timing;
    using Xunit;

    /// <summary>
    /// Tests of the <see cref="MarbleParser"/>.
    /// </summary>
    public sealed class MarbleTests
    {
        [Fact]
        public async Task TestComplete()
        {
            using (var vt = new VirtualTime())
            {
                var m = MarbleParser.Parse("-a-b-|").Dematerialize();
                var elements = await m.ToList(default);
                Assert.True("ab".SequenceEqual(elements));
                Assert.Equal(6, vt.Now.TimeOfDay.TotalSeconds);
            }
        }

        [Fact]
        public async Task TestError()
        {
            using (var vt = new VirtualTime())
            {
                var m = MarbleParser.Parse("-a-b-#").Dematerialize();
                await Assert.ThrowsAsync<MarbleException>(() => m.ToList(default));
                Assert.Equal(6, vt.Now.TimeOfDay.TotalSeconds);
            }
        }

        [Fact]
        public async Task TestForever()
        {
            using (var vt = new VirtualTime())
            {
                var m = MarbleParser.Parse("-a-b*-c-d").Dematerialize();
                var elements = await m.Take(7).ToList(default);
                Assert.True("abcdcdc".SequenceEqual(elements));
                Assert.Equal(14, vt.Now.TimeOfDay.TotalSeconds);
            }
        }
    }
}
