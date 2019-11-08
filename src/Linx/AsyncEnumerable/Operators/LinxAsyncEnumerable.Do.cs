namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Generate side effects while enumerating a sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Do<T>(this IAsyncEnumerable<T> source, Action<T> onNext = null, Action<Exception> onError = null, Action onCompleted = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (onNext == null)
                onNext = value => { };
            else
            {
                var d = onNext;
                onNext = value => { try { d(value); } catch { /**/ } };
            }

            return Create<T>(async (yield, token) =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        while (await ae.MoveNextAsync())
                        {
                            var current = ae.Current;
                            onNext(current);
                            if (!await yield(current).ConfigureAwait(false)) return;
                        }
                    }
                    finally { await ae.DisposeAsync(); }

                    if (onCompleted != null) try { onCompleted(); } catch { /**/ }
                }
                catch (Exception ex)
                {
                    // ReSharper disable once InvertIf
                    if (onError != null) try { onError(ex); } catch { /**/ }
                    throw;
                }
            });
        }
    }
}
