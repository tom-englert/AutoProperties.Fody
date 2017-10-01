using System;
using System.Reflection;

using AutoProperties;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

public class ClassWithSimpleInterceptor
{
#pragma warning disable 649
    private int _field;
#pragma warning restore 649
    private static PropertyInfo _pe = typeof(ClassWithSimpleInterceptor).GetProperty(nameof(Property1));

    // [GetInterceptor]
    private object GetInterceptor(string propertyName, Type propertyType)
    {
        return 42;
    }

    [SetInterceptor]
    private void SetInterceptor<T>(T value, string propertyName)
    {

    }
    private void SetInterceptor2(object value, string propertyName)
    {

    }

    public int Property1 { get; set; }

    public Type Property2 { get; set; }

    [GetInterceptor]

    private T GetInterceptor2<T>(string propertyName, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo)
    {
        return (T)Convert.ChangeType(42, typeof(T));
    }

    private object GetInterceptor3(string propertyName, Type propertyType, PropertyInfo propertyInfo, FieldInfo fieldInfo)
    {
        return Convert.ChangeType(42, propertyType);
    }

    public int CompilesTo
    {
        get => (int)GetInterceptor(nameof(Property1), typeof(int));
        set => SetInterceptor(value, nameof(Property1));
    }

    public string CompilesTo1
    {
        get
        {
            var propertyType = GetType().GetProperty(nameof(Property1)).PropertyType;

            return (string) GetInterceptor(nameof(Property1), propertyType);
        }
        set => SetInterceptor(nameof(Property1), value);
    }
}
