namespace Linx.Observable
{
    using System;
    using System.Threading;

    partial class LinxObservable
    {
        /// <summary>
        /// Samples <paramref name="source"/> at sampling ticks provided by <paramref name="sampler"/>.
        /// </summary>
        public static ILinxObservable<TSource> Sample<TSource, TSample>(this ILinxObservable<TSource> source, ILinxObservable<TSample> sampler)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sampler == null) throw new ArgumentNullException(nameof(sampler));

            return Create<TSource>(observer =>
            {
                var context = new SampleContext<TSource>(observer);

                try { source.SafeSubscribe(context.OnNextSource, context.OnSourceError, context.OnSourceCompleted, context.Token); }
                catch (Exception ex) { context.OnSourceError(ex); }

                try { sampler.SafeSubscribe(x => context.OnNextSample(), context.OnSampleError, context.OnSampleCompleted, context.Token); }
                catch (Exception ex) { context.OnSampleError(ex); }
            });
        }

        /// <summary>
        /// Samples <paramref name="source"/> at the specified interval.
        /// </summary>
        public static ILinxObservable<T> Sample<T>(this ILinxObservable<T> source, TimeSpan interval)
            => source.Sample(Interval(interval));

        private sealed class SampleContext<T>
        {
            // 2 bits while active
            private const int _sInitial = 0;
            private const int _sSource = 1;
            private const int _sSample = 2;
            private const int _sCompleted = 3;

            private const int _fSource = 1 << 2;
            private const int _fSample = 1 << 3;
            private const int _mFinal = _sCompleted | _fSource | _fSample;

            private readonly ILinxObserver<T> _observer;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _state;
            private T _next;
            private Exception _error;

            public SampleContext(ILinxObserver<T> observer)
            {
                _observer = observer;
                var token = observer.Token;
                if (token.CanBeCanceled) _ctr = token.Register(() => Complete(new OperationCanceledException(token)));
            }

            public CancellationToken Token => _cts.Token;

            public bool OnNextSource(T value)
            {
                lock (_cts)
                    switch (_state)
                    {
                        case _sInitial:
                            _next = value;
                            _state = _sSource;
                            return true;

                        case _sSource:
                            _next = value;
                            return true;

                        case _sSample:
                            Exception error;
                            try
                            {
                                if (_observer.OnNext(value))
                                {
                                    _state = _sInitial;
                                    return true;
                                }
                                error = null;
                            }
                            catch (Exception ex) { error = ex; }

                            Complete(error);
                            return false;

                        default:
                            return false;
                    }
            }

            public bool OnNextSample()
            {
                lock (_cts)
                    switch (_state)
                    {
                        case _sInitial:
                            _state = _sSample;
                            return true;

                        case _sSample:
                            return true;

                        case _sSource:
                            Exception error;
                            try
                            {
                                if (_observer.OnNext(_next))
                                {
                                    _state = _sInitial;
                                    return true;
                                }
                                error = null;
                            }
                            catch (Exception ex) { error = ex; }

                            Complete(error);
                            return false;

                        default:
                            return false;
                    }
            }

            public void OnSourceError(Exception error)
            {
                if (error == null) throw new ArgumentNullException(nameof(error));
                Complete(error);
                Finally(_fSource);
            }

            public void OnSourceCompleted()
            {
                Complete(null);
                Finally(_fSource);
            }

            public void OnSampleError(Exception error)
            {
                if (error == null) throw new ArgumentNullException(nameof(error));
                Complete(error);
                Finally(_fSample);
            }

            public void OnSampleCompleted()
            {
                Complete(null);
                Finally(_fSample);
            }

            private void Complete(Exception errorOpt)
            {
                lock (_cts)
                    switch (_state)
                    {
                        case _sInitial:
                        case _sSource:
                        case _sSample:
                            _next = default;
                            _error = errorOpt;
                            _state = _sCompleted;
                            break;
                        default:
                            return;
                    }

                _ctr.Dispose();
                try { _cts.Cancel(); } catch { /**/ }
            }

            private void Finally(int flag)
            {
                lock (_cts)
                {
                    _state |= flag;
                    if (_state != _mFinal) return;
                    if (_error == null) _observer.OnCompleted();
                    else _observer.OnError(_error);
                }
            }
        }
    }
}
