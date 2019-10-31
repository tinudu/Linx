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
            return maxConcurrent == 1 ? sources.Concat() : Create(token => new MergeEnumerator<T>(sources, maxConcurrent, token));
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            return sources.Async().Merge(maxConcurrent);
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> source, params IAsyncEnumerable<T>[] sources)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            return sources.Prepend(source).Async().Merge();
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> source, int maxConcurrent, params IAsyncEnumerable<T>[] sources)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            return sources.Prepend(source).Async().Merge(maxConcurrent);
        }

        private sealed class MergeEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sCompleted = 3;
            private const int _sFinal = 4;

            private readonly IAsyncEnumerable<IAsyncEnumerable<T>> _sources;
            private readonly int _maxConcurrent;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private readonly Queue<(T next, ManualResetValueTaskSource<bool> ts)> _queue = new Queue<(T, ManualResetValueTaskSource<bool>)>();
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
            private ManualResetValueTaskSource<bool> _tsEmitting, _tsOuter;
            private int _state, _active;
            private bool _outerIncrementActive;
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
                            var (next, ts) = _queue.Dequeue();
                            Current = next;
                            _tsEmitting = ts;
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }
                        else
                        {
                            var tsOuter = _active <= _maxConcurrent ? Linx.Clear(ref _tsOuter) : null;
                            _state = _sAccepting;
                            tsOuter?.SetResult(true);
                        }

                        tsEmitting.SetResult(true);
                        break;

                    case _sCompleted:
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
                ManualResetValueTaskSource<bool> tsOuter;
                switch (state)
                {
                    case _sInitial:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        return;

                    case _sAccepting:
                        Current = default;
                        _error = error;
                        tsOuter = Linx.Clear(ref _tsOuter);
                        _state = _sCompleted;
                        _ctr.Dispose();
                        Debug.Assert(_queue.Count == 0);
                        tsOuter?.SetResult(false);
                        _cts.TryCancel();
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        tsOuter = Linx.Clear(ref _tsOuter);
                        _state = _sCompleted;
                        _ctr.Dispose();
                        _tsEmitting.SetResult(false);
                        while (_queue.Count > 0)
                        {
                            var (_, ts) = _queue.Dequeue();
                            ts.SetResult(false);
                        }
                        tsOuter?.SetResult(false);
                        _cts.TryCancel();
                        break;

                    case _sCompleted:
                    case _sFinal:
                        _state = state;
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private void OnCompleted()
            {
                var state = Atomic.Lock(ref _state);
                Debug.Assert(_active > 0);
                _active--;
                switch (state)
                {
                    case _sAccepting:
                        if (_active == 0)
                        {
                            Current = default;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            Debug.Assert(_queue.Count == 0);
                            Debug.Assert(_tsOuter == null);
                            _cts.TryCancel();
                            Debug.Assert(_error == null);
                            _tsAccepting.SetResult(false);
                        }
                        else if (_tsOuter != null)
                        {
                            var tsOuter = Linx.Clear(ref _tsOuter);
                            if (_outerIncrementActive) _active++;
                            _state = _sAccepting;
                            tsOuter.SetResult(true);
                        }
                        else
                            _state = _sAccepting;
                        break;

                    case _sEmitting:
                        _state = _sEmitting;
                        break;

                    case _sCompleted:
                        if (_active == 0)
                        {
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                        }
                        else
                            _state = _sCompleted;
                        break;

                    default: // Initial, Final???
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private async void Produce()
            {
                try
                {
                    var tsOuter = new ManualResetValueTaskSource<bool>();

                    ValueTask<bool> Wait(bool incrementActive)
                    {
                        tsOuter.Reset();
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting when (_active <= _maxConcurrent):
                                if (incrementActive) _active++;
                                _state = _sAccepting;
                                tsOuter.SetResult(true);
                                break;

                            case _sAccepting:
                            case _sEmitting:
                                _tsOuter = tsOuter;
                                _outerIncrementActive = incrementActive;
                                _state = state;
                                break;

                            case _sCompleted:
                                _state = _sCompleted;
                                tsOuter.SetResult(false);
                                break;

                            default: // Initial, Final???
                                _state = state;
                                throw new Exception(state + "???");
                        }
                        return tsOuter.Task;
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
                                _queue.Enqueue((item, tsEmitting));
                                _state = _sEmitting;
                                if (!await tsEmitting.Task.ConfigureAwait(false)) return;
                                break;

                            case _sCompleted:
                                _state = _sCompleted;
                                return;

                            default: // Initial, Final???
                                _state = state;
                                throw new Exception(state + "???");
                        }

                        await tsEmitting.Task.ConfigureAwait(false);
                    }
                }
                catch (Exception ex) { OnError(ex); }
                finally { OnCompleted(); }
            }
        }
    }
}
