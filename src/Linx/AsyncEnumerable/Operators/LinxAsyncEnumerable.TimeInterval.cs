namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Records the time interval between consecutive values.
        /// </summary>
        public static IAsyncEnumerable<TimeInterval<T>> TimeInterval<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(GetEnumerator);

            async IAsyncEnumerator<TimeInterval<T>> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var time = Time.Current;
                var t0 = time.Now;
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                {
                    var t = time.Now;
                    yield return new TimeInterval<T>(t - t0, item);
                    t0 = t;
                }
            }
        }
    }
}
