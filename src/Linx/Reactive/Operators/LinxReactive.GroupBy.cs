namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
    {
        /// <summary>
        /// Group by a key.
        /// </summary>
        public static IAsyncEnumerable<IGroupedAsyncEnumerable<TKey, TSource>> GroupBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
            => new GroupByEnumerable<TSource, TKey>(source, keySelector, keyComparer, false);

        /// <summary>
        /// Group by a key; close a group when unsubscribed.
        /// </summary>
        public static IAsyncEnumerable<IGroupedAsyncEnumerable<TKey, TSource>> GroupByWhileObserved<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
            => new GroupByEnumerable<TSource, TKey>(source, keySelector, keyComparer, true);

        private sealed class GroupByEnumerable<TSource, TKey> : IAsyncEnumerable<IGroupedAsyncEnumerable<TKey, TSource>>
        {
            private readonly IAsyncEnumerable<TSource> _source;
            private readonly Func<TSource, TKey> _keySelector;
            private readonly IEqualityComparer<TKey> _keyComparer;
            private readonly bool _whileObserved;

            public GroupByEnumerable(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey> keyComparer,
                bool whileObserved)
            {
                _source = source ?? throw new ArgumentNullException(nameof(source));
                _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
                _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
                _whileObserved = whileObserved;
            }

            IAsyncEnumerator<IGroupedAsyncEnumerable<TKey, TSource>> IAsyncEnumerable<IGroupedAsyncEnumerable<TKey, TSource>>.GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<IGroupedAsyncEnumerable<TKey, TSource>>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingAccepting = 4;
                private const int _sFinal = 5;

                private readonly Dictionary<TKey, Group> _groups;
                private readonly CancellationTokenSource _cts = new CancellationTokenSource(); // request cancellation when Canceling[Accepting] and _nGroups == 0
                private readonly GroupByEnumerable<TSource, TKey> _enumerable;
                private CancellationTokenRegistration _ctr;
                private CoCompletionSource<bool> _ccsAccepting = CoCompletionSource<bool>.Init(); // pending MoveNextAsync
                private CoCompletionSource _ccsEmitting = CoCompletionSource.Init(); // await MoveNextAsync either on the group enumerator or a group
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private int _state;
                private Exception _error; // while canceling or when final
                private int _nGroups; // number of groups that are not final

                public Enumerator(GroupByEnumerable<TSource, TKey> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    _groups = new Dictionary<TKey, Group>(enumerable._keyComparer);
                    if (token.CanBeCanceled) _ctr = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public IGroupedAsyncEnumerable<TKey, TSource> Current { get; private set; }

                ICoAwaiter<bool> IAsyncEnumerator<IGroupedAsyncEnumerable<TKey, TSource>>.MoveNextAsync(bool continueOnCapturedContext)
                {
                    _ccsAccepting.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            Produce();
                            break;

                        case _sEmitting:
                            _state = _sAccepting;
                            _ccsEmitting.SetCompleted(null);
                            break;

                        case _sCanceling:
                            _state = _sCancelingAccepting;
                            break;

                        case _sFinal:
                            _state = _sFinal;
                            _ccsAccepting.SetCompleted(_error, false);
                            break;

                        default: // Accepting, CancelingAccepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _ccsAccepting.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(ErrorHandler.EnumeratorDisposedException);
                    return _atmbDisposed.Task;
                }

                private void Cancel(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    bool cancel;
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sEmitting:
                            _error = error;
                            cancel = _nGroups == 0;
                            _state = _sCanceling;
                            _ctr.Dispose();
                            if (cancel)
                                try { _cts.Cancel(); }
                                catch { /**/ }
                            _ccsEmitting.SetCompleted(null);
                            break;

                        case _sAccepting:
                            _error = error;
                            cancel = _nGroups == 0;
                            _state = _sCancelingAccepting;
                            _ctr.Dispose();
                            if (cancel)
                                try { _cts.Cancel(); }
                                catch { /**/ }
                            break;

                        default: // Canceling, CancelingAccepting, Final
                            _state = state;
                            break;
                    }
                }

                private async void Produce()
                {
                    Exception error;
                    try
                    {
                        var ae = _enumerable._source.GetAsyncEnumerator(_cts.Token);
                        try
                        {
                            while (await ae.MoveNextAsync())
                            {
                                var current = ae.Current;
                                var key = _enumerable._keySelector(current);

                                var state = Atomic.Lock(ref _state);
                                Group group;
                                try { _groups.TryGetValue(key, out group); }
                                catch { _state = state; throw; }

                                if (group == null && state == _sAccepting) // emit new group
                                {
                                    group = new Group(this, key);
                                    _nGroups++;

                                    // emit
                                    _ccsEmitting.Reset(false);
                                    _state = _sEmitting;
                                    Current = group;
                                    _ccsAccepting.SetCompleted(null, true);
                                    await _ccsEmitting.Awaiter;

                                    state = Atomic.Lock(ref _state);
                                    if (group.State == GroupState.Initial) // not enumerating
                                    {
                                        group.State = GroupState.TooLate;
                                        if (--_nGroups == 0 && state != _sAccepting)
                                        {
                                            _state = state;
                                            try { _cts.Cancel(); } catch {/**/}
                                            throw new OperationCanceledException(_cts.Token);
                                        }
                                    }
                                }

                                if (group == null)
                                    _state = state;
                                else
                                {
                                    if (group.State == GroupState.Enumerating) // but not Accepting
                                    {
                                        _ccsEmitting.Reset(false);
                                        group.State = GroupState.Emitting;
                                        _state = state;
                                        await _ccsEmitting.Awaiter;
                                        state = Atomic.Lock(ref _state);
                                    }

                                    if (group.State == GroupState.Accepting)
                                    {
                                        group.State = GroupState.Enumerating;
                                        _state = state;
                                        group.Current = current;
                                        group.CcsAccepting.SetCompleted(null, true);
                                    }
                                    else
                                        _state = state;
                                }

                                _cts.Token.ThrowIfCancellationRequested();
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }

                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    // finalize
                    {
                        var state = Atomic.Lock(ref _state);
                        if (error != null && (!(error is OperationCanceledException oce) || oce.CancellationToken != _cts.Token))
                            _error = error;
                        _state = _sFinal;
                        if (state == _sAccepting || state == _sCancelingAccepting)
                        {
                            Current = null;
                            _ccsAccepting.SetCompleted(_error, false);
                        }
                        _atmbDisposed.SetResult();

                        foreach (var group in _groups.Values)
                        {
                            if (group.State == GroupState.Final) continue;
                            group.Error = error;
                            var s = group.State;
                            group.State = GroupState.Final;
                            group.Ctr.Dispose();
                            if (s != GroupState.Accepting) continue;
                            group.Current = default;
                            group.CcsAccepting.SetCompleted(error, false);
                        }

                        _groups.Clear();
                    }
                }

                private enum GroupState : byte
                {
                    Initial, // GetAsyncEnumerator not called
                    TooLate, // GetAsyncEnumerator not called, will throw
                    Accepting, // pending MoveNextAsync
                    Enumerating, // enumeration started, not accepting
                    Emitting, // same as Enumerating, but Produce() awaits _ccsEmitting
                    Final // final state with or without error
                }

                private sealed class Group : IGroupedAsyncEnumerable<TKey, TSource>, IAsyncEnumerator<TSource>
                {
                    private readonly Enumerator _enumerator;
                    public TKey Key { get; }

                    public CoCompletionSource<bool> CcsAccepting = CoCompletionSource<bool>.Init();
                    public CancellationTokenRegistration Ctr;
                    public GroupState State;
                    public Exception Error;

                    public TSource Current { get; set; }

                    public Group(Enumerator enumerator, TKey key)
                    {
                        _enumerator = enumerator;
                        Key = key;
                    }

                    IAsyncEnumerator<TSource> IAsyncEnumerable<TSource>.GetAsyncEnumerator(CancellationToken token)
                    {
                        var state = Atomic.Lock(ref _enumerator._state);
                        switch (State)
                        {
                            case GroupState.Initial:
                                try
                                {
                                    _enumerator._groups.Add(Key, this);
                                    State = GroupState.Enumerating;
                                }
                                finally { _enumerator._state = state; }
                                if (token.CanBeCanceled) Ctr = token.Register(() => Cancel(new OperationCanceledException(token)));
                                return this;
                            case GroupState.TooLate:
                                _enumerator._state = state;
                                throw new InvalidOperationException("Group must be enumerated immediately.");
                            default:
                                _enumerator._state = state;
                                throw new InvalidOperationException("Group can be enumerated at most once.");
                        }
                    }

                    ICoAwaiter<bool> IAsyncEnumerator<TSource>.MoveNextAsync(bool continueOnCapturedContext)
                    {
                        CcsAccepting.Reset(continueOnCapturedContext);

                        var state = Atomic.Lock(ref _enumerator._state);
                        switch (State)
                        {
                            case GroupState.Initial:
                            case GroupState.TooLate:
                                // Pulling on the group rather than its enumerator? Invalid.
                                _enumerator._state = state;
                                CcsAccepting.SetCompleted(null, default);
                                throw new InvalidOperationException();

                            case GroupState.Enumerating:
                                State = GroupState.Accepting;
                                _enumerator._state = state;
                                break;

                            case GroupState.Emitting:
                                State = GroupState.Accepting;
                                _enumerator._state = state;
                                _enumerator._ccsEmitting.SetCompleted(null);
                                break;

                            case GroupState.Final:
                                _enumerator._state = state;
                                Current = default;
                                CcsAccepting.SetCompleted(Error, false);
                                break;

                            default: // Accepting???
                                _enumerator._state = state;
                                throw new Exception(State + "???");
                        }

                        return CcsAccepting.Awaiter;
                    }

                    Task IAsyncEnumerator<TSource>.DisposeAsync()
                    {
                        Cancel(ErrorHandler.EnumeratorDisposedException);
                        return Task.CompletedTask;
                    }

                    private void Cancel(Exception error)
                    {
                        var state = Atomic.Lock(ref _enumerator._state);
                        switch (State)
                        {
                            case GroupState.Initial:
                            case GroupState.TooLate:
                                // disposed the group rather than its enumerator? Ignore.
                                _enumerator._state = state;
                                break;

                            case GroupState.Enumerating:
                            case GroupState.Emitting:
                            case GroupState.Accepting:
                                if (state == _sFinal) // let Produce() do the finalization
                                {
                                    _enumerator._state = state;
                                    return;
                                }

                                Error = error;
                                var s = State;
                                State = GroupState.Final;
                                if (_enumerator._enumerable._whileObserved)
                                    try { _enumerator._groups.Remove(Key); }
                                    catch { /**/ }
                                var cancel = --_enumerator._nGroups == 0 && (state == _sCanceling || state == _sCancelingAccepting);
                                _enumerator._state = state;
                                Ctr.Dispose();
                                if (cancel)
                                    try { _enumerator._cts.Cancel(); }
                                    catch { /**/ }
                                switch (s)
                                {
                                    case GroupState.Emitting:
                                        _enumerator._ccsEmitting.SetCompleted(null);
                                        break;
                                    case GroupState.Accepting:
                                        Current = default;
                                        CcsAccepting.SetCompleted(Error, false);
                                        break;
                                }
                                break;

                            case GroupState.Final:
                                _enumerator._state = state;
                                break;

                            default:
                                _enumerator._state = state;
                                throw new Exception(State + "???");
                        }
                    }
                }
            }
        }
    }
}
