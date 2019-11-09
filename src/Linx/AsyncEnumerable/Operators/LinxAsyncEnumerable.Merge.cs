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
            private const int _sErrorQ = 3;
            private const int _sError = 4;
            private const int _sFinalQ = 5;
            private const int _sFinal = 6;

            private readonly IAsyncEnumerable<IAsyncEnumerable<T>> _sources;
            private readonly int _maxConcurrent;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private ManualResetValueTaskSource<bool> _tsEmitting, _tsMaxConcurrent;
            private readonly Queue<(T Next, ManualResetValueTaskSource<bool> Ts)> _queue = new Queue<(T, ManualResetValueTaskSource<bool>)>();
            private AsyncTaskMethodBuilder _atmbDisposed = default;
            private CancellationTokenRegistration _ctr;
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
                        var tsEmitting = Linx.Clear(ref _tsEmitting);
                        if (_queue.Count == 0)
                            _state = _sAccepting;
                        else
                        {
                            var (next, ts) = _queue.Dequeue();
                            Current = next;
                            _tsEmitting = ts;
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }
                        tsEmitting.SetResult(true);
                        break;

                    case _sErrorQ:
                    {
                        Debug.Assert(_queue.Count > 0);
                        var (next, ts) = _queue.Dequeue();
                        Current = next;
                        if (_queue.Count == 0)
                        {
                            _state = _sError;
                            _ctr.Dispose();
                        }
                        else
                            _state = _sErrorQ;
                        _tsAccepting.SetResult(true);
                        ts.SetResult(false);
                    }
                    break;

                    case _sFinalQ:
                    {
                        Debug.Assert(_queue.Count > 0);
                        var (next, ts) = _queue.Dequeue();
                        Current = next;
                        if (_queue.Count == 0)
                        {
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                        }
                        else
                            _state = _sFinalQ;
                        _tsAccepting.SetResult(true);
                        ts.SetResult(false);
                    }
                    break;

                    default:
                        Debug.Assert(state == _sError || state == _sFinal);
                        _state = state;
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
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                        _cts.TryCancel();
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        Linx.Clear(ref _tsEmitting).SetResult(false);
                        while (_queue.Count > 0)
                            _queue.Dequeue().Ts.SetResult(false);
                        Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                        _cts.TryCancel();
                        break;

                    case _sErrorQ:
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        while (_queue.Count > 0)
                            _queue.Dequeue().Ts.SetResult(false);
                        break;

                    case _sFinalQ:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        while (_queue.Count > 0)
                            _queue.Dequeue().Ts.SetResult(false);
                        _atmbDisposed.SetResult();
                        break;

                    default:
                        Debug.Assert(state == _sError || state == _sFinal);
                        _state = state;
                        break;
                }
            }

            private void OnError(Exception error)
            {
                Debug.Assert(error != null);

                var state = Atomic.Lock(ref _state);
                Debug.Assert(_active > 0);
                switch (state)
                {
                    case _sAccepting:
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                        _cts.TryCancel();
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        if (_queue.Count == 0)
                        {
                            _state = _sError;
                            _ctr.Dispose();
                        }
                        else
                            _state = _sErrorQ;
                        Linx.Clear(ref _tsEmitting).SetResult(false);
                        Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                        _cts.TryCancel();
                        break;

                    default:
                        Debug.Assert(state == _sErrorQ || state == _sError);
                        _state = state;
                        break;
                }
            }

            private void OnCompleted()
            {
                var state = Atomic.Lock(ref _state);
                Debug.Assert(_active > 0);
                if (--_active > 0)
                {
                    var tsMaxConcurrnt = Linx.Clear(ref _tsMaxConcurrent);
                    _state = state;
                    tsMaxConcurrnt?.SetResult(true);
                }
                else if (state == _sAccepting)
                {
                    _state = _sFinal;
                    _ctr.Dispose();
                    _cts.TryCancel();
                    _atmbDisposed.SetResult();
                    _tsAccepting.SetResult(false);
                }
                else
                {
                    Debug.Assert(state == _sError);
                    _state = _sFinal;
                    _atmbDisposed.SetResult();
                }
            }

            private async void ProduceOuter()
            {
                try
                {
                    var ts = new ManualResetValueTaskSource<bool>();
                    await foreach (var inner in _sources.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        while (true)
                        {
                            var state = Atomic.Lock(ref _state);
                            switch (state)
                            {
                                case _sAccepting:
                                case _sEmitting:
                                    if (_active > _maxConcurrent)
                                    {
                                        ts.Reset();
                                        _tsMaxConcurrent = ts;
                                        _state = state;
                                        if (!await ts.Task.ConfigureAwait(false))
                                            return;
                                        continue;
                                    }
                                    break;

                                default:
                                    Debug.Assert(state == _sErrorQ || state == _sError);
                                    _state = state;
                                    return;
                            }
                            _active++;
                            _state = state;
                            ProduceInner(inner);
                            break;
                        }
                    }
                }
                catch (Exception ex) { OnError(ex); }
                finally { OnCompleted(); }
            }

            private async void ProduceInner(IAsyncEnumerable<T> source)
            {
                try
                {
                    var ts = new ManualResetValueTaskSource<bool>();
                    await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                                ts.Reset();
                                Current = item;
                                _tsEmitting = ts;
                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                                if (!await ts.Task.ConfigureAwait(false))
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
                                Debug.Assert(state == _sErrorQ || state == _sError);
                                _state = state;
                                return;
                        }
                    }
                }
                catch (Exception ex) { OnError(ex); }
                finally { OnCompleted(); }
            }
        }
    }
}
