namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Sources;

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

            return maxConcurrent == 1 ? source.Select(selector) : new ParallelEnumerable<TSource, TResult>(source, selector, preserveOrder, maxConcurrent);
        }

        private sealed class ParallelEnumerable<TSource, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<TSource> _source;
            private readonly Func<TSource, CancellationToken, Task<TResult>> _selector;
            private readonly bool _preserveOrder;
            private readonly int _maxConcurrent;

            public ParallelEnumerable(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, Task<TResult>> selector, bool preserveOrder, int maxConcurrent)
            {
                _source = source;
                _selector = selector;
                _preserveOrder = preserveOrder;
                _maxConcurrent = maxConcurrent;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<TResult>
            {
                private const int _sInitial = 0;
                private const int _sActive = 1;
                private const int _sCanceling = 2;
                private const int _sFinal = 3;
                private const int _stateMask = 3;

                private const int _fPulling = 4;
                private const int _fIncrementing = 8;

                private readonly ParallelEnumerable<TSource, TResult> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private readonly ManualResetValueTaskSource<bool> _vtsPulling = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource<Unit> _vtsIncrementing = new ManualResetValueTaskSource<Unit>();
                private int _state;
                private int _active; // #started tasks + _queue.Count + (Producing ? 1 : 0)
                private Queue<TResult> _queue;

                public Enumerator(ParallelEnumerable<TSource, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _vtsPulling.Reset();

                    var state = Atomic.Lock(ref _state);
                    Debug.Assert((state & _fPulling) == 0);

                    switch (state & _stateMask)
                    {
                        case _sInitial:
                            _active = 1;
                            _state = _sActive | _fPulling;
                            Produce();
                            break;
                        case _sActive:
                            if (_queue == null || _queue.Count == 0)
                            {
                                Debug.Assert(_active > 0);
                                _state = state | _fPulling;
                            }
                            else
                            {
                                Debug.Assert(_active >= _queue.Count);
                                Current = _queue.Dequeue(); // no exception assumed
                                if ((state & _fIncrementing) != 0)
                                {
                                    _state = _sActive;
                                    _vtsIncrementing.SetResult(default);
                                }
                                else if (--_active == 0) // this was the last item
                                {
                                    _queue = null;
                                    _state = _sFinal;
                                    _eh.Cancel();
                                    _atmbDisposed.SetResult();
                                }
                                else
                                    _state = _sActive;
                                _vtsPulling.SetResult(true);
                            }
                            break;
                        case _sCanceling:
                            _state = _sCanceling | _fPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            Current = default;
                            _vtsPulling.SetExceptionOrResult(_eh.Error, false);
                            break;
                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _vtsPulling.GenericTask();
                }

                public ValueTask DisposeAsync()
                {
                    Cancel(ErrorHandler.EnumeratorDisposedException);
                    return new ValueTask(_atmbDisposed.Task);
                }

                private void Emit(TResult result)
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0);

                    switch (state & _stateMask)
                    {
                        case _sActive:
                            if ((state & _fPulling) != 0)
                            {
                                if ((state & _fIncrementing) != 0)
                                {
                                    _state = _sActive;
                                    _vtsIncrementing.SetResult(default);
                                }
                                else if (--_active == 0) // this was the last item
                                {
                                    _queue = null;
                                    _state = _sFinal;
                                    _eh.Cancel();
                                    _atmbDisposed.SetResult();
                                }
                                else
                                    _state = _sActive;

                                Current = result;
                                _vtsPulling.SetResult(true);
                            }
                            else // enqueue
                                try
                                {
                                    if (_queue == null) _queue = new Queue<TResult>();
                                    _queue.Enqueue(result);
                                    _state = state;
                                }
                                catch (Exception ex)
                                {
                                    _state = state;
                                    Throw(ex);
                                }
                            break;
                        case _sCanceling:
                            if (--_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                if ((state & _fPulling) != 0)
                                {
                                    Current = default;
                                    _vtsPulling.SetExceptionOrResult(_eh.Error, false);
                                }
                            }
                            break;
                        default: // Initial, Final
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void Throw(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state & _stateMask)
                    {
                        case _sActive:
                            _eh.SetInternalError(error);

                            if (_queue != null)
                            {
                                Debug.Assert(_active > _queue.Count);
                                _active -= _queue.Count + 1;
                                _queue = null;
                            }
                            else
                            {
                                Debug.Assert(_active > 0);
                                _active--;
                            }

                            if (_active == 0) // go final
                            {
                                Debug.Assert((state & _fIncrementing) == 0);
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                if ((state & _fPulling) != 0)
                                {
                                    Current = default;
                                    _vtsPulling.SetExceptionOrResult(_eh.Error, false);
                                }
                            }
                            else // go canceled
                            {
                                _state = _sCanceling | (state & _fPulling);
                                _eh.Cancel();
                                if ((state & _fIncrementing) != 0) _vtsIncrementing.SetException(new OperationCanceledException(_eh.InternalToken));
                            }
                            break;
                        case _sCanceling:
                            _eh.SetInternalError(error);

                            Debug.Assert(_active > 0);
                            if (--_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                if ((state & _fPulling) != 0)
                                {
                                    Current = default;
                                    _vtsPulling.SetExceptionOrResult(_eh.Error, false);
                                }
                            }
                            break;
                        default: // Initial, Final
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void Cancel(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state & _stateMask)
                    {
                        case _sInitial:
                        case _sActive:
                            _eh.SetExternalError(error);

                            if (_queue != null)
                            {
                                Debug.Assert(_active >= _queue.Count);
                                _active -= _queue.Count;
                                _queue = null;
                            }

                            if (_active == 0) // go final
                            {
                                Debug.Assert((state & _fIncrementing) == 0);
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                if ((state & _fPulling) != 0)
                                {
                                    Current = default;
                                    _vtsPulling.SetExceptionOrResult(_eh.Error, false);
                                }
                            }
                            else // go canceling
                            {
                                _state = _sCanceling | (state & _fPulling);
                                _eh.Cancel();
                                if ((state & _fIncrementing) != 0) _vtsIncrementing.SetException(new OperationCanceledException(_eh.InternalToken));
                            }
                            break;
                        default: // Canceled, Final
                            _state = state;
                            break;
                    }
                }

                private async void Produce()
                {
                    Debug.Assert(_active == 1);

                    try
                    {
                        var ae = _enumerable._source.WithCancellation(_eh.InternalToken).ConfigureAwait(false).GetAsyncEnumerator();
                        try
                        {
                            Action start;
                            if (_enumerable._preserveOrder)
                            {
                                var predecessor = Task.CompletedTask;
                                start = () => predecessor = Start(predecessor);

                                async Task Start(Task p)
                                {
                                    TResult result;
                                    try { result = await _enumerable._selector(ae.Current, _eh.InternalToken).ConfigureAwait(false); }
                                    catch (Exception ex)
                                    {
                                        Throw(ex);
                                        return;
                                    }

                                    await p.ConfigureAwait(false);
                                    Emit(result);
                                }
                            }
                            else
                            {
                                start = async () =>
                                {
                                    TResult result;
                                    try { result = await _enumerable._selector(ae.Current, _eh.InternalToken).ConfigureAwait(false); }
                                    catch (Exception ex)
                                    {
                                        Throw(ex);
                                        return;
                                    }

                                    Emit(result);
                                };
                            }

                            while (await ae.MoveNextAsync())
                            {
                                // increment _active
                                var state = Atomic.Lock(ref _state);
                                switch (state & _stateMask)
                                {
                                    case _sActive:
                                        if (_active > _enumerable._maxConcurrent)
                                        {
                                            _vtsIncrementing.Reset();
                                            _state = state | _fIncrementing;
                                            await _vtsIncrementing.Task();
                                        }
                                        else
                                        {
                                            _active++;
                                            _state = state;
                                        }

                                        break;
                                    case _sCanceling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);
                                    default: // Initial, Final
                                        _state = state;
                                        throw new Exception(state + "???");
                                }

                                start();
                                _eh.InternalToken.ThrowIfCancellationRequested();
                            }
                        }
                        finally { await ae.DisposeAsync(); }
                    }
                    catch (Exception ex) { Throw(ex); return; }

                    // decrement _active
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state & _stateMask)
                        {
                            case _sActive:
                                Debug.Assert(_active > 0);
                                if (--_active == 0)
                                {
                                    _queue = null;
                                    _state = _sFinal;
                                    _eh.Cancel();
                                    _atmbDisposed.SetResult();
                                    if ((state & _fPulling) != 0)
                                    {
                                        Current = default;
                                        _vtsPulling.SetExceptionOrResult(_eh.Error, false);
                                    }
                                }
                                else
                                    _state = state;
                                break;
                            case _sCanceling:
                                Debug.Assert(_active > 0);
                                if (--_active == 0)
                                {
                                    _queue = null;
                                    _state = _sFinal;
                                    _atmbDisposed.SetResult();
                                    if ((state & _fPulling) != 0)
                                    {
                                        Current = default;
                                        _vtsPulling.SetExceptionOrResult(_eh.Error, false);
                                    }
                                }
                                else
                                    _state = state;
                                break;
                            default:
                                _state = state;
                                throw new Exception(state + "???");
                        }
                    }
                }
            }
        }
    }
}
