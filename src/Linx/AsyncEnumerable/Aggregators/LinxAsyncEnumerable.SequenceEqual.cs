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

            var nCompleted = Return(Notification.Completed<T>());
            var nNext1 = first.Select(Notification.Next).Concat(nCompleted);
            var nNext2 = second.Select(Notification.Next).Concat(nCompleted);
            return await nNext1
                .Zip(nNext2, NotificationComparer<T>.GetComparer(comparer).Equals)
                .All(eq => eq, token)
                .ConfigureAwait(false);
        }
    }
}
