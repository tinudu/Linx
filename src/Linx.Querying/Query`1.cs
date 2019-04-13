namespace Linx.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public static class Query<TContext>
    {
        public static IQuery<TContext, TSource> Create<TSource>(Expression<Func<TContext, IEnumerable<TSource>>> lambda) => new Query<TContext, TSource>(lambda);
        public static IOrderedQuery<TContext, TSource> Create<TSource>(Expression<Func<TContext, IOrderedEnumerable<TSource>>> lambda) => new OrderedQuery<TContext, TSource>(lambda);
    }
}
