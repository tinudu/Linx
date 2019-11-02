namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
        
    public sealed class DemoTests
    {
        [Fact]
        public async Task GroupMerge()
        {
            const string colors = "grrbrggbr";
            var src = colors.Async().GroupBy(c => c).Merge();
            var result = new string(await src.ToArray(default));
            Assert.Equal(colors, result);
        }

        [Fact]
        public async Task GroupIndexMerge()
        {
            const string colors = "grrbrggbr";
            var src = colors.Async().GroupBy(c => c).Select(g => g.Select((_, i) => $"{g.Key}{i + 1}")).Merge();
            var result = await src.ToList(default);
            Assert.True(new[] { "g1", "r1", "r2", "b1", "r3", "g2", "g3", "b2", "r4" }.SequenceEqual(result));
        }

        [Fact]
        public async Task GroupAggregate()
        {
            const string colors = "grrbrggbr";
            var src = colors.Async()
                .GroupBy(c => c)
                .Parallel(async (g, t) => (g.Key, Value: await g.Count(t)));

            var result = await src.ToDictionary(kv => kv.Key, kv => kv.Value, default);
            Assert.Equal(3, result['g']);
            Assert.Equal(4, result['r']);
            Assert.Equal(2, result['b']);
        }

        [Fact]
        public async Task GroupBundle()
        {
            //                     112311231231222331122
            const string colors = "grrrrbrrrggggbrgrgrrg";
            var src = colors.Async()
                .GroupByWhileEnumerated(c => c)
                .Parallel(async (g, t) => $"{g.Key}{await g.Take(3).Count(default)}", true);

            var result = await src.ToList(default);
            Assert.True(new[] { "g3", "r3", "r3", "b2", "r3", "g3", "g2", "r2" }.SequenceEqual(result));
        }


        private sealed class ColorAndIndex
        {
            public char Color { get; }
            public int Index { get; }

            public ColorAndIndex(char color, int index)
            {
                Color = color;
                Index = index;
            }

            public override string ToString() => $"{Color}{Index}";
        }

        [Fact]
        public async Task Demo()
        {
            const string colors = "grrbrggbr";

            var src = colors.Async()
                .GroupBy(c => c)
                .Select(g => g.Select((c, i) => new ColorAndIndex(c, i)))
                .Merge();
            var bla = await src.ToList(default);

            var redAndOther = LinxAsyncEnumerable.Create(
                new
                {
                    RedIndex = default(int?),
                    Color = default(char),
                    ColorIndex = default(int)
                },
                async (yield, token) =>
                {
                    var stack = new Stack<ConnectDelegate>();
                    var groups = bla.Async().GroupBy(ci => ci.Color).Connectable(stack);
                    var red = ColorGroup('r').Prepend(null).Connectable(stack);
                    var enumeration = ColorGroup('g').Merge(ColorGroup('b'), ColorGroup('y'))
                        .Combine(red.Prepend(null), (c, r) => new { RedIndex = r?.Index, c.Color, ColorIndex = c.Index })
                        .CopyTo(yield, token);
                    stack.Connect();
                    await enumeration.ConfigureAwait(false);

                    IAsyncEnumerable<ColorAndIndex> ColorGroup(char color) => groups.Where(g => g.Key == color).Take(1).SelectMany(g => g);
                });

            var result = await redAndOther.ToList(default);
        }
    }
}
