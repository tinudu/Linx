namespace Linx.AsyncEnumerable.TaskProviders
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Sources;

    /// <summary>
    /// Provides manually resettable <see cref="ValueTask{T}"/>.
    /// </summary>
    public struct ManualResetProvider<T>
    {
        private readonly ManualResetValueTaskSource<T> _source;

        /// <summary>
        /// Initialize (use this instead of default constructor).
        /// </summary>
        // ReSharper disable once UnusedParameter.Local
        public ManualResetProvider(Unit _)
        {
            _source = new ManualResetValueTaskSource<T>();
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
        public ValueTask<T> Task => new ValueTask<T>(_source, _source.Version);

        /// <summary>
        /// Gets a <see cref="ConfiguredValueTaskAwaitable"/>.
        /// </summary>
        public ValueTask TaskNonGeneric => new ValueTask(_source, _source.Version);
    }

    /// <summary>
    /// Provides manually resettable <see cref="ValueTask"/>.
    /// </summary>
    public struct ManualResetProvider
    {
        private readonly ManualResetValueTaskSource<Unit> _source;

        /// <summary>
        /// Initialize (use this instead of default constructor).
        /// </summary>
        // ReSharper disable once UnusedParameter.Local
        public ManualResetProvider(Unit _)
        {
            _source = new ManualResetValueTaskSource<Unit>();
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
        public ValueTask Task => new ValueTask(_source, _source.Version);
    }
}