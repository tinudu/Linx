namespace Linx.Observable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxObservable
    {
        /// <summary>
        /// Determines whether any element.
        /// </summary>
        public static Task<bool> Any<T>(this ILinxObservable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var tcs = new TaskCompletionSource<bool>();
            try
            {
                var result = false;
                source.SafeSubscribe(
                    value =>
                    {
                        result = true;
                        return false;
                    },
                    error => tcs.TrySetException(error),
                    () => tcs.TrySetResult(result),
                    token);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == token) { tcs.TrySetCanceled(token); }
            catch (Exception ex) { tcs.TrySetException(ex); }

            return tcs.Task;
        }

        /// <summary>
        /// Determines whether any element of a sequence satisfies a condition.
        /// </summary>
        public static async Task<bool> Any<T>(
            this ILinxObservable<T> source,
            Func<T, bool> predicate,
            CancellationToken token) =>
            await source.Where(predicate).Any(token).ConfigureAwait(false);

    }
}