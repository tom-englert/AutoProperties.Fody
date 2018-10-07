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

    public class Five<TClass>
    {
        // https://github.com/tom-englert/AutoProperties.Fody/issues/8

        public int IntProp { get; set; }
        public string StringProp { get; set; }
        public TClass GenericProp { get; set; }

        private int _backingField;
        private static readonly PropertyInfo _propertyInfo = typeof(Five<TClass>).GetProperty(nameof(ReferenceImplementation));

        [InterceptIgnore]
        public int ReferenceImplementation
        {
            get => GetInterceptor(_propertyInfo, ref _backingField);
            set => SetInterceptor(_propertyInfo, ref _backingField, value);
        }

        [GetInterceptor]
        protected T GetInterceptor<T>(PropertyInfo propInfo, ref T fieldValue)
            => fieldValue;

        [SetInterceptor]
        protected void SetInterceptor<T>(PropertyInfo propInfo, ref T fieldValue, T newValue)
            => fieldValue = newValue;
    }

    public class BaseClass2<T> : TestLibrary.BaseClass1<T, double>
    {
        public string BaseName { get; set; }
    }

    public class FinalClass1 : TestLibrary.BaseClass2<string>
    {
        public string Name { get; set; }
    }

    public class FinalClass2 : BaseClass2<string>
    {
        public string Name { get; set; }
    }

    public class FinalClass3 : TestLibrary.FinalClass
    {
        public new string Name { get; set; }
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

            {
                var five = new Five<string>();

                var strValue = five.StringProp;
                five.StringProp = "asd";

                var intValue = five.IntProp;
                five.IntProp = 123;

                var genericValue = five.GenericProp;
                five.GenericProp = "123";
            }

            {
                var five = new Five<int>();

                var strValue = five.StringProp;
                five.StringProp = "asd";

                var intValue = five.IntProp;
                five.IntProp = 123;

                var genericValue = five.GenericProp;
                five.GenericProp = 123;
            }
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
            Assert.True(((ITestExplicit)testExplicit).Test); // throws
        }
    }
}
