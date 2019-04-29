﻿namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Produce side effects while enumerating a sequence.
        /// </summary>
        public static IAsyncEnumerableObs<T> Do<T>(this IAsyncEnumerableObs<T> source, Action<T> onNext = null, Action<Exception> onError = null, Action onCompleted = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (onNext == null)
                onNext = value => { };
            else
            {
                var d = onNext;
                onNext = value => { try { d(value); } catch { /**/ } };
            }

            return Produce<T>(async (yield, token) =>
            {
                try
                {
                    var ae = source.GetAsyncEnumerator(token);
                    try
                    {
                        while (await ae.MoveNextAsync())
                        {
                            var current = ae.Current;
                            onNext(current);
                            await yield(current);
                        }
                    }
                    finally { await ae.DisposeAsync().ConfigureAwait(false); }

                    if (onCompleted != null) try { onCompleted(); } catch { /**/ }
                }
                catch (Exception ex)
                {
                    if (onError != null) try { onError(ex); } catch { /**/ }
                    throw;
                }
            });
        }
    }
}
