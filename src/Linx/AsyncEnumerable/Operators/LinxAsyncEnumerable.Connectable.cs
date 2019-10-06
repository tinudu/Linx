namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Makes <paramref name="source"/> connectable.
        /// </summary>
        /// <param name="source">The source sequence.</param>
        /// <param name="connect">Set to a <see cref="ConnectDelegate"/> that, when invoked, connects to the <paramref name="source"/>.</param>
        /// <remarks>
        /// When <paramref name="connect"/> is called, all enumerators awaiting <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> will observe the same sequence.
        /// Late enumerators will experience a <see cref="AlreadyConnectedException"/>.
        /// </remarks>
        public static IAsyncEnumerable<T> Connectable<T>(this IAsyncEnumerable<T> source, out ConnectDelegate connect)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var connectable = new ConnectableEnumerable<T>(source);
            connect = connectable.Connect;
            return connectable;
        }

        /// <summary>
        /// Makes <paramref name="source"/> connectable, pushing the <see cref="ConnectDelegate"/> to the stack.
        /// </summary>
        public static IAsyncEnumerable<T> Connectable<T>(this IAsyncEnumerable<T> source, Stack<ConnectDelegate> stack)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (stack == null) throw new ArgumentNullException(nameof(stack));

            var connectable = new ConnectableEnumerable<T>(source);
            stack.Push(connectable.Connect);
            return connectable;
        }

        /// <summary>
        /// Invoke the specified <paramref name="stack"/> in LIFO order.
        /// </summary>
        public static void Connect(this Stack<ConnectDelegate> stack)
        {
            while (stack.Count > 0)
                stack.Pop()();
        }

        private sealed class ConnectableEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<T> _source;
            private readonly ManualResetEventSlim _gate = new ManualResetEventSlim(true);
            private readonly List<Enumerator> _enumerators = new List<Enumerator>();
            private readonly CancellationTokenSource _tcs = new CancellationTokenSource(); // as canceled as the last enumerator
            private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
            private int _active; // number of active enumerators (Accepting or Emitting)
            private bool _isConnected;

            public ConnectableEnumerable(IAsyncEnumerable<T> source) => _source = source;

            public async void Connect()
            {
                _gate.Wait();
                try
                {
                    if (_isConnected) return;
                    _isConnected = true;
                    _active = _enumerators.Count;
                    if (_active == 0) return;
                    Debug.Assert(_enumerators.All(e => e.State == EnumeratorState.Accepting));
                }
                finally { _gate.Set(); }

                Exception error = null;
                try
                {
                    var ae = _source.WithCancellation(_tcs.Token).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        while (await ae.MoveNextAsync())
                        {
                            var current = ae.Current;
                            int i;

                            _gate.Wait();
                            try
                            {
                                // pass 1: set accepting enumerators emitting, remove final enumerators
                                i = 0;
                                while (i < _enumerators.Count)
                                {
                                    var e = _enumerators[i];
                                    if (e.State == EnumeratorState.Accepting)
                                    {
                                        e.TsEmitting.Reset();
                                        e.State = EnumeratorState.Emitting;
                                        e.Current = current;
                                        i++;
                                    }
                                    else
                                    {
                                        Debug.Assert(e.State == EnumeratorState.Final);
                                        _enumerators.RemoveAt(i);
                                    }
                                }

                                Debug.Assert(_active == _enumerators.Count);
                            }
                            finally { _gate.Set(); }

                            if (_enumerators.Count == 0) return;

                            // pass 2: complete MoveNextAsync
                            foreach (var e in _enumerators)
                                e.TsAccepting.SetResult(true);

                            // pass 3: await emit completed, remove finals
                            i = 0;
                            while (i < _enumerators.Count)
                            {
                                var e = _enumerators[i];
                                if (await e.TsEmitting.Task.ConfigureAwait(false))
                                    i++;
                                else
                                    _enumerators.RemoveAt(i);
                            }

                            if (_enumerators.Count == 0) return;
                        }
                    }
                    finally
                    {
                        await ae.DisposeAsync();
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    _atmbDisposed.SetResult();
                    foreach (var e in _enumerators)
                        e.Complete(error);
                }
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private enum EnumeratorState : byte
            {
                Initial,
                Accepting,
                Emitting,
                Final
            }

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private readonly ConnectableEnumerable<T> _e;
                public readonly ManualResetValueTaskSource<bool> TsAccepting = new ManualResetValueTaskSource<bool>();
                public readonly ManualResetValueTaskSource<bool> TsEmitting = new ManualResetValueTaskSource<bool>();
                public EnumeratorState State;
                private CancellationTokenRegistration _ctr;
                private Exception _error;
                private Task _tDisposed;

                public Enumerator(ConnectableEnumerable<T> enumerable, CancellationToken token)
                {
                    _e = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => Complete(new OperationCanceledException(token)));
                }

                public T Current { get; set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    TsAccepting.Reset();

                    _e._gate.Wait();
                    switch (State)
                    {
                        case EnumeratorState.Initial:
                            if (_e._isConnected)
                            {
                                _error = AlreadyConnectedException.Instance;
                                _tDisposed = Task.CompletedTask;
                                State = EnumeratorState.Final;
                                _e._gate.Set();
                                TsAccepting.SetException(_error);
                            }
                            else
                            {
                                State = EnumeratorState.Accepting;
                                try
                                {
                                    _e._enumerators.Add(this);
                                    _e._gate.Set();
                                }
                                catch (Exception ex)
                                {
                                    _e._gate.Set();
                                    Complete(ex); 
                                }
                            }
                            break;

                        case EnumeratorState.Emitting:
                            State = EnumeratorState.Accepting;
                            _e._gate.Set();
                            TsEmitting.SetResult(true);
                            break;

                        case EnumeratorState.Final:
                            Current = default;
                            _e._gate.Set();
                            TsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        // ReSharper disable once RedundantCaseLabel
                        case EnumeratorState.Accepting:
                        default:
                            _e._gate.Set();
                            throw new Exception(State + "???");
                    }
                    return TsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    Complete(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_tDisposed);
                }

                public void Complete(Exception errorOpt)
                {
                    _e._gate.Wait();
                    switch (State)
                    {
                        case EnumeratorState.Initial:
                            _error = errorOpt;
                            _tDisposed = Task.CompletedTask;
                            State = EnumeratorState.Final;
                            _e._gate.Set();
                            _ctr.Dispose();
                            break;

                        case EnumeratorState.Accepting:
                            _error = errorOpt;
                            if (_e._isConnected)
                            {
                                Debug.Assert(_e._active > 0);
                                _tDisposed = --_e._active == 0 ? _e._atmbDisposed.Task : Task.CompletedTask;
                            }
                            else
                            {
                                _tDisposed = Task.CompletedTask;
                                _e._enumerators.Remove(this); // no exception assumed
                            }

                            State = EnumeratorState.Final;
                            _e._gate.Set();
                            _ctr.Dispose();
                            if (_tDisposed != Task.CompletedTask)
                                try { _e._tcs.Cancel(); } catch { /**/ }
                            TsAccepting.SetExceptionOrResult(errorOpt, false);
                            break;

                        case EnumeratorState.Emitting:
                            Debug.Assert(_e._isConnected && _e._active > 0);
                            _error = errorOpt;
                            _tDisposed = --_e._active == 0 ? _e._atmbDisposed.Task : Task.CompletedTask;
                            State = EnumeratorState.Final;
                            _e._gate.Set();
                            _ctr.Dispose();
                            if (_tDisposed != Task.CompletedTask)
                                try { _e._tcs.Cancel(); } catch { /**/ }
                            TsEmitting.SetResult(false);
                            break;

                        case EnumeratorState.Final:
                            _e._gate.Set();
                            break;

                        default:
                            _e._gate.Set();
                            throw new Exception(State + "???");
                    }
                }
            }
        }
    }
}
