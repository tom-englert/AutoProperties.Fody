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
}
