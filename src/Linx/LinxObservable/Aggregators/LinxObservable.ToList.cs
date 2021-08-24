using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.LinxObservable
{
    partial class LinxObservable
    {
        /// <summary>
        /// Aggregate elements into a list.
        /// </summary>
        public static async Task<List<T>> ToList<T>(this ILinxObservable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var aggregator = new ToListAggregator<T>(token);
            try { source.Subscribe(aggregator); }
            catch (Exception error) { aggregator.OnError(error); }
            return await aggregator.Aggregate.ConfigureAwait(false);
        }

        private sealed class ToListAggregator<T> : ILinxObserver<T>
        {
            private const int _sInitial = 0;
            private const int _sError = 1;
            private const int _sFinal = 2;

            private readonly CancellationTokenSource _cts = new();
            private readonly CancellationTokenRegistration _ctr;
            private int _state;
            private List<T> _result = new();
            private Exception _error;
            private readonly AsyncTaskMethodBuilder<List<T>> _atmb;

            public ToListAggregator(CancellationToken token)
            {
                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetError(new OperationCanceledException(token)));
            }

            public CancellationToken Token => _cts.Token;

            public Task<List<T>> Aggregate => _atmb.Task;

            public void OnNext(T item)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        var result = _result;
                        _state = _sInitial;
                        try { result.Add(item); }
                        catch (Exception error) { SetError(error); }
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            public void OnError(Exception error)
            {
                if (error == null) throw new ArgumentNullException(nameof(error));

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _result = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmb.SetException(error);
                        _cts.Cancel();
                        break;

                    case _sError:
                        error = Linx.Clear(ref _error);
                        _state = _sFinal;
                        _atmb.SetException(error);
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            public void OnCompleted()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        var result = Linx.Clear(ref _result);
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmb.SetResult(result);
                        _cts.Cancel();
                        break;

                    case _sError:
                        var error = Linx.Clear(ref _error);
                        _state = _sFinal;
                        _atmb.SetException(error);
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            private void SetError(Exception error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _result = default;
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        _cts.Cancel();
                        break;

                    default:
                        _state = state;
                        break;
                }
            }
        }
    }
}
