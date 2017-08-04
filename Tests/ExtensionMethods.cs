#pragma warning disable CCRSI_ContractForNotNull // Element with [NotNull] attribute does not have a corresponding not-null contract.
#pragma warning disable CCRSI_CreateContractInvariantMethod // Missing Contract Invariant Method.

using System;
using System.Reflection;

using JetBrains.Annotations;

namespace Tests
{
    public static class ExtensionMethods
    {
        [NotNull]
        public static dynamic GetInstance([NotNull] this Assembly assembly, [NotNull] string className, [NotNull] params object[] args)
        {
            var type = assembly.GetType(className, true);

            // ReSharper disable AssignNullToNotNullAttribute
            return Activator.CreateInstance(type, args);
        }
    }
}
