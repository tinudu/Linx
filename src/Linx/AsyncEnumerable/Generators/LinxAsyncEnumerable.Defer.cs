namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns an observable sequence that invokes the factory whenever it is enumerated.
        /// </summary>
        public static IAsyncEnumerable<T> Defer<T>(Func<IAsyncEnumerable<T>> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            return new AnonymousAsyncEnumerable<T>(token => factory().GetAsyncEnumerator(token), "Defer");
        }
    }
}
