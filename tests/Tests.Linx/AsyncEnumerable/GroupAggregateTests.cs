using System.Threading.Tasks;
using Linx.AsyncEnumerable;
using Xunit;

namespace Tests.Linx.AsyncEnumerable;

public sealed class GroupAggregateTests
{
    [Fact]
    public async Task Success()
    {
        var result = await "Abracadabra".ToAsync()
            .GroupAggregate(char.ToUpperInvariant, (g, t) => g.Count(t))
            .ToDictionary(default);
        Assert.Equal(5, result['A']);
        Assert.Equal(2, result['B']);
        Assert.Equal(1, result['C']);
        Assert.Equal(1, result['D']);
        Assert.Equal(2, result['R']);
    }
}
