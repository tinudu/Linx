﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

/// <summary>
/// Function to yield sequence elements to an <see cref="IAsyncEnumerator{T}"/>
/// </summary>
/// <returns>true if further elements are requested. false otherwise.</returns>
public delegate ValueTask<bool> YieldAsyncDelegate<in T>(T value);

/// <summary>
/// Coroutine to generate a sequence.
/// </summary>
/// <param name="yield"><see cref="YieldAsyncDelegate{T}"/> to which to yield sequence elements.</param>
/// <param name="token">Token on which cancellation is requested.</param>
/// <returns>A task that, when completed, notifies the end of the sequence.</returns>
public delegate Task ProduceAsyncDelegate<out T>(YieldAsyncDelegate<T> yield, CancellationToken token);

/// <summary>
/// Delegate to produce an aggregate from a sequence.
/// </summary>
public delegate ValueTask<TAggregate> AggregatorDelegate<in TSource, TAggregate>(IAsyncEnumerable<TSource> source, CancellationToken token);

/// <summary>
/// Delegate to consume a sequence.
/// </summary>
public delegate ValueTask ConsumerDelegate<in TSource>(IAsyncEnumerable<TSource> source, CancellationToken token);
