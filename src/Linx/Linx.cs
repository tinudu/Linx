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
    public static partial class Linx
    {
        private static readonly Task _never = new AsyncTaskMethodBuilder().Task;

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
        /// Gets a task that completes as <see cref="TaskStatus.Canceled"/> when the specified <paramref name="token"/> requests cancellation.
        /// </summary>
        public static Task WhenCanceled(this CancellationToken token)
        {
            if (token.IsCancellationRequested) return Task.FromCanceled(token);
            if (!token.CanBeCanceled) return _never;
            var atmb = new AsyncTaskMethodBuilder();
            if (token.CanBeCanceled) token.Register(() => atmb.SetException(new OperationCanceledException(token)));
            return atmb.Task;
        }
    }
}
