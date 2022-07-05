using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Linx.Async;

namespace Linx.AsyncEnumerable.Subjects;

/// <summary>
/// A <see cref="ISubject{T}"/> that guarantees enumeration of the whole source sequence.
/// </summary>
/// <remarks>Disallows enumeration once it's connected.</remarks>
public sealed class ColdSubject<T> : ISubject<T>, IAsyncEnumerable<T>
{
    private const int _sSubjInitial = 0;
    private const int _sSubjConnected = 1;
    private const int _sSubjDisposed = 2;

    private readonly IAsyncEnumerable<T> _source;
    private readonly CancellationTokenSource _cts = new();
    private AsyncTaskMethodBuilder _atmbDisposed = AsyncTaskMethodBuilder.Create();

    private int _state;

    // doubly linked list of enumerators
    private Enumerator? _first, _last;
    private int _count;

    /// <summary>
    /// Initialize.
    /// </summary>
    public ColdSubject(IAsyncEnumerable<T> source)
        => _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <inheritdoc/>
    public IAsyncEnumerable<T> AsyncEnumerable => this;

    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken token)
    {
        var state = Atomic.Lock(ref _state);
        switch (state)
        {
            case _sSubjInitial:
                Enumerator e;
                try
                {
                    e = new Enumerator(this);
                    checked { _count++; }

                    // append
                    if (_last is null)
                        _first = e;
                    else
                    {
                        e.Prev = _last;
                        _last.Next = e;
                    }
                    _last = e;
                }
                finally { _state = _sSubjInitial; }

                if (token.CanBeCanceled)
                    e.RegisterToken(token);

                return e;

            case _sSubjConnected:
                _state = _sSubjConnected;
                throw SubjectAlreadyConnectedException.Instance;

            case _sSubjDisposed:
                _state = _sSubjDisposed;
                throw SubjectDisposedException.Instance;

            default:
                _state = state;
                throw new Exception(state + "???");
        }
    }

    /// <inheritdoc/>
    public void Connect()
    {
        var state = Atomic.Lock(ref _state);
        switch (state)
        {
            case _sSubjInitial:
                if (_first is not null)
                {
                    _state = _sSubjConnected;
                    Produce();
                }
                else
                {
                    _state = _sSubjDisposed;
                    _atmbDisposed.SetResult();
                }
                break;

            case _sSubjConnected:
                _state = _sSubjConnected;
                throw SubjectAlreadyConnectedException.Instance;

            case _sSubjDisposed:
                _state = _sSubjDisposed;
                throw SubjectDisposedException.Instance;

            default:
                _state = state;
                throw new Exception(state + "???");
        }
    }

    private async void Produce()
    {
        Exception? error = null;
        try
        {
            await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
            {
                // pass 1: yield
                for (var current = _first; current != null; current = current.Next)
                    current.Yield(item);

                // pass 2: await and remove those returning false
                for (var current = _first; current != null;)
                {
                    var next = current.Next;

                    if (!await current.YieldResult.ConfigureAwait(false))
                        Remove(current);

                    current = next;
                }

                if (_count == 0)
                    return;
            }
        }
        catch (Exception ex) { error = ex; }
        finally
        {
            Atomic.Exchange(ref _state, _sSubjDisposed);
            _atmbDisposed.SetResult();
            for (var current = _first; current != null; current = current.Next)
                current.OnCompleted(error, false);
            _first = _last = null;
        }
    }

    private void Remove(Enumerator e)
    {
        if (e.Prev is null)
            _first = e.Next;
        else
            e.Prev.Next = e.Next;

        if (e.Next is null)
            _last = e.Prev;
        else
            e.Next.Prev = e.Prev;

        e.Prev = e.Next = null;
    }

    private sealed class Enumerator : IAsyncEnumerator<T>
    {
        private const int _sInitial = 0;
        private const int _sMoving = 1;
        private const int _sYielding = 2;
        private const int _sCanceled = 3;
        private const int _sDisposed = 4;

        public Enumerator? Prev, Next;

        private readonly ColdSubject<T> _parent;
        private readonly ManualResetValueTaskCompleter<bool> _vtcYield = new();
        private readonly ManualResetValueTaskCompleter<bool> _vtcMoving = new();
        private CancellationTokenRegistration _ctr;
        private int _state;
        private T? _current;
        private Exception? _error;
        private bool _isLastEnumerator;

        public Enumerator(ColdSubject<T> parent) => _parent = parent;

        public void RegisterToken(CancellationToken token)
        {
            try { _ctr = token.Register(() => OnCompleted(new OperationCanceledException(), false)); }
            catch (Exception ex) { OnCompleted(ex, false); }
        }

        public ValueTask<bool> YieldResult => _vtcYield.ValueTask;

        public void Yield(T item)
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sInitial:
                    _state = _sInitial;
                    OnCompleted(SubjectAlreadyConnectedException.Instance, false);
                    break;

                case _sMoving:
                    _current = item;
                    _vtcYield.Reset();
                    _state = _sYielding;
                    _vtcMoving.SetResult(true);
                    break;

                case _sCanceled:
                case _sDisposed:
                    _state = state;
                    break;

                default: // yielding???
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        public void OnCompleted(Exception? error, bool disposing)
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sInitial:
                case _sYielding:
                    if (disposing)
                    {
                        _vtcMoving.Reset();
                        Cancel(_sDisposed);
                        _vtcMoving.SetExceptionOrResult(error, false);
                    }
                    else
                    {
                        _error = error;
                        Cancel(_sCanceled);
                    }
                    _vtcYield.SetResult(false);
                    if (_isLastEnumerator)
                        _parent._cts.Cancel();
                    break;

                case _sMoving:
                    Cancel(_sDisposed);
                    _vtcYield.SetResult(false);
                    _vtcMoving.SetExceptionOrResult(error, false);
                    if (_isLastEnumerator)
                        _parent._cts.Cancel();
                    break;

                case _sCanceled:
                    if (disposing)
                    {
                        _vtcMoving.Reset();
                        _state = _sDisposed;
                        _vtcMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                    }
                    else
                        _state = _sCanceled;
                    break;

                case _sDisposed:
                    _state = _sDisposed;
                    break;
            }
        }

        private void Cancel(int state)
        {
            Debug.Assert(_state < 0);

            var parentState = Atomic.Lock(ref _parent._state);
            switch (parentState)
            {
                case _sSubjInitial:
                    _parent.Remove(this);
                    --_parent._count;
                    _parent._state = _sSubjInitial;
                    break;

                default:
                    _isLastEnumerator = --_parent._count == 0;
                    _parent._state = parentState;
                    break;
            }

            _state = state;
            _ctr.Dispose();
        }

        T IAsyncEnumerator<T>.Current => _current!;

        ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sMoving:
                    _state = state;
                    throw new InvalidOperationException();

                case _sInitial:
                case _sYielding:
                    _vtcMoving.Reset();
                    _state = _sMoving;
                    _vtcYield.SetResult(true);
                    return _vtcMoving.ValueTask;

                case _sCanceled:
                    _vtcMoving.Reset();
                    _state = _sDisposed;
                    _vtcMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                    return _vtcMoving.ValueTask;

                case _sDisposed:
                    _state = _sDisposed;
                    return _vtcMoving.ValueTask;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            OnCompleted(AsyncEnumeratorDisposedException.Instance, true);
            return _isLastEnumerator ? new(_parent._atmbDisposed.Task) : new();
        }
    }
}
