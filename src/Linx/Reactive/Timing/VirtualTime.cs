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
        private readonly PriorityQueue<Bucket> _queue = new PriorityQueue<Bucket>();
        private readonly Dictionary<DateTime, List<ITimerCompleter>> _completersByDue = new Dictionary<DateTime, List<ITimerCompleter>>();
        private readonly Stack<List<ITimerCompleter>> _pool = new Stack<List<ITimerCompleter>>(); // recicle empty timer lists
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
            if (Time.Current != Time.RealTime) throw new InvalidOperationException("Not real time.");

            Time.Current = this;
            Now = now;
            var advanceThread = new Thread(_ => Advance()) { Priority = ThreadPriority.Lowest };
            advanceThread.Start(null);
        }

        /// <inheritdoc />
        public DateTimeOffset Now { get; private set; }

        /// <inheritdoc />
        public Task Delay(DateTimeOffset due, CancellationToken token)
        {
            lock (_queue)
            {
                if (_isDisposed) return Task.FromException(new ObjectDisposedException(nameof(VirtualTime)));
                if (token.IsCancellationRequested) return Task.FromCanceled(token);

                try // to schedule
                {
                    var dueUtc = due.UtcDateTime;
                    if (!_completersByDue.TryGetValue(dueUtc, out var completers)) // no bucket for this time, so add
                    {
                        completers = GetEmptyCompleters();
                        _queue.Enqueue(new Bucket(dueUtc, completers));
                        _completersByDue.Add(dueUtc, completers);
                    }

                    var ttc = new TaskTimerCompleter { Atmb = new AsyncTaskMethodBuilder() };
                    var task = ttc.Atmb.Task;
                    completers.Add(ttc);
                    if (token.CanBeCanceled)
                        ttc.Ctr = token.Register(() =>
                        {
                            lock (_queue)
                            {
                                if (!completers.Remove(ttc)) return;
                            }
                            ttc.Atmb.SetException(new OperationCanceledException(token));
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
        public async Task Delay(TimeSpan delay, CancellationToken token) => await Delay(Now + delay, token).ConfigureAwait(false);

        /// <inheritdoc />
        public ITimer CreateTimer(TimerElapsedDelegte onElapsed) => new Timer(this, onElapsed);

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_queue)
            {
                if (_isDisposed) return;
                _isDisposed = true;
                Time.Current = Time.RealTime;
                Monitor.Pulse(_queue);
            }
        }

        private void Advance()
        {
            // loop to advance virtual time, running on a low prio thread
            while (true)
            {
                // yield to other threads so they can schedule
                Thread.Sleep(1);

                ITimerCompleter completer;
                lock (_queue)
                {
                    if (_isDisposed)
                    {
                        _queue.Clear();
                        _pool.Clear();
                        return;
                    }

                    while (true)
                    {
                        if(_queue.IsEmpty)
                        {
                            completer = null;
                            break;
                        }
                        var bucket = _queue.Peek();
                        if (bucket.Completers.Count > 0)
                        {
                            if (Now < bucket.DueUtc) Now = bucket.DueUtc;
                            completer = bucket.Completers[0];
                            bucket.Completers.RemoveAt(0);
                            break;
                        }
                        _queue.Dequeue();
                        _completersByDue.Remove(bucket.DueUtc);
                        _pool.Push(bucket.Completers);
                    }

                    if (completer == null)
                        Monitor.Wait(_queue);
                }
                completer?.Complete();
            }
        }

        // assumes lock
        private List<ITimerCompleter> GetEmptyCompleters() => _pool.Count > 0 ? _pool.Pop() : new List<ITimerCompleter>();

        private struct Bucket : IComparable<Bucket>
        {
            public readonly DateTime DueUtc;
            public readonly List<ITimerCompleter> Completers;

            public Bucket(DateTime dueUtc, List<ITimerCompleter> completers)
            {
                DueUtc = dueUtc;
                Completers = completers;
            }

            public int CompareTo(Bucket other) => DueUtc.CompareTo(other.DueUtc);
        }

        private interface ITimerCompleter
        {
            void Complete();
        }

        private sealed class TaskTimerCompleter : ITimerCompleter
        {
            public AsyncTaskMethodBuilder Atmb;
            public CancellationTokenRegistration Ctr;

            public void Complete()
            {
                Ctr.Dispose();
                Atmb.SetResult();
            }
        }

        private sealed class Timer : ITimer, ITimerCompleter
        {
            private enum State { Disabled, Enabled, Disposed }

            private readonly VirtualTime _vt;
            private readonly TimerElapsedDelegte _onElapsed;
            private State _state;
            private DateTimeOffset _due;

            public Timer(VirtualTime vt, TimerElapsedDelegte onElapsed)
            {
                _vt = vt;
                _onElapsed = onElapsed ?? throw new ArgumentNullException(nameof(onElapsed));
            }

            private void Add(DateTimeOffset due)
            {
                var dueUtc = due.UtcDateTime;
                if (!_vt._completersByDue.TryGetValue(dueUtc, out var completers)) // no bucket for this time, so add
                {
                    completers = _vt.GetEmptyCompleters();
                    _vt._queue.Enqueue(new Bucket(dueUtc, completers));
                    _vt._completersByDue.Add(dueUtc, completers);
                }
                completers.Add(this);
                _state = State.Enabled;
                _due = due;
                Monitor.Pulse(_vt._queue);
            }

            private void Remove()
            {
                _vt._completersByDue[_due.UtcDateTime].Remove(this);
                _state = State.Disabled;
            }

            public void Enable(TimeSpan delay) => Enable(_vt.Now + delay);

            public void Enable(DateTimeOffset due)
            {
                lock (_vt._queue)
                {
                    if (_vt._isDisposed) throw new ObjectDisposedException(nameof(VirtualTime));

                    switch (_state)
                    {
                        case State.Disabled:
                            Add(due);
                            break;
                        case State.Enabled:
                            if (due == _due) return;
                            Remove();
                            Add(due);
                            break;
                        case State.Disposed:
                            throw new ObjectDisposedException(nameof(ITimer));
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            public void Disable()
            {
                lock (_vt._queue)
                {
                    if (_vt._isDisposed) throw new ObjectDisposedException(nameof(VirtualTime));

                    switch (_state)
                    {
                        case State.Disabled:
                            break;
                        case State.Enabled:
                            Remove();
                            break;
                        case State.Disposed:
                            throw new ObjectDisposedException(nameof(ITimer));
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            public void Dispose()
            {
                lock (_vt._queue)
                    switch (_state)
                    {
                        case State.Disabled:
                            _state = State.Disposed;
                            break;
                        case State.Enabled:
                            if (!_vt._isDisposed) Remove();
                            _state = State.Disposed;
                            break;
                        case State.Disposed:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
            }

            public void Complete()
            {
                DateTimeOffset due;
                lock (_vt._queue)
                    switch (_state)
                    {
                        case State.Enabled:
                            due = _due;
                            _state = State.Disabled;
                            break;
                        case State.Disabled:
                        case State.Disposed:
                            return;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                try { _onElapsed(this, due); } catch { /**/ }
            }
        }
    }
}

