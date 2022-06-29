using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Tasking;

/// <summary>
/// A pool of <see cref="ILinxValueTaskSource"/> for reuse.
/// </summary>
/// <remarks>Items reset on <see cref="IValueTaskSource.GetResult(short)"/> and return to the pool.</remarks>
public class ValueTaskSourcePool
{
    private int _lock;
    private Node? _pool;

    /// <summary>
    /// Get a <see cref="ILinxValueTaskSource"/> from the pool, or a new one if pool is empty.
    /// </summary>
    public ILinxValueTaskSource GetValueTaskSource()
    {
        Atomic.Lock(ref _lock);
        Node node;
        if (_pool is null)
        {
            _lock = 0;
            node = new Node(this);
        }
        else
        {
            node = _pool;
            _pool = node.Next;
            _lock = 0;
            node.Next = null;
        }
        return node;
    }

    private sealed class Node : ILinxValueTaskSource
    {
        private readonly ValueTaskSourcePool _parent;

        public Node? Next;
        private ManualResetValueTaskSourceCore<Unit> _core;

        public Node(ValueTaskSourcePool parent) => _parent = parent;

        public ValueTask ValueTask => new(this, _core.Version);

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        public void SetException(Exception exception) => _core.SetException(exception);
        public void SetResult() => _core.SetResult(default);

        public void GetResult(short token)
        {
            try { _core.GetResult(token); }
            finally // return to pool
            {
                _core.Reset();
                Atomic.Lock(ref _parent._lock);
                Next = _parent._pool;
                _parent._pool = this;
                _parent._lock = 0;
            }
        }
    }
}


/// <summary>
/// A pool of <see cref="ILinxValueTaskSource{T}"/> for reuse.
/// </summary>
/// <remarks>Items reset on <see cref="IValueTaskSource{T}.GetResult(short)"/> and return to the pool.</remarks>
public class ValueTaskSourcePool<T>
{
    private int _lock;
    private Node? _pool;

    /// <summary>
    /// Get a <see cref="ILinxValueTaskSource{T}"/> from the pool, or a new one if pool is empty.
    /// </summary>
    public ILinxValueTaskSource<T> GetValueTaskSource()
    {
        Atomic.Lock(ref _lock);
        Node node;
        if (_pool is null)
        {
            _lock = 0;
            node = new Node(this);
        }
        else
        {
            node = _pool;
            _pool = node.Next;
            _lock = 0;
            node.Next = null;
        }
        return node;
    }

    private sealed class Node : ILinxValueTaskSource<T>, IValueTaskSource
    {
        private readonly ValueTaskSourcePool<T> _parent;

        public Node? Next;
        private ManualResetValueTaskSourceCore<T> _core;

        public Node(ValueTaskSourcePool<T> parent) => _parent = parent;

        public ValueTask<T> ValueTask => new(this, _core.Version);
        public ValueTask ValueTaskNonGeneric => new(this, _core.Version);

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
        public void SetException(Exception exception) => _core.SetException(exception);
        public void SetResult(T result) => _core.SetResult(result);

        public T GetResult(short token)
        {
            try { return _core.GetResult(token); }
            finally // return to pool
            {
                _core.Reset();
                Atomic.Lock(ref _parent._lock);
                Next = _parent._pool;
                _parent._pool = this;
                _parent._lock = 0;
            }
        }

        void IValueTaskSource.GetResult(short token) => GetResult(token);
    }
}
