namespace Linx.Timing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Collections;
    using TaskSources;

    /// <summary>
    /// Virtuala time.
    /// </summary>
    public sealed class VirtualTime : ITime, IDisposable
    {
        private const int _sStopped = 0;
        private const int _sStarted = 1;
        private const int _sIdle = 2;
        private const int _sDisposed = 3;

        private static readonly ObjectDisposedException _virtualTimeDisposed = new ObjectDisposedException(nameof(VirtualTime));

        private readonly PriorityQueue<Bucket> _queue = new PriorityQueue<Bucket>();
        private readonly Dictionary<DateTime, Queue<Timer>> _timersByDue = new Dictionary<DateTime, Queue<Timer>>();
        private readonly Stack<Queue<Timer>> _pool = new Stack<Queue<Timer>>(); // recicle empty timer lists
        private readonly ManualResetValueTaskSource _tsIdle = new ManualResetValueTaskSource();
        private int _state;

        /// <summary>
        /// Crete with <see cref="Now"/> being <see cref="DateTimeOffset.MinValue"/>.
        /// </summary>
        public VirtualTime() : this(DateTimeOffset.MinValue)
        {
        }

        /// <summary>
        /// Crete with <see cref="Now"/> being <paramref name="now"/>.
        /// </summary>
        public VirtualTime(DateTimeOffset now)
        {
            if (Time.Current != RealTime.Instance) throw new InvalidOperationException("Current time must be real time before switching to virtual time.");

            Time.Current = this;
            Now = now;
        }

        /// <inheritdoc />
        public DateTimeOffset Now { get; private set; }

        /// <summary>
        /// Start time.
        /// </summary>
        public void Start()
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sStopped:
                    _state = _sStarted;
                    Task.Run(Advance);
                    break;

                case _sStarted:
                case _sIdle:
                    _state = state;
                    break;

                case _sDisposed:
                    _state = _sDisposed;
                    throw _virtualTimeDisposed;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sStopped:
                case _sStarted:
                    _state = _sDisposed;
                    break;

                case _sIdle:
                    _state = _sDisposed;
                    _tsIdle.SetResult();
                    break;

                case _sDisposed:
                    _state = _sDisposed;
                    return;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }

            foreach (var t in _timersByDue.Values.SelectMany(ts => ts))
                t.Complete(_virtualTimeDisposed);
            _timersByDue.Clear();
            _queue.Clear();
            _pool.Clear();
            Time.Current = RealTime.Instance;
        }

        /// <inheritdoc />
        public async ValueTask Delay(TimeSpan due, CancellationToken token)
        {
            using var timer = new Timer(this, token);
            await timer.Delay(due).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async ValueTask Delay(DateTimeOffset due, CancellationToken token)
        {
            using var timer = new Timer(this, token);
            await timer.Delay(due).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public ITimer GetTimer(CancellationToken token) => new Timer(this, token);

        private async void Advance()
        {
            while (true)
            {
                Timer timer;

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sStarted:
                        while (true)
                        {
                            if (_queue.Count == 0)
                            {
                                timer = null;
                                _tsIdle.Reset();
                                _state = _sIdle;
                                break;
                            }

                            var bucket = _queue.Peek();
                            if (bucket.Timers.Count > 0)
                            {
                                if (Now < bucket.DueUtc) Now = bucket.DueUtc;
                                timer = bucket.Timers.Dequeue();
                                _state = _sStarted;
                                break;
                            }

                            _queue.Dequeue();
                            _timersByDue.Remove(bucket.DueUtc);
                            _pool.Push(bucket.Timers);
                        }

                        break;

                    case _sDisposed:
                        _state = _sDisposed;
                        return;

                    default: // stopped, idle???
                        _state = state;
                        throw new Exception(state + "???");
                }

                if (timer == null)
                    await _tsIdle.Task.ConfigureAwait(false);
                else
                    timer.Complete(null);
            }
        }

        private struct Bucket : IComparable<Bucket>
        {
            public readonly DateTime DueUtc;
            public readonly Queue<Timer> Timers;

            public Bucket(DateTime dueUtc, Queue<Timer> timers)
            {
                DueUtc = dueUtc;
                Timers = timers;
            }

            public int CompareTo(Bucket other) => DueUtc.CompareTo(other.DueUtc);
        }

        private sealed class Timer : ITimer
        {
            private const int _tInitial = 0;
            private const int _tWaiting = 1;
            private const int _tCanceled = 2;
            private const int _tDisposed = 3;

            private readonly ManualResetValueTaskSource _ts = new ManualResetValueTaskSource();
            private readonly VirtualTime _time;
            private readonly CancellationToken _token;
            private CancellationTokenRegistration _ctr;
            private int _state;

            public Timer(VirtualTime time, CancellationToken token)
            {
                _time = time;
                _token = token;
                if (_token.CanBeCanceled) _ctr = token.Register(TokenCanceled);
            }

            public ValueTask Delay(TimeSpan due) => Delay(_time.Now + due);

            public ValueTask Delay(DateTimeOffset due)
            {
                _ts.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _tInitial:
                        var timeState = Atomic.Lock(ref _time._state);
                        switch (timeState)
                        {
                            case _sStopped:
                            case _sStarted:
                            case _sIdle:
                                try
                                {
                                    var dueUtc = due.UtcDateTime;
                                    if (!_time._timersByDue.TryGetValue(dueUtc, out var timers))
                                    {
                                        timers = _time._pool.Count > 0 ? _time._pool.Pop() : new Queue<Timer>();
                                        _time._queue.Enqueue(new Bucket(dueUtc, timers));
                                        _time._timersByDue.Add(dueUtc, timers);
                                    }

                                    timers.Enqueue(this);
                                    _state = _tWaiting;
                                    if (timeState == _sIdle)
                                    {
                                        _time._state = _sStarted;
                                        _time._tsIdle.SetResult();
                                    }
                                    else
                                        _time._state = timeState;
                                }
                                catch (Exception ex)
                                {
                                    _state = _tInitial;
                                    _time._state = timeState;
                                    _ts.SetException(ex);
                                }

                                break;

                            case _sDisposed:
                                _time._state = _sDisposed;
                                _state = _tInitial;
                                _ts.SetException(_virtualTimeDisposed);
                                break;

                            default:
                                _time._state = timeState;
                                _state = _tInitial;
                                throw new Exception(timeState + "???");
                        }

                        break;

                    case _tCanceled:
                        _state = _tCanceled;
                        _ts.SetException(new OperationCanceledException(_token));
                        break;

                    case _tDisposed:
                        _state = _tDisposed;
                        _ts.SetException(new ObjectDisposedException(nameof(ITimer)));
                        break;

                    default: // _tWaiting???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _ts.Task;
            }

            public void Dispose()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _tInitial:
                        _state = _tDisposed;
                        _ctr.Dispose();
                        break;
                    case _tWaiting:
                        _state = _tDisposed;
                        _ctr.Dispose();
                        _ts.SetException(new ObjectDisposedException(nameof(ITimer)));
                        break;
                    default: // Canceled, Disposed
                        _state = _tDisposed;
                        break;
                }
            }

            private void TokenCanceled()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _tInitial:
                        _state = _tCanceled;
                        _ctr.Dispose();
                        break;
                    case _tWaiting:
                        _state = _tCanceled;
                        _ctr.Dispose();
                        _ts.SetException(new OperationCanceledException(_token));
                        break;
                    default: // Canceled, Disposed
                        _state = state;
                        break;
                }
            }

            public void Complete(Exception exception)
            {
                if (Atomic.CompareExchange(ref _state, _tInitial, _tWaiting) != _tWaiting) return;
                _ts.SetExceptionOrResult(exception);
            }
        }
    }
}