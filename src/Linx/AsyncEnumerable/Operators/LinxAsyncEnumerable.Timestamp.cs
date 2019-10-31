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
        /// Records the timestamp for each value.
        /// </summary>
        public static IAsyncEnumerable<Timestamped<T>> Timestamp<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(GetEnumerator);

            async IAsyncEnumerator<Timestamped<T>> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var time = Time.Current;
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    yield return new Timestamped<T>(time.Now, item);
            }
        }
    }
}
