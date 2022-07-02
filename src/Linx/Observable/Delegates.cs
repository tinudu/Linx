using System.Threading;
using System.Threading.Tasks;

namespace Linx.Observable;

/// <summary>
/// Function to yield sequence elements to an <see cref="ILinxObservable{T}"/> observer.
/// </summary>
/// <returns>true if further elements are requested. false otherwise.</returns>
public delegate bool YieldDelegate<in T>(T value);

/// <summary>
/// Coroutine to generate a sequence.
/// </summary>
/// <param name="yield"><see cref="YieldDelegate{T}"/> to which to yield sequence elements.</param>
/// <param name="token">Token on which cancellation is requested.</param>
/// <returns>A task that, when completed, notifies the end of the sequence.</returns>
public delegate Task ProduceDelegate<out T>(YieldDelegate<T> yield, CancellationToken token);
