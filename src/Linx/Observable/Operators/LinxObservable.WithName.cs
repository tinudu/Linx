namespace Linx.Observable
{
    using System;
    using System.Runtime.CompilerServices;

    partial class LinxObservable
    {
        /// <summary>
        /// Wrap <paramref name="source"/> such that its <see cref="object.ToString"/> returns the specified <paramref name="name"/>.
        /// </summary>
        public static ILinxObservable<T> WithName<T>(this ILinxObservable<T> source, [CallerMemberName] string name = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source is AnonymousLinxObservable<T> aae ? aae.WithName(name) : new AnonymousLinxObservable<T>(source.Subscribe, name);
        }
    }
}
