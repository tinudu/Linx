namespace Tests.Linx.AsyncEnumerable
{
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.AsyncEnumerable.Testing;
    using global::Linx.Timing;
    using Xunit;

    public sealed class MergeTests
    {
        [Fact]
        public async Task TestMerge()
        {
            var s1 = Marble.Parse("-a- -c- -e|").Dematerialize();
            var s2 = Marble.Parse("- -b- -d- -f|").Dematerialize();
            using (new VirtualTime())
            {
                var result = new string(await s1.Merge(s2).ToArray(default));
                Assert.Equal("abcdef", result);
            }
        }
    }
}
