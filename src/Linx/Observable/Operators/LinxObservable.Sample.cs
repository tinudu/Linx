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
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                var context = new SampleContext<TSource, TSample>(observer);
                source.SafeSubscribe(context.SourceObserver);
                sampler.SafeSubscribe(context.SampleObserver);
            });
        }

        /// <summary>
        /// Samples <paramref name="source"/> at the specified interval.
        /// </summary>
        public static ILinxObservable<T> Sample<T>(this ILinxObservable<T> source, TimeSpan interval)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Sample(Interval(interval));
        }

        private sealed class SampleContext<TSource, TSample>
        {
            private const int _sInitial = 0;
            private const int _sNext = 1;
            private const int _sSample = 2;
            private const int _sCompleted = 3;
            private const int _sCompletedSource = 4;
            private const int _sCompletedSample = 5;
            private const int _sFinal = 6;

            private readonly ILinxObserver<TSource> _observer;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _state;
            private TSource _next;
            private Exception _error;

            public ILinxObserver<TSource> SourceObserver { get; }
            public ILinxObserver<TSample> SampleObserver { get; }

            public SampleContext(ILinxObserver<TSource> observer)
            {
                _observer = observer;
                var token = observer.Token;
                if (token.CanBeCanceled) _ctr = token.Register(() => Complete(new OperationCanceledException(token)));
                SourceObserver = new SourceObs(this);
                SampleObserver = new SampleObs(this);
            }

            private sealed class SourceObs : ILinxObserver<TSource>
            {
                private readonly SampleContext<TSource, TSample> _c;
                public SourceObs(SampleContext<TSource, TSample> context) => _c = context;

                public CancellationToken Token => _c._cts.Token;

                public bool OnNext(TSource value)
                {
                    lock (_c)
                        switch (_c._state)
                        {
                            case _sInitial:
                            case _sNext:
                                _c._next = value;
                                _c._state = _sNext;
                                return true;

                            case _sSample:
                                _c._state = _sInitial;
                                Exception error;
                                try
                                {
                                    if (_c._observer.OnNext(value)) return true;
                                    error = null;
                                }
                                catch (Exception ex) { error = ex; }

                                _c.Complete(error);
                                return false;

                            default:
                                return false;
                        }
                }

                public void OnError(Exception error)
                {
                    if (error == null) throw new ArgumentNullException(nameof(error));
                    _c.Complete(error);
                    OnCompleted();
                }

                public void OnCompleted()
                {
                    bool final;
                    lock (_c)
                        switch (_c._state)
                        {
                            case _sInitial:
                            case _sNext:
                            case _sSample:
                                _c._next = default;
                                _c._state = _sCompletedSource;
                                final = false;
                                break;

                            case _sCompletedSample:
                                _c._state = _sFinal;
                                final = true;
                                break;

                            case _sCompleted:
                                _c._state = _sCompletedSource;
                                return;

                            default:
                                return;
                        }

                    if (final)
                    {
                        if (_c._error == null)
                            _c._observer.OnCompleted();
                        else
                            _c._observer.OnError(_c._error);
                    }
                    else
                    {
                        _c._ctr.Dispose();
                        _c._cts.TryCancel();
                    }
                }
            }

            private sealed class SampleObs : ILinxObserver<TSample>
            {
                private readonly SampleContext<TSource, TSample> _c;
                public SampleObs(SampleContext<TSource, TSample> context) => _c = context;

                public CancellationToken Token => _c._cts.Token;

                public bool OnNext(TSample _)
                {
                    lock (_c)
                        switch (_c._state)
                        {
                            case _sInitial:
                            case _sSample:
                                _c._state = _sSample;
                                return true;

                            case _sNext:
                                _c._state = _sInitial;
                                Exception error;
                                try
                                {
                                    if (_c._observer.OnNext(_c._next)) return true;
                                    error = null;
                                }
                                catch (Exception ex) { error = ex; }

                                _c.Complete(error);
                                return false;

                            default:
                                return false;
                        }
                }

                public void OnError(Exception error)
                {
                    if (error == null) throw new ArgumentNullException(nameof(error));
                    _c.Complete(error);
                    OnCompleted();
                }

                public void OnCompleted()
                {
                    bool final;
                    lock (_c)
                        switch (_c._state)
                        {
                            case _sInitial:
                            case _sNext:
                            case _sSample:
                                _c._next = default;
                                _c._state = _sCompletedSample;
                                final = false;
                                break;

                            case _sCompletedSource:
                                _c._state = _sFinal;
                                final = true;
                                break;

                            case _sCompleted:
                                _c._state = _sCompletedSample;
                                return;

                            default:
                                return;
                        }

                    if (final)
                    {
                        if (_c._error == null)
                            _c._observer.OnCompleted();
                        else
                            _c._observer.OnError(_c._error);
                    }
                    else
                    {
                        _c._ctr.Dispose();
                        _c._cts.TryCancel();
                    }
                }
            }

            private void Complete(Exception errorOpt)
            {
                lock (_cts)
                    switch (_state)
                    {
                        case _sInitial:
                        case _sNext:
                        case _sSample:
                            _error = errorOpt;
                            _state = _sCompleted;
                            break;
                        default:
                            return;
                    }

                _ctr.Dispose();
                _cts.TryCancel();
            }
        }
    }
}
