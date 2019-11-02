namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (maxConcurrent <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrent));

            return maxConcurrent == 1 ?
                sources.Concat() :
                Create(token => new MergeEnumerator<T>(sources, maxConcurrent, token));
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (maxConcurrent <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrent));

            return maxConcurrent == 1 ?
                sources.Concat() :
                Create(token => new MergeEnumerator<T>(sources.Async(), maxConcurrent, token));
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            return Create(token => new MergeEnumerator<T>(new[] { first, second }.Async(), int.MaxValue, token));
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> source, params IAsyncEnumerable<T>[] sources)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            return
                sources.Length == 0 ? source :
                Create(token => new MergeEnumerator<T>(sources.Prepend(source).Async(), int.MaxValue, token));
        }

        private sealed class MergeEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sError = 3;
            private const int _sFinal = 4;

            private readonly IAsyncEnumerable<IAsyncEnumerable<T>> _sources;
            private readonly int _maxConcurrent;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private readonly Queue<(T next, ManualResetValueTaskSource<bool> ts)> _queue = new Queue<(T, ManualResetValueTaskSource<bool>)>();
            private CancellationTokenRegistration _ctr;
            private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
            private int _state, _active;
            private ManualResetValueTaskSource<bool> _tsEmitting, _tsMaxConcurrent;
            private bool _incrementActive;
            private Exception _error;

            public MergeEnumerator(IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent, CancellationToken token)
            {
                Debug.Assert(sources != null);
                Debug.Assert(maxConcurrent > 0);

                _sources = sources;
                _maxConcurrent = maxConcurrent;
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            public T Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                _tsAccepting.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _active = 1;
                        _state = _sAccepting;
                        Produce();
                        break;

                    case _sEmitting:
                        var tsEmitting = _tsEmitting;

                        if (_queue.Count > 0)
                        {
                            var (next, ts) = _queue.Dequeue(); // no exception assumed
                            Current = next;
                            if (_active == 0 && _queue.Count == 0) // emitting the last result
                            {
                                _state = _sFinal;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                            }
                            else
                            {
                                _tsEmitting = ts;
                                _state = _sEmitting;
                            }
                            _tsAccepting.SetResult(true);
                        }
                        else if (_tsMaxConcurrent != null && _active <= _maxConcurrent)
                        {
                            var ts = Linx.Clear(ref _tsMaxConcurrent);
                            if (_incrementActive) _active++;
                            _state = _sAccepting;
                            ts.SetResult(true);
                        }
                        else
                            _state = _sAccepting;

                        tsEmitting.SetResult(true);
                        break;

                    case _sError:
                    case _sFinal:
                        Current = default;
                        _state = state;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default: // Accepting ???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                OnError(AsyncEnumeratorDisposedException.Instance);
                return new ValueTask(_atmbDisposed.Task);
            }

            private void OnError(Exception error)
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
                        return;

                    case _sAccepting:
                        Debug.Assert(_error == null && _active > 0 && _queue.Count == 0);
                        Current = default;
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                        _cts.TryCancel();
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        Debug.Assert(_error == null && _active > 0);
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                        _tsEmitting.SetResult(false);
                        while (_queue.Count > 0)
                        {
                            var (_, ts) = _queue.Dequeue();
                            ts.SetResult(false);
                        }
                        _cts.TryCancel();
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            private void OnCompleted()
            {
                var state = Atomic.Lock(ref _state);
                Debug.Assert(_active > 0);

                switch (state)
                {
                    case _sAccepting:
                        Debug.Assert(_error == null && _queue.Count == 0);
                        if (--_active == 0)
                        {
                            Current = default;
                            _state = _sFinal;
                            _ctr.Dispose();
                            Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                            _cts.TryCancel();
                            _atmbDisposed.SetResult();
                            _tsAccepting.SetResult(false);
                        }
                        else if (_tsMaxConcurrent != null)
                        {
                            var ts = Linx.Clear(ref _tsMaxConcurrent);
                            if (_incrementActive) _active++;
                            _state = _sAccepting;
                            ts.SetResult(true);
                        }
                        else
                            _state = _sAccepting;
                        break;

                    case _sEmitting:
                        if (--_active == 0)
                        {
                            if (_queue.Count == 0)
                            {
                                _state = _sFinal;
                                _ctr.Dispose();
                                Linx.Clear(ref _tsEmitting).SetResult(false);
                                Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sEmitting;

                            _cts.TryCancel();
                        }
                        else
                            _state = _sEmitting;
                        break;

                    case _sError:
                        if (--_active == 0)
                        {
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                        }
                        else
                            _state = _sError;
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private async void Produce()
            {
                try
                {
                    var tsMaxConcurrent = new ManualResetValueTaskSource<bool>();
                    ValueTask<bool> Wait(bool increment)
                    {
                        tsMaxConcurrent.Reset();
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting when (_active <= _maxConcurrent):
                                if (increment) _active++;
                                _state = _sAccepting;
                                tsMaxConcurrent.SetResult(true);
                                break;

                            case _sAccepting:
                            case _sEmitting:
                                _tsMaxConcurrent = tsMaxConcurrent;
                                _incrementActive = increment;
                                _state = state;
                                break;

                            case _sError:
                                _state = _sError;
                                tsMaxConcurrent.SetResult(false);
                                break;

                            default:
                                _state = state;
                                throw new Exception(state + "???");
                        }
                        return tsMaxConcurrent.Task;
                    }

                    await foreach (var inner in _sources.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        if (!await Wait(true).ConfigureAwait(false)) return;
                        Produce(inner);
                        if (!await Wait(false).ConfigureAwait(false)) return;
                    }
                }
                catch (Exception ex) { OnError(ex); }
                finally { OnCompleted(); }
            }

            private async void Produce(IAsyncEnumerable<T> inner)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var tsEmitting = new ManualResetValueTaskSource<bool>();
                    await foreach (var item in inner.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                                Current = item;
                                tsEmitting.Reset();
                                _tsEmitting = tsEmitting;
                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                                break;

                            case _sEmitting:
                                tsEmitting.Reset();
                                try { _queue.Enqueue((item, tsEmitting)); }
                                finally { _state = _sEmitting; }
                                break;

                            case _sError:
                                _state = _sError;
                                return;

                            default: // Initial, Final???
                                _state = state;
                                throw new Exception(state + "???");
                        }

                        if (!await tsEmitting.Task.ConfigureAwait(false)) 
                            return;
                    }
                }
                catch (Exception ex) { OnError(ex); }
                finally { OnCompleted(); }
            }
        }
    }
}
