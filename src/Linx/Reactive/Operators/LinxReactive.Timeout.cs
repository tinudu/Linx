﻿namespace Linx.Reactive
{
    using System;
    using Timing;

    partial class LinxReactive
    {
        /// <summary>
        /// Throws a <see cref="TimeoutException"/> if no element is observed within <paramref name="dueTime"/>.
        /// </summary>
        public static IAsyncEnumerableObs<T> Timeout<T>(this IAsyncEnumerableObs<T> source, TimeSpan dueTime)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (dueTime <= TimeSpan.Zero) return Throw<T>(new TimeoutException());

            return Produce<T>(async (yield, token) =>
            {
                var eh = ErrorHandler.Init();
                var canceled = 0;
                void Cancel(Exception externalError)
                {
                    // ReSharper disable once AccessToModifiedClosure
                    if (Atomic.Lock(ref canceled) == 0)
                    {
                        eh.SetExternalError(externalError);
                        canceled = 1;
                        eh.Cancel();
                    }
                    else
                        canceled = 1;
                }
                if (token.CanBeCanceled) eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));

                var timer = Time.Current.CreateTimer((t, d) => Cancel(new TimeoutException()));
                Exception internalError;
                try
                {
                    var ae = source.GetAsyncEnumerator(eh.InternalToken);
                    try
                    {
                        while (true)
                        {
                            var tMoveNext = ae.MoveNextAsync();
                            bool hasNext;
                            if (tMoveNext.IsCompleted)
                                hasNext = tMoveNext.GetAwaiter().GetResult();
                            else
                            {
                                timer.Enable(dueTime);
                                try { hasNext = await tMoveNext; }
                                finally { timer.Disable(); }
                            }
                            if (!hasNext) break;
                            await yield(ae.Current);
                        }
                    }
                    finally { await ae.DisposeAsync().ConfigureAwait(false); }
                    internalError = null;
                }
                catch (Exception ex) { internalError = ex; }
                finally { timer.Dispose(); }

                var cancel = Atomic.Lock(ref canceled) == 0;
                eh.SetInternalError(internalError);
                canceled = 1;
                if (cancel) eh.Cancel();
                eh.ThrowIfError();
            });
        }
    }
}
