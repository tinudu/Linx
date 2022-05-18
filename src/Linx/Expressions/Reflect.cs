using System.Linq.Expressions;
using System.Reflection;

namespace Linx.Expressions;

/// <summary>
/// Reflection utility.
/// </summary>
public static partial class Reflect
{
    private static ConstructorInfo? Constructor(LambdaExpression lambda) => ((NewExpression)lambda.Body).Constructor;
    private static MemberInfo Member(LambdaExpression lambda) => ((MemberExpression)lambda.Body).Member;
    private static MethodInfo Method(LambdaExpression lambda) => ((MethodCallExpression)lambda.Body).Method;
}
