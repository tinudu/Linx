using System;
using System.Collections.Generic;
using System.Linq;

namespace Linx.Enumerable;

partial class LinxEnumerable
{
    /// <summary>
    /// Returns the minimum element of a sequence, or a default value.
    /// </summary>
    public static T? MinOrNull<T>(this IEnumerable<T> source, IComparer<T>? comparer = null) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (comparer == null) comparer = Comparer<T>.Default;

        using var e = source.GetEnumerator();
        if (!e.MoveNext()) return default;
        var min = e.Current;
        while (e.MoveNext())
        {
            var current = e.Current;
            if (comparer.Compare(current, min) < 0) min = current;
        }
        return min;
    }

    /// <summary>
    /// Returns the minimum element of a projection of a sequence, or a default value.
    /// </summary>
    public static TResult? MinOrNull<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, IComparer<TResult>? comparer = null) where TResult : struct
        => source.Select(selector).MinOrNull(comparer);

    /// <summary>
    /// Returns the elements of a sequence witch have the minimum non-null key.
    /// </summary>
    public static IList<TSource> MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (comparer == null) comparer = Comparer<TKey>.Default;

        using var e = source.Select(x => (key: keySelector(x), value: x)).Where(x => x.key is not null).GetEnumerator();
        if(!e.MoveNext())
            return Array.Empty<TSource>();

        var item = e.Current;
        TKey min = item.key;
        var result = new List<TSource> { item.value };
        while(e.MoveNext())
        {
            item = e.Current;
            var cmp = comparer.Compare(item.key, min);
            if (cmp > 0) continue;
            if (cmp < 0)
            {
                min = item.key;
                result.Clear();
            }
            result.Add(item.value);
        }
        return result;
    }

    /// <summary>
    /// Returns the maximum element of a sequence, or a default value.
    /// </summary>
    public static T? MaxOrNull<T>(this IEnumerable<T> source, IComparer<T>? comparer = null) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (comparer == null) comparer = Comparer<T>.Default;

        using var e = source.GetEnumerator();
        if (!e.MoveNext()) return default;
        var max = e.Current;
        while (e.MoveNext())
        {
            var current = e.Current;
            if (comparer.Compare(current, max) > 0) max = current;
        }
        return max;
    }

    /// <summary>
    /// Returns the maximum element of a projection of a sequence, or a default value.
    /// </summary>
    public static TResult? MaxOrNull<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, IComparer<TResult>? comparer = null) where TResult : struct
        => source.Select(selector).MaxOrNull(comparer);

    /// <summary>
    /// Returns the elements of a sequence witch have the maximum non-null key.
    /// </summary>
    public static IList<TSource> MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (comparer == null) comparer = Comparer<TKey>.Default;

        using var e = source.Select(x => (key: keySelector(x), value: x)).Where(x => x.key is not null).GetEnumerator();
        if(!e.MoveNext())
            return Array.Empty<TSource>();

        var item = e.Current;
        TKey max = item.key;
        var result = new List<TSource> { item.value };
        while(e.MoveNext())
        {
            item = e.Current;
            var cmp = comparer.Compare(item.key, max);
            if (cmp < 0) continue;
            if (cmp > 0)
            {
                max = item.key;
                result.Clear();
            }
            result.Add(item.value);
        }
        return result;
    }

}