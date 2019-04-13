namespace Linx.Reactive.Timing
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Collections;

    /// <summary>
    /// Virtuala time.
    /// </summary>
    public sealed class VirtualTime : ITime, IDisposable
    {
        private enum State
        {
            Stopped,
            AdvanceTo,
            Started,
            Disposed
        }

        private readonly PriorityQueue<Bucket> _queue = new PriorityQueue<Bucket>();
        private readonly Dictionary<DateTime, List<Timer>> _timersByDue = new Dictionary<DateTime, List<Timer>>();
        private readonly Stack<List<Timer>> _pool = new Stack<List<Timer>>(); // recicle empty timer lists
        private State _state;
        private DateTimeOffset _advanceTo;

        /// <summary>
        /// Crete with <see cref="Now"/> being <see cref="DateTimeOffset.MinValue"/>.
        /// </summary>
        public VirtualTime() : this(DateTimeOffset.MinValue) { }

        /// <summary>
        /// Crete with <see cref="Now"/> being <paramref name="now"/>.
        /// </summary>
        public VirtualTime(DateTimeOffset now)
        {
            if (Time.Current != Time.RealTime) throw new InvalidOperationException("Not real time.");

            Time.Current = this;
            Now = now;
            var advanceThread = new Thread(_ => Advance()) { Priority = ThreadPriority.Lowest };
            advanceThread.Start(null);
        }

        /// <inheritdoc />
        public DateTimeOffset Now { get; private set; }

        /// <inheritdoc />
        public Task Wait(DateTimeOffset due, CancellationToken token)
        {
            lock (_queue)
            {
                if (_state == State.Disposed) return Task.FromException(new ObjectDisposedException(nameof(VirtualTime)));
                if (token.IsCancellationRequested) return Task.FromCanceled(token);

                try // to schedule
                {
                    var dueUtc = due.UtcDateTime;
                    if (!_timersByDue.TryGetValue(dueUtc, out var timers)) // no bucket for this time, so add
                    {
                        timers = GetEmptyTimers();
                        _queue.Enqueue(new Bucket(dueUtc, timers));
                        _timersByDue.Add(dueUtc, timers);
                    }

                    var timer = new Timer { Atmb = new AsyncTaskMethodBuilder() };
                    var task = timer.Atmb.Task;
                    timers.Add(timer);
                    if (token.CanBeCanceled)
                        timer.Ctr = token.Register(() =>
                        {
                            lock (_queue)
                            {
                                if (!timers.Remove(timer)) return;
                            }
                            timer.Atmb.SetException(new OperationCanceledException(token));
                        });

                    Monitor.Pulse(_queue);
                    return task;
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            }
        }

        /// <inheritdoc />
        public async Task Wait(TimeSpan delay, CancellationToken token) => await Wait(Now + delay, token).ConfigureAwait(false);

        /// <summary>
        /// Advance time to the specified <paramref name="time"/>.
        /// </summary>
        public void AdvanceTo(DateTimeOffset time)
        {
            lock (_queue)
            {
                if (_state == State.Disposed) throw new ObjectDisposedException(nameof(VirtualTime));
                if (time < Now) time = Now;
                _state = State.AdvanceTo;
                _advanceTo = time;
                Monitor.Pulse(_queue);
            }
        }

        /// <summary>
        /// Advance time by the specified <paramref name="span"/>.
        /// </summary>
        public void AdvanceBy(TimeSpan span)
        {
            if (span > TimeSpan.Zero) AdvanceTo(Now + span);
        }

        /// <summary>
        /// Advance while there are waits scheduled.
        /// </summary>
        public void Start()
        {
            lock (_queue)
            {
                if (_state == State.Disposed) throw new ObjectDisposedException(nameof(VirtualTime));
                _state = State.Started;
                Monitor.Pulse(_queue);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_queue)
            {
                if (_state == State.Disposed) return;
                _state = State.Disposed;
                Time.Current = Time.RealTime;
                Monitor.Pulse(_queue);
            }
        }

        private void Advance()
        {
            // loop to advance virtual time, running on a low prio thread
            while (true)
            {
                List<Timer> timers;
                while (true)
                {
                    Thread.Sleep(1); // yeald to other threads so they can schedule
                    lock (_queue)
                    {
                        if (_queue.IsEmpty)
                            switch (_state)
                            {
                                case State.Stopped:
                                case State.Started:
                                    break;
                                case State.AdvanceTo:
                                    if (_advanceTo < Now) Now = _advanceTo;
                                    _state = State.Stopped;
                                    break;
                                case State.Disposed:
                                    _pool.Clear();
                                    return;
                                default:
                                    throw new Exception(_state + "???");
                            }
                        else
                        {
                            var bucket = _queue.Peek();
                            if (bucket.Timers.Count == 0)
                            {
                                _queue.Dequeue();
                                _timersByDue.Remove(bucket.DueUtc);
                                _pool.Push(bucket.Timers);
                                continue;
                            }

                            if (bucket.DueUtc > Now)
                                switch (_state)
                                {
                                    case State.Stopped:
                                        break;
                                    case State.Started:
                                    case State.Disposed:
                                        Now = bucket.DueUtc;
                                        break;
                                    case State.AdvanceTo:
                                        if (bucket.DueUtc <= _advanceTo)
                                            Now = bucket.DueUtc;
                                        else
                                        {
                                            Now = _advanceTo;
                                            _state = State.Stopped;
                                        }

                                        break;
                                    default:
                                        throw new Exception(_state + "???");
                                }

                            if (bucket.DueUtc <= Now)
                            {
                                Now = bucket.DueUtc;
                                _queue.Dequeue();
                                _timersByDue.Remove(bucket.DueUtc);
                                timers = bucket.Timers;
                                foreach (var timer in timers) timer.Ctr.Dispose();

                                break;
                            }
                        }

                        Monitor.Wait(_queue);
                    }
                }

                foreach (var timer in timers) timer.Atmb.SetResult();
                timers.Clear();
                lock (_queue) _pool.Push(timers);
            }
        }

        // assumes lock
        private List<Timer> GetEmptyTimers() => _pool.Count > 0 ? _pool.Pop() : new List<Timer>();

        private struct Bucket : IComparable<Bucket>
        {
            public readonly DateTime DueUtc;
            public readonly List<Timer> Timers;

            public Bucket(DateTime dueUtc, List<Timer> timers)
            {
                DueUtc = dueUtc;
                Timers = timers;
            }

            public int CompareTo(Bucket other) => DueUtc.CompareTo(other.DueUtc);
        }

        private sealed class Timer
        {
            public AsyncTaskMethodBuilder Atmb;
            public CancellationTokenRegistration Ctr;
        }
    }
}

