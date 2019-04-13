namespace Linx.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class Express
    {
        public static object Eval(this Expression x)
        {
            switch (x.NodeType)
            {
                case ExpressionType.Constant:
                    return ((ConstantExpression)x).Value;
                case ExpressionType.MemberAccess:
                    {
                        var mx = (MemberExpression)x;
                        var target = mx.Expression?.Eval();
                        switch (mx.Member.MemberType)
                        {
                            case MemberTypes.Field:
                                return ((FieldInfo)mx.Member).GetValue(target);
                            case MemberTypes.Property:
                                return ((PropertyInfo)mx.Member).GetValue(target);
                            default:
                                throw new ArgumentOutOfRangeException(nameof(mx.Member.MemberType));
                        }
                    }
                case ExpressionType.Lambda:
                    return ((LambdaExpression)x).Compile();
                case ExpressionType.Quote:
                    return ((UnaryExpression)x).Operand;
                default:
                    throw new NotSupportedException("Only constant, member, delegate and lambda supported.");
            }
        }

        public static Expression<Func<TResult>> FuncX<TResult>(Expression<Func<TResult>> func) => func;
        public static Expression<Func<T, TResult>> FuncX<T, TResult>(Expression<Func<T, TResult>> func) => func;
        public static Expression<Func<T1, T2, TResult>> FuncX<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> func) => func;
        public static Expression<Func<T1, T2, T3, TResult>> FuncX<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> func) => func;
        public static Expression<Func<T1, T2, T3, T4, TResult>> FuncX<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> func) => func;
        public static Expression<Func<T1, T2, T3, T4, T5, TResult>> FuncX<T1, T2, T3, T4, T5, TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> func) => func;
        public static Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> FuncX<T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> func) => func;
        public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> FuncX<T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> func) => func;
        public static Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> FuncX<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> func) => func;

        public static Expression<Func<T1, T3>> Chain<T1, T2, T3>(this Expression<Func<T1, T2>> x1, Expression<Func<T2, T3>> x2)
        {
            var body = new ParameterReplaceVisitor(new[] { new KeyValuePair<ParameterExpression, Expression>(x2.Parameters[0], x1.Body) }).Visit(x2.Body);
            return Expression.Lambda<Func<T1, T3>>(body, x1.Parameters);
        }

        #region Inject

        public static Expression Inject<T, TResult>(this Expression<Func<T, TResult>> lambda, Expression x) => new ParameterReplaceVisitor(new[] { new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[0], x) }).Visit(lambda.Body);

        public static Expression Inject<T1, T2, TResult>(this Expression<Func<T1, T2, TResult>> lambda, Expression x1, Expression x2)
            => new ParameterReplaceVisitor(new[]
            {
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[0], x1),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[1], x2)
            }).Visit(lambda.Body);

        public static Expression Inject<T1, T2, T3, TResult>(this Expression<Func<T1, T2, T3, TResult>> lambda, Expression x1, Expression x2, Expression x3)
            => new ParameterReplaceVisitor(new[]
            {
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[0], x1),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[1], x2),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[2], x3)
            }).Visit(lambda.Body);

        public static Expression Inject<T1, T2, T3, T4, TResult>(this Expression<Func<T1, T2, T3, T4, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4)
            => new ParameterReplaceVisitor(new[]
            {
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[0], x1),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[1], x2),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[2], x3),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[3], x4)
            }).Visit(lambda.Body);

        public static Expression Inject<T1, T2, T3, T4, T5, TResult>(this Expression<Func<T1, T2, T3, T4, T5, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4, Expression x5)
            => new ParameterReplaceVisitor(new[]
            {
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[0], x1),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[1], x2),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[2], x3),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[3], x4),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[4], x5)
            }).Visit(lambda.Body);

        public static Expression Inject<T1, T2, T3, T4, T5, T6, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4, Expression x5, Expression x6)
            => new ParameterReplaceVisitor(new[]
            {
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[0], x1),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[1], x2),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[2], x3),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[3], x4),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[4], x5),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[5], x6)
            }).Visit(lambda.Body);

        public static Expression Inject<T1, T2, T3, T4, T5, T6, T7, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4, Expression x5, Expression x6, Expression x7)
            => new ParameterReplaceVisitor(new[]
            {
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[0], x1),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[1], x2),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[2], x3),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[3], x4),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[4], x5),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[5], x6),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[6], x7)
            }).Visit(lambda.Body);

        public static Expression Inject<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4, Expression x5, Expression x6, Expression x7, Expression x8)
            => new ParameterReplaceVisitor(new[]
            {
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[0], x1),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[1], x2),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[2], x3),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[3], x4),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[4], x5),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[5], x6),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[6], x7),
                new KeyValuePair<ParameterExpression, Expression>(lambda.Parameters[7], x8)
            }).Visit(lambda.Body);

        #endregion

        public static MethodInfo Method<TResult>(Expression<Func<TResult>> lambda) => Method((LambdaExpression)lambda);
        public static MethodInfo Method<T, TResult>(Expression<Func<T, TResult>> lambda) => Method((LambdaExpression)lambda);
        public static MethodInfo Method<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> lambda) => Method((LambdaExpression)lambda);
        public static MethodInfo Method<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> lambda) => Method((LambdaExpression)lambda);
        public static MethodInfo Method<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> lambda) => Method((LambdaExpression)lambda);
        public static MethodInfo Method<T1, T2, T3, T4, T5, TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> lambda) => Method((LambdaExpression)lambda);
        public static MethodInfo Method<T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> lambda) => Method((LambdaExpression)lambda);
        public static MethodInfo Method<T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> lambda) => Method((LambdaExpression)lambda);
        public static MethodInfo Method<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> lambda) => Method((LambdaExpression)lambda);
        private static MethodInfo Method(LambdaExpression lambda) => ((MethodCallExpression)lambda.Body).Method;

        private sealed class ParameterReplaceVisitor : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, Expression> _replacements;
            public ParameterReplaceVisitor(IEnumerable<KeyValuePair<ParameterExpression, Expression>> replacements) => _replacements = replacements.ToDictionary(kv => kv.Key, kv => kv.Value);
            protected override Expression VisitParameter(ParameterExpression node) => _replacements.TryGetValue(node, out var r) ? r : node;
        }
    }
}
