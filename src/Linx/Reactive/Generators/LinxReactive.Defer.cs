namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns an observable sequence that invokes the factory whenever it is enumerated.
        /// </summary>
        public static IAsyncEnumerableObs<T> Defer<T>(Func<IAsyncEnumerableObs<T>> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            return new AnonymousAsyncEnumerable<T>(token => factory().GetAsyncEnumerator(token));
        }
    }
}
