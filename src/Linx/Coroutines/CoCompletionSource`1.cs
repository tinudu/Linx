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
        public static CoCompletionSource<T> Init() => new CoCompletionSource<T>(new CoAwaiter());

        private readonly CoAwaiter _awaiter;
        private CoCompletionSource(CoAwaiter awaiter) => _awaiter = awaiter;

        /// <summary>
        /// Gets the awaiter controlled by this instance.
        /// </summary>
        public ICoAwaiter<T> Awaiter => _awaiter;

        /// <summary>
        /// Reset the awaiter.
        /// </summary>
        /// <exception cref="InvalidOperationException">Previous use of the awaiter in progress.</exception>
        public void Reset(bool continueOnCapturedContext) => _awaiter.Reset(continueOnCapturedContext);

        /// <summary>
        /// Complete with the specified <paramref name="exception"/> (if not null), or set the specified <paramref name="result"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Call not preceded by <see cref="Reset"/>.</exception>
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

            #region CoCompletionSource implementation

            public void Reset(bool continueOnCapturedContext)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sInitial)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

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

                _capturedContext = cc;
                _state = _sPending;
            }

            public void SetCompleted(Exception exception, T result)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sPending)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

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
                            throw new InvalidOperationException();
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
                var state = Atomic.Lock(ref _state);
                if (state == _sPending)
                {
                    if (_continuation != null)
                    {
                        _state = _sPending;
                        throw new InvalidOperationException();
                    }

                    var mres = new ManualResetEventSlim();
                    _continuation = mres.Set;
                    _capturedContext = null;
                    _state = _sPending;
                    mres.Wait();
                    state = Atomic.Lock(ref _state);
                }

                if (state != _sCompleted)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

                var exception = _exception;
                _exception = null;
                var result = _result;
                _result = default;
                _capturedContext = null;
                Debug.Assert(_continuation == null);
                _state = _sInitial;
                if (exception == null) return result;
                throw exception;
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
