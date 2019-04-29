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
        public static IAsyncEnumerableObs<IGroupedAsyncEnumerable<TKey, TSource>> GroupBy<TSource, TKey>(
            this IAsyncEnumerableObs<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
            => new GroupByEnumerable<TSource, TKey>(source, keySelector, keyComparer, false);

        /// <summary>
        /// Group by a key; close a group when unsubscribed.
        /// </summary>
        public static IAsyncEnumerableObs<IGroupedAsyncEnumerable<TKey, TSource>> GroupByWhileObserved<TSource, TKey>(
            this IAsyncEnumerableObs<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
            => new GroupByEnumerable<TSource, TKey>(source, keySelector, keyComparer, true);

        private sealed class GroupByEnumerable<TSource, TKey> : IAsyncEnumerableObs<IGroupedAsyncEnumerable<TKey, TSource>>
        {
            private readonly IAsyncEnumerableObs<TSource> _source;
            private readonly Func<TSource, TKey> _keySelector;
            private readonly IEqualityComparer<TKey> _keyComparer;
            private readonly bool _whileObserved;

            public GroupByEnumerable(
                IAsyncEnumerableObs<TSource> source,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey> keyComparer,
                bool whileObserved)
            {
                _source = source ?? throw new ArgumentNullException(nameof(source));
                _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
                _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
                _whileObserved = whileObserved;
            }

            IAsyncEnumeratorObs<IGroupedAsyncEnumerable<TKey, TSource>> IAsyncEnumerableObs<IGroupedAsyncEnumerable<TKey, TSource>>.GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumeratorObs<IGroupedAsyncEnumerable<TKey, TSource>>
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

                ICoAwaiter<bool> IAsyncEnumeratorObs<IGroupedAsyncEnumerable<TKey, TSource>>.MoveNextAsync(bool continueOnCapturedContext)
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
                            _ccsEmitting.SetResult();
                            break;

                        case _sCanceling:
                            _state = _sCancelingAccepting;
                            break;

                        case _sFinal:
                            _state = _sFinal;
                            if (_error == null) _ccsAccepting.SetResult(false);
                            else _ccsAccepting.SetException(_error);
                            break;

                        default: // Accepting, CancelingAccepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _ccsAccepting.Task;
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
                            _ccsEmitting.SetResult();
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
                                    _ccsAccepting.SetResult(true);
                                    await _ccsEmitting.Task;

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
                                        await _ccsEmitting.Task;
                                        state = Atomic.Lock(ref _state);
                                    }

                                    if (group.State == GroupState.Accepting)
                                    {
                                        group.State = GroupState.Enumerating;
                                        _state = state;
                                        group.Current = current;
                                        group.CcsAccepting.SetResult(true);
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
                            if (_error == null) _ccsAccepting.SetResult(false);
                            else _ccsAccepting.SetException(_error);
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
                            if (error == null) group.CcsAccepting.SetResult(false);
                            else group.CcsAccepting.SetException(error);
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

                private sealed class Group : IGroupedAsyncEnumerable<TKey, TSource>, IAsyncEnumeratorObs<TSource>
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

                    IAsyncEnumeratorObs<TSource> IAsyncEnumerableObs<TSource>.GetAsyncEnumerator(CancellationToken token)
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

                    ICoAwaiter<bool> IAsyncEnumeratorObs<TSource>.MoveNextAsync(bool continueOnCapturedContext)
                    {
                        CcsAccepting.Reset(continueOnCapturedContext);

                        var state = Atomic.Lock(ref _enumerator._state);
                        switch (State)
                        {
                            case GroupState.Initial:
                            case GroupState.TooLate:
                                // Pulling on the group rather than its enumerator? Invalid.
                                _enumerator._state = state;
                                CcsAccepting.SetResult(default);
                                throw new InvalidOperationException();

                            case GroupState.Enumerating:
                                State = GroupState.Accepting;
                                _enumerator._state = state;
                                break;

                            case GroupState.Emitting:
                                State = GroupState.Accepting;
                                _enumerator._state = state;
                                _enumerator._ccsEmitting.SetResult();
                                break;

                            case GroupState.Final:
                                _enumerator._state = state;
                                Current = default;
                                if (Error == null) CcsAccepting.SetResult(false);
                                else CcsAccepting.SetException(Error);
                                break;

                            default: // Accepting???
                                _enumerator._state = state;
                                throw new Exception(State + "???");
                        }

                        return CcsAccepting.Task;
                    }

                    Task IAsyncEnumeratorObs<TSource>.DisposeAsync()
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
                                        _enumerator._ccsEmitting.SetResult();
                                        break;
                                    case GroupState.Accepting:
                                        Current = default;
                                        if (Error == null) CcsAccepting.SetResult(false);
                                        else CcsAccepting.SetException(Error);
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
