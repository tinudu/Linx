﻿namespace Linx.Reactive
{
    using System.Linq;

    partial class LinxReactive
    {
        /// <summary>
        /// Generates an observable sequence of integral numbers within a specified range.
        /// </summary>
        public static IAsyncEnumerable<int> Range(int start, int count) => Enumerable.Range(start, count).Async();
    }
}
