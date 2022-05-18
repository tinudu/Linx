using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Linx.Collections;

/// <summary>
/// Implementation of a <see cref="KeyedCollection{TKey,TItem}"/> with a delegate to implement <see cref="KeyedCollection{TKey,TItem}.GetKeyForItem(TItem)"/>
/// </summary>
public class LinxKeyedCollection<TKey, TItem> : KeyedCollection<TKey, TItem> where TKey : notnull
{
    private readonly Func<TItem, TKey> _keySelector;

    /// <summary>
    /// Initialize.
    /// </summary>
    public LinxKeyedCollection(Func<TItem, TKey> keySelector) => _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));

    /// <summary>
    /// Initialize.
    /// </summary>
    public LinxKeyedCollection(Func<TItem, TKey> keySelector, IEqualityComparer<TKey> comparer) : base(comparer) => _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));

    /// <summary>
    /// Initialize.
    /// </summary>
    public LinxKeyedCollection(Func<TItem, TKey> keySelector, IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold) : base(comparer, dictionaryCreationThreshold) => _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));

    /// <inheritdoc />
    protected sealed override TKey GetKeyForItem(TItem item) => _keySelector(item);

#if (NETSTANDARD2_0)
    /// <summary>
    /// Try to get an item by key.
    /// </summary>
    public bool TryGetValue(TKey key, out TItem item)
    {
        if (Dictionary != null) return Dictionary.TryGetValue(key, out item);

        foreach (var i in Items)
        {
            if (Comparer.Equals(_keySelector(i), key))
            {
                item = i;
                return true;
            }
        }
        item = default;
        return false;
    }
#endif
}
