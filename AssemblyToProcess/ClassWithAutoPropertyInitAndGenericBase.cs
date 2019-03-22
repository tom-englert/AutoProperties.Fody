using System;
using System.Reflection;

using AutoProperties;

public class GenericBaseClass<TClass> 
{
    public int Prop1 { get; set; }

    public string Prop2 { get; set; } = "Test";

    public void Temp(TClass x)
    {

    }
    
    [SetInterceptor]
    protected void SetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, object newValue, T genericNewValue, ref T refToBackingField)
    {
        refToBackingField = genericNewValue;
    }

    [GetInterceptor]
    protected T GetValue<T>(string name, Type propertyType, PropertyInfo propertyInfo, object fieldValue, T genericFieldValue, ref T refToBackingField)
    {
        return genericFieldValue;
    }
}

public class ClassWithAutoPropertyInitAndGenericBase : GenericBaseClass<string>
{
    public string Prop3 { get; set; } = "Test2";
}
