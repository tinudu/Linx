namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class MergeTests
    {
        [Fact]
        public async Task TestMerge()
        {
            var s1 = Marble.Parse("-a- -c- -e|");
            var s2 = Marble.Parse("- -b- -d- -f|");
            var ex = Marble.Parse("-a-b-c-d-e-f|");
            var testee = s1.Merge(s2);
            await ex.AssertEqual(testee);
        }
    }
}
