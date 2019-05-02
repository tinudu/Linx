﻿namespace Linx.AsyncEnumerable.Subjects
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// A subject.
    /// </summary>
    public interface ISubject<in TSource, out TResult>
    {
        /// <summary>
        /// The output sequence.
        /// </summary>
        IAsyncEnumerable<TResult> Output { get; }

        /// <summary>
        /// Subscribe to the specified input source.
        /// </summary>
        /// <returns>A task that completes when all subscribers are unsubscribed.</returns>
        /// <exception cref="System.InvalidOperationException">Already subscribed.</exception>
        Task SubscribeTo(IAsyncEnumerable<TSource> input);
    }
}