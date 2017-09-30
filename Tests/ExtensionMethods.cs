using System;
using System.Reflection;

using JetBrains.Annotations;

namespace Tests
{
    public static class ExtensionMethods
    {
        [NotNull]
        public static dynamic GetInstance([NotNull] this Assembly assembly, [NotNull] string className, [NotNull, ItemNotNull] params object[] args)
        {
            var type = assembly.GetType(className, true);

            // ReSharper disable AssignNullToNotNullAttribute
            return Activator.CreateInstance(type, args);
        }
    }
}
