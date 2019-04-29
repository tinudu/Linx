namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
    {
        /// <summary>
        /// Buffers items in case the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerableObs<T> Buffer<T>(this IAsyncEnumerableObs<T> source, int maxSize = int.MaxValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return maxSize > 0 ? new BufferEnumerable<T>(source, maxSize) : source;
        }

        private sealed class BufferEnumerable<T> : IAsyncEnumerableObs<T>
        {
            private readonly IAsyncEnumerableObs<T> _source;
            private readonly int _maxSize;

            public BufferEnumerable(IAsyncEnumerableObs<T> source, int maxSize)
            {
                Debug.Assert(source != null);
                Debug.Assert(maxSize > 0);
                _source = source;
                _maxSize = maxSize;
            }

            public IAsyncEnumeratorObs<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumeratorObs<T>
            {
                private const int _sInitial = 0; // not enumerating
                private const int _sActive = 1; // enumerating
                private const int _sPulling = 2; // enumerating, queue empty
                private const int _sPushing = 3; // enumerating, queue full
                private const int _sCompleted = 4; // done enumerating, queue not empty
                private const int _sCanceling = 5; // enumerating and canceled
                private const int _sCancelingPulling = 6;
                private const int _sFinal = 7;

                private readonly BufferEnumerable<T> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private int _state;
                private CoCompletionSource<bool> _ccsPull = CoCompletionSource<bool>.Init();
                private CoCompletionSource _ccsPush = CoCompletionSource.Init();
                private Queue<T> _queue;

                public Enumerator(BufferEnumerable<T> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public T Current { get; private set; }

                public ICoAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext)
                {
                    _ccsPull.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sPulling;
                            Produce();
                            break;

                        case _sActive:
                            if (_queue == null || _queue.Count == 0)
                                _state = _sPulling;
                            else
                            {
                                Current = _queue.Dequeue(); // no exception assumed
                                _state = _sActive;
                                _ccsPull.SetResult(true);
                            }
                            break;

                        case _sPushing: // queue is full
                            Debug.Assert(_queue.Count > 0);
                            Current = _queue.Dequeue(); // no exception assumed
                            _state = _sActive;
                            _ccsPull.SetResult(true);
                            _ccsPush.SetResult();
                            break;

                        case _sCompleted:
                            Debug.Assert(_queue.Count > 0);
                            Current = _queue.Dequeue(); // no exception assumed
                            if (_queue.Count > 0)
                                _state = _sCompleted;
                            else
                            {
                                _queue = null;
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                            }
                            _ccsPull.SetResult(true);
                            break;

                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;

                        case _sFinal:
                            _state = _sFinal;
                            Current = default;
                            _eh.SetResultOrError(_ccsPull, false);
                            break;

                        default: // Pulling, CancelingPulling???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _ccsPull.Task;
                }

                public Task DisposeAsync()
                {
                    Cancel(ErrorHandler.EnumeratorDisposedException);
                    return _atmbDisposed.Task;
                }

                private void Cancel(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                        case _sCompleted:
                            _eh.SetExternalError(error);
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sActive:
                            _eh.SetExternalError(error);
                            _queue = null;
                            _state = _sCanceling;
                            _eh.Cancel();
                            break;

                        case _sPushing:
                            _eh.SetExternalError(error);
                            _queue = null;
                            _state = _sCanceling;
                            _eh.Cancel();
                            _ccsPush.SetException(new OperationCanceledException(_eh.InternalToken));
                            break;

                        case _sPulling:
                            _eh.SetExternalError(error);
                            _queue = null;
                            _state = _sCancelingPulling;
                            _eh.Cancel();
                            break;

                        case _sCanceling:
                        case _sCancelingPulling:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce()
                {
                    Exception error;
                    try
                    {
                        var ae = _enumerable._source.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            while (await ae.MoveNextAsync())
                            {
                                var state = Atomic.Lock(ref _state);
                                while (state == _sActive && _queue != null && _queue.Count >= _enumerable._maxSize)
                                {
                                    _ccsPush.Reset(false);
                                    _state = _sPushing;
                                    await _ccsPush.Task;
                                    state = Atomic.Lock(ref _state);
                                }
                                switch (state)
                                {
                                    case _sActive:
                                        try
                                        {
                                            if (_queue == null) _queue = new Queue<T>();
                                            _queue.Enqueue(ae.Current);
                                        }
                                        finally { _state = _sActive; }
                                        break;

                                    case _sPulling:
                                        Debug.Assert(_queue == null || _queue.Count == 0);
                                        try { Current = ae.Current; }
                                        catch { _state = _sPulling; throw; }
                                        _state = _sActive;
                                        _ccsPull.SetResult(true);
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCanceling:
                                    case _sCancelingPulling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Pushing, Completed, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }

                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    {
                        var state = Atomic.Lock(ref _state);
                        _eh.SetInternalError(error);
                        switch (state)
                        {
                            case _sActive:
                                if (_eh.Error == null && _queue != null && _queue.Count > 0)
                                    _state = _sCompleted;
                                else
                                {
                                    _queue = null;
                                    _state = _sFinal;
                                    _eh.Cancel();
                                    _atmbDisposed.SetResult();
                                }
                                break;

                            case _sPulling:
                                _queue = null;
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                Current = default;
                                _eh.SetResultOrError(_ccsPull, false);
                                break;

                            case _sCanceling:
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                break;

                            case _sCancelingPulling:
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                _eh.SetResultOrError(_ccsPull, false);
                                break;

                            default: // Initial, Pushing, Last, Final???
                                _state = state;
                                throw new Exception(_state + "???");
                        }
                    }
                }
            }
        }
    }
}
