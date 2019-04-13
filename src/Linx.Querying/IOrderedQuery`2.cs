namespace Linx.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public interface IOrderedQuery<TContext, TSource> : IQuery<TContext, TSource>
    {
        new Expression<Func<TContext, IOrderedEnumerable<TSource>>> Lambda { get; }
    }

    internal sealed class OrderedQuery<TContext, TSource> : Query<TContext, TSource>, IOrderedQuery<TContext, TSource>
    {
        public new Expression<Func<TContext, IOrderedEnumerable<TSource>>> Lambda { get; }

        public OrderedQuery(Expression<Func<TContext, IOrderedEnumerable<TSource>>> lambda) : base(Expression.Lambda<Func<TContext, IEnumerable<TSource>>>(lambda.Body, lambda.Parameters)) => Lambda = lambda;
    }
}
