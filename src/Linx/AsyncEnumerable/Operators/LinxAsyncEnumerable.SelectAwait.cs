using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Linx.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Projects each element of a sequence into a new form, using a async result selector.
    /// </summary>
    /// <param name="source">The source sequence.</param>
    /// <param name="resultSelector">Async result selector.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is null.</exception>
    public static IAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<TResult>> resultSelector)
        => new SelectAwaitIterator<TSource, TResult>(
            source ?? throw new ArgumentNullException(nameof(source)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)),
            true, // doesn't matter
            1);

    /// <summary>
    /// Projects each element of a sequence into a new form, using a async result selector.
    /// </summary>
    /// <param name="source">The source sequence.</param>
    /// <param name="resultSelector">Async result selector.</param>
    /// <param name="preserveOrder">
    /// If true, output items appear in order of their corresponding input item.
    /// If false, output items appear in order of completion of the result.
    /// </param>
    /// <param name="maxConcurrent">Specifies the maximum number of concurrent invocations of the <paramref name="resultSelector"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxConcurrent"/> is non-positive.</exception>
    public static IAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<TResult>> resultSelector,
        bool preserveOrder,
        int maxConcurrent = int.MaxValue)
        => new SelectAwaitIterator<TSource, TResult>(
            source ?? throw new ArgumentNullException(nameof(source)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)),
            preserveOrder,
            maxConcurrent > 0 ? maxConcurrent : throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Must be positive."));

    private sealed class SelectAwaitIterator<TSource, TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
    {
        private const int _sEnumerator = 0; // GetEnumeratorAsync has not been called yet
        private const int _sIdle = 1; // no pending async operation
        private const int _sMoving = 2; // pending MoveNextAsync
        private const int _sCanceled = 3; // idle, but the sequence has completed
        private const int _sDisposing = 4; // canceled and pending DisposeAsync
        private const int _sDisposingMoving = 5; // canceled and pending MoveNextAsync
        private const int _sDisposed = 6; // source enumeration and all result selectors completed

        private readonly IAsyncEnumerable<TSource> _source;
        private readonly Func<TSource, CancellationToken, Task<TResult>> _resultSelector;
        private readonly bool _preserveOrder;
        private readonly int _maxConcurrent;

        private readonly CancellationTokenSource _cts = new(); // cancel source enumeration and result selectors
        private readonly ManualResetValueTaskSource<bool> _tsMoving = new(); // returned by MoveNextAsync
        private TResult? _current;
        private CancellationTokenRegistration _ctr;

        private int _state;
        private Exception? _error; // final error when canceled, temporary when source enumeration completed
        private AsyncTaskMethodBuilder _atmbDisposed = Linx.CreateAsyncTaskMethodBuilder(); // completed when disposed

        // source enumeration control
        private ManualResetValueTaskSource<bool>? _tsProducer; // Produce() awaits this when starting or _maxConcurrent reached
        private bool _queueHasErrors; // stop creating new nodes
        private bool _producerIsCompleted;

        // queue of nodes
        private Node? _first, _last; // FIFO queue of nodes
        private int _nConcurrent; // # of Nodes in the queue + incomplete nodes not in the queue

        private Node? _pool; // recicled nodes

        public SelectAwaitIterator(
            IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, Task<TResult>> resultSelector,
            bool preserveOrder,
            int maxConcurrent)
        {
            Debug.Assert(source is not null);
            Debug.Assert(resultSelector is not null);
            Debug.Assert(maxConcurrent > 0);

            _source = source;
            _resultSelector = resultSelector;
            _preserveOrder = preserveOrder;
            _maxConcurrent = maxConcurrent;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
        {
            if (Atomic.CompareExchange(ref _state, _sIdle, _sEnumerator) != _sEnumerator) // already enumerating
                return new SelectAwaitIterator<TSource, TResult>(_source, _resultSelector, _preserveOrder, _maxConcurrent).GetAsyncEnumerator(token);

            Produce();
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token), false));
            return this;
        }

        public TResult Current => _current!;

        public ValueTask<bool> MoveNextAsync()
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sEnumerator:
                case _sMoving:
                case _sDisposingMoving:
                    _state = state;
                    throw new InvalidOperationException();

                case _sIdle:
                    _tsMoving.Reset();
                    Pulse(_sMoving);
                    return _tsMoving.Task;

                case _sCanceled:
                case _sDisposing:
                    _tsMoving.Reset();
                    Pulse(_sDisposingMoving);
                    return _tsMoving.Task;

                case _sDisposed:
                    _state = _sDisposed;
                    return _tsMoving.Task;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        public ValueTask DisposeAsync()
        {
            OnError(AsyncEnumeratorDisposedException.Instance, true);
            return new(_atmbDisposed.Task);
        }

        private void Enqueue(Node node)
        {
            Debug.Assert(_state < 0);
            Debug.Assert(node.Next is null);

            if (_first is null)
            {
                Debug.Assert(_last is null);
                _first = _last = node;
            }
            else
            {
                Debug.Assert(_last is not null && _last.Next is null);
                _last = _last.Next = node;
            }
        }

        private Node? TryDequeue()
        {
            Debug.Assert(_state < 0);

            if (_first is null || !_first.IsCompleted)
                return null;

            var node = _first;
            _first = node.Next;
            if (_first is null)
            {
                Debug.Assert(node.Next is null);
                _last = null;
            }
            else
                node.Next = null;

            Debug.Assert(_nConcurrent > 0);
            _nConcurrent--;
            return node;
        }

        private void Pulse(int state) // state or queue has changed
        {
            Debug.Assert(_state < 0);

            switch (state)
            {
                case _sIdle:
                case _sDisposed:
                    _state = state;
                    break;

                case _sMoving:
                    if (_nConcurrent > 0 || !_producerIsCompleted) // pulling from queue
                    {
                        var node = TryDequeue();
                        if (node is null) // no completed item at head of the queue
                        {
                            var ts = _nConcurrent < _maxConcurrent ? Linx.Clear(ref _tsProducer) : null;
                            if (_nConcurrent == 0) // consumer has cought up, so give nodes to the garbage collector
                                _pool = null;
                            _state = _sMoving;
                            ts?.SetResult(true);
                        }
                        else if (node.Exception is null) // we have a next
                        {
                            _current = node.Result;
                            var ts = Linx.Clear(ref _tsProducer);
                            node.Recycle();
                            _state = _sIdle;
                            ts?.SetResult(true);
                            _tsMoving.SetResult(true);
                        }
                        else
                            Cancel(node.Exception, _sDisposingMoving);
                    }
                    else
                        Cancel(_error, _sDisposingMoving);
                    break;

                case _sCanceled:
                    while (TryDequeue() is not null) { }
                    _state = _sCanceled;
                    break;

                case _sDisposing:
                case _sDisposingMoving:
                    while (TryDequeue() is not null) { }
                    if (_nConcurrent > 0 || !_producerIsCompleted)
                        _state = state;
                    else
                    {
                        _current = default;
                        if (state == _sDisposing)
                            _tsMoving.Reset();
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _tsMoving.SetExceptionOrResult(_error, false);
                    }
                    break;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        private void Cancel(Exception? error, int destinationState) // this is called once
        {
            Debug.Assert(_state < 0);
            Debug.Assert(!_cts.IsCancellationRequested);

            _error = error;
            _ctr.Dispose();
            var ts = Linx.Clear(ref _tsProducer);
            _pool = null;
            Pulse(destinationState);
            ts?.SetResult(false);
            _cts.Cancel();
        }

        private void OnError(Exception? error, bool disposing)
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sEnumerator:
                    _state = _sEnumerator;
                    throw new InvalidOperationException();

                case _sIdle:
                    Cancel(error, disposing ? _sDisposing : _sCanceled);
                    break;

                case _sMoving:
                    Cancel(error, _sDisposingMoving);
                    break;

                case _sCanceled:
                    Pulse(disposing ? _sDisposing : _sCanceled);
                    break;

                case _sDisposingMoving:
                case _sDisposing:
                    Pulse(state);
                    break;

                case _sDisposed:
                    _state = _sDisposed;
                    break;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        private async void Produce()
        {
            var ts = _tsProducer = new();
            Exception? error = null;
            try
            {
                if (!await ts.Task.ConfigureAwait(false))
                    return;

                await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
                {
                    var state = Atomic.Lock(ref _state);
loop:
                    switch (state)
                    {
                        case _sIdle:
                        case _sMoving:
                            if (_queueHasErrors)
                            {
                                _state = state;
                                return;
                            }

                            if (_nConcurrent >= _maxConcurrent)
                            {
                                ts.Reset();
                                _tsProducer = ts;
                                _state = state;
                                if (!await ts.Task.ConfigureAwait(false))
                                    return;
                                state = Atomic.Lock(ref _state);
                                goto loop;
                            }
                            break;

                        default:
                            _state = state;
                            return;
                    }

                    Node node;
                    try
                    {
                        if (_pool is null)
                            node = new(this);
                        else
                        {
                            node = _pool;
                            _pool = node.Next;
                            node.Next = null;
                        }
                        _nConcurrent++;
                        if (_preserveOrder) // enqueue pending
                            Enqueue(node);
                    }
                    finally
                    {
                        _state = state;
                    }
                    node.Start(item);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                var state = Atomic.Lock(ref _state);
                _producerIsCompleted = true;
                if (_error is null)
                    _error = error;
                Pulse(state);
            }
        }

        private sealed class Node // recyclable LL node controlling an async operation
        {
            public Node? Next; // in queue or pool
            public bool IsCompleted { get; private set; }
            public Exception? Exception { get; private set; }
            public TResult? Result { get; private set; }

            private readonly SelectAwaitIterator<TSource, TResult> _parent;
            private Action? _continuation;
            private ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter _awaiter;

            public Node(SelectAwaitIterator<TSource, TResult> parent) => _parent = parent;

            public void Start(TSource item)
            {
                Debug.Assert(!IsCompleted);

                try
                {
                    var awaiter = _parent._resultSelector(item, _parent._cts.Token).ConfigureAwait(false).GetAwaiter();
                    if (awaiter.IsCompleted)
                        OnCompleted(null, awaiter.GetResult());
                    else
                    {
                        if (_continuation is null)
                            _continuation = () =>
                            {
                                try
                                {
                                    OnCompleted(null, Linx.Clear(ref _awaiter).GetResult());
                                }
                                catch (Exception ex)
                                {
                                    OnCompleted(ex, default);
                                }
                            };
                        _awaiter = awaiter;
                        awaiter.OnCompleted(_continuation);
                    }
                }
                catch (Exception ex)
                {
                    OnCompleted(ex, default);
                }
            }

            public void Recycle()
            {
                Debug.Assert(_parent._state < 0);

                IsCompleted = false;
                Exception = null;
                Result = default;
                Next = _parent._pool;
                _parent._pool = this;
            }

            private void OnCompleted(Exception? exception, TResult? result)
            {
                Debug.Assert(!IsCompleted);

                var state = Atomic.Lock(ref _parent._state);
                Debug.Assert(_parent._nConcurrent > 0);

                IsCompleted = true;
                Exception = exception;
                Result = result;
                ManualResetValueTaskSource<bool>? ts;
                if (exception is null)
                    ts = null;
                else
                {
                    _parent._queueHasErrors = true;
                    _parent._pool = null;
                    ts = Linx.Clear(ref _parent._tsProducer);
                }
                if (!_parent._preserveOrder) // only enqueue when completed (otherwise already in queue)
                    _parent.Enqueue(this);
                if (_parent._first == this)
                    _parent.Pulse(state);
                else
                    _parent._state = state;
                ts?.SetResult(false);
            }
        }
    }
}

