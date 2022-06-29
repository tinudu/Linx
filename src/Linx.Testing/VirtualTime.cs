using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linx.Collections;
using Linx.Timing;

namespace Linx.Testing;

/// <summary>
/// Virtual time.
/// </summary>
public sealed partial class VirtualTime : ITime
{
    /// <summary>
    /// Run the <paramref name="asyncAction"/> on virtual time.
    /// </summary>
    /// <param name="asyncAction">The action.</param>
    /// <param name="t0">Optional, default <see cref="DateTimeOffset.MinValue"/>Start of time.</param>
    /// <returns>The point in time when the action completed.</returns>
    public static DateTimeOffset Run(Func<VirtualTime, Task> asyncAction, DateTimeOffset? t0 = default)
    {
        if (asyncAction is null) throw new ArgumentNullException(nameof(asyncAction));

        // run on thread pool so continuations run synchronously
        return Task.Run(() =>
        {
            var vt = new VirtualTime(t0 ?? default);

            async Task<DateTimeOffset> TimeStamp()
            {
                await asyncAction(vt).ConfigureAwait(false);
                return vt.Now;
            }

            var resultAwaiter = TimeStamp().ConfigureAwait(false).GetAwaiter();
            vt.Start();

            return resultAwaiter.IsCompleted
                ? resultAwaiter.GetResult()
                : throw new Exception("Operation did not complete until the end of (virtual) time.");
        }).Result;
    }

    /// <summary>
    /// Run the <paramref name="asyncFunc"/> on virtual time.
    /// </summary>
    /// <param name="asyncFunc">The action.</param>
    /// <param name="t0">Optional, default <see cref="DateTimeOffset.MinValue"/>Start of time.</param>
    /// <returns>The timestamped result.</returns>
    public static Timestamped<T> Run<T>(Func<VirtualTime, Task<T>> asyncFunc, DateTimeOffset? t0 = default)
    {
        if (asyncFunc is null) throw new ArgumentNullException(nameof(asyncFunc));

        // run on thread pool so continuations run synchronously
        return Task.Run(() =>
        {
            var vt = new VirtualTime(t0 ?? default);

            async Task<Timestamped<T>> TimeStamp()
            {
                var result = await asyncFunc(vt).ConfigureAwait(false);
                return new(vt.Now, result);
            }

            var resultAwaiter = TimeStamp().ConfigureAwait(false).GetAwaiter();
            vt.Start();

            return resultAwaiter.IsCompleted
                ? resultAwaiter.GetResult()
                : throw new Exception("Operation did not complete until the end of (virtual) time.");
        }).Result;
    }

    private DateTimeOffset _now;
    private readonly PriorityQueue<Bucket> _queue = new();
    private readonly Dictionary<DateTime, Queue<Timer>> _timersByDue = new();
    private readonly Stack<Queue<Timer>> _pool = new(); // recicle empty timer queues

    private VirtualTime(DateTimeOffset t0)
    {
        _now = t0.ToUniversalTime();
    }

    /// <inheritdoc />
    public DateTimeOffset Now
    {
        get
        {
            DateTimeOffset value;
            lock (_queue)
                value = _now;
            return value;
        }
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

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that requests cancellation at the specified time.
    /// </summary>
    public CancellationToken CancelAt(DateTimeOffset due)
    {
        var cts = new CancellationTokenSource();
        this.Schedule(cts.Cancel, due, default);
        return cts.Token;
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

    private void Enqueue(Timer timer, DateTimeOffset due)
    {
        Exception? error = null;
        lock (_queue)
        {
            if (due > _now)
                try
                {
                    var dueUtc = due.UtcDateTime;
                    if (!_timersByDue.TryGetValue(dueUtc, out var timers))
                    {
                        timers = _pool.Count > 0 ? _pool.Pop() : new Queue<Timer>();
                        _queue.Enqueue(new Bucket(dueUtc, timers));
                        _timersByDue.Add(dueUtc, timers);
                    }

                    timers.Enqueue(timer);
                    return;
                }
                catch (Exception ex) { error = ex; }
        }

        if (error is null)
            timer.SetResult();
        else
            timer.SetException(error);
    }

    private void Start()
    {
        while (true)
        {
            Queue<Timer>? timers;
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    var bucket = _queue.Dequeue();
                    _now = bucket.DueUtc;
                    timers = bucket.Timers;
                }
                else
                {
                    timers = null;
                    _now = DateTimeOffset.MaxValue;
                }
            }

            if (timers is null)
                return;

            while (timers.Count > 0)
                timers.Dequeue().SetResult();
            _pool.Push(timers);
        }
    }
}