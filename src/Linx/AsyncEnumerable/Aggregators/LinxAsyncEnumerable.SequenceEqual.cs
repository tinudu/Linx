namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Compare two secquences for equality.
        /// </summary>
        public static async Task<bool> SequenceEqual<T>(this IAsyncEnumerable<T> first, IEnumerable<T> second, CancellationToken token, IEqualityComparer<T> comparer = null)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (comparer == null) comparer = EqualityComparer<T>.Default;

            token.ThrowIfCancellationRequested();
            var ae = first.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                using (var e = second.GetEnumerator())
                {
                    while (true)
                    {
                        var hasNext1 = await ae.MoveNextAsync();
                        var hasNext2 = e.MoveNext();
                        if (hasNext1 != hasNext2) return false;
                        if (!hasNext1) return true;
                        if (!comparer.Equals(ae.Current, e.Current)) return false;
                    }
                }
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}
