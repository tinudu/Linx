namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Start a task for every item and emit its result.
        /// </summary>
        /// <param name="source">The source sequence.</param>
        /// <param name="selector">A delegate to create a task.</param>
        /// <param name="preserveOrder">true to emit result items in the order of their source items, false to emit them as soon as available.</param>
        /// <param name="maxConcurrent">Maximum number of concurrent tasks.</param>
        public static IAsyncEnumerable<TResult> Parallel<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, Task<TResult>> selector,
            bool preserveOrder = false,
            int maxConcurrent = int.MaxValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (maxConcurrent <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrent));

            return maxConcurrent == 1 ?
                source.Select(selector) :
                new ParallelEnumerable<TSource, TResult>(source, selector, preserveOrder, maxConcurrent);
        }

        /// <summary>
        /// Start a task for every item and emit its result.
        /// </summary>
        /// <param name="source">The source sequence.</param>
        /// <param name="selector">A delegate to create a task.</param>
        /// <param name="preserveOrder">true to emit result items in the order of their source items, false to emit them as soon as available.</param>
        /// <param name="maxConcurrent">Maximum number of concurrent tasks.</param>
        public static IAsyncEnumerable<TResult> Parallel<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, CancellationToken, Task<TResult>> selector,
            bool preserveOrder = false,
            int maxConcurrent = int.MaxValue)
            => source.Async().Parallel(selector, preserveOrder, maxConcurrent);

        private sealed class ParallelEnumerable<TSource, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<TSource> _source;
            private readonly Func<TSource, CancellationToken, Task<TResult>> _selector;
            private readonly bool _preserveOrder;
            private readonly int _maxConcurrent;

            public ParallelEnumerable(IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, Task<TResult>> selector,
                bool preserveOrder,
                int maxConcurrent)
            {
                Debug.Assert(source != null);
                Debug.Assert(maxConcurrent > 1);

                _source = source;
                _selector = selector;
                _preserveOrder = preserveOrder;
                _maxConcurrent = maxConcurrent;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            public override string ToString() => "Parallel";

            private sealed class Enumerator : IAsyncEnumerator<TResult>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sError = 3;
                private const int _sFinal = 4;

                private readonly ParallelEnumerable<TSource, TResult> _enumerable;
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly Queue<TResult> _queue = new Queue<TResult>();
                private CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state, _active;
                private ManualResetValueTaskSource<bool> _tsMaxConcurrent;
                private bool _incrementActive;
                private Exception _error;

                public Enumerator(ParallelEnumerable<TSource, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _active = 1;
                            _state = _sAccepting;
                            Produce();
                            break;

                        case _sEmitting:
                            if (_queue.Count > 0)
                            {
                                Current = _queue.Dequeue(); // no exception assumed
                                if (_active == 0 && _queue.Count == 0) // emitting the last result
                                {
                                    _state = _sFinal;
                                    _ctr.Dispose();
                                    _atmbDisposed.SetResult();
                                }
                                else
                                    _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                            }
                            else if (_tsMaxConcurrent != null && _active <= _enumerable._maxConcurrent)
                            {
                                var ts = Linx.Clear(ref _tsMaxConcurrent);
                                if (_incrementActive) _active++;
                                _state = _sAccepting;
                                ts.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sError:
                        case _sFinal:
                            Current = default;
                            _state = state;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // Accepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _tsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    OnError(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_atmbDisposed.Task);
                }

                private void OnNext(TResult value)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Current = value;
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                            break;

                        case _sEmitting:
                            try { _queue.Enqueue(value); }
                            finally { _state = _sEmitting; }
                            break;

                        default:
                            _state = state;
                            break;
                    }
                }

                private void OnError(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            Debug.Assert(_error == null && _active > 0 && _queue.Count == 0);
                            Current = default;
                            _error = error;
                            _state = _sError;
                            _ctr.Dispose();
                            Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetException(error);
                            break;

                        case _sEmitting:
                            Debug.Assert(_error == null && _active > 0);
                            _error = error;
                            _state = _sError;
                            _ctr.Dispose();
                            Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                            _queue.Clear();
                            _cts.TryCancel();
                            break;

                        default:
                            _state = state;
                            break;
                    }
                }

                private void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0);

                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert(_error == null && _queue.Count == 0);
                            if (--_active == 0)
                            {
                                Current = default;
                                _state = _sFinal;
                                _ctr.Dispose();
                                Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                                _cts.TryCancel();
                                _atmbDisposed.SetResult();
                                _tsAccepting.SetResult(false);
                            }
                            else if (_tsMaxConcurrent != null)
                            {
                                var ts = Linx.Clear(ref _tsMaxConcurrent);
                                if (_incrementActive) _active++;
                                _state = _sAccepting;
                                ts.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sEmitting:
                            if (--_active == 0)
                            {
                                if (_queue.Count == 0)
                                {
                                    _state = _sFinal;
                                    _ctr.Dispose();
                                    Linx.Clear(ref _tsMaxConcurrent)?.SetResult(false);
                                    _atmbDisposed.SetResult();
                                }
                                else
                                    _state = _sEmitting;

                                _cts.TryCancel();
                            }
                            else
                                _state = _sEmitting;
                            break;

                        case _sError:
                            if (--_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sError;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce()
                {
                    try
                    {
                        Action<TSource> startTask;
                        if (_enumerable._preserveOrder)
                        {
                            async Task StartCore(TSource item, Task pred)
                            {
                                try
                                {
                                    _cts.Token.ThrowIfCancellationRequested();
                                    var result = await _enumerable._selector(item, _cts.Token).ConfigureAwait(false);
                                    await pred.ConfigureAwait(false);
                                    OnNext(result);
                                }
                                catch (Exception ex) { OnError(ex); }
                                finally { OnCompleted(); }
                            }

                            var predecessor = Task.CompletedTask;
                            startTask = item => predecessor = StartCore(item, predecessor);
                        }
                        else
                            startTask = async item =>
                            {
                                try
                                {
                                    _cts.Token.ThrowIfCancellationRequested();
                                    var result = await _enumerable._selector(item, _cts.Token).ConfigureAwait(false);
                                    OnNext(result);
                                }
                                catch (Exception ex) { OnError(ex); }
                                finally { OnCompleted(); }
                            };

                        var tsMaxConcurrent = new ManualResetValueTaskSource<bool>();
                        ValueTask<bool> Wait(bool increment)
                        {
                            tsMaxConcurrent.Reset();
                            var state = Atomic.Lock(ref _state);
                            switch (state)
                            {
                                case _sAccepting when (_active <= _enumerable._maxConcurrent):
                                    if (increment) _active++;
                                    _state = _sAccepting;
                                    tsMaxConcurrent.SetResult(true);
                                    break;

                                case _sAccepting:
                                case _sEmitting:
                                    _tsMaxConcurrent = tsMaxConcurrent;
                                    _incrementActive = increment;
                                    _state = state;
                                    break;

                                case _sError:
                                    _state = _sError;
                                    tsMaxConcurrent.SetResult(false);
                                    break;

                                default:
                                    _state = state;
                                    throw new Exception(state + "???");
                            }
                            return tsMaxConcurrent.Task;
                        }

                        await foreach (var item in _enumerable._source.WithCancellation(_cts.Token).ConfigureAwait(false))
                        {
                            if (!await Wait(true).ConfigureAwait(false)) return;
                            startTask(item);
                            if (!await Wait(false).ConfigureAwait(false)) return;
                        }
                    }
                    catch (Exception ex) { OnError(ex); }
                    finally { OnCompleted(); }
                }
            }
        }
    }
}
