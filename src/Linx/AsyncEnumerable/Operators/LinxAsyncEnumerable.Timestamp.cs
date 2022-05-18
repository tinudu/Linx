using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using global::Linx.Timing;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Records the timestamp for each value.
    /// </summary>
    public static IAsyncEnumerable<Timestamped<T>> Timestamp<T>(this IAsyncEnumerable<T> source, ITime time)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (time is null) time = Time.RealTime;

        return Iterator();

        async IAsyncEnumerable<Timestamped<T>> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                yield return new Timestamped<T>(time.Now, item);
        }
    }
}
