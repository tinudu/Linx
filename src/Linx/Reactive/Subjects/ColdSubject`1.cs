namespace Linx.Reactive.Subjects
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    /// <summary>
    /// A <see cref="ISubject{T}"/> that disallowes late subscribers.
    /// </summary>
    public sealed class ColdSubject<T> : ISubject<T>
    {
        private readonly List<Enumerator> _enumerators;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private int _state; // 0: initial, 1: subscribed
        private int _activeEnumerators;

        /// <inheritdoc />
        public ColdSubject()
        {
            _enumerators = new List<Enumerator>();
            Output = new AnonymousAsyncEnumerable<T>(t => new Enumerator(this, t));
        }

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="capacity">Expected number of subscribers.</param>
        public ColdSubject(int capacity)
        {
            _enumerators = new List<Enumerator>(capacity);
            Output = new AnonymousAsyncEnumerable<T>(t => new Enumerator(this, t));
        }

        /// <inheritdoc />
        public IAsyncEnumerable<T> Output { get; }

        /// <inheritdoc />
        public async Task SubscribeTo(IAsyncEnumerable<T> input)
        {
            if (Atomic.Set(ref _state, 1 | Atomic.LockBit) != 0)
            {
                _state = 1;
                throw new InvalidOperationException("Already subscribed.");
            }

            if (_enumerators.Count == 0)
            {
                _state = 1;
                return;
            }

            Debug.Assert(_enumerators.TrueForAll(e => e.State == EnumeratorState.Pulling));
            _activeEnumerators = _enumerators.Count;
            _state = 1;
            Exception error;
            try
            {
                var ae = input.GetAsyncEnumerator(_cts.Token);
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;

                        // set pulling enumerators pushing
                        for (var ix = _enumerators.Count - 1; ix >= 0; ix--)
                        {
                            var e = _enumerators[ix];
                            Atomic.Lock(ref _state);
                            if (e.State == EnumeratorState.Pulling)
                            {
                                e.CcsPushing.Reset(false);
                                e.Current = current;
                                e.State = EnumeratorState.Pushing;
                                _state = 1;
                                e.CcsPulling.SetCompleted(null, true);
                            }
                            else // final
                            {
                                _enumerators.RemoveAt(ix); // no exception assumed
                                _state = 1;
                            }
                        }
                        if (_enumerators.Count == 0) return;

                        // await all pushed
                        for (var ix = _enumerators.Count - 1; ix >= 0; ix--)
                        {
                            var e = _enumerators[ix];
                            await e.CcsPushing.Awaiter;
                            if (e.State != EnumeratorState.Pulling)
                                _enumerators.RemoveAt(ix); // no exception assumed
                        }
                        if (_enumerators.Count == 0) return;
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
                error = null;
            }
            catch (Exception ex) { error = ex; }

            // set pulling enumerators final
            Atomic.Lock(ref _state);
            for (var ix = _enumerators.Count - 1; ix >= 0; ix--)
            {
                var e = _enumerators[ix];
                if (e.State == EnumeratorState.Pulling)
                {
                    e.Error = error;
                    e.Current = default;
                    e.State = EnumeratorState.Final;
                }
                else
                    _enumerators.RemoveAt(ix); // no exception assumed
            }
            _state = 1;

            if (_enumerators.Count == 0) return;

            // complete pulling
            foreach (var e in _enumerators)
            {
                e.Ctr.Dispose();
                e.CcsPulling.SetCompleted(error, false);
            }
            _enumerators.Clear();
            try { _cts.Cancel(); } catch {  /**/ }
        }

        private enum EnumeratorState { Initial, Pulling, Pushing, Final }

        private sealed class Enumerator : IAsyncEnumerator<T>
        {
            private readonly ColdSubject<T> _subject;

            public CancellationTokenRegistration Ctr;
            public EnumeratorState State;
            public CoCompletionSource<bool> CcsPulling = CoCompletionSource<bool>.Init();
            public CoCompletionSource CcsPushing = CoCompletionSource.Init();
            public T Current { get; set; }
            public Exception Error;

            public Enumerator(ColdSubject<T> subject, CancellationToken token)
            {
                _subject = subject;
                if (token.CanBeCanceled) Ctr = token.Register(() => Cancel(new OperationCanceledException(token)));
            }

            ICoAwaiter<bool> IAsyncEnumerator<T>.MoveNextAsync(bool continueOnCapturedContext)
            {
                CcsPulling.Reset(continueOnCapturedContext);

                var subjState = Atomic.Lock(ref _subject._state);
                switch (State)
                {
                    case EnumeratorState.Initial:
                        try
                        {
                            if (subjState != 0) throw new InvalidOperationException("Subject already subscribed.");
                            _subject._enumerators.Insert(0, this);
                            State = EnumeratorState.Pulling;
                            _subject._state = 0;
                        }
                        catch (Exception ex)
                        {
                            Error = ex;
                            State = EnumeratorState.Final;
                            _subject._state = subjState;
                            Ctr.Dispose();
                            CcsPulling.SetCompleted(ex, false);
                        }
                        break;

                    case EnumeratorState.Pushing:
                        Debug.Assert(subjState == 1);
                        State = EnumeratorState.Pulling;
                        _subject._state = 1;
                        CcsPushing.SetCompleted(null);
                        break;

                    case EnumeratorState.Final:
                        _subject._state = subjState;
                        CcsPulling.SetCompleted(Error, false);
                        break;

                    case EnumeratorState.Pulling:
                        _subject._state = subjState;
                        throw new Exception(State + "???");

                    default:
                        _subject._state = subjState;
                        throw new Exception(State + "???");
                }

                return CcsPulling.Awaiter;
            }

            Task IAsyncEnumerator<T>.DisposeAsync()
            {
                Cancel(ErrorHandler.EnumeratorDisposedException);
                return Task.CompletedTask;
            }

            private void Cancel(Exception error)
            {
                var subjState = Atomic.Lock(ref _subject._state);
                switch (State)
                {
                    case EnumeratorState.Initial:
                        Error = error;
                        State = EnumeratorState.Final;
                        _subject._state = subjState;
                        Ctr.Dispose();
                        break;

                    case EnumeratorState.Pulling:
                        Error = error;
                        Current = default;
                        State = EnumeratorState.Final;
                        if (subjState == 0)
                        {
                            _subject._enumerators.Remove(this); // no exception assumed
                            _subject._state = 0;
                        }
                        else
                        {
                            Debug.Assert(_subject._activeEnumerators > 0);
                            var cancel = --_subject._activeEnumerators == 0;
                            _subject._state = 1;
                            if (cancel)
                                try { _subject._cts.Cancel(); }
                                catch { /**/ }
                        }
                        Ctr.Dispose();
                        CcsPulling.SetCompleted(error, false);
                        break;

                    case EnumeratorState.Pushing:
                        Error = error;
                        State = EnumeratorState.Final;
                        {
                            Debug.Assert(subjState == 1 && _subject._activeEnumerators > 0);
                            var cancel = --_subject._activeEnumerators == 0;
                            _subject._state = 1;
                            if (cancel)
                                try { _subject._cts.Cancel(); }
                                catch { /**/ }
                        }
                        Ctr.Dispose();
                        CcsPushing.SetCompleted(null);
                        break;

                    case EnumeratorState.Final:
                        _subject._state = subjState;
                        break;
                }
            }
        }
    }
}
