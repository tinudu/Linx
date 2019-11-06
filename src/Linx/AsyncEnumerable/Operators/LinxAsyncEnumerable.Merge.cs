namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Observable;
    using Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (maxConcurrent <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrent));

            return maxConcurrent == 1 ?
                sources.Concat() :
                new MergeObservable<T>(sources, maxConcurrent).Buffer();
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (maxConcurrent <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrent));

            return maxConcurrent == 1 ?
                sources.Concat() :
                new MergeObservable<T>(sources.Async(), maxConcurrent).Buffer();
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            return new MergeObservable<T>(new[] { first, second }.Async(), int.MaxValue).Buffer();
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, params IAsyncEnumerable<T>[] sources)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            return new MergeObservable<T>(sources.Prepend(second).Prepend(first).Async(), int.MaxValue).Buffer();
        }

        private sealed class MergeObservable<T> : ILinxObservable<T>
        {
            private readonly IAsyncEnumerable<IAsyncEnumerable<T>> _sources;
            private readonly int _maxConcurrent;

            public MergeObservable(IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent)
            {
                Debug.Assert(sources != null && maxConcurrent > 0);
                _sources = sources;
                _maxConcurrent = maxConcurrent;
            }

            public void Subscribe(ILinxObserver<T> observer)
            {
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                try
                {
                    observer.Token.ThrowIfCancellationRequested();
                    new Sink(observer, _maxConcurrent).ProduceOuter(_sources);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            }

            private sealed class Sink
            {
                private const int _sInitial = 0;
                private const int _sCanceling = 1;
                private const int _sFinal = 2;

                private readonly ILinxObserver<T> _observer;
                private readonly int _maxConcurrent;
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private ManualResetValueTaskSource<bool> _tsMaxConcurrent;
                private CancellationTokenRegistration _ctr;
                private int _state;
                private int _active = 1;
                private Exception _error;

                public Sink(ILinxObserver<T> observer, int maxConcurrent)
                {
                    _observer = observer;
                    _maxConcurrent = maxConcurrent;
                    var token = observer.Token;
                    if (token.CanBeCanceled) _ctr = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                private void Cancel(Exception errorOpt)
                {
                    var state = Atomic.Lock(ref _state);
                    if (state == _sInitial)
                    {
                        var tsBlocked = _tsMaxConcurrent;
                        _error = errorOpt;
                        _state = _sCanceling;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        tsBlocked?.SetResult(false);
                    }
                    else
                        _state = state;
                }

                private void Complete()
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0 && state != _sFinal);

                    if (--_active == 0) // go Final
                    {
                        if (state == _sInitial)
                        {
                            var tsBlocked = Linx.Clear(ref _tsMaxConcurrent);
                            _state = _sFinal;
                            _ctr.Dispose();
                            _cts.TryCancel();
                            tsBlocked?.SetResult(false);
                        }
                        else // canceling
                            _state = _sFinal;

                        // Buffer() is synchronized, no locking required
                        if (_error == null)
                            _observer.OnCompleted();
                        else
                            _observer.OnError(_error);
                    }
                    else if (state == _sInitial)
                    {
                        var tsBlocked = Linx.Clear(ref _tsMaxConcurrent);
                        _state = _sInitial;
                        tsBlocked?.SetResult(true);
                    }
                    else
                        _state = state;
                }

                public async void ProduceOuter(IAsyncEnumerable<IAsyncEnumerable<T>> sources)
                {
                    try
                    {
                        var tsMaxConcurrent = new ManualResetValueTaskSource<bool>();

                        // ReSharper disable once PossibleMultipleEnumeration
                        await foreach (var sequence in sources.WithCancellation(_cts.Token).ConfigureAwait(false))
                            while (true)
                            {
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sInitial when (_active <= _maxConcurrent):
                                        _active++;
                                        _state = _sInitial;
                                        ProduceInner(sequence);
                                        break;

                                    case _sInitial:
                                        tsMaxConcurrent.Reset();
                                        _tsMaxConcurrent = tsMaxConcurrent;
                                        _state = _sInitial;
                                        if (!await tsMaxConcurrent.Task.ConfigureAwait(false))
                                            return;
                                        continue;

                                    default: // Canceling
                                        _state = state;
                                        return;
                                }
                                break;
                            }
                    }
                    catch (Exception ex) { Cancel(ex); }
                    finally { Complete(); }
                }

                private async void ProduceInner(IAsyncEnumerable<T> sequence)
                {
                    try
                    {
                        await foreach (var item in sequence.WithCancellation(_cts.Token).ConfigureAwait(false))
                        {
                            if ((_state & ~Atomic.LockBit) != _sInitial)
                                return;

                            if (_observer.OnNext(item)) // Buffer() is synchronized, no locking required
                                continue;

                            Cancel(null);
                            return;
                        }
                    }
                    catch (Exception ex) { Cancel(ex); }
                    finally { Complete(); }
                }
            }
        }
    }
}
