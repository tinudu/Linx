namespace Linx.Jsxn.TypeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Collections;
    using Enumerable;

    /// <summary>
    /// A function type.
    /// </summary>
    public sealed class FunctionType : NonNullableType
    {
        private static readonly Dictionary<IEnumerable<JsxnType>, FunctionType> _instances = new Dictionary<IEnumerable<JsxnType>, FunctionType>(SequenceComparer<JsxnType>.Default);

        /// <summary>
        /// Gets the <see cref="FunctionType"/> with the specified signature.
        /// </summary>
        public static FunctionType GetType(IEnumerable<JsxnType> argumentTypes, JsxnType returnType)
        {
            if (argumentTypes == null) throw new ArgumentNullException(nameof(argumentTypes));
            var argumentTypesRo = argumentTypes.Select(t => t ?? throw new ArgumentException("Null element.")).ToList().AsReadOnly();
            var signature = new JsxnType[argumentTypesRo.Count + 1];
            argumentTypesRo.CopyTo(signature, 0);
            signature[argumentTypesRo.Count] = returnType ?? throw new ArgumentNullException(nameof(returnType));
            lock (_instances)
            {
                if (_instances.TryGetValue(signature, out var result)) return result;
                result = new FunctionType(argumentTypesRo, returnType);
                _instances.Add(signature, result);
                return result;
            }
        }

        /// <summary>
        /// Gets the argument types.
        /// </summary>
        public IReadOnlyList<JsxnType> ArgumentTypes { get; }

        /// <summary>
        /// Gets the return type.
        /// </summary>
        public JsxnType ReturnType { get; }

        private FunctionType(IReadOnlyList<JsxnType> argumentTypes, JsxnType returnType)
        {
            ArgumentTypes = argumentTypes;
            ReturnType = returnType;
        }

        /// <inheritdoc />
        public override string ToString() => $"({string.Join(", ", ArgumentTypes)}) => {ReturnType}";
    }
}