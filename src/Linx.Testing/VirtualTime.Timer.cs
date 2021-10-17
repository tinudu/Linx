using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Linx.Tasks;
using Linx.Timing;

namespace Linx.Testing
{
    partial class VirtualTime
    {
        private sealed class Timer : ITimer
        {
            private const int _tInitial = 0;
            private const int _tWaiting = 1;
            private const int _tFinal = 2;

            private static readonly ObjectDisposedException _timerDisposedException = new(nameof(ITimer));

            private readonly ManualResetValueTaskSource _tsDelay = new();
            private readonly VirtualTime _time;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private Exception _error;

            public Timer(VirtualTime time, CancellationToken token)
            {
                _time = time;
                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetException(new OperationCanceledException()));
            }

            public ValueTask Delay(TimeSpan due) => Delay(_time.Now + due);

            public ValueTask Delay(DateTimeOffset due)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _tInitial:
                        _tsDelay.Reset();
                        _state = _tWaiting;
                        _time.Enqueue(this, due);
                        return _tsDelay.Task;

                    case _tFinal:
                        _state = _tFinal;
                        _tsDelay.Reset();
                        _tsDelay.SetException(_error);
                        return _tsDelay.Task;

                    case _tWaiting:
                        throw new InvalidOperationException("Not reentrant.");

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public void Dispose() => SetException(_timerDisposedException);

            public void SetResult()
            {
                if (Atomic.CompareExchange(ref _state, _tInitial, _tWaiting) == _tWaiting)
                    _tsDelay.SetResult();
            }

            public void SetException(Exception error)
            {
                Debug.Assert(error is not null);

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _tInitial:
                        _error = error;
                        _state = _tFinal;
                        _tsDelay.Reset();
                        _ctr.Dispose();
                        _tsDelay.SetException(error);
                        break;

                    case _tWaiting:
                        _error = error;
                        _state = _tFinal;
                        _ctr.Dispose();
                        _tsDelay.SetException(error);
                        break;

                    case _tFinal:
                        _state = _tFinal;
                        break;

                    default:
                        _state = state;
                        throw new Exception(_state + "???");
                }
            }
        }
    }
}
