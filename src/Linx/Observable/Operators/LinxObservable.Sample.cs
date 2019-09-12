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

                try { source.Subscribe(context.OnNext, context.OnSourceError, context.OnSourceCompleted, context.Token); }
                catch (Exception ex) { context.OnSourceError(ex); }

                try { source.Subscribe(x => context.OnNext(), context.OnSampleError, context.OnSampleCompleted, context.Token); }
                catch (Exception ex) { context.OnSampleError(ex); }
            });
        }

        private sealed class SampleContext<T>
        {
            // 2 bits while active
            private const int _sInitial = 0;
            private const int _sSource = 1;
            private const int _sSample = 2;
            private const int _sCompleted = 3;

            private const int _fSource = 1 << 2;
            private const int _fSample = 1 << 3;
            private const int _mFinal = _fSource | _fSample;

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

            public bool OnNext(T value)
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

                        case _fSample:
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

            public void OnSourceError(Exception error) => OnError(error, _fSource);

            public void OnSourceCompleted() => OnCompleted(_fSource);

            public bool OnNext()
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

            public void OnSampleError(Exception error) => OnError(error, _fSample);

            public void OnSampleCompleted() => OnCompleted(_fSample);

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

            private void Finally(int flag) { }

            private void OnError(Exception error, int flag)
            {
                Complete(error ?? new ArgumentNullException(nameof(error)));
                Finally(flag);
            }

            private void OnCompleted(int flag)
            {
                Complete(null);
                Finally(flag);
            }
        }

        /// <summary>
        /// Samples <paramref name="source"/> at the specified interval.
        /// </summary>
        public static ILinxObservable<T> Sample<T>(this ILinxObservable<T> source, TimeSpan interval)
            => source.Sample(Interval(interval));

        /// <summary>
        /// Samples <paramref name="source"/> at the specified interval.
        /// </summary>
        public static ILinxObservable<T> Sample<T>(this ILinxObservable<T> source, int intervalMilliseconds)
            => source.Sample(Interval(intervalMilliseconds));

        private sealed class SampleObserver<T> : ILinxObserver<T>
        {
            private const int _sInitial = 0;
            private const int _sSource = 1;
            private const int _sSampler = 2;
            private const int _sCompleted2 = 3;
            private const int _sCompleted1 = 4;
            private const int _sFinal = 5;

            private readonly ILinxObserver<T> _observer;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _state;
            private T _next;
            private Exception _error;

            public SampleObserver(ILinxObserver<T> observer)
            {
                _observer = observer;
                var token = observer.Token;
                if (token.CanBeCanceled) _ctr = token.Register(() => Complete(new OperationCanceledException(token)));
            }

            public CancellationToken Token => _cts.Token;

            public bool OnNext(T value)
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

                        case _sSampler:
                            try
                            {
                                if (_observer.OnNext(value))
                                {
                                    _state = _sInitial;
                                    return true;
                                }
                            }
                            catch (Exception ex) { _error = ex; }
                            _next = default;
                            _state = _sCompleted2;
                            break;

                        //case _sCompleted2:
                        //case _sCompleted1:
                        //case _sFinal:
                        default:
                            return false;
                    }
                _ctr.Dispose();
                try { _cts.Cancel(); } catch { /**/ }
                return false;
            }

            public bool OnSample()
            {
                lock (_cts)
                    switch (_state)
                    {
                        case _sInitial:
                            _state = _sSampler;
                            return true;

                        case _sSampler:
                            return true;

                        case _sSource:
                            try
                            {
                                if (_observer.OnNext(_next))
                                {
                                    _state = _sInitial;
                                    return true;
                                }
                            }
                            catch (Exception ex) { _error = ex; }
                            _next = default;
                            _state = _sCompleted2;
                            break;

                        //case _sCompleted2:
                        //case _sCompleted1:
                        //case _sFinal:
                        default:
                            return false;
                    }
                _ctr.Dispose();
                try { _cts.Cancel(); } catch { /**/ }
                return false;
            }

            public void OnError(Exception error)
            {
                Complete(error ?? new ArgumentNullException(nameof(error)));
                Finally();
            }

            public void OnCompleted()
            {
                Complete(null);
                Finally();
            }

            private void Complete(Exception errorOpt)
            {
                lock (_cts)
                    switch (_state)
                    {
                        case _sInitial:
                        case _sSource:
                        case _sSampler:
                            _error = errorOpt;
                            _next = default;
                            _state = _sCompleted2;
                            break;

                        case _sCompleted2:
                        case _sCompleted1:
                        case _sFinal:
                            return;

                        default:
                            throw new Exception(_state + "???");
                    }
                _ctr.Dispose();
                try { _cts.Cancel(); } catch { /**/ }
            }

            private void Finally()
            {
                lock (_cts)
                    switch (_state)
                    {
                        case _sCompleted2:
                            _state = _sCompleted1;
                            return;

                        case _sCompleted1:
                            _state = _sFinal;
                            break;

                        //case _sInitial:
                        //case _sSource:
                        //case _sSampler:
                        //case _sFinal:
                        default:
                            throw new Exception(_state + "???");
                    }
                if (_error == null) _observer.OnCompleted();
                else _observer.OnError(_error);
            }
        }
    }
}
