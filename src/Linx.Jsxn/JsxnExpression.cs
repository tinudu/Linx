namespace Linx.Jsxn
{
    using System.Linq.Expressions;
    using TypeSystem;

    /// <summary>
    /// Base class for a Jsxn expression.
    /// </summary>
    public abstract class JsxnExpression
    {
        /// <summary>
        /// Gets the node type.
        /// </summary>
        public abstract ExpressionType NodeType { get; }

        /// <summary>
        /// Gets the <see cref="JsxnType"/>.
        /// </summary>
        public abstract JsxnType Type { get; }

        internal JsxnExpression() { }
    }
}
