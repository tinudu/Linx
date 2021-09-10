namespace Linx
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Common convenience methods.
    /// </summary>
    public static partial class Linx
    {
        /// <summary>
        /// Sets the value at specified storage location to the default value.
        /// </summary>
        /// <returns>The previous value at the location.</returns>
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clear<T>(ref T storageLocation)
        {
            var oldValue = storageLocation;
            storageLocation = default;
            return oldValue;
        }

        /// <summary>
        /// Exchanges the value at specified storage location with the specified value.
        /// </summary>
        /// <returns>The previous value at the location.</returns>
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Exchange<T>(ref T storageLocation, T value)
        {
            var oldValue = storageLocation;
            storageLocation = value;
            return oldValue;
        }

        /// <summary>
        /// Invoke the specified action with the specified argument.
        /// </summary>
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke<TArgument>(this TArgument argument, Action<TArgument> action) => action(argument);

        /// <summary>
        /// Invoke the specified function with the specified argument and return the result.
        /// </summary>
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult Invoke<TArgument, TResult>(this TArgument argument, Func<TArgument, TResult> function) => function(argument);

        /// <summary>
        /// Await cancellation.
        /// </summary>
        /// <example>
        /// <code>
        /// token.ThrowIfCancellationRequested();
        /// throw await token.WhenCancellationRequested();
        /// </code>
        /// </example>
        public static Task<OperationCanceledException> WhenCancellationRequested(this CancellationToken token)
        {
            var atmb = new AsyncTaskMethodBuilder<OperationCanceledException>();
            try
            {
                if (token.IsCancellationRequested)
                    atmb.SetResult(new OperationCanceledException(token));
                else if (token.CanBeCanceled)
                    token.Register(() =>
                    {
                        try { atmb.SetResult(new OperationCanceledException(token)); }
                        catch (Exception ex) { atmb.SetException(ex); }
                    });
            }
            catch (Exception ex) { atmb.SetException(ex); }
            return atmb.Task;
        }

        /// <summary>
        /// Defensive cancellation.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="cts"/> is null.</exception>
        public static void TryCancel(this CancellationTokenSource cts)
        {
            if (cts is null) throw new ArgumentNullException(nameof(cts));

            try { cts.Cancel(); }
            catch { /**/ }
        }

        /// <summary>
        /// Enumerate buffer capacities starting with <paramref name="maxCapacity"/>, dividing by 2 until a value of 4..7 is reached.
        /// </summary>
        internal static IEnumerable<int> Capacities(int maxCapacity)
        {
            yield return maxCapacity;
            while (maxCapacity > 7)
            {
                maxCapacity >>= 1;
                yield return maxCapacity;
            }
        }
    }
}
