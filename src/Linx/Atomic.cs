using System;
using System.Diagnostics;
using System.Threading;

namespace Linx;

/// <summary>
/// Supports atomic state updates.
/// </summary>
[DebuggerStepThrough]
public static class Atomic
{
    /// <summary>
    /// Spin wait until the state is non-negative.
    /// </summary>
    /// <returns>The non-negative state.</returns>
    public static int Read(in int state)
    {
        var result = state;
        if (result < 0)
        {
            var sw = new SpinWait();
            do
            {
                sw.SpinOnce();
                result = state;
            } while (result < 0);
        }
        return result;
    }

    /// <summary>
    /// Spin wait until the state is non-negative, then update it to its one's complement.
    /// </summary>
    /// <returns>The state before the update.</returns>
    public static int Lock(ref int state)
    {
        var result = state;
        while (result >= 0)
        {
            var changed = Interlocked.CompareExchange(ref state, ~result, result);
            if (changed == result)
                return result;
            result = changed;
        }

        var sw = new SpinWait();
        while (true)
        {
            do
            {
                sw.SpinOnce();
                result = state;
            } while (result < 0);

            do
            {
                var changed = Interlocked.CompareExchange(ref state, ~result, result);
                if (changed == result)
                    return result;
                result = changed;
            } while (result >= 0);

            sw.Reset();
        }
    }

    /// <summary>
    /// Spin wait until the state is non-negative, then update it to the specified <paramref name="value"/>.
    /// </summary>
    /// <returns>The state before the update.</returns>
    public static int Exchange(ref int state, int value)
    {
        var result = state;
        while (result >= 0)
        {
            var changed = Interlocked.CompareExchange(ref state, value, result);
            if (changed == result)
                return result;
            result = changed;
        }

        var sw = new SpinWait();
        while (true)
        {
            do
            {
                sw.SpinOnce();
                result = state;
            } while (result < 0);

            do
            {
                var changed = Interlocked.CompareExchange(ref state, value, result);
                if (changed == result)
                    return result;
                result = changed;
            } while (result >= 0);

            sw.Reset();
        }
    }

    /// <summary>
    /// Spin wait until the state is non-negative, then update it to the specified <paramref name="value"/> iff the previous value is <paramref name="comparand"/>.
    /// </summary>
    /// <returns>The previous state.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparand"/> is negative.</exception>
    public static int CompareExchange(ref int state, int value, int comparand)
    {
        if (comparand < 0) throw new ArgumentOutOfRangeException(nameof(comparand), "Is negative.");

        var result = Interlocked.CompareExchange(ref state, value, comparand);
        if (result < 0)
        {
            var sw = new SpinWait();
            do
            {
                sw.SpinOnce();
                result = Interlocked.CompareExchange(ref state, value, comparand);
            } while (result < 0);
        }
        return result;
    }
}
