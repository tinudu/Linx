namespace Linx.Reactive
{
    using System;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns an observable sequence that terminates with an exception.
        /// </summary>
        public static IAsyncEnumerableObs<T> Throw<T>(Exception exception)
        {
            var t = Task.FromException<T>(exception ?? new ArgumentNullException(nameof(exception)));

            return Produce<T>((yield, token) => t);
        }

        /// <summary>
        /// Returns an observable sequence that terminates with an exception.
        /// </summary>
        public static IAsyncEnumerableObs<T> Throw<T>(T sample, Exception exception)
        {
            var t = Task.FromException<T>(exception ?? new ArgumentNullException(nameof(exception)));

            return Produce<T>((yield, token) => t);
        }
    }
}
