﻿namespace Linx.Coroutines
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Collections;

    /// <summary>
    /// Represents the producer side of a <see cref="ICoAwaiter{T}"/>.
    /// </summary>
    [DebuggerNonUserCode]
    public struct PooledCoCompletionSource<T>
    {
        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="continueOnCapturedContext">Whether continuations should be scheduled on the current <see cref="SynchronizationContext"/>.</param>
        public static PooledCoCompletionSource<T> Init(bool continueOnCapturedContext) => new PooledCoCompletionSource<T>(CoAwaiter.Init(continueOnCapturedContext));

        private readonly CoAwaiter _awaiter;
        private PooledCoCompletionSource(CoAwaiter awaiter) => _awaiter = awaiter;

        /// <summary>
        /// Gets the <see cref="ICoAwaiter{T}"/> that is completed by this completion source.
        /// </summary>
        public ICoAwaiter<T> Awaiter => _awaiter;

        /// <summary>
        /// Set the <see cref="Awaiter"/> completed.
        /// </summary>
        /// <param name="exception">If not null, sets an exception.</param>
        /// <param name="result">The result, if <paramref name="exception"/> is null.</param>
        public void SetCompleted(Exception exception, T result) => _awaiter.SetCompleted(exception, result);

        [DebuggerNonUserCode]
        private sealed class CoAwaiter : ICoAwaiter<T>
        {
            private static readonly Pool<CoAwaiter> _pool = new Pool<CoAwaiter>();

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static CoAwaiter Init(bool continueOnCapturedContext)
            {
                if (_pool.TryGet(out var awaiter))
                    awaiter._state = _sPending;
                else
                    awaiter = new CoAwaiter();

                if (!continueOnCapturedContext) return awaiter;

                var cc = SynchronizationContext.Current;
                if (cc != null && cc.GetType() != typeof(SynchronizationContext))
                    awaiter._capturedContext = cc;
                return awaiter;
            }

            private const int _sPending = 0;
            private const int _sCompleted = 1;
            private const int _sFinal = 2;

            private int _state;
            private SynchronizationContext _capturedContext;
            private Action _continuation;
            private T _result;
            private Exception _exception;

            private CoAwaiter() { }

            public void SetCompleted(Exception exception, T result)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sPending)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

                if (exception != null) _exception = exception;
                else _result = result;

                var continuation = _continuation;
                _continuation = null;
                var cc = _capturedContext;
                _state = _sCompleted;

                if (continuation != null)
                    RunOrScheduleContinuation(continuation, cc);
            }

            #region ICoAwaiter implementation

            public bool IsCompleted => _state == _sCompleted;

            public void OnCompleted(Action continuation)
            {
                if (continuation == null) throw new ArgumentNullException(nameof(continuation));

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sPending:
                        if (_continuation == null)
                        {
                            _continuation = continuation;
                            _state = _sPending;
                        }
                        else
                        {
                            _state = _sPending;
                            throw new InvalidOperationException();
                        }
                        break;

                    case _sCompleted:
                        var cc = _capturedContext;
                        _state = _sCompleted;
                        RunOrScheduleContinuation(continuation, cc);
                        break;

                    default: // Final
                        _state = state;
                        throw new InvalidOperationException();
                }
            }

            public T GetResult()
            {
                var state = Atomic.Lock(ref _state);
                if (state == _sPending)
                {
                    if (_continuation == null)
                    {
                        _state = _sPending;
                        throw new InvalidOperationException();
                    }

                    var mres = new ManualResetEventSlim();
                    _capturedContext = null;
                    _continuation = mres.Set;
                    _state = _sPending;
                    mres.Wait();
                    state = Atomic.Lock(ref _state);
                }

                if (state != _sCompleted)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

                _capturedContext = null;
                var exception = _exception;
                _exception = null;
                var result = _result;
                _result = default;
                _state = _sFinal;
                _pool.Return(this);

                if (exception != null) throw exception;
                return result;
            }

            void ICoAwaiter.GetResult() => GetResult();

            ICoAwaiter<T> ICoAwaiter<T>.GetAwaiter() => this;
            ICoAwaiter ICoAwaiter.GetAwaiter() => this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void RunOrScheduleContinuation(Action continuation, SynchronizationContext sc)
            {
                if (sc != null && sc != SynchronizationContext.Current)
                    sc.Post(_ => continuation(), null);
                else
                    try { continuation(); }
                    catch { /* not our problem */ }
            }

            #endregion
        }
    }
}
