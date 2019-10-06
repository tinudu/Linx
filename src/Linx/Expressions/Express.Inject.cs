namespace Linx.Expressions
{
    using System;
    using System.Linq.Expressions;

    partial class Express
    {
        /// <summary>
        /// Inject the specified expression into the body of <paramref name="lambda"/> in place of its parameter.
        /// </summary>
        /// <returns>The body of <paramref name="lambda"/> with parameter replaced.</returns>
        public static Expression Inject<T1, TResult>(this Expression<Func<T1, TResult>> lambda, Expression x1)
            => new ParameterReplaceVisitor(new[]
            {
                (lambda.Parameters[0], x1),
            }).Visit(lambda.Body);

        /// <summary>
        /// Inject the specified expressions into the body of <paramref name="lambda"/> in place of its parameters.
        /// </summary>
        /// <returns>The body of <paramref name="lambda"/> with parameters replaced.</returns>
        public static Expression Inject<T1, T2, TResult>(this Expression<Func<T1, T2, TResult>> lambda, Expression x1, Expression x2)
            => new ParameterReplaceVisitor(new[]
            {
                (lambda.Parameters[0], x1),
                (lambda.Parameters[1], x2),
            }).Visit(lambda.Body);

        /// <summary>
        /// Inject the specified expressions into the body of <paramref name="lambda"/> in place of its parameters.
        /// </summary>
        /// <returns>The body of <paramref name="lambda"/> with parameters replaced.</returns>
        public static Expression Inject<T1, T2, T3, TResult>(this Expression<Func<T1, T2, T3, TResult>> lambda, Expression x1, Expression x2, Expression x3)
            => new ParameterReplaceVisitor(new[]
            {
                (lambda.Parameters[0], x1),
                (lambda.Parameters[1], x2),
                (lambda.Parameters[2], x3),
            }).Visit(lambda.Body);

        /// <summary>
        /// Inject the specified expressions into the body of <paramref name="lambda"/> in place of its parameters.
        /// </summary>
        /// <returns>The body of <paramref name="lambda"/> with parameters replaced.</returns>
        public static Expression Inject<T1, T2, T3, T4, TResult>(this Expression<Func<T1, T2, T3, T4, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4)
            => new ParameterReplaceVisitor(new[]
            {
                (lambda.Parameters[0], x1),
                (lambda.Parameters[1], x2),
                (lambda.Parameters[2], x3),
                (lambda.Parameters[3], x4),
            }).Visit(lambda.Body);

        /// <summary>
        /// Inject the specified expressions into the body of <paramref name="lambda"/> in place of its parameters.
        /// </summary>
        /// <returns>The body of <paramref name="lambda"/> with parameters replaced.</returns>
        public static Expression Inject<T1, T2, T3, T4, T5, TResult>(this Expression<Func<T1, T2, T3, T4, T5, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4, Expression x5)
            => new ParameterReplaceVisitor(new[]
            {
                (lambda.Parameters[0], x1),
                (lambda.Parameters[1], x2),
                (lambda.Parameters[2], x3),
                (lambda.Parameters[3], x4),
                (lambda.Parameters[4], x5),
            }).Visit(lambda.Body);

        /// <summary>
        /// Inject the specified expressions into the body of <paramref name="lambda"/> in place of its parameters.
        /// </summary>
        /// <returns>The body of <paramref name="lambda"/> with parameters replaced.</returns>
        public static Expression Inject<T1, T2, T3, T4, T5, T6, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4, Expression x5, Expression x6)
            => new ParameterReplaceVisitor(new[]
            {
                (lambda.Parameters[0], x1),
                (lambda.Parameters[1], x2),
                (lambda.Parameters[2], x3),
                (lambda.Parameters[3], x4),
                (lambda.Parameters[4], x5),
                (lambda.Parameters[5], x6),
            }).Visit(lambda.Body);

        /// <summary>
        /// Inject the specified expressions into the body of <paramref name="lambda"/> in place of its parameters.
        /// </summary>
        /// <returns>The body of <paramref name="lambda"/> with parameters replaced.</returns>
        public static Expression Inject<T1, T2, T3, T4, T5, T6, T7, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4, Expression x5, Expression x6, Expression x7)
            => new ParameterReplaceVisitor(new[]
            {
                (lambda.Parameters[0], x1),
                (lambda.Parameters[1], x2),
                (lambda.Parameters[2], x3),
                (lambda.Parameters[3], x4),
                (lambda.Parameters[4], x5),
                (lambda.Parameters[5], x6),
                (lambda.Parameters[6], x7),
            }).Visit(lambda.Body);

        /// <summary>
        /// Inject the specified expressions into the body of <paramref name="lambda"/> in place of its parameters.
        /// </summary>
        /// <returns>The body of <paramref name="lambda"/> with parameters replaced.</returns>
        public static Expression Inject<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> lambda, Expression x1, Expression x2, Expression x3, Expression x4, Expression x5, Expression x6, Expression x7, Expression x8)
            => new ParameterReplaceVisitor(new[]
            {
                (lambda.Parameters[0], x1),
                (lambda.Parameters[1], x2),
                (lambda.Parameters[2], x3),
                (lambda.Parameters[3], x4),
                (lambda.Parameters[4], x5),
                (lambda.Parameters[5], x6),
                (lambda.Parameters[6], x7),
                (lambda.Parameters[7], x8),
            }).Visit(lambda.Body);

    }
}