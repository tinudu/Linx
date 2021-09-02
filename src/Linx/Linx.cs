namespace Linx
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Common convenience methods.
    /// </summary>
    public static class Linx
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
        /// <see cref="CancellationTokenSource.Cancel()"/> catching exception.
        /// </summary>
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryCancel(this CancellationTokenSource cts)
        {
            try { cts.Cancel(); } catch {/**/}
        }

        /// <summary>
        /// Gets a task that completes when the specified <paramref name="token"/> requests cancellation.
        /// </summary>
        [DebuggerNonUserCode]
        public static Task<OperationCanceledException> WhenCanceledAsync(this CancellationToken token)
        {
            var atmb = new AsyncTaskMethodBuilder<OperationCanceledException>();
            if (token.IsCancellationRequested)
                atmb.SetResult(new OperationCanceledException(token));
            else if (token.CanBeCanceled)
                token.Register(() => atmb.SetResult(new OperationCanceledException(token)));
            return atmb.Task;
        }
    }
}
