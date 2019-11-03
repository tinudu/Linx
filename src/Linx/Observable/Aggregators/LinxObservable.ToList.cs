namespace Linx.Observable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxObservable
    {
        /// <summary>
        /// Aggregate to <see cref="List{T}"/>.
        /// </summary>
        public static Task<List<T>> ToList<T>(this ILinxObservable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var tcs = new TaskCompletionSource<List<T>>();
            try
            {
                var result = new List<T>();
                source.SafeSubscribe(
                    value =>
                    {
                        result.Add(value);
                        return true;
                    },
                    error => tcs.TrySetException(error),
                    () => tcs.TrySetResult(result),
                    token);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == token) { tcs.TrySetCanceled(token); }
            catch (Exception ex) { tcs.TrySetException(ex); }

            return tcs.Task;
        }
    }
}
