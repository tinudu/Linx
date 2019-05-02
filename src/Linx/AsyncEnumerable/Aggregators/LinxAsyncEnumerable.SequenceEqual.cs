namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Compare two sequences for equality.
        /// </summary>
        public static async Task<bool> SequenceEqual<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, CancellationToken token, IEqualityComparer<T> comparer = null)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            token.ThrowIfCancellationRequested();
            var notificationComparer = NotificationComparer<T>.GetComparer(comparer);
            var seq1 = first.Select(Notification.Next).Concat(Return(Notification.Completed<T>()));
            var seq2 = second.Select(Notification.Next).Concat(Return(Notification.Completed<T>()));
            var ae = seq1.Zip(seq2, notificationComparer.Equals).WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                while (await ae.MoveNextAsync())
                    if (!ae.Current)
                        return false;
            }
            finally { await ae.DisposeAsync(); }
            return true;
        }

        /// <summary>
        /// Compare two sequences for equality.
        /// </summary>
        public static async Task<bool> SequenceEqual<T>(this IAsyncEnumerable<T> first, IEnumerable<T> second, CancellationToken token, IEqualityComparer<T> comparer = null)
            => await first.SequenceEqual(second.Async(), token, comparer);
    }
}
