namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Start a task for every item and emit its result.
        /// </summary>
        /// <param name="source">The source sequence.</param>
        /// <param name="selector">A delegate to create a task.</param>
        /// <param name="preserveOrder">true to emit result items in the order of their source items, false to emit them as soon as available.</param>
        /// <param name="maxConcurrent">Maximum number of concurrent tasks.</param>
        public static IAsyncEnumerable<TResult> Parallel<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, Task<TResult>> selector,
            bool preserveOrder = false,
            int maxConcurrent = int.MaxValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (maxConcurrent <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrent));

            return maxConcurrent == 1 ?
                source.Select(selector) :
                Create(token => new ParallelEnumerator<TSource, TResult>(source, selector, preserveOrder, maxConcurrent, token));

        }

        private sealed class ParallelEnumerator<TSource, TResult> : IAsyncEnumerator<TResult>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sCanceling = 3;
            private const int _sCancelingAccepting = 4;
            private const int _sFinal = 5;

            private readonly IAsyncEnumerable<TSource> _source;
            private readonly Func<TSource, CancellationToken, Task<TResult>> _selector;
            private readonly bool _preserveOrder;
            private readonly int _maxConcurrent;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private AsyncTaskMethodBuilder _atmbDisposed = default;
            private ErrorHandler _eh = ErrorHandler.Init();
            private int _state;
            private int _active;
            private Queue<(TResult next, AsyncTaskMethodBuilder ack)> _ready;
            private ManualResetValueTaskSource _tsMaxConcurrent;

            public ParallelEnumerator(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, Task<TResult>> selector,
                bool preserveOrder,
                int maxConcurrent,
                CancellationToken token)
            {
                _source = source;
                _selector = selector;
                _preserveOrder = preserveOrder;
                _maxConcurrent = maxConcurrent;
                if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token), true));
            }

            public TResult Current { get; private set; }

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
                        if (_ready == null || _ready.Count == 0)
                            _state = _sAccepting;
                        else
                        {
                            var (next, ack) = _ready.Dequeue();
                            Current = next;
                            _state = _sEmitting;
                            ack.SetResult();
                            _tsAccepting.SetResult(true);
                        }
                        break;

                    case _sCanceling:
                        _state = _sCancelingAccepting;
                        break;

                    case _sFinal:
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_eh.Error, false);
                        break;

                    default: // Accepting, CancelingAccepting???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                Cancel(AsyncEnumeratorDisposedException.Instance, true);
                return new ValueTask(_atmbDisposed.Task);
            }

            private void Cancel(Exception error, bool external)
            {
                Debug.Assert(error != null);

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        Debug.Assert(external);
                        _eh.SetExternalError(error);
                        _eh.SetInternalError(new OperationCanceledException(_eh.InternalToken));
                        _state = _sFinal;
                        _eh.Cancel();
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                    case _sEmitting:
                        if (external) _eh.SetExternalError(error);
                        else _eh.SetInternalError(error);

                        Queue<(TResult next, AsyncTaskMethodBuilder ack)> ready;
                        if (_ready != null)
                        {
                            ready = _ready.Count > 0 ? _ready : null;
                            _ready = null;
                        }
                        else
                            ready = null;

                        var tsMaxConcurrent = _tsMaxConcurrent;
                        _tsMaxConcurrent = null;

                        _state = state == _sAccepting ? _sCancelingAccepting : _sCanceling;
                        _eh.Cancel();

                        if (ready != null)
                        {
                            var oce = new OperationCanceledException(_eh.InternalToken);
                            foreach (var (_, ack) in ready)
                                ack.SetException(oce);
                        }
                        tsMaxConcurrent?.SetException(new OperationCanceledException(_eh.InternalToken));
                        break;

                    default: // Canceling, CancelingAccepting, Final
                        if (!external) _eh.SetInternalError(error);
                        _state = state;
                        break;
                }
            }

            private Task Emit(TResult next)
            {
                Task tAck;
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                        Current = next;
                        _state = _sEmitting;
                        _tsAccepting.SetResult(true);
                        tAck = Task.CompletedTask;
                        break;

                    case _sEmitting:
                        try
                        {
                            var ack = new AsyncTaskMethodBuilder();
                            tAck = ack.Task;
                            if (_ready == null) _ready = new Queue<(TResult, AsyncTaskMethodBuilder)>();
                            _ready.Enqueue((next, ack));
                        }
                        catch (Exception ex) { tAck = Task.FromException(ex); }
                        _state = _sEmitting;
                        break;

                    case _sCanceling:
                    case _sCancelingAccepting:
                        tAck = Task.FromCanceled(_eh.InternalToken);
                        break;

                    default: // Initial, Final???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return tAck;
            }

            private void DecrementActive()
            {
                var state = Atomic.Lock(ref _state);
                Debug.Assert(_active > 0);

                if (--_active == 0)
                {
                    Debug.Assert(_ready == null || _ready.Count == 0);
                    _ready = null;

                    Debug.Assert(_tsMaxConcurrent == null);

                    switch (state)
                    {
                        case _sAccepting:
                            Current = default;
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            _tsAccepting.SetExceptionOrResult(_eh.Error, false);
                            break;

                        case _sEmitting:
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sCanceling:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            break;

                        case _sCancelingAccepting:
                            Current = default;
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            _tsAccepting.SetExceptionOrResult(_eh.Error, false);
                            break;

                        default: // Initial, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }
                else if (_tsMaxConcurrent != null)
                {
                    var tsMaxConcurrent = _tsMaxConcurrent;
                    _tsMaxConcurrent = null;
                    _state = state;
                    tsMaxConcurrent.SetResult();
                }
                else
                    _state = state;
            }

            private async void Produce()
            {
                Debug.Assert(_active == 1);

                try
                {
                    Action<TSource> start;
                    if (_preserveOrder)
                    {
                        var predecessor = Task.CompletedTask;
                        start = item => predecessor = Start(item, predecessor);

                        async Task Start(TSource item, Task p)
                        {
                            try
                            {
                                var result = await _selector(item, _eh.InternalToken).ConfigureAwait(false);
                                await p.ConfigureAwait(false);
                                await Emit(result).ConfigureAwait(false);
                            }
                            catch (Exception ex) { Cancel(ex, false); }

                            DecrementActive();
                        }
                    }
                    else
                        start = async item =>
                        {
                            try
                            {
                                var result = await _selector(item, _eh.InternalToken).ConfigureAwait(false);
                                await Emit(result).ConfigureAwait(false);
                            }
                            catch (Exception ex) { Cancel(ex, false); }

                            DecrementActive();
                        };

                    var tsMaxConcurrent = new ManualResetValueTaskSource();

                    var ae = _source.WithCancellation(_eh.InternalToken).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        while (await ae.MoveNextAsync())
                        {
                            var current = ae.Current;
                            bool wait;

                            var state = Atomic.Lock(ref _state);
                            switch (state)
                            {
                                case _sAccepting:
                                case _sEmitting:
                                    wait = ++_active > _maxConcurrent;
                                    if (wait)
                                    {
                                        tsMaxConcurrent.Reset();
                                        _tsMaxConcurrent = tsMaxConcurrent;
                                    }
                                    _state = state;
                                    break;

                                case _sCanceling:
                                case _sCancelingAccepting:
                                    _state = state;
                                    throw new OperationCanceledException(_eh.InternalToken);

                                default: // Initial, Final
                                    _state = state;
                                    throw new Exception(state + "???");
                            }

                            start(current);
                            if (wait) await tsMaxConcurrent.Task.ConfigureAwait(false);
                        }
                    }
                    finally { await ae.DisposeAsync(); }
                }
                catch (Exception ex) { Cancel(ex, false); }

                DecrementActive();
            }
        }
    }
}
