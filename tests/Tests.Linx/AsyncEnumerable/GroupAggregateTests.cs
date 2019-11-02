namespace Tests.Linx.AsyncEnumerable
{
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using Xunit;

    public sealed class GroupAggregateTests
    {
        [Fact]
        public async Task Success()
        {
            var result = await "Abracadabra".Async()
                .GroupAggregate(char.ToUpperInvariant, (g, t) => g.Count(t))
                .ToDictionary(default);
            Assert.Equal(5, result['A']);
            Assert.Equal(2, result['B']);
            Assert.Equal(1, result['C']);
            Assert.Equal(1, result['D']);
            Assert.Equal(2, result['R']);
        }
    }
}
