namespace Linx.Observable
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxObservable
    {
        /// <summary>
        /// Determines whether a sequence contains any elements.
        /// </summary>
        public static Task<bool> Any<T>(this ILinxObservable<T> source, CancellationToken token)
        {
            try
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                token.ThrowIfCancellationRequested();

                var aggregator = new AnyAggregator<T>(token);
                source.Subscribe(aggregator);
                return aggregator.Task;
            }
            catch (Exception ex) { return Task.FromException<bool>(ex); }
        }

        /// <summary>
        /// Determines whether any element of a sequence satisfies a condition.
        /// </summary>
        public static Task<bool> Any<T>(this ILinxObservable<T> source, Func<T, bool> predicate, CancellationToken token)
        {
            try { return source.Where(predicate).Any(token); }
            catch (Exception ex) { return Task.FromException<bool>(ex); }
        }

        private sealed class AnyAggregator<T> : ILinxObserver<T>
        {
            private const int _sFalse = 0;
            private const int _sTrue = 1;
            private const int _sCompleted = 2;

            private AsyncTaskMethodBuilder<bool> _atmb = new AsyncTaskMethodBuilder<bool>();
            private int _state;

            public AnyAggregator(CancellationToken token) => Token = token;

            public Task<bool> Task => _atmb.Task;

            public CancellationToken Token { get; }

            public bool OnNext(T value)
            {
                Token.ThrowIfCancellationRequested();

                var s = Atomic.Lock(ref _state);
                switch (s)
                {
                    case _sFalse:
                        _state = _sTrue;
                        return true;
                    default:
                        _state = s;
                        return false;
                }
            }

            public void OnError(Exception error)
            {
                if (error == null) throw new ArgumentNullException(nameof(error));

                var s = Atomic.Lock(ref _state);
                if (s != _sCompleted)
                {
                    _state = _sCompleted;
                    _atmb.SetException(error);
                }
                else
                    _state = _sCompleted;
            }

            public void OnCompleted()
            {
                var s = Atomic.Lock(ref _state);
                if (s != _sCompleted)
                {
                    _state = _sCompleted;
                    _atmb.SetResult(s != _sFalse);
                }
                else
                    _state = _sCompleted;
            }
        }
    }
}
