using AutoProperties;
using System;
// ReSharper disable All

namespace TestLibrary
{
    public class ExternalInterceptorBase
    {
        private int _field = 42;

        [GetInterceptor]
        protected object GetInterceptor(string propertyName, Type propertyType)
        {
            return Convert.ChangeType(_field, propertyType);
        }

        [SetInterceptor]
        protected void SetInterceptor(object value, string propertyName)
        {
            _field = Convert.ToInt32(value);
        }
    }

    public class ExternalInterceptorBaseWithGenerics
    {
        private int _field = 42;

        [GetInterceptor]
        protected T GetInterceptor<T>(string propertyName)
        {
            return (T)Convert.ChangeType(_field, typeof(T));
        }

        [SetInterceptor]
        protected void SetInterceptor<T>(T value, string propertyName)
        {
            _field = Convert.ToInt32(value);
        }
    }

    public class BaseClass1<T1, T2>
    {
        public T1 DoSomething()
        {
            return default(T1);
        }
        public T2 DoSomethingElse()
        {
            return default(T2);
        }

        [GetInterceptor]
        protected object Getter(string propertyName)
        {
            return null;
        }

        [SetInterceptor]
        protected void Setter(string propertyName, object newValue)
        {
        }
    }

    public class BaseClass2<T> : BaseClass1<T, int>
    {
        public string BaseName { get; set; }
    }

    public class FinalClass : BaseClass2<string>
    {
        public string Name { get; set; }
    }
}
