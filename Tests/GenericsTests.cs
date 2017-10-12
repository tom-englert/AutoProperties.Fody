using System;
using System.Collections.Generic;
using System.Linq;

using AutoProperties.Fody;

using Mono.Cecil;
using Mono.Cecil.Rocks;

using NUnit.Framework;

namespace CecilTests
{
    public class GenericsTests
    {
        [Test]
        public void GenericTest()
        {
            var module = ModuleDefinition.ReadModule(typeof(GenericsTests).Assembly.Location);

            foreach (var type in module.GetTypes().Where(t => t.IsClass && t.BaseType != null))
            {
                var expected = type.CustomAttributes.Select(ca => ca.ConstructorArguments.FirstOrDefault().Value as string).FirstOrDefault();
                if (expected != null)
                {
                    TestContext.Out.WriteLine(type);
                    TestContext.Out.Flush();

                    var method = module.ImportReference(type.GetSelfAndBaseTypes().Select(t => t.GetMethods().FirstOrDefault(m => m.Name == "Method")).FirstOrDefault(m => m != null));

                    var methodReference = method.GetReference(type);
                    Assert.AreEqual(expected, methodReference.ToString());
                }
            }
        }

    }

    namespace Set1
    {
        [Description("System.Void CecilTests.Set1.Zero::Method()")]
        class Zero
        {
            public void Method()
            {
            }
        }


        [Description("System.Void CecilTests.Set1.One`1<T>::Method()")]
        class One<T>
        {
            public void Method()
            {
            }
        }

        [Description("System.Void CecilTests.Set1.One`1<T1>::Method()")]
        class Two<T1> : One<T1>
        {
        }

        [Description("System.Void CecilTests.Set1.One`1<System.String>::Method()")]
        class Three : One<string>
        {
        }

        [Description("System.Void CecilTests.Set1.One`1<T3>::Method()")]
        class Four<T2, T3> : Two<T3>
        {
        }

        [Description("System.Void CecilTests.Set1.One`1<T2>::Method()")]
        class Five<T2, T3> : Two<T2>
        {
        }

        [Description("System.Void CecilTests.Set1.One`1<System.Int32>::Method()")]
        class Six : Five<int, string>
        {
        }

        [Description("System.Void CecilTests.Set1.One`1<System.String>::Method()")]
        class Seven : Four<int, string>
        {
        }

        [Description("System.Void CecilTests.Set1.One`1<System.Int32>::Method()")]
        class Eight<T> : Six
        {
        }

        [Description("System.Void CecilTests.Set1.One`1<System.String>::Method()")]
        class Nine<T> : Seven
        {
        }

        [Description("System.Void CecilTests.Set1.One`1<System.String>::Method()")]
        class Ten<Q> : Nine<Q>
        {
        }


    }

    namespace Set2
    {
        [Description("System.Void CecilTests.Set2.One`1<T>::Method(M)")]
        class One<T>
        {
            public void Method<M>(M param)
            {
            }
        }

        [Description("System.Void CecilTests.Set2.One`1<T1>::Method(M)")]
        class Two<T1> : One<T1>
        {
        }

        [Description("System.Void CecilTests.Set2.One`1<System.String>::Method(M)")]
        class Three : One<string>
        {
        }

        [Description("System.Void CecilTests.Set2.One`1<T3>::Method(M)")]
        class Four<T2, T3> : Two<T3>
        {
        }

        [Description("System.Void CecilTests.Set2.One`1<T2>::Method(M)")]
        class Five<T2, T3> : Two<T2>
        {
        }

        [Description("System.Void CecilTests.Set2.One`1<System.Int32>::Method(M)")]
        class Six : Five<int, string>
        {
        }

        [Description("System.Void CecilTests.Set2.One`1<System.String>::Method(M)")]
        class Seven : Four<int, string>
        {
        }
    }

    namespace Set3
    {
        [Description("System.Void CecilTests.Set3.One`1<T>::Method()")]
        class One<T>
        {
            public void Method()
            {
            }
        }

        [Description("System.Void CecilTests.Set3.One`1<System.String>::Method()")]
        class Two : One<string>
        {
        }

        [Description("System.Void CecilTests.Set3.One`1<System.String>::Method()")]
        class Three : Two
        {
        }

        [Description("System.Void CecilTests.Set3.One`1<System.String>::Method()")]
        class Four<T2, T3> : Two
        {
        }

        [Description("System.Void CecilTests.Set3.Five`2<T2,T3>::Method()")]
        class Five<T2, T3> : Four<T3, T2>
        {
            public new void Method()
            {
            }
        }

        [Description("System.Void CecilTests.Set3.Five`2<System.Int32,System.String>::Method()")]
        class Six : Five<int, string>
        {

        }

        [Description("System.Void CecilTests.Set3.Five`2<System.String,System.Int32>::Method()")]
        class Seven : Five<string, int>
        {
        }
    }
}
