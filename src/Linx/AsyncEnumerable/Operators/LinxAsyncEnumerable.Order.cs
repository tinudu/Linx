using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Define an ascending order on <paramref name="source"/> using the specified <paramref name="comparer"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<T> Order<T>(
        this IAsyncEnumerable<T> source,
        IComparer<T>? comparer = null)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (comparer is null) comparer = Comparer<T>.Default;

        return new OrderedAsyncEnumerableImpl<T>(source, comparer.Compare);
    }

    /// <summary>
    /// Define a descending order on <paramref name="source"/> using the specified <paramref name="comparer"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<T> OrderDescending<T>(
        this IAsyncEnumerable<T> source,
        IComparer<T>? comparer = null)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (comparer is null) comparer = Comparer<T>.Default;

        return new OrderedAsyncEnumerableImpl<T>(source, (x, y) => comparer.Compare(y, x));
    }

    /// <summary>
    /// Define an ascending order on <paramref name="source"/> using the specified <paramref name="comparison"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> Order<TSource>(
        this IAsyncEnumerable<TSource> source,
        Comparison<TSource> comparison)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (comparison is null) throw new ArgumentNullException(nameof(comparison));

        return new OrderedAsyncEnumerableImpl<TSource>(source, comparison);
    }

    /// <summary>
    /// Define a descending order on <paramref name="source"/> using the specified <paramref name="comparison"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> OrderDescending<TSource>(
        this IAsyncEnumerable<TSource> source,
        Comparison<TSource> comparison)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (comparison is null) throw new ArgumentNullException(nameof(comparison));

        return new OrderedAsyncEnumerableImpl<TSource>(source, (x, y) => comparison(y, x));
    }

    /// <summary>
    /// Define an ascending order on <paramref name="source"/> using the specified <paramref name="keyComparer"/> on a projection defined by <paramref name="keySelector"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey>? keyComparer = null)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
        if (keyComparer is null) keyComparer = Comparer<TKey>.Default;

        return new OrderedAsyncEnumerableImpl<TSource>(source, (x, y) => keyComparer.Compare(keySelector(x), keySelector(y)));
    }

    /// <summary>
    /// Define a descending order on <paramref name="source"/> using the specified <paramref name="keyComparer"/> on a projection defined by <paramref name="keySelector"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey>? keyComparer = null)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
        if (keyComparer is null) keyComparer = Comparer<TKey>.Default;

        return new OrderedAsyncEnumerableImpl<TSource>(source, (x, y) => keyComparer.Compare(keySelector(y), keySelector(x)));
    }

    /// <summary>
    /// Define an ascending order on <paramref name="source"/> using the specified <paramref name="keyComparison"/> on a projection defined by <paramref name="keySelector"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Comparison<TKey> keyComparison)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
        if (keyComparison is null) throw new ArgumentNullException(nameof(keyComparison));

        return new OrderedAsyncEnumerableImpl<TSource>(source, (x, y) => keyComparison(keySelector(x), keySelector(y)));
    }

    /// <summary>
    /// Define a descending order on <paramref name="source"/> using the specified <paramref name="keyComparison"/> on a projection defined by <paramref name="keySelector"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Comparison<TKey> keyComparison)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
        if (keyComparison is null) throw new ArgumentNullException(nameof(keyComparison));

        return new OrderedAsyncEnumerableImpl<TSource>(source, (x, y) => keyComparison(keySelector(y), keySelector(x)));
    }

    /// <summary>
    /// Define an ascending secondary order on <paramref name="source"/> using the specified <paramref name="keyComparer"/> on a projection defined by <paramref name="keySelector"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
        this IOrderedAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey>? keyComparer = null)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
        if (keyComparer is null) keyComparer = Comparer<TKey>.Default;

        return new OrderedAsyncEnumerableImpl<TSource>(source.Source, source.Comparison, (x, y) => keyComparer.Compare(keySelector(x), keySelector(y)));
    }

    /// <summary>
    /// Define a descending secondary order on <paramref name="source"/> using the specified <paramref name="keyComparer"/> on a projection defined by <paramref name="keySelector"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
        this IOrderedAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey>? keyComparer = null)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
        if (keyComparer is null) keyComparer = Comparer<TKey>.Default;

        return new OrderedAsyncEnumerableImpl<TSource>(source.Source, source.Comparison, (x, y) => keyComparer.Compare(keySelector(y), keySelector(x)));
    }

    /// <summary>
    /// Define an ascending secondary order on <paramref name="source"/> using the specified <paramref name="keyComparison"/> on a projection defined by <paramref name="keySelector"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(
        this IOrderedAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Comparison<TKey> keyComparison)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
        if (keyComparison is null) throw new ArgumentNullException(nameof(keyComparison));

        return new OrderedAsyncEnumerableImpl<TSource>(source.Source, source.Comparison, (x, y) => keyComparison(keySelector(x), keySelector(y)));
    }

    /// <summary>
    /// Define a descending secondary order on <paramref name="source"/> using the specified <paramref name="keyComparison"/> on a projection defined by <paramref name="keySelector"/>.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(
        this IOrderedAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Comparison<TKey> keyComparison)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
        if (keyComparison is null) throw new ArgumentNullException(nameof(keyComparison));

        return new OrderedAsyncEnumerableImpl<TSource>(source.Source, source.Comparison, (x, y) => keyComparison(keySelector(y), keySelector(x)));
    }

    private sealed class OrderedAsyncEnumerableImpl<T> : IOrderedAsyncEnumerable<T>
    {
        public IAsyncEnumerable<T> Source { get; init; }
        public Comparison<T> Comparison { get; init; }

        public OrderedAsyncEnumerableImpl(IAsyncEnumerable<T> source, Comparison<T> comparison)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
        }

        public OrderedAsyncEnumerableImpl(IAsyncEnumerable<T> source, Comparison<T> primary, Comparison<T> secondary)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (primary is null) throw new ArgumentNullException(nameof(primary));
            if (secondary is null) throw new ArgumentNullException(nameof(secondary));

            Source = source;
            Comparison = (x, y) =>
            {
                var cmp = primary(x, y);
                return cmp == 0 ? secondary(x, y) : cmp;
            };
        }
    }
}
