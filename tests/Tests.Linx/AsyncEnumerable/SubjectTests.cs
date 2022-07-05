using System.Linq;
using System.Threading.Tasks;
using Linx.AsyncEnumerable;
using Xunit;

namespace Tests.Linx.AsyncEnumerable;

public sealed class SubjectTests
{
    [Fact]
    public async Task Success()
    {
        var src = new[] { 1, 2, 3 };
        var subj = src.Cold().CreateSubject();
        var t1 = subj.AsyncEnumerable.ToList(default);
        var t2 = subj.AsyncEnumerable.Skip(1).First(default);
        subj.Connect();
        var r1 = await t1;
        var r2 = await t2;
        Assert.True(src.SequenceEqual(r1));
        Assert.Equal(2, r2);
    }

    [Fact]
    public async Task TestTooLate()
    {
        var src = new[] { 1, 2, 3 };
        var subj = src.Cold().CreateSubject();

        var e = subj.AsyncEnumerable.ConfigureAwait(false).GetAsyncEnumerator();
        try
        {
            subj.Connect();
            await Assert.ThrowsAsync<SubjectDisposedException>(async () => await e.MoveNextAsync());
        }
        finally { await e.DisposeAsync(); }
        Assert.Throws<SubjectAlreadyConnectedException>(() => subj.AsyncEnumerable.GetAsyncEnumerator());
    }
}
