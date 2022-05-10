namespace Linx.Expressions
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    partial class Express
    {
        /// <summary>
        /// Evaluate an expression.
        /// </summary>
        /// <exception cref="NotSupportedException">Not a constant, field, property, delegate or quoted expression.</exception>
        public static object? Eval(this Expression x)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (x.NodeType)
            {
                case ExpressionType.Constant:
                    return ((ConstantExpression)x).Value;
                case ExpressionType.MemberAccess:
                {
                    var mx = (MemberExpression)x;
                    var target = mx.Expression?.Eval();
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    return mx.Member.MemberType switch
                    {
                        MemberTypes.Field => ((FieldInfo) mx.Member).GetValue(target),
                        MemberTypes.Property => ((PropertyInfo) mx.Member).GetValue(target),
                        _ => throw new Exception($"Unexpected member type {mx.Member.MemberType}.")
                    };
                }
                case ExpressionType.Lambda:
                    return ((LambdaExpression)x).Compile();
                case ExpressionType.Quote:
                    return ((UnaryExpression)x).Operand;
                default:
                    throw new NotSupportedException("Only constant, member, delegate and lambda supported.");
            }
        }

    }
}
