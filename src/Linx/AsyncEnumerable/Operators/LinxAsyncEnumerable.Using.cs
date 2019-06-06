namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Constructs a sequence that depends on a resource object.
        /// </summary>
        public static IAsyncEnumerable<T> Using<TResource, T>(
            Func<TResource> resourceFactory,
            Func<TResource, IAsyncEnumerable<T>> sequenceFactory)
            where TResource : IDisposable
        {
            if (resourceFactory == null) throw new ArgumentNullException(nameof(resourceFactory));
            if (sequenceFactory == null) throw new ArgumentNullException(nameof(sequenceFactory));

            return Produce<T>(async (yield, token) =>
            {
                var resource = resourceFactory();
                try { await sequenceFactory(resource).CopyTo(yield, token).ConfigureAwait(false); }
                finally { resource.Dispose(); }
            });
        }
    }
}
