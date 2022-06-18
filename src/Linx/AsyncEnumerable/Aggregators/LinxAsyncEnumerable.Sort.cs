using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    public static async Task<List<T>> Sort<T>(this IOrderedAsyncEnumerable<T> source, CancellationToken token)
    {
        var result = await source.Source.ToList(token).ConfigureAwait(false);
        result.Sort(source.Comparison);
        return result;
    }

    public static async Task<List<KeyValuePair<int, T>>> SortStable<T>(this IOrderedAsyncEnumerable<T> source, CancellationToken token)
    {
        return await source.Source
            .Index()
            .OrderBy(kv => kv.Value, source.Comparison)
            .ThenBy(kv => kv.Key)
            .Sort(token);
    }

}
