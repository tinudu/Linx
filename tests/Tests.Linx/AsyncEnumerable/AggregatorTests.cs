namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.Enumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using Xunit;

    public class AggregatorTests
    {
        [Fact]
        public async Task TestAll()
        {
            var result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().All(x => x < 10, CancellationToken.None);
            Assert.True(result);

            result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().All(x => x < 2, CancellationToken.None);
            Assert.False(result);

            result = await LinxAsyncEnumerable.Empty<int>().All(x => x == 0, CancellationToken.None);
            Assert.True(result);
        }

        [Fact]
        public async Task TestAny()
        {
            var result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().Any(CancellationToken.None);
            Assert.True(result);

            result = await LinxAsyncEnumerable.Empty<int>().Any(CancellationToken.None);
            Assert.False(result);
        }

        [Fact]
        public async Task TestFirst()
        {
            var result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().First(CancellationToken.None);
            Assert.Equal(1, result);
            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().First(CancellationToken.None));
        }

        [Fact]
        public async Task TestLast()
        {
            var result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().Last(CancellationToken.None);
            Assert.Equal(3, result);
            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().Last(CancellationToken.None));
        }

        [Fact]
        public async Task TestMultiAggregate()
        {
            var result = await Enumerable.Range(1, 3).ToAsyncEnumerable().MultiAggregate(
                (s, t) => s.Sum(t),
                (s, t) => s.First(t),
                (s, t) => s.ElementAt(1, t),
                (sum, first, second) => new { sum, first, second },
                default);
            Assert.Equal(6, result.sum);
            Assert.Equal(1, result.first);
            Assert.Equal(2, result.second);
        }

        [Fact]
        public async Task TestMultiAggregateFail()
        {
            var tResult = new[] { 2, 0, 1 }.ToAsyncEnumerable().MultiAggregate((s, t) => s.First(t),
                (s, t) => s.ToList(t),
                (s, t) => s.Select(i => 1 / i).Sum(t),
                (first, all, sumInv) => new { first, all, sumInv },
                default);
            await Assert.ThrowsAsync<DivideByZeroException>(async () => await tResult);
        }

        [Fact]
        public async Task TestMultiAggregateCancel()
        {
            using (var vt = new VirtualTime())
            {
                var src = Marble.Parse("01--2|", null, 0, 1, 2).DematerializeAsyncEnumerable();
                var cts = new CancellationTokenSource();
                var tResult = src.MultiAggregate(
                    (s, t) => s.ToList(t),
                    (s, t) => s.Sum(t),
                    (all, sum) => new { all, sum },
                    cts.Token);
                // ReSharper disable once MethodSupportsCancellation
                var tCancel = vt.Schedule(() => cts.Cancel(), TimeSpan.FromSeconds(1));
                vt.Start();
                await tCancel;
                var oce = await Assert.ThrowsAsync<OperationCanceledException>(() => tResult);
                Assert.Equal(cts.Token, oce.CancellationToken);
            }
        }

        [Fact]
        public async Task TestAverage()
        {
            Assert.Equal(5.5, await Enumerable.Range(1, 10).ToAsyncEnumerable().Average(default));
        }

        [Fact]
        public async Task TestSingle()
        {
            var result = await new[] { 42 }.ToAsyncEnumerable().Single(CancellationToken.None);
            Assert.Equal(42, result);

            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().Single(CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => new[] { 1, 2, 3 }.ToAsyncEnumerable().Single(CancellationToken.None));
        }

        [Fact]
        public async Task TestSingleMaybe()
        {
            var result = await new[] { 42 }.ToAsyncEnumerable().SingleMaybe(CancellationToken.None);
            Assert.Equal(42, result.Value);

            result = await LinxAsyncEnumerable.Empty<int>().SingleMaybe(CancellationToken.None);
            Assert.False(result.HasValue);

            await Assert.ThrowsAsync<InvalidOperationException>(() => new[] { 1, 2, 3 }.ToAsyncEnumerable().SingleMaybe(CancellationToken.None));
        }

        [Fact]
        public async Task TestToList()
        {
            var source = new[] { 1, 2, 3 }.ToAsyncEnumerable();
            var result = await source.ToList(CancellationToken.None);
            Assert.True(new[] { 1, 2, 3 }.SequenceEqual(result));
        }
    }
}
