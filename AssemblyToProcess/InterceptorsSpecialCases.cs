using System;
using System.ComponentModel.Composition;
using System.Reflection;

using AutoProperties;

using Xunit;

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
            Assert.True(one.Test); 

            var two = new Two<object>();
            Assert.True(two.Test); 

            var three = new Three();
            Assert.False(three.Test); // => interceptors of base class are private!

            var four = new Four();
            Assert.True(four.Test);
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
            Assert.NotNull(propertyInfo);
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
            Assert.True(((ITestExplicit) testExplicit).Test); // throws
        }
    }
}
