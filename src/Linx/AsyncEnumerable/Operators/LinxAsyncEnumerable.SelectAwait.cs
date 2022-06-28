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
        Func<TSource, CancellationToken, ValueTask<TResult>> resultSelector)
        => new SelectAwaitIterator<TSource, TResult>(
            source ?? throw new ArgumentNullException(nameof(source)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)),
            true, 1);

    /// <summary>
    /// Projects each element of a sequence into a new form, using a async result selector.
    /// </summary>
    /// <param name="source">The source sequence.</param>
    /// <param name="resultSelector">Async result selector.</param>
    /// <param name="preserveOrder">
    /// If true, output items appear in order of their corresponding input item.
    /// If false, output items appear in order of completion.
    /// </param>
    /// <param name="maxConcurrent">Specifies the maximum number of concurrent invocations of the <paramref name="resultSelector"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxConcurrent"/> is non-positive.</exception>
    public static IAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<TResult>> resultSelector,
        bool preserveOrder,
        int maxConcurrent = int.MaxValue)
        => new SelectAwaitIterator<TSource, TResult>(
            source ?? throw new ArgumentNullException(nameof(source)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)),
            preserveOrder,
            maxConcurrent > 0 ? maxConcurrent : throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Must be positive."));

    private sealed class SelectAwaitIterator<TSource, TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
    {
        private const int _sEnumerator = 0; // GetEnumeratorAsync has not been called
        private const int _sIdle = 1; // no pending async operation
        private const int _sMoving = 2; // pending MoveNextAsync
        private const int _sCanceled = 3; // idle but no more items
        private const int _sDisposing = 4; // canceled and pending DisposeAsync
        private const int _sDisposingMoving = 5; // canceled and pending MoveNextAsync
        private const int _sDisposed = 6; // final state

        private readonly IAsyncEnumerable<TSource> _source;
        private readonly Func<TSource, CancellationToken, ValueTask<TResult>> _resultSelector;
        private readonly bool _preserveOrder;
        private readonly int _maxConcurrent;

        private readonly CancellationTokenSource _cts = new(); // cancels source enumeration and result selectors
        private readonly ManualResetValueTaskSource<bool> _tsMoving = new(); // returned by MoveNextAsync
        private TResult? _current;
        private CancellationTokenRegistration _ctr;

        private int _state;
        private Exception? _error; // final error when canceled, temporary when source enumeration completed
        private AsyncTaskMethodBuilder _atmbDisposed = AsyncTaskMethodBuilder.Create(); // returned by DisposeAsync

        // source enumeration control
        private ManualResetValueTaskSource<bool>? _tsProducer; // Produce() awaits this
        private bool _queueHasErrors; // stop creating new nodes
        private bool _producerIsCompleted;

        // FIFO queue of nodes
        private Node? _first, _last;
        private int _count;

        private Node? _nodePool; // recicled nodes

        public SelectAwaitIterator(
            IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TResult>> resultSelector,
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

        private SelectAwaitIterator(SelectAwaitIterator<TSource, TResult> parent)
        {
            _source = parent._source;
            _resultSelector = parent._resultSelector;
            _preserveOrder = parent._preserveOrder;
            _maxConcurrent = parent._maxConcurrent;
        }

        public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
        {
            if (Atomic.CompareExchange(ref _state, _sIdle, _sEnumerator) != _sEnumerator) // already enumerating
                return new SelectAwaitIterator<TSource, TResult>(this).GetAsyncEnumerator(token);

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

            Debug.Assert(_count > 0);
            _count--;
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
                    if (_count > 0 || !_producerIsCompleted) // pulling from queue
                    {
                        var node = TryDequeue();
                        if (node is null) // no completed item at head of the queue
                        {
                            var ts = _count < _maxConcurrent ? Linx.Clear(ref _tsProducer) : null;
                            if (_count == 0 && _nodePool is not null) // consumer has cought up, prune the pool
                                _nodePool.Next = null;
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
                    if (_count > 0 || !_producerIsCompleted)
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
            _nodePool = null;
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
            // wait for 1st _sMoving
            var ts = _tsProducer = new();
            if (!await ts.Task.ConfigureAwait(false))
                return;

            Exception? error = null;
            try
            {
                await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
                {
                    // wait for _sMoving and start a node
                    while (true)
                    {
                        var state = Atomic.Lock(ref _state);
                        if (_queueHasErrors)
                        {
                            _state = state;
                            return;
                        }

                        switch (state)
                        {
                            case _sIdle: // await sMoving
                                ts.Reset();
                                _tsProducer = ts;
                                _state = _sIdle;
                                if (!await ts.Task.ConfigureAwait(false))
                                    return;
                                continue;

                            case _sMoving:
                                Debug.Assert(_count < _maxConcurrent);

                                Node? node;
                                if (_nodePool is null)
                                    try
                                    {
                                        node = new(this);
                                    }
                                    catch
                                    {
                                        _state = _sMoving;
                                        throw;
                                    }
                                else
                                {
                                    node = _nodePool;
                                    _nodePool = node.Next;
                                    node.Next = null;
                                }

                                if (_preserveOrder)
                                    Enqueue(node);
                                _count++;
                                _state = _sMoving;
                                node.Start(item);
                                break;

                            default:
                                _state = state;
                                return;
                        }
                        break;
                    }

                    // wait for _sMoving and _count < _maxConcurrent
                    while (true)
                    {
                        var state = Atomic.Lock(ref _state);
                        if (_queueHasErrors)
                        {
                            _state = state;
                            return;
                        }

                        switch (state)
                        {
                            case _sMoving when _count < _maxConcurrent:
                                _state = _sMoving;
                                break;

                            case _sIdle:
                            case _sMoving:
                                ts.Reset();
                                _tsProducer = ts;
                                _state = state;
                                if (!await ts.Task.ConfigureAwait(false))
                                    return;
                                continue;

                            default:
                                _state = state;
                                return;
                        }

                        break;
                    }
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
            private ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter _awaiter;

            public Node(SelectAwaitIterator<TSource, TResult> parent) => _parent = parent;

            public void Start(TSource item)
            {
                Debug.Assert(!IsCompleted && _parent._count > 0);

                try
                {
#pragma warning disable CA2012 // Use ValueTasks correctly
                    var awaiter = _parent._resultSelector(item, _parent._cts.Token).ConfigureAwait(false).GetAwaiter();
#pragma warning restore CA2012 // Use ValueTasks correctly
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
                Next = _parent._nodePool;
                _parent._nodePool = this;
            }

            private void OnCompleted(Exception? exception, TResult? result)
            {
                Debug.Assert(!IsCompleted && _parent._count > 0);

                var state = Atomic.Lock(ref _parent._state);
                IsCompleted = true;
                Exception = exception;
                Result = result;
                if (!_parent._preserveOrder) // only enqueue when completed (otherwise already in queue)
                    _parent.Enqueue(this);

                if (_parent._first == this)
                    _parent.Pulse(state);
                else if (exception is null)
                    _parent._state = state;
                else
                {
                    _parent._queueHasErrors = true;
                    _parent._nodePool = null;
                    var ts = Linx.Clear(ref _parent._tsProducer);
                    _parent._state = state;
                    ts?.SetResult(false);
                }
            }
        }
    }
}

