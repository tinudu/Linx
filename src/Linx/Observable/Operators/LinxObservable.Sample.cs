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
                var sampleObserver = new SampleObserver<TSource>(observer);

                try { source.Subscribe(sampleObserver); }
                catch (Exception ex) { sampleObserver.OnError(ex); }

                try { sampler.Subscribe(value => sampleObserver.OnSample(), sampleObserver.OnError, sampleObserver.OnCompleted, sampleObserver.Token); }
                catch (Exception ex) { sampleObserver.OnError(ex); }
            });
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
