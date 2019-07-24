namespace Linx
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Supports atomic state updates using a lock bit.
    /// </summary>
    [DebuggerStepThrough]
    public static class Atomic
    {
        /// <summary>
        /// Bit 31, indicating a state is locked.
        /// </summary>
        public const int LockBit = 1 << 31;

        /// <summary>
        /// Spin wait until the lock bit is cleared, then set it.
        /// </summary>
        /// <param name="state">The state to update.</param>
        /// <returns>The previous state.</returns>
        public static int Lock(ref int state)
        {
            var oldState = state;
            if ((oldState & LockBit) == 0 && Interlocked.CompareExchange(ref state, oldState | LockBit, oldState) == oldState) return oldState;

            var sw = new SpinWait();
            while (true)
            {
                sw.SpinOnce();
                oldState = state;
                if ((oldState & LockBit) == 0 && Interlocked.CompareExchange(ref state, oldState | LockBit, oldState) == oldState) return oldState;
            }
        }

        /// <summary>
        /// Spin wait until the lock bit is cleared, then set to <paramref name="value"/>.
        /// </summary>
        /// <param name="state">The state to update.</param>
        /// <param name="value">The new state (may include the lock bit).</param>
        /// <returns>The previous state.</returns>
        public static int Exchange(ref int state, int value)
        {
            var oldState = Interlocked.Exchange(ref state, value);
            if ((oldState & LockBit) == 0) return oldState;

            var sw = new SpinWait();
            while (true)
            {
                sw.SpinOnce();
                oldState = Interlocked.Exchange(ref state, value);
                if ((oldState & LockBit) == 0) return oldState;
            }
        }


        /// <summary>
        /// Spin wait until the lock bit is cleared, then set to <paramref name="value"/> iff the previous value is <paramref name="test"/>.
        /// </summary>
        /// <param name="state">The state to update.</param>
        /// <param name="value">The new state (may include the lock bit).</param>
        /// <param name="test">The value <paramref name="state"/> must have in order for the state to be updated.</param>
        /// <returns>The previous state.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="test"/> has the lock bit set.</exception>
        public static int CompareExchange(ref int state, int value, int test)
        {
            var oldState = Interlocked.CompareExchange(ref state, value, test);
            if ((oldState & LockBit) == 0) return oldState;

            if ((test & LockBit) != 0) throw new ArgumentOutOfRangeException(nameof(test), "Lock bit is set.");

            var sw = new SpinWait();
            while(true)
            {
                sw.SpinOnce();
                oldState = Interlocked.CompareExchange(ref state, value, test);
                if ((oldState & LockBit) == 0) return oldState;
            }
        }
    }
}
