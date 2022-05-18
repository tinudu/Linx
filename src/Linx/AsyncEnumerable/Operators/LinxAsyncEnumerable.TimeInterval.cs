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
    /// Records the time interval between consecutive values.
    /// </summary>
    public static IAsyncEnumerable<TimeInterval<T>> TimeInterval<T>(this IAsyncEnumerable<T> source, ITime time)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (time is null) time = Time.RealTime;

        return Iterator();

        async IAsyncEnumerable<TimeInterval<T>> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var t0 = time.Now;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            {
                var t = time.Now;
                yield return new TimeInterval<T>(t - t0, item);
                t0 = t;
            }
        }
    }
}
