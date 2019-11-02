namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class GroupByTests
    {
        private static Task<string> Stringify(IAsyncEnumerable<(char, int)> source, CancellationToken token)
            => source.Aggregate(
                new StringBuilder(),
                (sb, x) =>
                {
                    sb.Append(x.Item1);
                    sb.Append(x.Item2);
                    return sb;
                },
                sb => sb.ToString(),
                token);


        [Fact]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public async Task Success()
        {
            var source = "Abracadabra".Async().GroupBy(char.ToUpperInvariant);

            // run to end
            var r = await Stringify(source.Parallel(async (g, t) => (g.Key, Count: await g.Count(t)), true), default);
            Assert.Equal("A5B2R2C1D1", r);

            // dispose group early
            r = await Stringify(source.Parallel(async (g, t) => (g.Key, Count: await g.Take(3).Count(t)), true), default);
            Assert.Equal("A3B2R2C1D1", r);

            // dispose GroupBy early
            r = await Stringify(source.Take(3).Parallel(async (g, t) => (g.Key, Count: await g.Count(t)), true), default);
            Assert.Equal("A5B2R2", r);

            // cancel GroupBy
            r = await Stringify(source.Take(2).Parallel(async (g, t) => (g.Key, Count: await g.Take(1).Count(t)), true), default);
            Assert.Equal("A1B1", r);
        }

        [Fact]
        public async Task Error()
        {
            var source = Marble.Parse("Abracadabra#").GroupBy(char.ToUpperInvariant);

            var groups = new List<char>();
            async Task<bool> Selector(IAsyncGrouping<char, char> g, CancellationToken t)
            {
                try
                {
                    await g.IgnoreElements(t).ConfigureAwait(false);
                    return false;
                }
                catch (MarbleException)
                {
                    groups.Add(g.Key);
                    throw;
                }
            }

            await Assert.ThrowsAsync<MarbleException>(() => source.Parallel(Selector).Any(default));
            groups.Sort();
            Assert.Equal("ABCDR", new string(groups.ToArray()));
        }

        [Fact]
        public async Task WhileEnumerated()
        {
            var result = await "ABabABababABababab".Async()
                .GroupByWhileEnumerated(char.ToUpper)
                .Parallel(async (g, t) => new string(await g.TakeUntil(char.IsUpper).ToArray(t).ConfigureAwait(false)), true)
                .ToList(default);
            var expected = new[] { "A", "B", "aA", "bB", "aaA", "bbB", "aaa", "bbb" };
            Assert.True(expected.SequenceEqual(result));
        }
    }
}
