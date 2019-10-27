namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Wrap <paramref name="source"/> such that its <see cref="object.ToString"/> returns the specified <paramref name="name"/>.
        /// </summary>
        public static IAsyncEnumerable<T> WithName<T>(this IAsyncEnumerable<T> source, [CallerMemberName] string name = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source is AnonymousAsyncEnumerable<T> aae ? aae.WithName(name) : new AnonymousAsyncEnumerable<T>(source.GetAsyncEnumerator, name);
        }
    }
}
