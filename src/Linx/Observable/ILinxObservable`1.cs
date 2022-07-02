using System.Threading;
using System.Threading.Tasks;

namespace Linx.Observable;

/// <summary>
/// Represents a sequence to which notifications are pushed synchronously.
/// </summary>
public interface ILinxObservable<out T>
{
    /// <summary>
    /// Start generating items.
    /// </summary>
    /// <param name="yield">A <see cref="YieldDelegate{T}"/> to which to push to.</param>
    /// <param name="token"><see cref="CancellationToken"/> that may request cancellation.</param>
    /// <returns></returns>
    Task Subscribe(YieldDelegate<T> yield, CancellationToken token);
}
