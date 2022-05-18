using System.Diagnostics;
using System.Threading;

namespace Linx;

/// <summary>
/// Supports atomic state updates using the sign bit as a lock.
/// </summary>
[DebuggerStepThrough]
public static class Atomic
{
    /// <summary>
    /// Spin wait until the lock is cleared, then return the unlocked state.
    /// </summary>
    /// <param name="state">The state to read.</param>
    /// <returns>The state.</returns>
    public static int Read(in int state)
    {
        var result = state;
        if (result >= 0)
            return result;

        var sw = new SpinWait();
        while (true)
        {
            sw.SpinOnce();
            result = state;
            if (result >= 0)
                return result;
        }
    }

    /// <summary>
    /// Spin wait until the lock is cleared, then aquire it.
    /// </summary>
    /// <param name="state">The state to update.</param>
    /// <returns>The previous state.</returns>
    public static int Lock(ref int state)
    {
        var oldState = state;
        if (oldState >= 0 && Interlocked.CompareExchange(ref state, oldState | int.MinValue, oldState) == oldState)
            return oldState;

        var sw = new SpinWait();
        while (true)
        {
            sw.SpinOnce();
            oldState = state;
            if (oldState >= 0 && Interlocked.CompareExchange(ref state, oldState | int.MinValue, oldState) == oldState)
                return oldState;
        }
    }

    /// <summary>
    /// Spin wait until the lock  is cleared, then set to <paramref name="value"/>.
    /// </summary>
    /// <param name="state">The state to update.</param>
    /// <param name="value">The new state (may include the lock bit).</param>
    /// <returns>The previous state.</returns>
    public static int Exchange(ref int state, int value)
    {
        var oldState = state;
        if (oldState >= 0 && Interlocked.CompareExchange(ref state, value, oldState) == oldState)
            return oldState;

        var sw = new SpinWait();
        while (true)
        {
            sw.SpinOnce();
            oldState = state;
            if (oldState >= 0 && Interlocked.CompareExchange(ref state, value, oldState) == oldState)
                return oldState;
        }
    }

    /// <summary>
    /// Spin wait until the lock is cleared, then set to <paramref name="value"/> iff the previous value is <paramref name="comparand"/>.
    /// </summary>
    /// <param name="state">The state to update.</param>
    /// <param name="value">The new state (may include the lock bit).</param>
    /// <param name="comparand">The value <paramref name="state"/> must have in order for the state to be updated.</param>
    /// <returns>The previous state.</returns>
    /// <remarks>Don't include the lock in <paramref name="comparand"/>, or it will result  in an infinite loop.</remarks>
    public static int CompareExchange(ref int state, int value, int comparand)
    {
        var oldState = Interlocked.CompareExchange(ref state, value, comparand);
        if (oldState >= 0)
            return oldState;

        var sw = new SpinWait();
        while (true)
        {
            sw.SpinOnce();
            oldState = Interlocked.CompareExchange(ref state, value, comparand);
            if (oldState >= 0)
                return oldState;
        }
    }
}
