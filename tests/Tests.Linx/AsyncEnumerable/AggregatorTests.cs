namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using Xunit;

    public class AggregatorTests
    {
        [Fact]
        public async Task TestAll()
        {
            var result = await new[] { 1, 2, 3 }.Async().All(x => x < 10, CancellationToken.None);
            Assert.True(result);

            result = await new[] { 1, 2, 3 }.Async().All(x => x < 2, CancellationToken.None);
            Assert.False(result);

            result = await LinxAsyncEnumerable.Empty<int>().All(x => x == 0, CancellationToken.None);
            Assert.True(result);
        }

        [Fact]
        public async Task TestAny()
        {
            var result = await new[] { 1, 2, 3 }.Async().Any(CancellationToken.None);
            Assert.True(result);

            result = await LinxAsyncEnumerable.Empty<int>().Any(CancellationToken.None);
            Assert.False(result);
        }

        [Fact]
        public async Task TestFirst()
        {
            var result = await new[] { 1, 2, 3 }.Async().First(CancellationToken.None);
            Assert.Equal(1, result);
            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().First(CancellationToken.None));
        }

        [Fact]
        public async Task TestLast()
        {
            var result = await new[] { 1, 2, 3 }.Async().Last(CancellationToken.None);
            Assert.Equal(3, result);
            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().Last(CancellationToken.None));
        }

        [Fact]
        public void TestMultiAggregate()
        {
            var result = Enumerable.Range(1, 3).MultiAggregate((s, t) => s.Sum(t),
                (s, t) => s.First(t),
                (s, t) => s.ElementAt(1, t),
                (sum, first, second) => new { sum, first, second });
            Assert.Equal(6, result.sum);
            Assert.Equal(1, result.first);
            Assert.Equal(2, result.second);
        }

        [Fact]
        public async Task TestMultiAggregateFail()
        {
            var tResult = new[] { 2, 0, 1 }.Async().MultiAggregate((s, t) => s.First(t),
                (s, t) => s.ToList(t),
                (s, t) => s.Select(i => 1 / i).Sum(t),
                (first, all, sumInv) => new { first, all, sumInv },
                default);
            await Assert.ThrowsAsync<DivideByZeroException>(async () => await tResult);
        }

        [Fact]
        public async Task TestMultiAggregateCancel()
        {
            SynchronizationContext.SetSynchronizationContext(null);
            var cts = new CancellationTokenSource();
            var src = LinxAsyncEnumerable.Produce<int>(async (yield, token) =>
            {
                await yield(1);
                await yield(2);
                cts.Cancel();
                await yield(3);
            });
            var tResult = src.MultiAggregate(
                (s, t) => s.ToList(t),
                (s, t) => s.Sum(t),
                (all, sum) => new { all, sum },
                cts.Token);
            var oce = await Assert.ThrowsAsync<OperationCanceledException>(() => tResult);
            Assert.Equal(cts.Token, oce.CancellationToken);
        }

        [Fact]
        public async Task TestAverage()
        {
            Assert.Equal(5.5, await LinxAsyncEnumerable.Range(1, 10).Average(default));
        }

        [Fact]
        public async Task TestSingle()
        {
            var result = await new[] { 42 }.Async().Single(CancellationToken.None);
            Assert.Equal(42, result);

            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().Single(CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => new[] { 1, 2, 3 }.Async().Single(CancellationToken.None));
        }

        [Fact]
        public async Task TestSingleOrDefault()
        {
            var result = await new[] { 42 }.Async().SingleOrDefault(CancellationToken.None);
            Assert.Equal(42, result);

            result = await LinxAsyncEnumerable.Empty<int>().SingleOrDefault(CancellationToken.None);
            Assert.Equal(0, result);

            await Assert.ThrowsAsync<InvalidOperationException>(() => new[] { 1, 2, 3 }.Async().SingleOrDefault(CancellationToken.None));
        }

        [Fact]
        public async Task TestSingleOrNull()
        {
            var result = await new[] { 42 }.Async().SingleOrNull(CancellationToken.None);
            Assert.Equal(42, result);

            result = await LinxAsyncEnumerable.Empty<int>().SingleOrNull(CancellationToken.None);
            Assert.False(result.HasValue);

            await Assert.ThrowsAsync<InvalidOperationException>(() => new[] { 1, 2, 3 }.Async().SingleOrNull(CancellationToken.None));
        }

        [Fact]
        public async Task TestToList()
        {
            var source = new[] { 1, 2, 3 }.Async();
            var result = await source.ToList(CancellationToken.None);
            Assert.True(new[] { 1, 2, 3 }.SequenceEqual(result));
        }
    }
}
