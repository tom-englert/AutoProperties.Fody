using System;
using System.IO;
using System.Reflection;

using JetBrains.Annotations;

namespace Tests
{
    public static class ExtensionMethods
    {
        [NotNull]
        public static dynamic GetInstance([NotNull] this Assembly assembly, [NotNull] string className, [NotNull, ItemNotNull] params object[] args)
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                var type = assembly.GetType(className, true);

                // ReSharper disable AssignNullToNotNullAttribute
                return Activator.CreateInstance(type, args);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        [CanBeNull]
        private static Assembly CurrentDomain_AssemblyResolve([NotNull] object sender, [NotNull] ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name);
            var location = Path.GetDirectoryName(args.RequestingAssembly.Location);
            var fullName = Path.Combine(location, name.Name + ".dll");

            if (File.Exists(fullName))
                return Assembly.LoadFrom(fullName);
            
            return null;
        }
    }
}
