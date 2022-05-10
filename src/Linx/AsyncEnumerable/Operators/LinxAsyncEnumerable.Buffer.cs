using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Linx.Tasks;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Buffer all items.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source, bool runContinuationsAsynchronously = false)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new BufferIterator<T>(source, int.MaxValue, runContinuationsAsynchronously);
        }

        /// <summary>
        /// Buffer up to <paramref name="maxCapacity"/> items.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCapacity"/> is non-positive.</exception>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source, int maxCapacity, bool runContinuationsAsynchronously = false)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Must be positive.");

            return new BufferIterator<T>(source, maxCapacity, runContinuationsAsynchronously);
        }

        private sealed class BufferIterator<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sEmitting = 1;
            private const int _sAccepting = 2;
            private const int _sCompleted = 3;
            private const int _sFinal = 4;

            private readonly IAsyncEnumerable<T> _source;
            private readonly int _maxCapacity, _initialCapacity;

            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
            private ManualResetValueTaskSource<bool>? _tsProduce;
            private readonly CancellationTokenSource _cts = new();
            private CancellationTokenRegistration _ctr;
            private AsyncTaskMethodBuilder _atmbDisposed;
            private int _state;
            private Exception? _error;

            private int _offset, _count;
            private T?[]? _queue;

            public BufferIterator(IAsyncEnumerable<T> source, int maxCapacity, bool runContinuationsAsyncronously)
            {
                Debug.Assert(source is not null);
                Debug.Assert(maxCapacity > 0);

                _source = source;
                _maxCapacity = maxCapacity;
                _initialCapacity = Linx.Capacities(maxCapacity).Last();
                _tsAccepting.RunContinuationsAsynchronously = runContinuationsAsyncronously;
            }

            private BufferIterator(BufferIterator<T> parent)
            {
                _source = parent._source;
                _maxCapacity = parent._maxCapacity;
                _initialCapacity = parent._initialCapacity;
                _tsAccepting.RunContinuationsAsynchronously = parent._tsAccepting.RunContinuationsAsynchronously;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            {
                var state = Atomic.Lock(ref _state);

                if (state != _sInitial)
                {
                    _state = state;
                    return new BufferIterator<T>(this).GetAsyncEnumerator(token);
                }

                var tsProduce = _tsProduce = new();
                _state = _sEmitting;
                Produce(tsProduce);

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));

                return this;
            }

            public T Current { get; private set; } = default!;

            public ValueTask<bool> MoveNextAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sEmitting:
                        _tsAccepting.Reset();
                        var tsProduce = Linx.Clear(ref _tsProduce);
                        if (_count == 0)
                            _state = _sAccepting;
                        else
                        {
                            Current = Dequeue();
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }
                        tsProduce?.SetResult(true);
                        return _tsAccepting.Task;

                    case _sCompleted:
                        Debug.Assert(_count > 0);
                        _tsAccepting.Reset();
                        Current = Dequeue();
                        if (_count > 0)
                            _state = _sCompleted;
                        else
                        {
                            _state = _sFinal;
                            _ctr.Dispose();
                            _cts.TryCancel();
                            _queue = null;
                        }
                        _tsAccepting.SetResult(true);
                        return _tsAccepting.Task;

                    case _sFinal:
                        _state = _sFinal;
                        _tsAccepting.Reset();
                        Current = default!;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        return _tsAccepting.Task;

                    case _sInitial:
                    case _sAccepting:
                        _state = state;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                Current = default!;
                return new(_atmbDisposed.Task);
            }

            private T Dequeue()
            {
                Debug.Assert(_queue is not null && _count > 0);
                Debug.Assert((_state & Atomic.LockBit) != 0);

                T result = Linx.Clear(ref _queue[_offset])!;
                if (--_count == 0)
                {
                    _offset = 0;
                    if (_queue.Length > _initialCapacity)
                        _queue = null;
                }
                else if (++_offset == _queue.Length)
                    _offset = 0;
                return result;
            }

            private void SetFinal(Exception? errorOrNot)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sInitial;
                        throw new InvalidOperationException();

                    case _sEmitting:
                    case _sAccepting:
                    case _sCompleted:
                        _error = errorOrNot;
                        var tsProduce = Linx.Clear(ref _tsProduce);
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        tsProduce?.SetResult(false);
                        if (state == _sAccepting)
                        {
                            Current = default!;
                            _tsAccepting.SetExceptionOrResult(errorOrNot, false);
                        }
                        _queue = null;
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private async void Produce(ManualResetValueTaskSource<bool> tsProduce)
            {
                Exception? error = null;
                try
                {
                    if (!await tsProduce.Task.ConfigureAwait(false))
                        return;

                    await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        bool loop;
                        do
                        {
                            var state = Atomic.Lock(ref _state);
                            switch (state)
                            {
                                case _sEmitting:
                                    if (_count == _maxCapacity)
                                    {
                                        tsProduce.Reset();
                                        _tsProduce = tsProduce;
                                        _state = _sEmitting;
                                        if (!await tsProduce.Task.ConfigureAwait(false))
                                            return;
                                        loop = true;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            if (_queue is null)
                                            {
                                                Debug.Assert(_offset == 0 && _count == 0);
                                                _queue = new T[_initialCapacity];
                                                _queue[0] = item;
                                                _count = 1;
                                            }
                                            else if (_count == _queue.Length)
                                            {
                                                var q = new T[Linx.Capacities(_maxCapacity).TakeWhile(c => c > _queue.Length).Last()];
                                                var c0 = _queue.Length - _offset;
                                                var c1 = _count - c0;
                                                if (c1 <= 0)
                                                    Array.Copy(_queue, _offset, q, 0, _count);
                                                else
                                                {
                                                    Array.Copy(_queue, _offset, q, 0, c0);
                                                    Array.Copy(_queue, 0, q, c0, c1);
                                                }
                                                _queue = q;
                                                _offset = 0;
                                                _queue[_count++] = item;
                                            }
                                            else if (_offset == 0)
                                                _queue[_count++] = item;
                                            else
                                            {
                                                var ix = _offset - _queue.Length + _count++;
                                                _queue[ix >= 0 ? ix : ix + _queue.Length] = item;
                                            }
                                        }
                                        finally { _state = _sEmitting; }
                                        loop = false;
                                    }
                                    break;

                                case _sAccepting:
                                    Debug.Assert(_count == 0);
                                    Current = item;
                                    _state = _sEmitting;
                                    _tsAccepting.SetResult(true);
                                    loop = false;
                                    break;

                                default:
                                    Debug.Assert(state == _sFinal);
                                    _state = state;
                                    return;
                            }
                        }
                        while (loop);
                    }
                }
                catch (Exception ex) { error = ex; }
                finally
                {
                    _atmbDisposed.SetResult();

                    var state = Atomic.Lock(ref _state);
                    if (state == _sEmitting && _count > 0)
                    {
                        _error = error;
                        _state = _sCompleted;
                    }
                    else
                    {
                        _state = state;
                        SetFinal(error);
                    }
                }
            }
        }
    }
}
