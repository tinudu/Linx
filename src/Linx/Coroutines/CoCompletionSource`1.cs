namespace Linx.Coroutines
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Represents the producer side of a <see cref="ICoAwaiter{T}"/>.
    /// </summary>
    [DebuggerNonUserCode]
    public struct CoCompletionSource<T>
    {
        /// <summary>
        /// Initialize with a new <see cref="ICoAwaiter{T}"/>.
        /// </summary>
        public static CoCompletionSource<T> Init() => new CoCompletionSource<T>(default);

        private readonly CoAwaiter _awaiter;

        /// <summary>
        /// The awaiter controlled by this instance.
        /// </summary>
        public ICoAwaiter<T> Awaiter => _awaiter;

        // ReSharper disable once UnusedParameter.Local
        private CoCompletionSource(Unit _) => _awaiter = new CoAwaiter();

        /// <summary>
        /// Reset the awaiter.
        /// </summary>
        public void Reset(bool continueOnCapturedContext) => _awaiter.Reset(continueOnCapturedContext);

        /// <summary>
        /// Complete with the specified <paramref name="exception"/> (if not null), or set the specified <paramref name="result"/>.
        /// </summary>
        public void SetCompleted(Exception exception, T result) => _awaiter.SetCompleted(exception, result);

        [DebuggerNonUserCode]
        private sealed class CoAwaiter : ICoAwaiter<T>
        {
            private const int _sInitial = 0;
            private const int _sPending = 1;
            private const int _sCompleted = 2;

            private int _state;
            private SynchronizationContext _capturedContext;
            private Action _continuation;
            private T _result;
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

            public void SetCompleted(Exception exception, T result)
            {
                // Pending -> Completed
                if (Atomic.TestAndSet(ref _state, _sPending, _sCompleted | Atomic.LockBit) != _sPending) throw new InvalidOperationException();

                // set exception or result
                if (exception != null) _exception = exception;
                else _result = result;

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

            public T GetResult()
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
                                var result = _result;
                                _result = default;
                                _state = _sInitial;
                                return result;
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

            void ICoAwaiter.GetResult() => GetResult();

            ICoAwaiter<T> ICoAwaiter<T>.GetAwaiter() => this;
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
