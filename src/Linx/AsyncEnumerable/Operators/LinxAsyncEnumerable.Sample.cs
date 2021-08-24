namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Samples <paramref name="source"/> at sampling ticks provided by <paramref name="sampler"/>.
        /// </summary>
        public static IAsyncEnumerable<TSource> Sample<TSource, TSample>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TSample> sampler)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sampler == null) throw new ArgumentNullException(nameof(sampler));
            return Create(token => new SampleEnumerator<TSource, TSample>(source, sampler, token));
        }

        /// <summary>
        /// Samples <paramref name="source"/> at the specified interval.
        /// </summary>
        public static IAsyncEnumerable<T> Sample<T>(this IAsyncEnumerable<T> source, TimeSpan interval)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var sampler = Interval(interval);
            return Create(token => new SampleEnumerator<T, DateTimeOffset>(source, sampler, token));
        }

        private sealed class SampleEnumerator<TSource, TSample> : IAsyncEnumerator<TSource>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sSourceAccepting = 2;
            private const int _sSampleAccepting = 3;
            private const int _sEmitting = 4;
            private const int _sSourceEmitting = 5;
            private const int _sSampleEmitting = 6;
            private const int _sNextEmitting = 7;
            private const int _sLastEmitting = 8;
            private const int _sFinal = 9;

            private readonly IAsyncEnumerable<TSource> _source;
            private readonly IAsyncEnumerable<TSample> _sampler;
            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
            private AsyncTaskMethodBuilder _atmbDisposed = default;
            private CancellationTokenRegistration _ctr;
            private int _state, _active;
            private TSource _next;
            private Exception _error;

            public SampleEnumerator(IAsyncEnumerable<TSource> source, IAsyncEnumerable<TSample> sampler, CancellationToken token)
            {
                Debug.Assert(source != null);
                Debug.Assert(sampler != null);

                _source = source;
                _sampler = sampler;
                if (token.CanBeCanceled)
                    _ctr = token.Register(() => Dispose(new OperationCanceledException(token)));
            }

            public TSource Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                _tsAccepting.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _active = 2;
                        _state = _sAccepting;
                        ProduceSource();
                        ProduceSample();
                        break;

                    case _sEmitting:
                        _state = _sAccepting;
                        break;

                    case _sSourceEmitting:
                        _state = _sSourceAccepting;
                        break;

                    case _sSampleEmitting:
                        _state = _sSampleAccepting;
                        break;

                    case _sNextEmitting:
                        Current = _next;
                        _state = _sEmitting;
                        _tsAccepting.SetResult(true);
                        break;

                    case _sLastEmitting:
                        Current = Linx.Clear(ref _next);
                        _state = _sFinal;
                        _ctr.Dispose();
                        _tsAccepting.SetResult(true);
                        break;

                    default:
                        Debug.Assert(state == _sFinal);
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;
                }

                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                Dispose(AsyncEnumeratorDisposedException.Instance);
                return new ValueTask(_atmbDisposed.Task);
            }

            private void Dispose(Exception error)
            {
                Debug.Assert(error != null);

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                    case _sSourceAccepting:
                    case _sSampleAccepting:
                        Current = _next = default;
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                    case _sSourceEmitting:
                    case _sSampleEmitting:
                    case _sNextEmitting:
                        _next = default;
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        break;

                    case _sLastEmitting:
                        _next = default;
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        break;

                    default:
                        Debug.Assert(state == _sFinal);
                        _state = _sFinal;
                        break;
                }
            }

            private void Complete(Exception errorOpt)
            {
                if (Interlocked.Decrement(ref _active) == 0)
                    _atmbDisposed.SetResult();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                    case _sSourceAccepting:
                    case _sSampleAccepting:
                        Current = _next = default;
                        _error = errorOpt;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        _tsAccepting.SetExceptionOrResult(errorOpt, false);
                        break;

                    case _sEmitting:
                    case _sSourceEmitting:
                    case _sSampleEmitting:
                        _next = default;
                        _error = errorOpt;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        break;

                    case _sNextEmitting:
                        _error = errorOpt;
                        _state = _sLastEmitting;
                        _cts.TryCancel();
                        break;

                    default:
                        Debug.Assert(state == _sLastEmitting || state == _sFinal);
                        _state = state;
                        break;
                }
            }

            private async void ProduceSource()
            {
                Exception error = null;
                try
                {
                    await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                            case _sSourceAccepting:
                                _next = item;
                                _state = _sSourceAccepting;
                                break;

                            case _sSampleAccepting:
                                Current = item;
                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                                if (_cts.IsCancellationRequested)
                                    return;
                                break;

                            case _sEmitting:
                            case _sSourceEmitting:
                                _next = item;
                                _state = _sSourceEmitting;
                                break;

                            case _sSampleEmitting:
                            case _sNextEmitting:
                                _next = item;
                                _state = _sNextEmitting;
                                break;

                            default:
                                Debug.Assert(state == _sLastEmitting || state == _sFinal);
                                _state = state;
                                return;
                        }
                    }
                }
                catch (Exception ex) { error = ex; }
                finally { Complete(error); }
            }

            private async void ProduceSample()
            {
                Exception error = null;
                try
                {
                    if (_cts.IsCancellationRequested)
                        return;

                    await foreach (var _ in _sampler.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                            case _sSampleAccepting:
                                _state = _sSampleAccepting;
                                break;

                            case _sSourceAccepting:
                                Current = _next;
                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                                if (_cts.IsCancellationRequested)
                                    return;
                                break;

                            case _sEmitting:
                            case _sSampleEmitting:
                                _state = _sSampleEmitting;
                                break;

                            case _sSourceEmitting:
                            case _sNextEmitting:
                                _state = _sNextEmitting;
                                break;

                            default:
                                Debug.Assert(state == _sLastEmitting || state == _sFinal);
                                _state = state;
                                return;
                        }
                    }
                }
                catch (Exception ex) { error = ex; }
                finally { Complete(error); }
            }
        }
    }
}
