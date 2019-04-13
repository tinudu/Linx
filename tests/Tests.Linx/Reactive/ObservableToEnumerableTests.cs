namespace Tests.Linx.Reactive
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.Reactive;
    using Xunit;

    public sealed class ObservableToEnumerableTests
    {
        //[Fact]
        //public async Task TestLatest()
        //{
        //    var src = Enumerable.Range(0, int.MaxValue).Async()
        //        .Where(async (i, t) =>
        //        {
        //            await Task.Delay(1, t).ConfigureAwait(false);
        //            return true;
        //        })
        //        .Latest();

        //    var result = new List<int>();
        //    var ae = src.GetAsyncEnumerator(CancellationToken.None);
        //    try
        //    {
        //        while (await ae.MoveNextAsync().ConfigureAwait(false))
        //        {
        //            result.Add(ae.Current);
        //            if (result.Count == 10) break;
        //            await Task.Delay(100).ConfigureAwait(false);
        //        }
        //    }
        //    finally { await ae.DisposeAsync().ConfigureAwait(false); }

        //    Assert.True(Enumerable.Range(0, result.Count - 1).All(i => result[i] < result[i + 1]));
        //}

        //[Fact]
        //public async Task TestBuffer()
        //{
        //    var src = Enumerable.Range(0, int.MaxValue).Async()
        //        .Where(async (i, t) =>
        //        {
        //            await Task.Delay(1, t).ConfigureAwait(false);
        //            return true;
        //        })
        //        .Buffer();

        //    var result = new List<int>();
        //    var ae = src.GetAsyncEnumerator(CancellationToken.None);
        //    try
        //    {
        //        while (await ae.MoveNextAsync().ConfigureAwait(false))
        //        {
        //            result.AddRange(ae.Current);
        //            if (result.Count >= 100) break;
        //            await Task.Delay(1000).ConfigureAwait(false);
        //        }
        //    }
        //    finally { await ae.DisposeAsync().ConfigureAwait(false); }

        //    Assert.True(Enumerable.Range(0, result.Count).SequenceEqual(result));
        //}
    }
}
