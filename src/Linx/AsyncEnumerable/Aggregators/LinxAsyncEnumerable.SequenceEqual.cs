using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linx.Notifications;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Compare two sequences for equality.
    /// </summary>
    public static async ValueTask<bool> SequenceEqual<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, CancellationToken token, IEqualityComparer<T>? comparer = null)
    {
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));
        token.ThrowIfCancellationRequested();

        var m1 = first.Select(Notification.Next).Append(Notification.Completed<T>());
        var m2 = second.Select(Notification.Next).Append(Notification.Completed<T>());
        return await m1
            .Zip(m2, NotificationComparer<T>.GetComparer(comparer, null).Equals)
            .All(eq => eq, token)
            .ConfigureAwait(false);
    }
}
