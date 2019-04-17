namespace Tests.Linx.Reactive
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.Reactive;
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

            result = await LinxReactive.Empty<int>().All(x => x == 0, CancellationToken.None);
            Assert.True(result);
        }

        [Fact]
        public async Task TestAny()
        {
            var result = await new[] { 1, 2, 3 }.Async().Any(CancellationToken.None);
            Assert.True(result);

            result = await LinxReactive.Empty<int>().Any(CancellationToken.None);
            Assert.False(result);
        }

        [Fact]
        public async Task TestFirst()
        {
            var result = await new[] { 1, 2, 3 }.Async().First(CancellationToken.None);
            Assert.Equal(1, result);
            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxReactive.Empty<int>().First(CancellationToken.None));
        }

        [Fact]
        public async Task TestLast()
        {
            var result = await new[] { 1, 2, 3 }.Async().Last(CancellationToken.None);
            Assert.Equal(3, result);
            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxReactive.Empty<int>().Last(CancellationToken.None));
        }

        [Fact]
        public void TestMultiAggregate()
        {
            var result = Enumerable.Range(1, 3).MultiAggregate(
                (s, t) => s.Sum(t),
                (s, t) => s.Take(2).ToList(t),
                (sum, list) => new { sum, list });
            Assert.Equal(6, result.sum);
            Assert.True(new[] { 1, 2 }.SequenceEqual(result.list));
        }

        [Fact]
        public async Task TestSingle()
        {
            var result = await new[] { 42 }.Async().Single(CancellationToken.None);
            Assert.Equal(42, result);

            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxReactive.Empty<int>().Single(CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => new[] { 1, 2, 3 }.Async().Single(CancellationToken.None));
        }

        [Fact]
        public async Task TestSingleOrDefault()
        {
            var result = await new[] { 42 }.Async().SingleOrDefault(CancellationToken.None);
            Assert.Equal(42, result);

            result = await LinxReactive.Empty<int>().SingleOrDefault(CancellationToken.None);
            Assert.Equal(0, result);

            await Assert.ThrowsAsync<InvalidOperationException>(() => new[] { 1, 2, 3 }.Async().SingleOrDefault(CancellationToken.None));
        }

        [Fact]
        public async Task TestSingleOrNull()
        {
            var result = await new[] { 42 }.Async().SingleOrNull(CancellationToken.None);
            Assert.Equal(42, result);

            result = await LinxReactive.Empty<int>().SingleOrNull(CancellationToken.None);
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
