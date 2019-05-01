namespace Linx.AsyncEnumerable.TaskProviders
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Sources;

    /// <summary>
    /// Provides manually resettable <see cref="ValueTask"/>.
    /// </summary>
    [DebuggerNonUserCode]
    public sealed class ManualResetTaskProvider : IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<Unit> _core = new ManualResetValueTaskSourceCore<Unit>();

        /// <summary>Resets to prepare for the next operation.</summary>
        public void Reset() => _core.Reset();

        /// <summary>Completes successfully.</summary>
        public void SetResult() => _core.SetResult(default);

        /// <summary>Completes with an error.</summary>
        public void SetException(Exception exception) => _core.SetException(exception);

        /// <summary>Completes with an error or successfully.</summary>
        public void SetExceptionOrResult(Exception exception)
        {
            if (exception != null)
                _core.SetException(exception);
            else
                _core.SetResult(default);
        }

        /// <summary>
        /// Gets a <see cref="ValueTask"/>.
        /// </summary>
        public ValueTask Task => new ValueTask(this, _core.Version);

        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);
        void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
    }
}