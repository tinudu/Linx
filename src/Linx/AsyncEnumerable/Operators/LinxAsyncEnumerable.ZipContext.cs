namespace Linx.AsyncEnumerable
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

    partial class LinxAsyncEnumerable
    {
        private sealed class ZipContext
        {
            private readonly CancellationTokenSource _cts;
            private readonly Func<ConfiguredValueTaskAwaitable<bool>>[] _moveNexts;
            private readonly ManualResetValueTaskSource<bool> _ts = new();
            private bool _completed;
            private Exception? _error;
            private int _active;

            public ZipContext(CancellationTokenSource cts, params Func<ConfiguredValueTaskAwaitable<bool>>[] moveNexts)
            {
                _cts = cts;
                _moveNexts = moveNexts;
            }

            public void SetError(Exception error)
            {
                var a = Atomic.Lock(ref _active);
                if (_completed)
                {
                    _active = a;
                    return;
                }

                _completed = true;
                _error = error;
                _active = a;
                _cts.TryCancel();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                _ts.Reset();
                if (_completed)
                    _ts.SetExceptionOrResult(_error, false);
                else
                {
                    _active = _moveNexts.Length;
                    foreach (var mn in _moveNexts)
                        MoveNextAsync(mn);
                }

                return _ts.Task;
            }

            private async void MoveNextAsync(Func<ConfiguredValueTaskAwaitable<bool>> moveNext)
            {
                bool completed;
                Exception? error;
                if (_completed)
                {
                    completed = false;
                    error = null;
                }
                else
                    try
                    {
                        completed = !await moveNext();
                        error = null;
                    }
                    catch (Exception ex)
                    {
                        completed = true;
                        error = ex;
                    }

                var a = Atomic.Lock(ref _active) - 1;
                if (completed)
                {
                    if (_completed) // someone was faster, ignore result
                        completed = false; // prevent cancellation below
                    else
                    {
                        _completed = true;
                        _error = error;
                    }
                }
                _active = a;

                if (completed)
                    _cts.TryCancel();
                if (a == 0)
                    _ts.SetExceptionOrResult(_error, !_completed);
            }
        }
    }
}
