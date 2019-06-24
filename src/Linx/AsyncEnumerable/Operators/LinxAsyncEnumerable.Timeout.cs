namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Throws a <see cref="TimeoutException"/> if no element is observed within <paramref name="dueTime"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Timeout<T>(this IAsyncEnumerable<T> source, TimeSpan dueTime)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (dueTime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(dueTime));

            return Produce<T>(async (yield, token) =>
            {
                var eh = ErrorHandler.Init();
                var canceled = 0;

                if (token.CanBeCanceled) eh.ExternalRegistration = token.Register(() =>
                {
                    // ReSharper disable once AccessToModifiedClosure
                    if( Atomic.Lock(ref canceled) == 0)
                    {
                        eh.SetExternalError(new OperationCanceledException(token));
                        canceled = 1;
                        eh.Cancel();
                    }
                    else
                        canceled = 1;
                });

                Exception internalError;
                try
                {
                    var ae = source.WithCancellation(eh.InternalToken).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        using (var timer = Time.Current.GetTimer(default))
                        {
                            var continuations = 0;
                            var ts = new ManualResetValueTaskSource();

                            void MoveNextCompleted()
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                if (Interlocked.Decrement(ref continuations) != 0)
                                    // ReSharper disable once AccessToDisposedClosure
                                    timer.Elapse();
                                else
                                    ts.SetResult();
                            }

                            void TimerElapsed()
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                if (Interlocked.Decrement(ref continuations) != 0)
                                {
                                    // ReSharper disable once AccessToModifiedClosure
                                    if (Interlocked.CompareExchange(ref canceled, 1, 0) != 0) return;
                                    eh.SetInternalError(new TimeoutException());
                                    eh.Cancel();
                                }
                                else
                                    ts.SetResult();
                            }

                            while (true)
                            {
                                var aMoveNext = ae.MoveNextAsync().GetAwaiter();
                                if (!aMoveNext.IsCompleted)
                                {
                                    continuations = 2;
                                    ts.Reset();
                                    aMoveNext.OnCompleted(MoveNextCompleted);
                                    timer.Delay(dueTime).ConfigureAwait(false).GetAwaiter().OnCompleted(TimerElapsed);
                                    await ts.Task.ConfigureAwait(false);
                                }

                                if (!aMoveNext.GetResult()) break;
                                await yield(ae.Current).ConfigureAwait(false);
                            }
                        }
                    }
                    finally { await ae.DisposeAsync(); }
                    internalError = null;
                }
                catch (Exception ex) { internalError = ex; }

                eh.SetInternalError(internalError);
                if (Interlocked.CompareExchange(ref canceled, 1, 0) == 0) eh.Cancel();
                eh.ThrowIfError();
            });
        }
    }
}
