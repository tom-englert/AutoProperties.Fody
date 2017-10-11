using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using AutoProperties;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace AssemblyToProcess
{
    public class One
    {
        public bool Test { get; set; }

        [GetInterceptor]
        private object GetValue(string name, FieldInfo f, PropertyInfo p) => p.Name == name && f.Name == $"<{name}>k__BackingField";

        [SetInterceptor]
        private void SetValue(string name) => throw new NotImplementedException();
    }


    public class Two<T>
    {
        public bool Test { get; set; }

        [GetInterceptor]
        private object GetValue(string name) => true;

        [SetInterceptor]
        private void SetValue(string name) => throw new NotImplementedException();
    }


    public class ThreeBase
    {
        [GetInterceptor]
        private object GetValue(string name) => true;

        [SetInterceptor]
        protected void SetValue(string name) => throw new NotImplementedException();
    }


    public class Three : ThreeBase
    {
        public bool Test { get; set; }
    }


    public class FourBase<T>
    {
        [GetInterceptor]
        protected object GetValue(string name, FieldInfo f, PropertyInfo p) => true;

        [SetInterceptor]
        protected void SetValue(string name) => throw new NotImplementedException();
    }


    public class Four : FourBase<object>
    {
        public bool Test { get; set; }
    }

    public static class GenericTests
    {
        [Export]
        public static void Run()
        {
            var one = new One();
            Assert.IsTrue(one.Test); 

            var two = new Two<object>();
            Assert.IsTrue(two.Test); 

            var three = new Three();
            Assert.IsFalse(three.Test); // => interceptors of base class are private!

            var four = new Four();
            Assert.IsTrue(four.Test);
        }
    }

    public interface ITestExplicit
    {
        bool Test { get; set; }
    }

    public class TestExplicit : ITestExplicit
    {
        [GetInterceptor]
        protected object GetValue(PropertyInfo propertyInfo)
        {
            Assert.IsNotNull(propertyInfo);
            return true;
        }

        [SetInterceptor]
        protected void SetValue(string name) => throw new NotImplementedException();

        bool ITestExplicit.Test { get; set; }


        [Export]
        public static void Run()
        {
            var t = typeof(TestExplicit).GetProperty("AssemblyToProcess.ITestExplicit.Test", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var testExplicit = new TestExplicit();
            Assert.IsTrue(((ITestExplicit) testExplicit).Test); // throws
        }
    }
}
