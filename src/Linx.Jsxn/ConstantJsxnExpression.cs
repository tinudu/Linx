namespace Linx.Jsxn
{
    using System.Linq.Expressions;
    using TypeSystem;

    /// <summary>
    /// A constant expression.
    /// </summary>
    public sealed class ConstantJsxnExpression : JsxnExpression
    {
        /// <summary>
        /// <see cref="ExpressionType.Constant"/>.
        /// </summary>
        public override ExpressionType NodeType => ExpressionType.Constant;

        /// <inheritdoc />
        public override JsxnType Type { get; }

        private ConstantJsxnExpression(JsxnType type)
        {
            Type = type;
        }
    }
}
