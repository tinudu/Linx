namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class DemoTests
    {
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
            var src = "grrbrggbr".Async()
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

                    IAsyncEnumerable<ColorAndIndex> ColorGroup(char color) => LinxAsyncEnumerable.Create<ColorAndIndex>(async (y, t) =>
                    {
                        var ae = groups.Where(g => g.Key == color).WithCancellation(t).ConfigureAwait(false).GetAsyncEnumerator();
                        ConfiguredCancelableAsyncEnumerable<ColorAndIndex>.Enumerator ae1;
                        try
                        {
                            if (!await ae.MoveNextAsync()) return;
                            ae1 = ae.Current.WithCancellation(t).ConfigureAwait(false).GetAsyncEnumerator();
                        }
                        finally { await ae.DisposeAsync(); }

                        try
                        {
                            while (await ae1.MoveNextAsync() && await y(ae1.Current).ConfigureAwait(false)) { }
                        }
                        finally { await ae.DisposeAsync(); }
                    });
                });

            var result = await redAndOther.ToList(default);
        }
    }
}
