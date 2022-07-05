using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TResult>(this
        IAsyncEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        Func<TAggregate1, TAggregate2, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TResult>(this
        IEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        Func<TAggregate1, TAggregate2, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>(this
        IAsyncEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        Func<TAggregate1, TAggregate2, TAggregate3, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>(this
        IEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        Func<TAggregate1, TAggregate2, TAggregate3, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>(this
        IAsyncEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>(this
        IEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>(this
        IAsyncEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        AggregatorDelegate<TSource, TAggregate5> aggregator5,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>(this
        IEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        AggregatorDelegate<TSource, TAggregate5> aggregator5,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>(this
        IAsyncEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        AggregatorDelegate<TSource, TAggregate5> aggregator5,
        AggregatorDelegate<TSource, TAggregate6> aggregator6,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>(this
        IEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        AggregatorDelegate<TSource, TAggregate5> aggregator5,
        AggregatorDelegate<TSource, TAggregate6> aggregator6,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>(this
        IAsyncEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        AggregatorDelegate<TSource, TAggregate5> aggregator5,
        AggregatorDelegate<TSource, TAggregate6> aggregator6,
        AggregatorDelegate<TSource, TAggregate7> aggregator7,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>(this
        IEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        AggregatorDelegate<TSource, TAggregate5> aggregator5,
        AggregatorDelegate<TSource, TAggregate6> aggregator6,
        AggregatorDelegate<TSource, TAggregate7> aggregator7,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>(this
        IAsyncEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        AggregatorDelegate<TSource, TAggregate5> aggregator5,
        AggregatorDelegate<TSource, TAggregate6> aggregator6,
        AggregatorDelegate<TSource, TAggregate7> aggregator7,
        AggregatorDelegate<TSource, TAggregate8> aggregator8,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, aggregator8, resultSelector, token);

    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>(this
        IEnumerable<TSource> source,
        AggregatorDelegate<TSource, TAggregate1> aggregator1,
        AggregatorDelegate<TSource, TAggregate2> aggregator2,
        AggregatorDelegate<TSource, TAggregate3> aggregator3,
        AggregatorDelegate<TSource, TAggregate4> aggregator4,
        AggregatorDelegate<TSource, TAggregate5> aggregator5,
        AggregatorDelegate<TSource, TAggregate6> aggregator6,
        AggregatorDelegate<TSource, TAggregate7> aggregator7,
        AggregatorDelegate<TSource, TAggregate8> aggregator8,
        Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult> resultSelector,
        CancellationToken token)
        => source.Cold().MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, aggregator8, resultSelector, token);

}