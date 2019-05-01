namespace Linx.Expressions
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    partial class Reflect
    {
        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor(Expression<Action> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<TResult>(Expression<Func<TResult>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member(Expression<Action> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<TResult>(Expression<Func<TResult>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method(Expression<Action> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<TResult>(Expression<Func<TResult>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1>(Expression<Action<T1>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, TResult>(Expression<Func<T1, TResult>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1>(Expression<Action<T1>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, TResult>(Expression<Func<T1, TResult>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1>(Expression<Action<T1>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, TResult>(Expression<Func<T1, TResult>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2>(Expression<Action<T1, T2>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2>(Expression<Action<T1, T2>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2>(Expression<Action<T1, T2>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3>(Expression<Action<T1, T2, T3>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3>(Expression<Action<T1, T2, T3>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3>(Expression<Action<T1, T2, T3>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3, T4, T5, TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3, T4, T5, TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3, T4, T5, TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3, T4, T5, T6>(Expression<Action<T1, T2, T3, T4, T5, T6>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3, T4, T5, T6>(Expression<Action<T1, T2, T3, T4, T5, T6>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3, T4, T5, T6>(Expression<Action<T1, T2, T3, T4, T5, T6>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3, T4, T5, T6, T7>(Expression<Action<T1, T2, T3, T4, T5, T6, T7>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="ConstructorInfo"/>.
        /// </summary>
        public static ConstructorInfo Constructor<T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> lambda) => Constructor((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3, T4, T5, T6, T7>(Expression<Action<T1, T2, T3, T4, T5, T6, T7>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MemberInfo"/>.
        /// </summary>
        public static MemberInfo Member<T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> lambda) => Member((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3, T4, T5, T6, T7>(Expression<Action<T1, T2, T3, T4, T5, T6, T7>> lambda) => Method((LambdaExpression)lambda);

        /// <summary>
        /// Convenience function to reflect a <see cref="MethodInfo"/>.
        /// </summary>
        public static MethodInfo Method<T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> lambda) => Method((LambdaExpression)lambda);

    }
}
