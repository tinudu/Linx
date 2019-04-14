namespace Linx.Coroutines
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Represents the producer side of a <see cref="ICoAwaiter"/>.
    /// </summary>
    [DebuggerNonUserCode]
    public struct CoAwaiterCompleter
    {
        /// <summary>
        /// Initialize with a new <see cref="ICoAwaiter"/>.
        /// </summary>
        public static CoAwaiterCompleter Init() => new CoAwaiterCompleter(default);

        private readonly CoAwaiter _awaiter;

        /// <summary>
        /// The awaiter controlled by this instance.
        /// </summary>
        public ICoAwaiter Awaiter => _awaiter;

        // ReSharper disable once UnusedParameter.Local
        private CoAwaiterCompleter(Unit _) => _awaiter = new CoAwaiter();

        /// <summary>
        /// Reset the awaiter.
        /// </summary>
        public void Reset(bool continueOnCapturedContext) => _awaiter.Reset(continueOnCapturedContext);

        /// <summary>
        /// Complete with the specified <paramref name="exception"/> (if not null), or normally otherwise.
        /// </summary>
        public void SetCompleted(Exception exception) => _awaiter.SetCompleted(exception);

        [DebuggerNonUserCode]
        private sealed class CoAwaiter : ICoAwaiter
        {
            private const int _sInitial = 0;
            private const int _sPending = 1;
            private const int _sCompleted = 2;

            private int _state;
            private SynchronizationContext _capturedContext;
            private Action _continuation;
            private Exception _exception;

            #region CoAwaiterCompleter implementation

            public void Reset(bool continueOnCapturedContext)
            {
                // capture the synchronization context
                SynchronizationContext cc;
                if (continueOnCapturedContext)
                {
                    cc = SynchronizationContext.Current;
                    if (cc != null && cc.GetType() == typeof(SynchronizationContext)) // not derived, treat as no context
                        cc = null;
                }
                else
                    cc = null;

                // Initial -> Pending with captured context
                if (Atomic.TestAndSet(ref _state, _sInitial, _sPending | Atomic.LockBit) != _sInitial) throw new InvalidOperationException();
                _capturedContext = cc;
                _state = _sPending;
            }

            public void SetCompleted(Exception exception)
            {
                // Pending -> Completed
                if (Atomic.TestAndSet(ref _state, _sPending, _sCompleted | Atomic.LockBit) != _sPending) throw new InvalidOperationException();

                // set exception
                _exception = exception;

                if (_continuation == null)
                {
                    _state = _sCompleted;
                    return;
                }

                var continuation = _continuation;
                _continuation = null;
                var cc = _capturedContext;
                _state = _sCompleted;

                RunOrScheduleContinuation(continuation, cc);
            }

            #endregion

            #region ICoAwaiter implementation

            public bool IsCompleted => _state == _sCompleted;

            public void OnCompleted(Action continuation)
            {
                if (continuation == null) throw new ArgumentNullException(nameof(continuation));

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sPending:
                        if (_continuation != null)
                        {
                            _state = _sPending;
                            throw new InvalidOperationException("Continuation already registered.");
                        }

                        _continuation = continuation;
                        _state = _sPending;
                        return;
                    case _sCompleted:
                        var cc = _capturedContext;
                        _state = _sCompleted;
                        RunOrScheduleContinuation(continuation, cc);
                        return;
                    default: // _sInitial
                        _state = state;
                        throw new InvalidOperationException();
                }
            }

            public void GetResult()
            {
                while (true)
                {
                    ManualResetEventSlim mres; // block here if pending

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sCompleted: // set Initial and return or throw
                            if (_exception == null)
                            {
                                _state = _sInitial;
                                return;
                            }

                            var exception = _exception;
                            _exception = null;
                            _state = _sInitial;
                            throw exception;

                        case _sPending:
                            try
                            {
                                if (_continuation != null) throw new InvalidOperationException("Continuation already registered.");

                                mres = new ManualResetEventSlim(false);
                                _continuation = mres.Set;
                                _capturedContext = null;
                            }
                            finally { _state = _sPending; }
                            break;
                        default: // Initial
                            _state = state;
                            throw new InvalidOperationException();
                    }

                    mres.Wait();
                }
            }

            ICoAwaiter ICoAwaiter.GetAwaiter() => this;

            #endregion

            #region continuation handling

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void RunOrScheduleContinuation(Action continuation, SynchronizationContext sc)
            {
                if (sc != null && SynchronizationContext.Current != sc)
                    sc.Post(_ => continuation(), null);
                else
                    try { continuation(); }
                    catch { /* not our problem */ }
            }

            #endregion
        }
    }
}
