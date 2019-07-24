namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Convert a <see cref="IEnumerable{T}"/> to a <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Async<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Generate<T>(async (yield, token) =>
            {
                foreach (var element in source)
                    if (!await yield(element).ConfigureAwait(false))
                        return;
            });
        }

        /// <summary>
        /// Convert a <see cref="IObservable{T}"/> to a <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <remarks>
        /// Subscribed to on the thread pool on first <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>.
        /// Blocks on <see cref="IObserver{T}.OnNext"/> and <see cref="IObserver{T}.OnCompleted"/> if the enumerator is not pulled.
        ///
        /// <see cref="IAsyncDisposable.DisposeAsync"/> and cancellation occur immediately with best effort disposal
        /// of the subscription.
        /// </remarks>
        public static IAsyncEnumerable<T> Async<T>(this IObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new ObservableAsyncEnumerable<T>(source);
        }

        private sealed class ObservableAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IObservable<T> _source;

            public ObservableAsyncEnumerable(IObservable<T> source) => _source = source;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(_source, token);

            private sealed class Enumerator : IAsyncEnumerator<T>, IObserver<T>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;

                private readonly object _gate = new object();
                private readonly IObservable<T> _source;
                private CancellationTokenRegistration _ctr;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private int _state;
                private Exception _error;
                private IDisposable _subscription;

                public Enumerator(IObservable<T> source, CancellationToken token)
                {
                    _source = source;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public T Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsMoveNext.Reset();

                    int oldState;
                    lock (_gate)
                    {
                        oldState = _state;
                        switch (oldState)
                        {
                            case _sInitial:
                                _state = _sAccepting;
                                break;

                            case _sEmitting:
                                _state = _sAccepting;
                                Monitor.Pulse(_gate);
                                break;

                            case _sCompleted:
                                break;

                            default: // accepting???
                                throw new Exception(oldState + "???");
                        }
                    }

                    switch (oldState)
                    {
                        case _sInitial:
                            Task.Run(() => Subscribe());
                            break;

                        case _sEmitting:
                            break;

                        case _sCompleted:
                            _tsMoveNext.SetExceptionOrResult(_error, false);
                            break;

                        default: // accepting???
                            throw new Exception(oldState + "???");
                    }

                    return _tsMoveNext.Task;
                }

                public ValueTask DisposeAsync()
                {
                    OnError(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(Task.CompletedTask);
                }

                public void OnNext(T value)
                {
                    lock (_gate)
                    {
                        while (_state == _sEmitting)
                            Monitor.Wait(_gate);

                        if (_state == _sCompleted)
                            return;

                        Debug.Assert(_state == _sAccepting);
                        Current = value;
                        _state = _sEmitting;
                    }

                    _tsMoveNext.SetResult(true);
                }

                public void OnCompleted()
                {
                    lock (_gate)
                    {
                        while (_state == _sEmitting)
                            Monitor.Wait(_gate);

                        if (_state == _sCompleted)
                            return;

                        Debug.Assert(_state == _sAccepting);
                        _state = _sCompleted;
                    }

                    _ctr.Dispose();
                    _subscription?.Dispose();
                    _tsMoveNext.SetResult(false);
                }

                public void OnError(Exception error)
                {
                    if (error == null) throw new ArgumentNullException(nameof(error));

                    bool wasAccepting;
                    lock (_gate)
                    {
                        if (_state == _sCompleted)
                            return;

                        wasAccepting = _state == _sAccepting;
                        _error = error;
                        _state = _sCompleted;
                        Monitor.PulseAll(_gate);
                    }

                    _ctr.Dispose();
                    _subscription?.Dispose();
                    if (wasAccepting) _tsMoveNext.SetException(error);
                }

                private void Subscribe()
                {
                    try
                    {
                        var subscription = _source.Subscribe(this);
                        lock (_gate)
                            if (_state != _sCompleted)
                            {
                                _subscription = subscription;
                                return;
                            }
                        subscription.Dispose();
                    }
                    catch (Exception ex) { OnError(ex); }
                }
            }
        }
    }
}
