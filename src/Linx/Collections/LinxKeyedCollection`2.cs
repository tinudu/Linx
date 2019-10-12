namespace Linx.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Implementation of a <see cref="KeyedCollection{TKey,TItem}"/> with a delegate to implement <see cref="KeyedCollection{TKey,TItem}.GetKeyForItem(TItem)"/>
    /// </summary>
    public class LinxKeyedCollection<TKey, TItem> : KeyedCollection<TKey, TItem>
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
        protected override TKey GetKeyForItem(TItem item) => _keySelector(item);
    }
}
