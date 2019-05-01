namespace Linx.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Expressions;

    public static class Query
    {
        private static readonly MethodInfo _miAll = Reflect.Method((IEnumerable<int> xs) => xs.All(x => false)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miAny = Reflect.Method((IEnumerable<int> xs) => xs.Any()).GetGenericMethodDefinition();
        private static readonly MethodInfo _miAny1 = Reflect.Method((IEnumerable<int> xs) => xs.Any(x => false)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miConcat = Reflect.Method((IEnumerable<int> xs) => xs.Concat(new int[0])).GetGenericMethodDefinition();
        private static readonly MethodInfo _miGroupByK = Reflect.Method((IEnumerable<int> xs) => xs.GroupBy(x => 0)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miGroupByKe = Reflect.Method((IEnumerable<int> xs) => xs.GroupBy(x => 0, x => 0)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miGroupByKr = Reflect.Method((IEnumerable<int> xs) => xs.GroupBy(x => 0, (k, vs) => 0)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miGroupByKer = Reflect.Method((IEnumerable<int> xs) => xs.GroupBy(x => 0, x => 0, (k, vs) => 0)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miOrderBy = Reflect.Method((IEnumerable<int> xs) => xs.OrderBy(x => x)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miOrderByDescending = Reflect.Method((IEnumerable<int> xs) => xs.OrderByDescending(x => x)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miSelect = Reflect.Method((IEnumerable<int> xs) => xs.Select(x => 0)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miSelect1 = Reflect.Method((IEnumerable<int> xs) => xs.Select((x, i) => 0)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miThenBy = Reflect.Method((IOrderedEnumerable<int> xs) => xs.ThenBy(x => x)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miThenByDescending = Reflect.Method((IOrderedEnumerable<int> xs) => xs.ThenByDescending(x => x)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miWhere = Reflect.Method((IEnumerable<int> xs) => xs.Where(x => false)).GetGenericMethodDefinition();
        private static readonly MethodInfo _miWhere1 = Reflect.Method((IEnumerable<int> xs) => xs.Where((x, i) => false)).GetGenericMethodDefinition();

        public static Expression<Func<TContext, bool>> All<TContext, TSource>(this IQuery<TContext, TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return Expression.Lambda<Func<TContext, bool>>(Expression.Call(_miAll.MakeGenericMethod(typeof(TSource)), source.Lambda.Body, predicate), source.Lambda.Parameters);
        }

        public static Expression<Func<TContext, bool>> Any<TContext, TSource>(this IQuery<TContext, TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Expression.Lambda<Func<TContext, bool>>(Expression.Call(_miAny.MakeGenericMethod(typeof(TSource)), source.Lambda.Body), source.Lambda.Parameters);
        }

        public static Expression<Func<TContext, bool>> Any<TContext, TSource>(this IQuery<TContext, TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return Expression.Lambda<Func<TContext, bool>>(Expression.Call(_miAny1.MakeGenericMethod(typeof(TSource)), source.Lambda.Body, predicate), source.Lambda.Parameters);
        }

        public static IQuery<TContext, TSource> Concat<TContext, TSource>(this IQuery<TContext, TSource> source1, IQuery<TContext, TSource> source2)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            var lambda = Expression.Lambda<Func<TContext, IEnumerable<TSource>>>(Expression.Call(_miConcat.MakeGenericMethod(typeof(TSource)), source1.Lambda.Body, source2.Lambda.Inject(source1.Lambda.Parameters[0])), source1.Lambda.Parameters);
            return new Query<TContext, TSource>(lambda);
        }

        public static IQuery<TContext, IGrouping<TKey, TSource>> GroupBy<TContext, TSource, TKey>(this IQuery<TContext, TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            var lambda = Expression.Lambda<Func<TContext, IEnumerable<IGrouping<TKey, TSource>>>>(Expression.Call(_miGroupByK.MakeGenericMethod(typeof(TSource), typeof(TKey)), source.Lambda.Body, keySelector), source.Lambda.Parameters);
            return new Query<TContext, IGrouping<TKey, TSource>>(lambda);
        }

        public static IQuery<TContext, IGrouping<TKey, TElement>> GroupBy<TContext, TSource, TKey, TElement>(this IQuery<TContext, TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));
            var lambda = Expression.Lambda<Func<TContext, IEnumerable<IGrouping<TKey, TElement>>>>(Expression.Call(_miGroupByKe.MakeGenericMethod(typeof(TSource), typeof(TKey), typeof(TElement)), source.Lambda.Body, keySelector, elementSelector), source.Lambda.Parameters);
            return new Query<TContext, IGrouping<TKey, TElement>>(lambda);
        }

        public static IQuery<TContext, TResult> GroupBy<TContext, TSource, TKey, TResult>(this IQuery<TContext, TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            var lambda = Expression.Lambda<Func<TContext, IEnumerable<TResult>>>(Expression.Call(_miGroupByKr.MakeGenericMethod(typeof(TSource), typeof(TKey), typeof(TResult)), source.Lambda.Body, keySelector, resultSelector), source.Lambda.Parameters);
            return new Query<TContext, TResult>(lambda);
        }

        public static IQuery<TContext, TResult> GroupBy<TContext, TSource, TKey, TElement, TResult>(this IQuery<TContext, TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            var lambda = Expression.Lambda<Func<TContext, IEnumerable<TResult>>>(Expression.Call(_miGroupByKer.MakeGenericMethod(typeof(TSource), typeof(TKey), typeof(TElement), typeof(TResult)), source.Lambda.Body, keySelector, elementSelector, resultSelector), source.Lambda.Parameters);
            return new Query<TContext, TResult>(lambda);
        }

        public static IOrderedQuery<TContext, TSource> OrderBy<TContext, TSource, TKey>(this IQuery<TContext, TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            var lambda = Expression.Lambda<Func<TContext, IOrderedEnumerable<TSource>>>(Expression.Call(_miOrderBy.MakeGenericMethod(typeof(TSource), typeof(TKey)), source.Lambda.Body, keySelector), source.Lambda.Parameters);
            return new OrderedQuery<TContext, TSource>(lambda);
        }

        public static IOrderedQuery<TContext, TSource> OrderByDescending<TContext, TSource, TKey>(this IQuery<TContext, TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            var lambda = Expression.Lambda<Func<TContext, IOrderedEnumerable<TSource>>>(Expression.Call(_miOrderByDescending.MakeGenericMethod(typeof(TSource), typeof(TKey)), source.Lambda.Body, keySelector), source.Lambda.Parameters);
            return new OrderedQuery<TContext, TSource>(lambda);
        }

        public static IQuery<TContext, TResult> Select<TContext, TSource, TResult>(this IQuery<TContext, TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            var lamba = Expression.Lambda<Func<TContext, IEnumerable<TResult>>>(Expression.Call(_miSelect.MakeGenericMethod(typeof(TSource), typeof(TResult)), source.Lambda.Body, selector), source.Lambda.Parameters);
            return new Query<TContext, TResult>(lamba);
        }

        public static IQuery<TContext, TResult> Select<TContext, TSource, TResult>(this IQuery<TContext, TSource> source, Expression<Func<TSource, int, TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            var lamba = Expression.Lambda<Func<TContext, IEnumerable<TResult>>>(Expression.Call(_miSelect1.MakeGenericMethod(typeof(TSource), typeof(TResult)), source.Lambda.Body, selector), source.Lambda.Parameters);
            return new Query<TContext, TResult>(lamba);
        }

        public static IOrderedQuery<TContext, TSource> ThenBy<TContext, TSource, TKey>(this IOrderedQuery<TContext, TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            var lambda = Expression.Lambda<Func<TContext, IOrderedEnumerable<TSource>>>(Expression.Call(_miThenBy.MakeGenericMethod(typeof(TSource), typeof(TKey)), source.Lambda.Body, keySelector), source.Lambda.Parameters);
            return new OrderedQuery<TContext, TSource>(lambda);
        }

        public static IOrderedQuery<TContext, TSource> ThenByDescending<TContext, TSource, TKey>(this IOrderedQuery<TContext, TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            var lambda = Expression.Lambda<Func<TContext, IOrderedEnumerable<TSource>>>(Expression.Call(_miThenByDescending.MakeGenericMethod(typeof(TSource), typeof(TKey)), source.Lambda.Body, keySelector), source.Lambda.Parameters);
            return new OrderedQuery<TContext, TSource>(lambda);
        }

        public static IQuery<TContext, TSource> Where<TContext, TSource>(this IQuery<TContext, TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var lambda = Expression.Lambda<Func<TContext, IEnumerable<TSource>>>(Expression.Call(_miWhere.MakeGenericMethod(typeof(TSource)), source.Lambda.Body, predicate), source.Lambda.Parameters);
            return new Query<TContext, TSource>(lambda);
        }

        public static IQuery<TContext, TSource> Where<TContext, TSource>(this IQuery<TContext, TSource> source, Expression<Func<TSource, int, bool>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var lambda = Expression.Lambda<Func<TContext, IEnumerable<TSource>>>(Expression.Call(_miWhere1.MakeGenericMethod(typeof(TSource)), source.Lambda.Body, predicate), source.Lambda.Parameters);
            return new Query<TContext, TSource>(lambda);
        }
    }
}
