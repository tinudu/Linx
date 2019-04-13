namespace Linx.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class QueryProvider
    {
        public static async Task<IList<TSource>> ExecuteAsync<TContext, TSource>(this IQueryProvider<TContext> provider, IQuery<TContext, TSource> query)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (query == null) throw new ArgumentNullException(nameof(query));

            var result = await provider.ExecuteAsync(query.Lambda);
            return result as IList<TSource> ?? result.ToList();
        }
    }
}
