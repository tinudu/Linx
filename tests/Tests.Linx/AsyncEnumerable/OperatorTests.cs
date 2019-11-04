namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class OperatorTests
    {
        [Fact]
        public async Task TestConcat()
        {
            var r = Enumerable.Range(0, 3).Async();
            // ReSharper disable PossibleMultipleEnumeration
            var result = await r.Concat(r, r).ToList(default);
            // ReSharper restore PossibleMultipleEnumeration
            Assert.True(new[] { 0, 1, 2, 0, 1, 2, 0, 1, 2 }.SequenceEqual(result));
        }

        [Fact]
        public async Task TestTake()
        {
            var result = await new[] { 1, 2, 3, 4 }.Async().Where(i => i != 2).Take(2).ToList(CancellationToken.None);
            Assert.True(new[] { 1, 3 }.SequenceEqual(result));
        }

        [Fact]
        public async Task TestZip()
        {
            var src1 = Marble.Parse("a-  - -bc- -d- ---e");
            var src2 = Marble.Parse(" -fg-h-  -i- -|  ");
            var expt = Marble.Parse(" -x - -xx- -x- ---|", default, "af", "bg", "ch", "di");
            var testee = src1.Zip(src2, (x, y) => $"{x}{y}");
            await expt.AssertEqual(testee);
        }
    }
}