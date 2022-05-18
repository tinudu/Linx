using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Linx.Expressions;

partial class Express
{
    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, Expression> _replacements;
        public ParameterReplaceVisitor(IEnumerable<(ParameterExpression p, Expression x)> replacements) => _replacements = replacements.ToDictionary(kv => kv.p, kv => kv.x);
        protected override Expression VisitParameter(ParameterExpression node) => _replacements.TryGetValue(node, out var r) ? r : node;
    }
}
