using System;
using System.Reflection;

using AutoProperties;
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

public class GenericBaseClass<T> 
{
    public T Prop1 { get; set; }

    public string Prop2 { get; set; } = "Test";

    public void Temp(T x)
    {

    }
    
    [SetInterceptor]
    // ReSharper disable once RedundantAssignment
    protected void SetValue<T1>(string name, Type propertyType, PropertyInfo propertyInfo, object newValue, T1 genericNewValue, ref T1 refToBackingField)
    {
        refToBackingField = genericNewValue;
    }

    [GetInterceptor]
    protected T1 GetValue<T1>(string name, Type propertyType, PropertyInfo propertyInfo, object fieldValue, T1 genericFieldValue, ref T1 refToBackingField)
    {
        return genericFieldValue;
    }
}

public class ClassWithAutoPropertyInitAndGenericBase : GenericBaseClass<int>
{
    public string Prop3 { get; set; } = "Test2";
}
