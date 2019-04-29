namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Determines whether all elements of a sequence satisfy a condition.
        /// </summary>
        public static async Task<bool> All<T>(this IAsyncEnumerableObs<T> source, Func<T, bool> predicate, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                while(await ae.MoveNextAsync())
                    if (!predicate(ae.Current))
                        return false;
                return true;
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }
    }
}
