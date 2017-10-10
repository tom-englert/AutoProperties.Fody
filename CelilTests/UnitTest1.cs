using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Rocks;

using NUnit.Framework;

namespace CelilTests
{
    public class GenericsTests
    {
        [Test]
        public void GenericTest()
        {
            var module = ModuleDefinition.ReadModule(typeof(GenericsTests).Assembly.Location);
            var method = module.ImportReference(module.GetType(typeof(One<>).FullName).GetMethods().Single(m => m.Name == "Method"));

            foreach (var type in module.GetTypes().Where(t => t.IsClass && t.BaseType != null))
            {
                var expected = type.CustomAttributes.Select(ca => ca.ConstructorArguments.FirstOrDefault().Value as string).FirstOrDefault();
                if (expected != null)
                {
                    TestContext.Out.WriteLine(type);
                    TestContext.Out.Flush();

                    var methodReference = method.MakeGeneric(type);
                    Assert.AreEqual(expected, methodReference.ToString());
                }
            }
        }

    }

    static class ExtensionMethods
    {
        public static TypeReference MakeGeneric(this TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException();

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        public static MethodReference MakeGeneric(this MethodReference self, TypeReference callingType)
        {
            var calleeType = self.DeclaringType.Resolve();

            if (callingType.Resolve() == calleeType)
                return self;

            TypeReference baseType = callingType;

            IList<TypeReference> genericArguments = callingType.GenericParameters.OfType<TypeReference>().ToArray();
            IList<GenericParameter> genericParameters = callingType.GenericParameters.ToArray();

            while (true)
            {
                baseType = baseType.Resolve().BaseType;
                if (baseType == null)
                    return null;

                if (baseType.IsGenericInstance)
                {
                    var args = ((GenericInstanceType)baseType).GenericArguments.ToArray();

                    if (genericParameters != null)
                    {
                        for (int i = 0; i < args.Count(); i++)
                        {
                            if (args[i].ContainsGenericParameter)
                            {
                                for (int k = 0; k < genericParameters.Count; k++)
                                {
                                    if (genericParameters[k].Name == args[i].Name)
                                    {
                                        args[i] = genericArguments[k];
                                    }
                                }
                            }
                        }
                    }

                    genericArguments = args;
                    genericParameters = baseType.GetElementType().GenericParameters.ToArray();
                }

                if (baseType.Resolve() == calleeType)
                    break;
            }

            if (!baseType.IsGenericInstance)
                return self;

            var reference = new MethodReference(self.Name, self.ReturnType)
            {
                DeclaringType = self.DeclaringType.MakeGeneric(genericArguments.ToArray()),
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var generic_parameter in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

            return reference;
        }
    }

    [Description("System.Void CelilTests.One`1::Method()")]
    class One<T>
    {
        public void Method()
        {
        }
    }

    [Description("System.Void CelilTests.One`1<T1>::Method()")]
    class Two<T1> : One<T1>
    {
    }

    [Description("System.Void CelilTests.One`1<System.String>::Method()")]
    class Three : One<string>
    {
    }

    [Description("System.Void CelilTests.One`1<T3>::Method()")]
    class Four<T2, T3> : Two<T3>
    {
    }

    [Description("System.Void CelilTests.One`1<T2>::Method()")]
    class Five<T2, T3> : Two<T2>
    {
    }

    [Description("System.Void CelilTests.One`1<System.Int32>::Method()")]
    class Six : Five<int, string>
    {
    }

    [Description("System.Void CelilTests.One`1<System.String>::Method()")]
    class Seven : Four<int, string>
    {
    }
}
