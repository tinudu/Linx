// ReSharper disable once CheckNamespace
namespace System.Threading.Tasks.Sources
{
    using Linx;

    /// <summary>
    /// This is similarly gonna be in Core 2.1 or 3.0.
    /// </summary>
    public sealed class ManualResetValueTaskSource : IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<Unit> _mrvtsc = new ManualResetValueTaskSourceCore<Unit>();

        /// <summary>
        /// Gets the <see cref="ValueTask"/> controlled by this instance.
        /// </summary>
        public ValueTask Task => new ValueTask(this, _mrvtsc.Version);

        /// <summary>Resets to prepare for the next operation.</summary>
        public void Reset() => _mrvtsc.Reset();

        /// <summary>Completes with a successfully.</summary>
        public void SetResult() => _mrvtsc.SetResult(default);

        /// <summary>Complets with an error.</summary>
        public void SetException(Exception error) => _mrvtsc.SetException(error);

        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _mrvtsc.GetStatus(token);
        void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _mrvtsc.OnCompleted(continuation, state, token, flags);
        void IValueTaskSource.GetResult(short token) => _mrvtsc.GetResult(token);
    }

    /// <summary>
    /// This is similarly gonna be in Core 2.1 or 3.0.
    /// </summary>
    public sealed class ManualResetValueTaskSource<TResult> : IValueTaskSource<TResult>
    {
        private ManualResetValueTaskSourceCore<TResult> _mrvtsc = new ManualResetValueTaskSourceCore<TResult>();

        /// <summary>
        /// Gets the <see cref="ValueTask{T}"/> controlled by this instance.
        /// </summary>
        public ValueTask<TResult> Task => new ValueTask<TResult>(this, _mrvtsc.Version);

        /// <summary>Resets to prepare for the next operation.</summary>
        public void Reset() => _mrvtsc.Reset();

        /// <summary>Completes with a successfully with a result.</summary>
        public void SetResult(TResult result) => _mrvtsc.SetResult(result);

        /// <summary>Complets with an error.</summary>
        public void SetException(Exception error) => _mrvtsc.SetException(error);

        ValueTaskSourceStatus IValueTaskSource<TResult>.GetStatus(short token) => _mrvtsc.GetStatus(token);
        void IValueTaskSource<TResult>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _mrvtsc.OnCompleted(continuation, state, token, flags);
        TResult IValueTaskSource<TResult>.GetResult(short token) => _mrvtsc.GetResult( token);
    }
}
