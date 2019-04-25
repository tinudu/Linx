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
            do
            {
                sw.SpinOnce();
                oldState = state;
                if ((oldState & LockBit) == 0 && Interlocked.CompareExchange(ref state, oldState | LockBit, oldState) == oldState) return oldState;
            } while (true);
        }

        /// <summary>
        /// Spin wait until the lock bit is cleared, then set state to <paramref name="set"/> iff <paramref name="test"/>.
        /// </summary>
        /// <param name="state">The state to update.</param>
        /// <param name="test">The value <paramref name="state"/> must have in order for the state to be updated (don't include the lock bit!).</param>
        /// <param name="set">The new state (may include the lock bit).</param>
        /// <returns>The previous state.</returns>
        public static int TestAndSet(ref int state, int test, int set)
        {
            if ((test & LockBit) != 0) throw new ArgumentOutOfRangeException(nameof(test), "Lock bit is set.");

            var oldState = Interlocked.CompareExchange(ref state, set, test);
            if ((oldState & LockBit) == 0) return oldState;

            var sw = new SpinWait();
            do
            {
                sw.SpinOnce();
                oldState = Interlocked.CompareExchange(ref state, set, test);
                if ((oldState & LockBit) == 0) return oldState;
            } while (true);
        }

        /// <summary>
        /// Spin wait until the token is canceled.
        /// </summary>
        public static void WaitCanceled(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            var sw = new SpinWait();
            do sw.SpinOnce();
            while (!token.IsCancellationRequested);
        }
    }
}
