namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskProviders;
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

                Exception internalError;
                try
                {
                    var ae = source.WithCancellation(eh.InternalToken).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        using (var timer = Time.Current.GetTimer(default))
                        {
                            var waiting = 0;
                            var tp = new ManualResetTaskProvider();

                            void TimerElapsed()
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                if (Interlocked.Decrement(ref waiting) != 0)
                                    Cancel(new TimeoutException());
                                else
                                    tp.SetResult();
                            }

                            void MoveNextCompleted()
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                if (Interlocked.Decrement(ref waiting) != 0)
                                    // ReSharper disable once AccessToDisposedClosure
                                    timer.Elapse();
                                else
                                    tp.SetResult();
                            }

                            while (true)
                            {
                                var aMoveNext = ae.MoveNextAsync().GetAwaiter();
                                if (!aMoveNext.IsCompleted)
                                {
                                    waiting = 2;
                                    tp.Reset();
                                    aMoveNext.OnCompleted(MoveNextCompleted);
                                    timer.Delay(dueTime).ConfigureAwait(false).GetAwaiter().OnCompleted(TimerElapsed);
                                    await tp.Task.ConfigureAwait(false);
                                }

                                if (!aMoveNext.GetResult()) break;
                                await yield(ae.Current);
                            }
                        }
                    }
                    finally { await ae.DisposeAsync(); }
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
