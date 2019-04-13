namespace Linx.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public interface IQueryProvider<TContext>
    {
        Task<TResult> ExecuteAsync<TResult>(Expression<Func<TContext, TResult>> lambda);
    }
}
