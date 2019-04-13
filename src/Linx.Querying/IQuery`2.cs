namespace Linx.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public interface IQuery<TContext, TSource>
    {
        Expression<Func<TContext, IEnumerable<TSource>>> Lambda { get; }
    }

    internal class Query<TContext, TSource> : IQuery<TContext, TSource>
    {
        public Expression<Func<TContext, IEnumerable<TSource>>> Lambda { get; }

        public Query(Expression<Func<TContext, IEnumerable<TSource>>> lambda) => Lambda = lambda ?? throw new ArgumentNullException(nameof(lambda));

        public override string ToString() => Lambda.ToString();
    }
}
