using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Fody;

using JetBrains.Annotations;

using Mono.Cecil;

namespace AutoProperties.Fody
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    internal class SystemReferences
    {
        public SystemReferences([NotNull] BaseModuleWeaver weaver)
        {
            GetFieldFromHandle = weaver.ImportMethod(() => FieldInfo.GetFieldFromHandle(default(RuntimeFieldHandle)));
            PropertyInfoType = weaver.ImportType<PropertyInfo>();
            GetTypeFromHandle = weaver.ImportMethod(() => Type.GetTypeFromHandle(default(RuntimeTypeHandle)));
            GetPropertyInfo = weaver.TryImportMethod(() => default(Type).GetProperty(default(string), default(BindingFlags)));
        }

        [NotNull]
        public MethodReference GetTypeFromHandle { get; }

        [NotNull]
        public TypeReference PropertyInfoType { get; }

        [NotNull]
        public MethodReference GetFieldFromHandle { get; }

        [CanBeNull]
        public MethodReference GetPropertyInfo { get; }
    }

    static class ext
    {
        public static MethodReference ImportMethod<TResult>([NotNull] this BaseModuleWeaver weaver, [NotNull] Expression<Func<TResult>> expression)
        {
            GetMethodInfo(expression, out var methodName, out var declaringTypeName, out var argumentTypeNames);

            var typeDefinition = weaver.FindType(declaringTypeName);

            try
            {
                var method = typeDefinition.Methods
                    .Single(m => (m.Name == methodName + "x") && m.Parameters.Select(p => p.ParameterType.Name).SequenceEqual(argumentTypeNames));

                return weaver.ModuleDefinition.ImportReference(method);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Method {methodName} does not exist on type {declaringTypeName}", ex);
            }
        }

        public static MethodReference TryImportMethod<TResult>([NotNull] this BaseModuleWeaver weaver, [NotNull] Expression<Func<TResult>> expression)
        {
            GetMethodInfo(expression, out var methodName, out var declaringTypeName, out var argumentTypeNames);

            if (!weaver.TryFindType(declaringTypeName, out var typeDefinition))
                return null;

            var method = typeDefinition.Methods
                .FirstOrDefault(m => m.Name == methodName && m.Parameters.Select(p => p.ParameterType.Name).SequenceEqual(argumentTypeNames));

            if (method == null)
                return null;

            return weaver.ModuleDefinition.ImportReference(method);
        }

        public static TypeReference ImportType<T>([NotNull] this BaseModuleWeaver weaver)
        {
            return weaver.ModuleDefinition.ImportReference(weaver.FindType(typeof(T).Name));
        }

        public static TypeReference TryImportType<T>([NotNull] this BaseModuleWeaver weaver)
        {
            if (!weaver.TryFindType(typeof(T).Name, out var typeDefinition))
                return null;

            return weaver.ModuleDefinition.ImportReference(typeDefinition);
        }

        private static void GetMethodInfo<TResult>(Expression<Func<TResult>> expression, out string methodName, out string declaringTypeName, out string[] argumentTypeNames)
        {
            if (!(expression.Body is MethodCallExpression methodCall))
                throw new ArgumentException("Only method call expression is supported.", nameof(expression));

            methodName = methodCall.Method.Name;
            declaringTypeName = methodCall.Method.DeclaringType.Name;
            argumentTypeNames = methodCall.Arguments.Select(a => a.Type.Name).ToArray();
        }
    }
}