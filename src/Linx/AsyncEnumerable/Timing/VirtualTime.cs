namespace Linx.AsyncEnumerable.Timing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Collections;
    using TaskProviders;

    /// <summary>
    /// Virtuala time.
    /// </summary>
    public sealed class VirtualTime : ITime, IDisposable
    {
        private readonly PriorityQueue<Bucket> _queue = new PriorityQueue<Bucket>();
        private readonly Dictionary<DateTime, List<IElapse>> _timersByDue = new Dictionary<DateTime, List<IElapse>>();
        private readonly Stack<List<IElapse>> _pool = new Stack<List<IElapse>>(); // recicle empty timer lists
        private bool _isDisposed;

        /// <summary>
        /// Crete with <see cref="Now"/> being <see cref="DateTimeOffset.MinValue"/>.
        /// </summary>
        public VirtualTime() : this(DateTimeOffset.MinValue) { }

        /// <summary>
        /// Crete with <see cref="Now"/> being <paramref name="now"/>.
        /// </summary>
        public VirtualTime(DateTimeOffset now)
        {
            if (Time.Current != RealTime.Instance) throw new InvalidOperationException("Not real time.");

            Time.Current = this;
            Now = now;
            var advanceThread = new Thread(_ => Advance()) { Priority = ThreadPriority.Lowest };
            advanceThread.Start(null);
        }

        /// <inheritdoc />
        public DateTimeOffset Now { get; private set; }

        /// <inheritdoc />
        public Task Delay(TimeSpan delay, CancellationToken token) => Delay(Now + delay, token);

        /// <inheritdoc />
        public async Task Delay(DateTimeOffset due, CancellationToken token)
        {
            if (due <= Now) return;
            token.ThrowIfCancellationRequested();

            var dueUtc = due.UtcDateTime;
            var timer = new TaskTimer();
            if (token.CanBeCanceled) timer.Ctr = token.Register(() =>
            {
                Remove(dueUtc, timer);
                timer.Elapse(new OperationCanceledException(token));
            });
            Add(dueUtc, timer);
            await timer.Task;
        }

        /// <inheritdoc />
        public ITimer GetTimer(CancellationToken token) => new Timer(this, token);

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_queue)
            {
                if (_isDisposed) return;
                _isDisposed = true;
                Monitor.Pulse(_queue);
            }
            Time.Current = RealTime.Instance;

            var error = new ObjectDisposedException(nameof(VirtualTime));
            foreach (var t in _timersByDue.Values.SelectMany(ts => ts))
                t.Elapse(error);
            _timersByDue.Clear();
            _queue.Clear();
            _pool.Clear();
        }

        private void Add(DateTime dueUtc, IElapse timer)
        {
            lock (_queue)
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(VirtualTime));
                if (!_timersByDue.TryGetValue(dueUtc, out var timers))
                {
                    timers = _pool.Count > 0 ? _pool.Pop() : new List<IElapse>();
                    _timersByDue.Add(dueUtc, timers);
                    _queue.Enqueue(new Bucket(dueUtc, timers));
                }

                timers.Add(timer);
                Monitor.Pulse(_queue);
            }
        }

        private void Remove(DateTime dueUtc, IElapse timer)
        {
            lock (_queue)
            {
                if (!_isDisposed && _timersByDue.TryGetValue(dueUtc, out var timers))
                    timers.Remove(timer);
            }
        }

        private void Advance()
        {
            // loop to advance virtual time, running on a low prio thread
            while (true)
            {
                // yield to other threads so they can schedule
                Thread.Sleep(1);

                IElapse timer;
                lock (_queue)
                {
                    if (_isDisposed)
                        return;

                    while (true)
                    {
                        if (_queue.IsEmpty)
                        {
                            timer = null;
                            break;
                        }
                        var bucket = _queue.Peek();
                        if (bucket.Timers.Count > 0)
                        {
                            if (Now < bucket.DueUtc) Now = bucket.DueUtc;
                            timer = bucket.Timers[0];
                            bucket.Timers.RemoveAt(0);
                            break;
                        }
                        _queue.Dequeue();
                        _timersByDue.Remove(bucket.DueUtc);
                        _pool.Push(bucket.Timers);
                    }

                    if (timer == null)
                        Monitor.Wait(_queue);
                }
                timer?.Elapse(null);
            }
        }

        private interface IElapse
        {
            void Elapse(Exception exception);
        }

        private struct Bucket : IComparable<Bucket>
        {
            public readonly DateTime DueUtc;
            public readonly List<IElapse> Timers;

            public Bucket(DateTime dueUtc, List<IElapse> timers)
            {
                DueUtc = dueUtc;
                Timers = timers;
            }

            public int CompareTo(Bucket other) => DueUtc.CompareTo(other.DueUtc);
        }

        private sealed class TaskTimer : IElapse
        {
            private AsyncTaskMethodBuilder _atmb = new AsyncTaskMethodBuilder();
            private int _state;

            public CancellationTokenRegistration Ctr;
            public Task Task => _atmb.Task;

            public void Elapse(Exception exception)
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) != 0) return;
                Ctr.Dispose();
                if (exception == null) _atmb.SetResult();
                else _atmb.SetException(exception);
            }
        }

        private sealed class Timer : ITimer, IElapse
        {
            private const int _sInitial = 0;
            private const int _sWaiting = 1;
            private const int _sCanceled = 2;
            private const int _sDisposed = 3;

            private readonly ManualResetTaskProvider _tp = new ManualResetTaskProvider();
            private readonly VirtualTime _time;
            private readonly CancellationToken _token;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private DateTime _dueUtc;

            public Timer(VirtualTime time, CancellationToken token)
            {
                _time = time;
                _token = token;
                if (_token.CanBeCanceled) _ctr = token.Register(TokenCanceled);
            }

            public ValueTask Delay(DateTimeOffset due)
            {
                _tp.Reset();
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        if (due <= _time.Now)
                        {
                            _state = _sInitial;
                            _tp.SetResult();
                        }
                        else
                        {
                            _dueUtc = due.UtcDateTime;
                            _state = _sWaiting;
                            try { _time.Add(_dueUtc, this); }
                            catch (Exception ex)
                            {
                                if (Atomic.TestAndSet(ref _state, _sWaiting, _sInitial) == _sWaiting)
                                    _tp.SetException(ex);
                            }
                        }

                        break;
                    case _sCanceled:
                        _state = _sCanceled;
                        _tp.SetException(new OperationCanceledException(_token));
                        break;
                    case _sDisposed:
                        _state = _sDisposed;
                        _tp.SetException(new ObjectDisposedException(nameof(ITimer)));
                        break;
                    default: // _sWaiting???
                        _state = state;
                        _tp.SetException(new InvalidOperationException());
                        break;
                }

                return _tp.Task;
            }

            public ValueTask Delay(TimeSpan due) => Delay(_time.Now + due);

            public void Elapse()
            {
                if (Atomic.TestAndSet(ref _state, _sWaiting, _sInitial) != _sWaiting) return;
                _time.Remove(_dueUtc, this);
                _tp.SetResult();
            }

            public void Dispose()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sDisposed;
                        _ctr.Dispose();
                        break;
                    case _sWaiting:
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _tp.SetException(new ObjectDisposedException(nameof(ITimer)));
                        break;
                    default: // Canceled, Disposed
                        _state = _sDisposed;
                        break;
                }
            }

            private void TokenCanceled()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sCanceled;
                        _ctr.Dispose();
                        break;
                    case _sWaiting:
                        _state = _sCanceled;
                        _ctr.Dispose();
                        _tp.SetException(new OperationCanceledException(_token));
                        break;
                    default: // Canceled, Disposed
                        _state = state;
                        break;
                }
            }

            public void Elapse(Exception exception)
            {
                if (Atomic.TestAndSet(ref _state, _sWaiting, _sInitial) != _sWaiting) return;
                if (exception == null) _tp.SetResult();
                else _tp.SetException(exception);
            }
        }
    }
}

