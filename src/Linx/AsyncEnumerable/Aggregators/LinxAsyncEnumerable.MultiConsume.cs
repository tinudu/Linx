using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Multiple consumers sharing a subscription.
    /// </summary>
    public static ValueTask MultiConsume<T>(this IAsyncEnumerable<T> source, IEnumerable<ConsumerDelegate<T>> consumers, CancellationToken token)
        => source.Cold().MultiConsume(consumers, token);

    /// <summary>
    /// Multiple consumers sharing a subscription.
    /// </summary>
    public static ValueTask MultiConsume<T>(this IAsyncEnumerable<T> source, CancellationToken token, params ConsumerDelegate<T>[] consumers)
        => source.Cold().MultiConsume(consumers, token);
}
