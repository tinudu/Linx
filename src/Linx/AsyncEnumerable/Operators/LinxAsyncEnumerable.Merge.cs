namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

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
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, params IAsyncEnumerable<T>[] sources)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            return Create(token => new MergeEnumerator<T>(sources.Prepend(second).Prepend(first).Async(), int.MaxValue, token));
        }

        private sealed class MergeEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sLastEmitting = 3;
            private const int _sFinal = 4;

            private readonly IAsyncEnumerable<IAsyncEnumerable<T>> _sources;
            private readonly int _maxConcurrent;
            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
            private readonly Queue<(T Next, ManualResetValueTaskSource<bool> Ts)> _queue = new();
            private AsyncTaskMethodBuilder _atmbDisposed = default;
            private CancellationTokenRegistration _ctr;
            private ManualResetValueTaskSource<bool> _tsMaxConcurrent;
            private int _state, _active;
            private Exception _error;

            public MergeEnumerator(IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent, CancellationToken token)
            {
                Debug.Assert(sources != null);
                Debug.Assert(maxConcurrent > 1);

                _sources = sources;
                _maxConcurrent = maxConcurrent;

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => Dispose(new OperationCanceledException(token)));
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
                        ProduceOuter();
                        break;

                    case _sEmitting:
                        if (_queue.Count == 0)
                            _state = _sAccepting;
                        else
                        {
                            var (next, ts) = _queue.Dequeue();
                            Current = next;
                            _state = _sEmitting;
                            ts.SetResult(true);
                            _tsAccepting.SetResult(true);
                        }
                        break;

                    case _sLastEmitting:
                    {
                        Debug.Assert(_queue.Count > 0);
                        var (next, ts) = _queue.Dequeue();
                        Current = next;
                        if (_queue.Count == 0)
                        {
                            _state = _sFinal;
                            _ctr.Dispose();
                        }
                        else
                            _state = _sLastEmitting;
                        ts.SetResult(false);
                        _tsAccepting.SetResult(true);
                    }
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
                        Current = default;
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                        while (_queue.Count > 0)
                            _queue.Dequeue().Ts.SetResult(false);
                        break;

                    case _sLastEmitting:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        while (_queue.Count > 0)
                            _queue.Dequeue().Ts.SetResult(false);
                        break;

                    default:
                        Debug.Assert(state == _sFinal);
                        _state = _sFinal;
                        break;
                }
            }

            private void Complete(Exception errorOpt)
            {
                var state = Atomic.Lock(ref _state);
                Debug.Assert(_active > 0);
                switch (state)
                {
                    case _sAccepting:
                        if (--_active == 0)
                        {
                            Current = default;
                            _error = errorOpt;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _cts.TryCancel();
                            _atmbDisposed.SetResult();
                            _tsAccepting.SetExceptionOrResult(errorOpt, false);
                        }
                        else
                        {
                            var tsMaxConcurrent = Linx.Clear(ref _tsMaxConcurrent);
                            if (errorOpt != null)
                            {
                                Current = default;
                                _error = errorOpt;
                                _state = _sFinal;
                                _ctr.Dispose();
                                _cts.TryCancel();
                                tsMaxConcurrent?.SetResult(false);
                                _tsAccepting.SetException(errorOpt);
                            }
                            else
                            {
                                _state = _sAccepting;
                                tsMaxConcurrent?.SetResult(true);
                            }
                        }
                        break;

                    case _sEmitting:
                        if (--_active == 0)
                        {
                            _error = errorOpt;
                            if (_queue.Count == 0)
                            {
                                _state = _sFinal;
                                _ctr.Dispose();
                            }
                            else
                                _state = _sLastEmitting;
                            _cts.TryCancel();
                            _atmbDisposed.SetResult();
                        }
                        else
                        {
                            var tsMaxConcurrent = Linx.Clear(ref _tsMaxConcurrent);
                            if (errorOpt != null)
                            {
                                _error = errorOpt;
                                if (_queue.Count == 0)
                                {
                                    _state = _sFinal;
                                    _ctr.Dispose();
                                }
                                else
                                    _state = _sLastEmitting;
                                _cts.TryCancel();
                                tsMaxConcurrent?.SetResult(false);
                            }
                            else
                            {
                                _state = _sEmitting;
                                tsMaxConcurrent?.SetResult(true);
                            }
                        }
                        break;

                    default:
                        Debug.Assert(state == _sLastEmitting || state == _sFinal);
                        if (--_active == 0)
                        {
                            _state = state;
                            _atmbDisposed.SetResult();
                        }
                        else
                            _state = state;
                        break;
                }
            }

            private async void ProduceOuter()
            {
                Exception error = null;
                try
                {
                    var tsMaxConcurrent = new ManualResetValueTaskSource<bool>();
                    await foreach (var inner in _sources.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                            case _sEmitting:
                                if (++_active <= _maxConcurrent)
                                {
                                    _state = state;
                                    ProduceInner(inner);
                                    if (_cts.IsCancellationRequested)
                                        return;
                                }
                                else
                                {
                                    tsMaxConcurrent.Reset();
                                    _tsMaxConcurrent = tsMaxConcurrent;
                                    _state = state;
                                    ProduceInner(inner);
                                    if (!await tsMaxConcurrent.Task.ConfigureAwait(false))
                                        return;
                                }
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

            private async void ProduceInner(IAsyncEnumerable<T> source)
            {
                Exception error = null;
                try
                {
                    if (_cts.IsCancellationRequested)
                        return;

                    var ts = new ManualResetValueTaskSource<bool>();
                    await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                                Current = item;
                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                                if (_cts.IsCancellationRequested)
                                    return;
                                break;

                            case _sEmitting:
                                ts.Reset();
                                try { _queue.Enqueue((item, ts)); }
                                finally { _state = _sEmitting; }
                                if (!await ts.Task.ConfigureAwait(false))
                                    return;
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
