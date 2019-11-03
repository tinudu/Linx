namespace Linx.Observable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxObservable
    {
        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        public static Task<TAccumulate> Aggregate<TSource, TAccumulate>(
            this ILinxObservable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, (TAccumulate a, bool b)> accumulator,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));

            var tcs = new TaskCompletionSource<TAccumulate>();
            try
            {
                token.ThrowIfCancellationRequested();

                source.SafeSubscribe(
                    value =>
                    {
                        var (a, b) = accumulator(seed, value);
                        seed = a;
                        return b;
                    },
                    error => tcs.TrySetException(error),
                    () => tcs.TrySetResult(seed),
                    token);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == token) { tcs.TrySetCanceled(token); }
            catch (Exception ex) { tcs.TrySetException(ex); }

            return tcs.Task;
        }

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        public static async Task<TResult> Aggregate<TSource, TAccumulate, TResult>(
            this ILinxObservable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, (TAccumulate, bool)> accumulator,
            Func<TAccumulate, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            token.ThrowIfCancellationRequested();

            return resultSelector(await source.Aggregate(seed, accumulator, token).ConfigureAwait(false));
        }
    }
}
