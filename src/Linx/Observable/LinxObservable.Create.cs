using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.Observable;

partial class LinxObservable
{
    /// <summary>
    /// Create a <see cref="ILinxObservable{T}"/> defined by a <see cref="ProduceDelegate{T}"/> coroutine.
    /// </summary>
    public static ILinxObservable<T> Create<T>(ProduceDelegate<T> produce, [CallerMemberName] string? displayName = default)
        => new CoroutineObservable<T>(produce, displayName);

    /// <summary>
    /// Create a <see cref="ILinxObservable{T}"/> defined by a <see cref="ProduceDelegate{T}"/> coroutine.
    /// </summary>
    public static ILinxObservable<T> Create<T>(T _, ProduceDelegate<T> produce, [CallerMemberName] string? displayName = default)
        => new CoroutineObservable<T>(produce, displayName);

    private sealed class CoroutineObservable<T> : ILinxObservable<T>
    {
        private readonly ProduceDelegate<T> _produce;
        private readonly string _displayName;

        public CoroutineObservable(ProduceDelegate<T> produce, string? displayName)
        {
            _produce = produce ?? throw new ArgumentNullException(nameof(produce));
            _displayName = displayName ?? nameof(ILinxObservable<T>);
        }

        public Task Subscribe(YieldDelegate<T> yield, CancellationToken token) => _produce(yield, token);

        public override string ToString() => _displayName;
    }
}
