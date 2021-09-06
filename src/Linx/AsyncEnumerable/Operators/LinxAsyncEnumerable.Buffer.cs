namespace Linx.AsyncEnumerable
{
    using global::Linx.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Decouples the source from its consumer; buffered items are retrieved individually.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source) => source.Buffer(int.MaxValue, false);

        /// <summary>
        /// Decouples the source from its consumer; buffered items are retrieved individually.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCapacity"/> is non-positive.</exception>
        /// <remarks>
        /// <paramref name="backpressure"/> controls what happens if <paramref name="maxCapacity"/> is reached:
        /// A value of true excerts backpressure on the source.
        /// A value of false terminates the sequence with an exception.
        /// </remarks>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source, int maxCapacity, bool backpressure)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Must be positive.");

            return backpressure ?
                Create(token => new BufferBackpressureEnumerator<T>(source, maxCapacity, token)) :
                Create(token => new BufferThrowEnumerator<T>(source, maxCapacity, token));
        }

        private sealed class BufferBackpressureEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sCompleted = 3;
            private const int _sFinal = 4;

            private readonly IAsyncEnumerable<T> _source;
            private readonly int _maxCapacity;
            private readonly IEnumerable<int> _capacities;
            private readonly int _initialCapacity;

            private readonly LinxCancellationTokenSource _cts = new();
            private readonly CancellationTokenRegistration _ctr;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
            private readonly AsyncTaskMethodBuilder _atmbDisposed;
            private int _state;
            private Exception _error;
            private T[] _buffer;
            private int _offset, _count;
            private ManualResetValueTaskSource _tsBackpressure;

            public BufferBackpressureEnumerator(IAsyncEnumerable<T> source, int maxCapacity, CancellationToken token)
            {
                Debug.Assert(source is not null);
                Debug.Assert(maxCapacity > 0);

                _source = source;
                _maxCapacity = maxCapacity;
                _capacities = Linx.Capacities(maxCapacity);
                _initialCapacity = _capacities.Last();

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
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
                        if (_count == 0)
                            _state = _sAccepting;
                        else // dequeue
                        {
                            Current = Linx.Clear(ref _buffer[_offset++]);
                            if (--_count == 0)
                            {
                                _offset = 0;
                                if (_buffer.Length > _initialCapacity)
                                    _buffer = null;
                            }
                            else if (_offset == _buffer.Length)
                                _offset = 0;
                            var tsBp = Linx.Clear(ref _tsBackpressure);
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                            tsBp?.SetResult();
                        }
                        break;

                    case _sCompleted:
                        Debug.Assert(_count > 0);
                        Current = Linx.Clear(ref _buffer[_offset++]);
                        if (--_count == 0)
                        {
                            _buffer = null;
                            _state = _sFinal;
                            _ctr.Dispose();
                        }
                        else
                        {
                            if (_offset == _buffer.Length)
                                _offset = 0;
                            _state = _sCompleted;
                        }
                        _tsAccepting.SetResult(true);
                        break;

                    case _sFinal:
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default: // _sAccepting???
                        _state = _sAccepting;
                        SetFinal(new Exception(state + "???"));
                        break;
                }

                return _tsAccepting.Task;
            }

            public async ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                Current = default;
                await _atmbDisposed.Task.ConfigureAwait(false);
            }

            private void SetFinal(Exception error)
            {
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
                        Debug.Assert(_count == 0);
                        Current = default;
                        _error = error;
                        _buffer = null;
                        _state = _sFinal;
                        _cts.TryCancel();
                        _ctr.Dispose();
                        _tsAccepting.SetExceptionOrResult(error, false);
                        break;

                    case _sEmitting:
                        _error = error;
                        _buffer = null;
                        var tsBp = Linx.Clear(ref _tsBackpressure);
                        _state = _sFinal;
                        _cts.TryCancel();
                        _ctr.Dispose();
                        tsBp?.SetException(_cts.WhenCancellationRequested.Result);
                        break;

                    case _sCompleted:
                        _error = error;
                        _buffer = null;
                        _state = _sFinal;
                        _ctr.Dispose();
                        break;

                    default: // _sFinal
                        _state = state;
                        break;
                }
            }

            private async void Produce()
            {
                Exception error = null;
                try
                {
                    ManualResetValueTaskSource tsBp = new();
                    tsBp.Reset();

                    await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        bool loop;
                        do
                        {
                            var state = Atomic.Lock(ref _state);
                            switch (state)
                            {
                                case _sAccepting:
                                    Debug.Assert(_count == 0);
                                    Current = item;
                                    _state = _sEmitting;
                                    _tsAccepting.SetResult(true);
                                    _cts.Token.ThrowIfCancellationRequested();
                                    loop = false;
                                    break;

                                case _sEmitting:
                                    if (_count == _maxCapacity) // backpressure
                                    {
                                        _tsBackpressure = tsBp;
                                        _state = _sEmitting;
                                        await tsBp.Task.ConfigureAwait(false);
                                        tsBp.Reset();
                                        loop = true;
                                    }
                                    else // enqueue
                                    {
                                        if (_buffer is null)
                                            try
                                            {
                                                Debug.Assert(_count == 0 && _offset == 0);
                                                _buffer = new T[_initialCapacity];
                                                _buffer[0] = item;
                                                _count = 1;
                                            }
                                            finally { _state = _sEmitting; }
                                        else if (_count == _buffer.Length) // increase buffer size
                                            try
                                            {
                                                var newBuffer = new T[_capacities.TakeWhile(i => i > _count).Last()];
                                                if (_offset == 0)
                                                    Array.Copy(_buffer, newBuffer, _count);
                                                else
                                                {
                                                    var c = _count - _offset;
                                                    Array.Copy(_buffer, _offset, newBuffer, 0, c);
                                                    Array.Copy(_buffer, 0, newBuffer, c, _offset);
                                                    _offset = 0;
                                                }
                                                newBuffer[_count++] = item;
                                                _buffer = newBuffer;
                                            }
                                            finally { _state = _sEmitting; }
                                        else if (_offset == 0)
                                        {
                                            _buffer[_count++] = item;
                                            _state = _sEmitting;
                                        }
                                        else
                                        {
                                            var ix = _offset - _buffer.Length + _count;
                                            _buffer[ix > 0 ? ix : ix + _buffer.Length] = item;
                                            _count++;
                                        }
                                        loop = false;
                                    }
                                    break;

                                default: // _sFinal
                                    _state = state;
                                    throw _cts.WhenCancellationRequested.Result;
                            }
                        } while (loop);
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
                        _cts.TryCancel();
                    }
                    else
                    {
                        _state = state;
                        SetFinal(error);
                    }
                }
            }
        }

        private sealed class BufferThrowEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sCompleted = 3;
            private const int _sFinal = 4;

            private readonly IAsyncEnumerable<T> _source;
            private readonly int _maxCapacity;
            private readonly IEnumerable<int> _capacities;
            private readonly int _initialCapacity;

            private readonly LinxCancellationTokenSource _cts = new();
            private readonly CancellationTokenRegistration _ctr;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
            private readonly AsyncTaskMethodBuilder _atmbDisposed;
            private int _state;
            private Exception _error;
            private T[] _buffer;
            private int _offset, _count;

            public BufferThrowEnumerator(IAsyncEnumerable<T> source, int maxCapacity, CancellationToken token)
            {
                Debug.Assert(source is not null);
                Debug.Assert(maxCapacity > 0);

                _source = source;
                _maxCapacity = maxCapacity;
                _capacities = Linx.Capacities(maxCapacity);
                _initialCapacity = _capacities.Last();

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
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
                        if (_count == 0)
                            _state = _sAccepting;
                        else // dequeue
                        {
                            Current = Linx.Clear(ref _buffer[_offset++]);
                            if (--_count == 0)
                            {
                                _offset = 0;
                                if (_buffer.Length > _initialCapacity)
                                    _buffer = null;
                            }
                            else if (_offset == _buffer.Length)
                                _offset = 0;
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }
                        break;

                    case _sCompleted:
                        Debug.Assert(_count > 0);
                        Current = Linx.Clear(ref _buffer[_offset++]);
                        if (--_count == 0)
                        {
                            _buffer = null;
                            _state = _sFinal;
                            _ctr.Dispose();
                        }
                        else
                        {
                            if (_offset == _buffer.Length)
                                _offset = 0;
                            _state = _sCompleted;
                        }
                        _tsAccepting.SetResult(true);
                        break;

                    case _sFinal:
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default: // _sAccepting???
                        _state = _sAccepting;
                        SetFinal(new Exception(state + "???"));
                        break;
                }

                return _tsAccepting.Task;
            }

            public async ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                Current = default;
                await _atmbDisposed.Task.ConfigureAwait(false);
            }

            private void SetFinal(Exception error)
            {
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
                        Debug.Assert(_count == 0);
                        Current = default;
                        _error = error;
                        _buffer = null;
                        _state = _sFinal;
                        _cts.TryCancel();
                        _ctr.Dispose();
                        _tsAccepting.SetExceptionOrResult(error, false);
                        break;

                    case _sEmitting:
                        _error = error;
                        _buffer = null;
                        _state = _sFinal;
                        _cts.TryCancel();
                        _ctr.Dispose();
                        break;

                    case _sCompleted:
                        _error = error;
                        _buffer = null;
                        _state = _sFinal;
                        _ctr.Dispose();
                        break;

                    default: // _sFinal
                        _state = state;
                        break;
                }
            }

            private async void Produce()
            {
                Exception error = null;
                try
                {
                    await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                                Debug.Assert(_count == 0);
                                Current = item;
                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                                _cts.Token.ThrowIfCancellationRequested();
                                break;

                            case _sEmitting:
                                if (_count == _maxCapacity) // queue is full
                                {
                                    _state = _sEmitting;
                                    throw new Exception(Strings.QueueIsFull);
                                }
                                else // enqueue
                                {
                                    if (_buffer is null)
                                        try
                                        {
                                            Debug.Assert(_count == 0 && _offset == 0);
                                            _buffer = new T[_initialCapacity];
                                            _buffer[0] = item;
                                            _count = 1;
                                        }
                                        finally { _state = _sEmitting; }
                                    else if (_count == _buffer.Length) // increase buffer size
                                        try
                                        {
                                            var newBuffer = new T[_capacities.TakeWhile(i => i > _count).Last()];
                                            if (_offset == 0)
                                                Array.Copy(_buffer, newBuffer, _count);
                                            else
                                            {
                                                var c = _count - _offset;
                                                Array.Copy(_buffer, _offset, newBuffer, 0, c);
                                                Array.Copy(_buffer, 0, newBuffer, c, _offset);
                                                _offset = 0;
                                            }
                                            newBuffer[_count++] = item;
                                            _buffer = newBuffer;
                                        }
                                        finally { _state = _sEmitting; }
                                    else if (_offset == 0)
                                    {
                                        _buffer[_count++] = item;
                                        _state = _sEmitting;
                                    }
                                    else
                                    {
                                        var ix = _offset - _buffer.Length + _count;
                                        _buffer[ix > 0 ? ix : ix + _buffer.Length] = item;
                                        _count++;
                                    }
                                }
                                break;

                            default: // _sFinal
                                _state = state;
                                throw _cts.WhenCancellationRequested.Result;
                        }
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
                        _cts.TryCancel();
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
