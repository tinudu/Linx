namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using Coroutines;
    using Timing;

    partial class LinxReactive
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
                var time = Time.Current;
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

                Exception internalError;
                try
                {
                    var ae = source.GetAsyncEnumerator(eh.InternalToken);
                    try
                    {
                        var ccsWait = CoCompletionSource.Init();
                        while (true)
                        {
                            var aMoveNext = ae.MoveNextAsync();
                            if (!aMoveNext.IsCompleted)
                            {
                                // await aMoveNext or due time, whatever comes first
                                ccsWait.Reset(false);
                                var wait = 2;
                                var ctsWait = new CancellationTokenSource();

                                aMoveNext.OnCompleted(() =>
                                {
                                    ctsWait.Cancel();
                                    if (Interlocked.Decrement(ref wait) == 0) ccsWait.SetCompleted(null);
                                });

                                var aWait = time.Wait(time.Now + dueTime, ctsWait.Token).ConfigureAwait(false).GetAwaiter();
                                aWait.OnCompleted(() =>
                                {
                                    Exception waitError;
                                    try
                                    {
                                        aWait.GetResult();
                                        waitError = new TimeoutException();
                                    }
                                    catch (OperationCanceledException oce) when (oce.CancellationToken == ctsWait.Token) { waitError = null; }
                                    catch (Exception ex) { waitError = ex; }
                                    if (waitError != null) Cancel(waitError);
                                    if (Interlocked.Decrement(ref wait) == 0) ccsWait.SetCompleted(null);
                                });

                                await ccsWait.Awaiter;
                            }

                            if (!aMoveNext.GetResult())
                                break;
                            await yield(ae.Current);
                        }
                    }
                    finally { await ae.DisposeAsync().ConfigureAwait(false); }
                    internalError = null;
                }
                catch (Exception ex) { internalError = ex; }

                var cancel = Atomic.Lock(ref canceled) == 0;
                eh.SetInternalError(internalError);
                canceled = 1;
                if (cancel) eh.Cancel();
                eh.ThrowIfError();
            });
        }
    }
}
