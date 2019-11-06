namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Group by a key.
        /// </summary>
        public static IAsyncEnumerable<IAsyncGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            return Create(token => new GroupByEnumerator<TSource, TKey>(source, keySelector, keyComparer, false, token));
        }

        /// <summary>
        /// Group by a key, close groups once they are no longer enumerated.
        /// </summary>
        public static IAsyncEnumerable<IAsyncGrouping<TKey, TSource>> GroupByWhileEnumerated<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            return Create(token => new GroupByEnumerator<TSource, TKey>(source, keySelector, keyComparer, true, token));
        }

        private sealed class GroupByEnumerator<TSource, TKey> : IAsyncEnumerator<IAsyncGrouping<TKey, TSource>>
        {
            private const int _sGroup = 0; // GetAsyncEnumerator not called
            private const int _sInitial = 1; // GetAsyncEnumerator called
            private const int _sAccepting = 2; // MoveNextAsync called
            private const int _sEmitting = 3; // MoveNextAsync acknowleded
            private const int _sDisposed = 4; // Disposed

            private readonly IAsyncEnumerable<TSource> _source;
            private readonly Func<TSource, TKey> _keySelector;
            private readonly Dictionary<Boxed<TKey>, Group> _groups;
            private bool _whileEnumerated;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder(); // Task for last consumer
            private int _active;

            public GroupByEnumerator(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey> keyComparer,
                bool whileEnumerated,
                CancellationToken token)
            {
                Debug.Assert(source != null);
                Debug.Assert(keySelector != null);

                _source = source;
                _keySelector = keySelector;
                _whileEnumerated = whileEnumerated;
                _groups = new Dictionary<Boxed<TKey>, Group>(Boxed.GetEqualityComparer(keyComparer));

                if (token.CanBeCanceled) _ctr = token.Register(() => Dispose(new OperationCanceledException(token)));
            }

            #region outer enumerator context

            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private readonly ManualResetValueTaskSource _tsEmitting = new ManualResetValueTaskSource();
            private CancellationTokenRegistration _ctr;
            private int _state = _sInitial;
            private Exception _error;
            private Task _tDisposed;

            public IAsyncGrouping<TKey, TSource> Current { get; private set; }

            ValueTask<bool> IAsyncEnumerator<IAsyncGrouping<TKey, TSource>>.MoveNextAsync()
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
                        _state = _sAccepting;
                        _tsEmitting.SetResult();
                        break;

                    case _sDisposed:
                        _state = _sDisposed;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default: // Accepting???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsAccepting.Task;
            }

            ValueTask IAsyncDisposable.DisposeAsync()
            {
                Dispose(AsyncEnumeratorDisposedException.Instance);
                return new ValueTask(_tDisposed);
            }

            private void Dispose(Exception errorOpt)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                    case _sEmitting:
                        break;

                    case _sAccepting:
                        Current = default;
                        break;

                    case _sDisposed:
                        _state = _sDisposed;
                        return;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }

                _error = errorOpt;
                if (_active > 0 && --_active == 0)
                {
                    _tDisposed = _atmbDisposed.Task;
                    _state = _sDisposed;
                    _cts.TryCancel();
                }
                else
                {
                    _tDisposed = Task.CompletedTask;
                    _state = _sDisposed;
                }
                _ctr.Dispose();
                switch (state)
                {
                    case _sAccepting:
                        _tsAccepting.SetExceptionOrResult(errorOpt, false);
                        break;
                    case _sEmitting:
                        _tsEmitting.SetResult();
                        break;
                }
            }

            #endregion

            private async void Produce()
            {
                Exception error = null;
                try
                {
                    await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var key = new Boxed<TKey>(_keySelector(item));

                        Group group;
                        {
                            var state = Atomic.Lock(ref _state);
                            Debug.Assert(state == _sAccepting || state == _sDisposed);

                            if (_active == 0)
                            {
                                Debug.Assert(state == _sDisposed);
                                _state = _sDisposed;
                                return;
                            }

                            try
                            {
                                if (_groups.TryGetValue(key, out group))
                                    _state = state;
                                else if (state == _sAccepting) // create and emit a new group
                                {
                                    group = new Group(this, key);
                                    _groups.Add(key, group);
                                    _active++;
                                    _tsEmitting.Reset();
                                    Current = group;
                                    _state = state = _sEmitting;
                                    _tsAccepting.SetResult(true);
                                    await _tsEmitting.Task.ConfigureAwait(false);
                                }
                                else // Disposed, ignore item
                                {
                                    _state = _sDisposed;
                                    continue;
                                }
                            }
                            catch
                            {
                                _state = state;
                                throw;
                            }
                        }

                        {
                            var groupState = Atomic.Lock(ref group.State);
                            switch (groupState)
                            {
                                case _sInitial:
                                case _sGroup:
                                    group.State = groupState;
                                    group.Dispose(AlreadyConnectedException.Instance);
                                    break;

                                case _sAccepting:
                                    group.TsEmitting.Reset();
                                    group.Current = item;
                                    group.State = _sEmitting;
                                    group.TsAccepting.SetResult(true);
                                    await group.TsEmitting.Task.ConfigureAwait(false);
                                    break;

                                case _sDisposed:
                                    group.State = _sDisposed;
                                    break;

                                default: // Emitting???
                                    group.State = groupState;
                                    throw new Exception(group.State + "???");
                            }
                        }

                        _cts.Token.ThrowIfCancellationRequested();
                    }
                }
                catch (Exception ex) { error = ex; }
                finally
                {
                    _atmbDisposed.SetResult();
                    Dispose(error);
                    _whileEnumerated = false; // prevent groups from modifying the dictionary
                    foreach (var group in _groups.Values)
                        group.Dispose(error);
                    _groups.Clear();
                }
            }

            private sealed class Group : IAsyncGrouping<TKey, TSource>, IAsyncEnumerator<TSource>
            {
                private readonly GroupByEnumerator<TSource, TKey> _enumerator;
                private readonly Boxed<TKey> _key;

                public readonly ManualResetValueTaskSource<bool> TsAccepting = new ManualResetValueTaskSource<bool>();
                public readonly ManualResetValueTaskSource TsEmitting = new ManualResetValueTaskSource();
                public int State;
                private CancellationTokenRegistration _ctr;
                private Exception _error;
                private Task _tDisposed;

                TKey IAsyncGrouping<TKey, TSource>.Key => _key.Value;
                public TSource Current { get; set; }

                public Group(GroupByEnumerator<TSource, TKey> enumerator, Boxed<TKey> key)
                {
                    _enumerator = enumerator;
                    _key = key;
                }

                IAsyncEnumerator<TSource> IAsyncEnumerable<TSource>.GetAsyncEnumerator(CancellationToken token)
                {
                    var state = Atomic.Lock(ref State);
                    switch (state)
                    {
                        case _sGroup:
                            if (token.CanBeCanceled)
                                _ctr = token.Register(() => Dispose(new OperationCanceledException(token)));
                            State = _sInitial;
                            return this;

                        case _sDisposed:
                            State = _sDisposed;
                            return this;

                        default:
                            State = state;
                            throw new NotSupportedException($"Multiple enumerators on {nameof(IAsyncGrouping<TKey, TSource>)}.");
                    }
                }

                ValueTask<bool> IAsyncEnumerator<TSource>.MoveNextAsync()
                {
                    TsAccepting.Reset();
                    var state = Atomic.Lock(ref State);
                    switch (state)
                    {
                        case _sGroup:
                            TsAccepting.SetResult(false);
                            State = _sGroup;
                            throw new InvalidOperationException();

                        case _sInitial:
                            State = _sAccepting;
                            break;

                        case _sEmitting:
                            State = _sAccepting;
                            TsEmitting.SetResult();
                            break;

                        case _sDisposed:
                            Current = default;
                            State = _sDisposed;
                            TsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // Accepting???
                            _enumerator._state = state;
                            throw new Exception(State + "???");
                    }
                    return TsAccepting.Task;
                }

                ValueTask IAsyncDisposable.DisposeAsync()
                {
                    Dispose(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_tDisposed);
                }

                public void Dispose(Exception errorOpt)
                {
                    var state = Atomic.Lock(ref State);
                    switch (state)
                    {
                        case _sGroup:
                        case _sInitial:
                        case _sEmitting:
                            break;

                        case _sAccepting:
                            Current = default;
                            break;

                        case _sDisposed:
                            State = _sDisposed;
                            return;

                        default:
                            State = state;
                            throw new Exception(state + "???");
                    }

                    var eState = Atomic.Lock(ref _enumerator._state);
                    Debug.Assert(_enumerator._active > 0);
                    var last = --_enumerator._active == 0;
                    _tDisposed = last ? _enumerator._tDisposed : Task.CompletedTask;
                    if (_enumerator._whileEnumerated)
                        try { _enumerator._groups.Remove(_key); }
                        catch { /* key comparer buggy? */ }
                    _enumerator._state = eState;

                    _error = errorOpt;
                    State = _sDisposed;
                    _ctr.Dispose();
                    if (last) _enumerator._cts.TryCancel();

                    switch (state)
                    {
                        case _sAccepting:
                            TsAccepting.SetExceptionOrResult(errorOpt, false);
                            break;
                        case _sEmitting:
                            TsEmitting.SetResult();
                            break;
                    }
                }
            }
        }
    }
}
