namespace Linx.AsyncEnumerable.TaskProviders
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Sources;

    /// <summary>
    /// Provides manually resettable <see cref="ConfiguredValueTaskAwaitable{T}"/>.
    /// </summary>
    public struct ManualResetConfiguredProvider<T>
    {
        private readonly ManualResetValueTaskSource<T> _source;
        private readonly bool _continueOnCapturedContext;

        /// <summary>
        /// Initialize.
        /// </summary>
        public ManualResetConfiguredProvider(bool continueOnCapturedContext)
        {
            _source = new ManualResetValueTaskSource<T>();
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>Resets to prepare for the next operation.</summary>
        public void Reset() => _source.Reset();

        /// <summary>Completes successfully.</summary>
        public void SetResult(T result) => _source.SetResult(result);

        /// <summary>Completes with an error.</summary>
        public void SetException(Exception exception) => _source.SetException(exception);

        /// <summary>Completes with an error or successfully.</summary>
        public void SetExceptionOrResult(Exception exception, T result)
        {
            if (exception != null)
                _source.SetException(exception);
            else
                _source.SetResult(result);
        }

        /// <summary>
        /// Gets a <see cref="ConfiguredValueTaskAwaitable{TResult}"/>.
        /// </summary>
        public ConfiguredValueTaskAwaitable<T> Awaitable => new ValueTask<T>(_source, _source.Version).ConfigureAwait(_continueOnCapturedContext);

        /// <summary>
        /// Gets a <see cref="ConfiguredValueTaskAwaitable"/>.
        /// </summary>
        public ConfiguredValueTaskAwaitable AwaitableNonGeneric => new ValueTask(_source, _source.Version).ConfigureAwait(_continueOnCapturedContext);
    }

    /// <summary>
    /// Provides manually resettable <see cref="ConfiguredValueTaskAwaitable"/>.
    /// </summary>
    public struct ManualResetConfiguredProvider
    {
        private readonly ManualResetValueTaskSource<Unit> _source;
        private readonly bool _continueOnCapturedContext;

        /// <summary>
        /// Initialize.
        /// </summary>
        public ManualResetConfiguredProvider(bool continueOnCapturedContext)
        {
            _source = new ManualResetValueTaskSource<Unit>();
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>Resets to prepare for the next operation.</summary>
        public void Reset() => _source.Reset();

        /// <summary>Completes successfully.</summary>
        public void SetResult() => _source.SetResult(default);

        /// <summary>Completes with an error.</summary>
        public void SetException(Exception exception) => _source.SetException(exception);

        /// <summary>Completes with an error or successfully.</summary>
        public void SetExceptionOrResult(Exception exception)
        {
            if (exception != null)
                _source.SetException(exception);
            else
                _source.SetResult(default);
        }

        /// <summary>
        /// Gets a <see cref="ConfiguredValueTaskAwaitable"/>.
        /// </summary>
        public ConfiguredValueTaskAwaitable Awaitable => new ValueTask(_source, _source.Version).ConfigureAwait(_continueOnCapturedContext);
    }
}