namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Observable;
    using TaskSources;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Buffers items in case the consumer is slower than the generator.
        /// </summary>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source) => source.ToLinxObservable().Buffer();

        /// <summary>
        /// Buffers items up to a maximum size in case the consumer is slower than the generator.
        /// </summary>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source, int maxSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return
                maxSize <= 0 ? source :
                maxSize == int.MaxValue ? source.Buffer()
                : Create(token => new BufferEnumerator<T>(source, maxSize, token));
        }

        private sealed class BufferEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sError = 3;
            private const int _sLast = 4;
            private const int _sFinal = 5;

            private readonly IAsyncEnumerable<T> _source;
            private readonly int _maxSize;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private readonly Queue<T> _queue = new Queue<T>();
            private ManualResetValueTaskSource<bool> _tsQueueFull;
            private int _state;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbDisposed = default;

            public BufferEnumerator(IAsyncEnumerable<T> source, int maxSize, CancellationToken token)
            {
                Debug.Assert(source != null);
                Debug.Assert(maxSize > 0 && maxSize < int.MaxValue);

                _source = source;
                _maxSize = maxSize;
                _token = token;
            }

            public T Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                _tsAccepting.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sAccepting;
                        Produce();
                        break;

                    case _sEmitting:
                        if (_queue.Count > 0)
                        {
                            Current = _queue.Dequeue();
                            if (_queue.Count == 0) // consumer now faster than producer
                                try { _queue.TrimExcess(); }
                                catch {/**/}
                            var ts = Linx.Clear(ref _tsQueueFull);
                            _state = _sEmitting;
                            ts?.SetResult(true);
                            _tsAccepting.SetResult(true);
                        }
                        else
                            _state = _sAccepting;
                        break;

                    case _sError:
                        Current = default;
                        _state = _sError;
                        _tsAccepting.SetException(_error);
                        break;

                    case _sLast:
                        Debug.Assert(_error == null && _queue.Count > 0);
                        Current = _queue.Dequeue();
                        if (_queue.Count == 0)
                        {
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                        }
                        else
                            _state = _sLast;
                        _tsAccepting.SetResult(true);
                        break;

                    case _sFinal:
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default:
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
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        Current = default;
                        _error = error;
                        _state = _sError;
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sError;
                        _queue.Clear();
                        Linx.Clear(ref _tsQueueFull)?.SetResult(false);
                        break;

                    case _sLast:
                        _error = error;
                        _state = _sFinal;
                        _queue.Clear();
                        _atmbDisposed.SetResult();
                        break;

                    default: // Error, Final
                        _state = state;
                        break;
                }
            }

            private async void Produce()
            {
                try
                {
                    _token.ThrowIfCancellationRequested();

                    var tsQueueFull = new ManualResetValueTaskSource<bool>();
                    await foreach (var item in _source.WithCancellation(_token).ConfigureAwait(false))
                        while (true)
                        {
                            var state = Atomic.Lock(ref _state);
                            switch (state)
                            {
                                case _sAccepting:
                                    Current = item;
                                    _state = _sEmitting;
                                    _tsAccepting.SetResult(true);
                                    break;

                                case _sEmitting:
                                    if (_queue.Count == _maxSize)
                                    {
                                        tsQueueFull.Reset();
                                        _tsQueueFull = tsQueueFull;
                                        _state = _sEmitting;
                                        if (!await tsQueueFull.Task.ConfigureAwait(false))
                                            return;
                                        continue;
                                    }

                                    try { _queue.Enqueue(item); }
                                    finally { _state = _sEmitting; }
                                    break;

                                case _sError:
                                    _state = _sError;
                                    return;

                                default: // Initial, Last, Final???
                                    _state = state;
                                    throw new Exception(state + "???");
                            }

                            break;
                        }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
                finally
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert(_queue.Count == 0);
                            Current = default;
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            _tsAccepting.SetResult(false);
                            break;

                        case _sEmitting when (_queue.Count > 0):
                            _state = _sLast;
                            break;

                        case _sEmitting:
                        case _sError:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            break;

                        default: // Initial, Last, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }
            }
        }
    }
}
