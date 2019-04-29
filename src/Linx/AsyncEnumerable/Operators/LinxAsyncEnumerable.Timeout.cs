namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Throws a <see cref="TimeoutException"/> if no element is observed within <paramref name="dueTime"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Timeout<T>(this IAsyncEnumerable<T> source, TimeSpan dueTime)
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
                    var ae = source.WithCancellation(eh.InternalToken).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        while (true)
                        {
                            var tMoveNext = ae.MoveNextAsync();
                            var aMoveNext = tMoveNext.GetAwaiter();
                            bool hasNext;
                            if (aMoveNext.IsCompleted)
                                hasNext = aMoveNext.GetResult();
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
                    finally { await ae.DisposeAsync(); }
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
