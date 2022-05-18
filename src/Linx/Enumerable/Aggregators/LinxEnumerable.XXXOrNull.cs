using System;
using System.Collections.Generic;
using System.Linq;

namespace Linx.Enumerable;

partial class LinxEnumerable
{
    /// <summary>
    /// Returns the first element of a sequence, or a default value.
    /// </summary>
    public static T? FirstOrNull<T>(this IEnumerable<T> source) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        // ReSharper disable once GenericEnumeratorNotDisposed
        using var e = source.GetEnumerator();
        return e.MoveNext() ? e.Current : default(T?);
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies a condition, or a default value.
    /// </summary>
    public static T? FirstOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : struct
        => source.Where(predicate).FirstOrNull();

    /// <summary>
    /// Returns the element at a specified index in a sequence, or a default value.
    /// </summary>
    public static T? ElementAtOrNull<T>(this IEnumerable<T> source, int index) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (index < 0) return default;

        var i = 0;
        foreach (var item in source)
        {
            if (i == index) return item;
            i++;
        }
        return default;
    }

    /// <summary>
    /// Returns the last element of a sequence, or a default value.
    /// </summary>
    public static T? LastOrNull<T>(this IEnumerable<T> source) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        // ReSharper disable once GenericEnumeratorNotDisposed
        using var e = source.GetEnumerator();
        if( !e.MoveNext()) return default;
        var last = e.Current;
        while (e.MoveNext()) last = e.Current;
        return last;
    }

    /// <summary>
    /// Returns the last element of a sequence that satisfies a condition, or a default value.
    /// </summary>
    public static T? LastOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : struct
        => source.Where(predicate).LastOrNull();

    /// <summary>
    /// Returns the single element of a sequence, or a default value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
    public static T? SingleOrNull<T>(this IEnumerable<T> source) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        // ReSharper disable once GenericEnumeratorNotDisposed
        using var e = source.GetEnumerator();
        if (!e.MoveNext()) return default;
        var single = e.Current;
        if (!e.MoveNext()) throw new InvalidOperationException(Strings.SequenceContainsMultipleElements);
        return single;
    }

    /// <summary>
    /// Returns the single element of a sequence that satisfies a condition, or a default value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
    public static T? SingleOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : struct
        => source.Where(predicate).SingleOrNull();
}
