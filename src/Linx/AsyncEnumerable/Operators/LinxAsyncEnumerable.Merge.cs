namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Enumerable;
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
            return maxConcurrent == 1 ? sources.Concat() : new MergeEnumerable<T>(sources, maxConcurrent);
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
            => sources.Async().Merge(maxConcurrent);

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second) => new[] { first, second }.Async().Merge();

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(params IAsyncEnumerable<T>[] sources) => sources.Async().Merge();

        private sealed class MergeEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<IAsyncEnumerable<T>> _sources;
            private readonly int _maxConcurrent;

            public MergeEnumerable(IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent)
            {
                _sources = sources;
                _maxConcurrent = maxConcurrent;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingAccepting = 4;
                private const int _sFinal = 5;

                private readonly MergeEnumerable<T> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private ErrorHandler _eh = ErrorHandler.Init();
                private int _state;
                private int _active;
                private Queue<(T next, ManualResetValueTaskSource ack)> _ready;
                private ManualResetValueTaskSource _tsMaxConcurrent;

                public Enumerator(MergeEnumerable<T> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token), true));
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

                            Queue<(T next, ManualResetValueTaskSource ack)> ready;
                            if (_ready != null)
                            {
                                ready = _ready.Count > 0 ? _ready : null;
                                _ready = null;
                            }
                            else
                                ready = null;

                            var tsMaxConcurrent = _tsMaxConcurrent;
                            _tsMaxConcurrent = null;

                            _state = _sCancelingAccepting;
                            _eh.Cancel();

                            if (ready != null)
                            {
                                var oce = new OperationCanceledException(_eh.InternalToken);
                                foreach (var (_, ack) in ready)
                                    ack.SetException(oce);
                            }
                            tsMaxConcurrent?.SetException(new OperationCanceledException(_eh.InternalToken));
                            break;

                        case _sCanceling:
                        case _sCancelingAccepting:
                            if (!external) _eh.SetInternalError(error);
                            _state = state;
                            break;

                        default: // Final
                            _state = state;
                            break;
                    }
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
                        var tsMaxConcurrent = new ManualResetValueTaskSource();

                        var ae = _enumerable._sources.WithCancellation(_eh.InternalToken).ConfigureAwait(false).GetAsyncEnumerator();
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
                                        wait = ++_active > _enumerable._maxConcurrent;
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

                                    default: // Initial, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }

                                Produce(current);
                                if (wait) await tsMaxConcurrent.Task.ConfigureAwait(false);
                            }
                        }
                        finally { await ae.DisposeAsync(); }
                    }
                    catch (Exception ex) { Cancel(ex, false); }

                    DecrementActive();
                }

                private async void Produce(IAsyncEnumerable<T> inner)
                {
                    try
                    {
                        var ts = new ManualResetValueTaskSource();
                        var ae = inner.WithCancellation(_eh.InternalToken).ConfigureAwait(false).GetAsyncEnumerator();
                        try
                        {
                            while (await ae.MoveNextAsync())
                            {
                                var current = ae.Current;
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sAccepting:
                                        Current = current;
                                        _state = _sEmitting;
                                        _tsAccepting.SetResult(true);
                                        break;

                                    case _sEmitting:
                                        ts.Reset();
                                        try
                                        {
                                            if (_ready == null) _ready = new Queue<(T, ManualResetValueTaskSource)>();
                                            _ready.Enqueue((current, ts));
                                        }
                                        finally { _state = _sEmitting; }
                                        await ts.Task.ConfigureAwait(false);
                                        break;

                                    case _sCanceling:
                                    case _sCancelingAccepting:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
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
}
